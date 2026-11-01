using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Model;
using Frameset.Core.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Frameset.Core.Linq
{
    public class SqlExpressionVisitor : ExpressionVisitor
    {
        private List<string> _tablesClause = [];
        private StringBuilder _whereClause = new();
        private StringBuilder _groupByClause = new();
        private StringBuilder _havingClause = new();

        private Dictionary<string, string> _aliasMap = [];
        private Dictionary<Type, string> _typeToAliasMap = new Dictionary<Type, string>();
        private List<string> _selectFields = [];
        private int _aliasCount = 0;
        private string _activeContext = "WHERE";
        public List<DbParameter> Parameters { get; } = [];
        private readonly IAdoDao _dao;
        int _paramCount = 0;
        private bool _isNegated = false;
        public SqlExpressionVisitor(IAdoDao dao)
        {
            this._dao = dao;
        }
        public string Translate(Expression expression)
        {
            _tablesClause.Clear(); _whereClause.Clear(); _groupByClause.Clear(); _havingClause.Clear();
            _selectFields.Clear(); _aliasMap.Clear(); _typeToAliasMap.Clear();
            _aliasCount = 0; _activeContext = "WHERE";

            Visit(expression);
            string? selectStr = null;
            if (_selectFields.Count == 0)
            {
                if (_typeToAliasMap.Count == 1)
                {
                    Type entityType = _typeToAliasMap.Keys.First();
                    SqlUtils.GetSelectFields(entityType, out selectStr);
                }
            }
            else
            {
                selectStr = string.Join(", ", _selectFields);
            }
            string tablesStr = string.Join(", ", _tablesClause);

            var sql = new StringBuilder($"SELECT {selectStr} FROM {tablesStr}");

            if (_whereClause.Length > 0) sql.Append($" WHERE {_whereClause}");
            if (_groupByClause.Length > 0) sql.Append($" GROUP BY {_groupByClause}");
            if (_havingClause.Length > 0) sql.Append($" HAVING {_havingClause}");

            return sql.ToString();
        }
        protected override Expression VisitInvocation(InvocationExpression node)
        {
            if (node.Expression is LambdaExpression lambda)
            {
                var parameterMap = lambda.Parameters.Zip(node.Arguments, (p, a) => new { p, a }).ToDictionary(x => x.p, x => x.a);
                var rewrittenBody = ParameterReplacer.Replace(lambda.Body, parameterMap);
                return Visit(rewrittenBody);
            }
            return base.VisitInvocation(node);
        }
        protected override Expression VisitUnary(UnaryExpression node)
        {
            // Intercept logical negation operator (!)
            if (node.NodeType == ExpressionType.Not)
            {
                bool previousNegationState = _isNegated;
                _isNegated = !_isNegated; // Toggle negation state

                // If it's a structural boolean flag, handling changes. For methods/binaries, evaluate down.
                Visit(node.Operand);

                _isNegated = previousNegationState; // Restore token context state
                return node;
            }

            return base.VisitUnary(node);
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            foreach (var arg in node.Arguments.OfType<LambdaExpression>())
            {
                foreach (var param in arg.Parameters)
                {
                    if (_typeToAliasMap.TryGetValue(param.Type, out string? alias))
                    {
                        _aliasMap.TryAdd(param.Name, alias);
                    }
                }
            }
            //string operation
            if (node.Method.DeclaringType == typeof(string))
            {
                if (node.Method.Name.Equals("Equals"))
                {
                    Expression left = node.Object ?? node.Arguments[0];
                    Expression right = node.Object == null ? node.Arguments[1] : node.Arguments[0];
                    AppendToActiveClause("(");
                    Visit(left);
                    AppendToActiveClause(_isNegated ? "<>" : " = ");
                    Visit(right);
                    AppendToActiveClause(")");
                    return node;
                }
                var value = EvaluateLocalExpression(node.Arguments[0]);
                switch (node.Method.Name)
                {
                    case "Contains":
                        Trace.Assert(node.Object != null);
                        Visit(node.Object);
                        AppendToActiveClause(_isNegated ? " not like " : " like ");
                        RegisterParameter('%' + value?.ToString() + '%');
                        break;
                    case "StartsWith":
                        Trace.Assert(node.Object != null);
                        Visit(node.Object);
                        AppendToActiveClause(_isNegated ? " not like " : " like ");
                        RegisterParameter(value?.ToString() + '%');
                        break;
                    case "EndsWith":
                        Trace.Assert(node.Object != null);
                        Visit(node.Object);
                        AppendToActiveClause(_isNegated ? " not like " : " like ");
                        RegisterParameter('%' + value?.ToString());
                        break;
                }
                return node;
            }
            // in operation
            if (node.Method.Name == "Contains" && typeof(IEnumerable).IsAssignableFrom(node.Method.DeclaringType))
            {
                Expression collectionExpression = node.Object ?? node.Arguments[0];
                Expression itemExpression = node.Object == null ? node.Arguments[1] : node.Arguments[0];

                var collectionValue = EvaluateLocalExpression(collectionExpression) as IEnumerable;
                if (collectionValue != null)
                {
                    var paramTokens = new List<string>();
                    foreach (var val in collectionValue)
                    {
                        _paramCount++;
                        string paramName = $"@p{_paramCount}";
                        Parameters.Add(_dao.GetDialect().WrapParameter(paramName, val ?? DBNull.Value));
                        paramTokens.Add(paramName);
                    }

                    if (paramTokens.Count > 0)
                    {
                        Visit(itemExpression);
                        AppendToActiveClause(_isNegated ? $" NOT IN ({string.Join(", ", paramTokens)})" : $" IN ({string.Join(", ", paramTokens)})");
                    }
                    else
                    {
                        AppendToActiveClause("1 = 0");
                    }
                    return node;
                }
            }

            switch (node.Method.Name)
            {
                case "Join":
                    Visit(node.Arguments[0]);
                    Visit(node.Arguments[1]);
                    var outerKeyLambda = (LambdaExpression)UnwrapQuotes(node.Arguments[2]);
                    var innerKeyLambda = (LambdaExpression)UnwrapQuotes(node.Arguments[3]);
                    var resultLambda = (LambdaExpression)UnwrapQuotes(node.Arguments[4]);
                    string? outerFieldName = outerKeyLambda.Parameters[0].Name;

                    if (resultLambda.Parameters.Count >= 2)
                    {
                        string? outerKeyName = resultLambda.Parameters[0].Name;
                        string? innerKeyName = resultLambda.Parameters[1].Name;
                        Trace.Assert(!string.IsNullOrWhiteSpace(innerKeyName) && !string.IsNullOrWhiteSpace(outerKeyName));
                        _aliasMap.TryAdd(outerKeyName, _typeToAliasMap[outerKeyLambda.Parameters[0].Type]);
                        _aliasMap.TryAdd(innerKeyName, _typeToAliasMap[innerKeyLambda.Parameters[0].Type]);
                    }

                    // 3. Append relational equality check condition directly into the WHERE queue buffer
                    if (_whereClause.Length > 0) _whereClause.Append(" AND ");

                    _whereClause.Append("(");
                    Visit(outerKeyLambda.Body);
                    _whereClause.Append(" = ");
                    Visit(innerKeyLambda.Body);
                    _whereClause.Append(")");
                    break;
                case "Where":
                    Visit(node.Arguments[0]);
                    _activeContext = _groupByClause.Length > 0 ? "HAVING" : "WHERE";

                    var filterClause = _activeContext == "WHERE" ? _whereClause : _havingClause;
                    if (filterClause.Length > 0) AppendToActiveClause(" AND ");

                    Visit(UnwrapQuotes(node.Arguments[1]));
                    break;
                case "Select":
                    Visit(node.Arguments[0]);
                    _activeContext = "SELECT";
                    var lambda = (LambdaExpression)UnwrapQuotes(node.Arguments[1]);

                    if (lambda.Body is NewExpression newExp && newExp.Members != null)
                    {
                        for (int i = 0; i < newExp.Arguments.Count; i++)
                        {
                            var oldFields = _selectFields;
                            _selectFields = new List<string>();

                            Visit(newExp.Arguments[i]);

                            string expressionSegment = string.Join("", _selectFields);
                            oldFields.Add($"{expressionSegment} AS {newExp.Members[i].Name}");
                            _selectFields = oldFields;
                        }
                    } else // Fallback for raw arithmetic equations mapped directly without dynamic type wrapping definitions
                    {
                        var oldFields = _selectFields; _selectFields = new List<string>();
                        Visit(lambda.Body);
                        oldFields.Add(string.Join("", _selectFields));
                        _selectFields = oldFields;
                    }
                    break;
                case "Sum":
                case "Max":
                case "Min":
                case "Average":
                    AppendToActiveClause($"{node.Method.Name.ToUpper()}(");
                    if (node.Arguments.Count > 0) Visit(UnwrapQuotes(node.Arguments[0]));
                    AppendToActiveClause(")");
                    break;
                case "Equals":
                    Trace.Assert(node.Object != null);
                    Visit(UnwrapQuotes(node.Object));
                    AppendToActiveClause(" = ");
                    Visit(node.Arguments[0]);
                    break;

            }
            return node;
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                _aliasCount++;
                string alias = GetLatestAlias();
                Type entityType = queryable.ElementType;
                Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)));
                EntityContent content = EntityReflectUtils.GetEntityInfo(entityType);
                _typeToAliasMap.TryAdd(entityType, alias);
                _tablesClause.Add($"{content.TableName} AS {alias}");
                return node;
            }
            object? value = node.Value;
            RegisterParameter(value);

            return node;
        }
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression paramNode)
            {
                Type? memberType = node.Expression.Type;
                if (memberType != null && !memberType.IsSubclassOf(typeof(BaseEntity)))
                {
                    memberType = node.Type;
                }
                if (memberType != null && _typeToAliasMap.TryGetValue(memberType, out string? alias))
                {
                    Dictionary<string, FieldContent> fields = EntityReflectUtils.GetFieldsMap(memberType);
                    if (fields.TryGetValue(node.Member.Name, out FieldContent? content))
                    {
                        AppendToActiveClause($"{alias}.{content.FieldName}");
                    }
                    else
                    {
                        AppendToActiveClause($"{alias}");
                    }
                }
            }
            else if (node.Expression?.NodeType == ExpressionType.MemberAccess)
            {
                string fieldName = node.Member.Name;

                if (node.Expression.Type.IsModelType())
                {
                    Visit(node.Expression);
                    Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(node.Expression.Type);
                    if (fieldMap.TryGetValue(fieldName, out FieldContent? content))
                    {
                        AppendToActiveClause($".{content.FieldName}");
                    }
                }
                else
                {
                    //右侧为对象值访问user.Id
                    MemberInfo member = node.Member;
                    object? value = EvaluateLocalExpression(node);
                    RegisterParameter(value);
                }
            }
            else if (node.Expression?.NodeType == ExpressionType.Constant)
            {
                //右值为对象中定义基本类型变量 
                MemberInfo member = node.Member;
                object? value = EvaluateLocalExpression(node);
                if (value != null)
                {
                    if (value.GetType().IsFundamentalType())
                    {
                        RegisterParameter(value);
                    }
                }
            }
            else
            {
                string cleanMember = node.Member.Name;
                if (node.Expression != null)
                {
                    Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(node.Expression.Type);
                    if (fieldMap.TryGetValue(cleanMember, out FieldContent? content))
                    {
                        AppendToActiveClause($".{content.FieldName}");
                    }
                }
                else
                {
                    //右值为static的常量 Constatns.VALID
                    if (node.Type.IsFundamentalType())
                    {
                        if (node.Member.MemberType == MemberTypes.Field && ((FieldInfo)node.Member).IsStatic)
                        {
                            object? value = EvaluateLocalExpression(node);
                            RegisterParameter(value);
                        }
                    }
                }
            }
            return node;

        }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            bool isLeftNull = IsNullConstant(node.Left);
            bool isRightNull = IsNullConstant(node.Right);
            if (isLeftNull || isRightNull)
            {
                Expression validNode = isLeftNull ? node.Right : node.Left;
                AppendToActiveClause("(");
                Visit(validNode);
                bool finalIsEqual = _isNegated ? (node.NodeType != ExpressionType.Equal) : (node.NodeType == ExpressionType.Equal);
                if (finalIsEqual)
                    AppendToActiveClause(" IS NULL");
                else
                    AppendToActiveClause(" IS NOT NULL");
                AppendToActiveClause(")");
                return node;
            }
            AppendToActiveClause("(");
            Visit(node.Left);
            ExpressionType opType = node.NodeType;
            if (_isNegated)
            {
                if (opType == ExpressionType.AndAlso) opType = ExpressionType.OrElse;
                else if (opType == ExpressionType.OrElse) opType = ExpressionType.AndAlso;
            }
            switch (opType)
            {
                case ExpressionType.Equal: AppendToActiveClause(" = "); break;
                case ExpressionType.NotEqual: AppendToActiveClause(" <> "); break;
                case ExpressionType.GreaterThan: AppendToActiveClause(" > "); break;
                case ExpressionType.LessThanOrEqual: AppendToActiveClause(" <= "); break;
                case ExpressionType.AndAlso: AppendToActiveClause(" AND "); break;
                case ExpressionType.Or: AppendToActiveClause(" OR "); break;
                case ExpressionType.OrElse: AppendToActiveClause(" OR "); break;
                case ExpressionType.Multiply: AppendToActiveClause(" * "); break;
                case ExpressionType.Add: AppendToActiveClause(" + "); break;
                case ExpressionType.Subtract: AppendToActiveClause(" - "); break;
                case ExpressionType.Divide: AppendToActiveClause(" / "); break;
                case ExpressionType.Not: AppendToActiveClause(" NOT "); break;
                default: throw new NotSupportedException($"Operator '{node.NodeType}' not implemented.");
            }

            Visit(node.Right);
            AppendToActiveClause(")");
            return node;
        }

        private string GetLatestAlias() => $"t{_aliasCount}";
        private Expression UnwrapQuotes(Expression exp)
        {
            while (exp.NodeType == ExpressionType.Quote) exp = ((UnaryExpression)exp).Operand;
            return exp;
        }
        private void AppendToActiveClause(string value)
        {
            if (_activeContext == "SELECT") _selectFields.Add(value);
            else if (_activeContext == "WHERE")
            {
                _whereClause.Append(value);
            }
            else if (_activeContext == "GROUPBY") _groupByClause.Append(value);
            else if (_activeContext == "HAVING") _havingClause.Append(value);

        }
        private void RegisterParameter(object? value)
        {
            if (value == null)
            {
                AppendToActiveClause("NULL");
                return;
            }
            _paramCount++;
            string parameterName = $"@p{_paramCount}";
            Parameters.Add(_dao.GetDialect().WrapParameter(parameterName, value));
            AppendToActiveClause(parameterName);
        }
        private object? EvaluateLocalExpression(Expression expr)
        {
            if (expr is ConstantExpression ce) return ce.Value;
            var lambda = Expression.Lambda(expr);
            return lambda.Compile().DynamicInvoke();
        }
        private bool IsNullConstant(Expression expr)
        {
            if (expr is ConstantExpression ce && ce.Value == null) return true;
            try { return EvaluateLocalExpression(expr) == null; } catch { return false; }
        }

    }
    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, Expression> _map;
        private ParameterReplacer(Dictionary<ParameterExpression, Expression> map) => _map = map;
        public static Expression Replace(Expression body, Dictionary<ParameterExpression, Expression> map) => new ParameterReplacer(map).Visit(body);
        protected override Expression VisitParameter(ParameterExpression node) => _map.TryGetValue(node, out var replacement) ? replacement : base.VisitParameter(node);
    }
}
