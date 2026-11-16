using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Model;
using Frameset.Core.Utils;
using Microsoft.IdentityModel.Tokens;
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
        private string _tablesClause = string.Empty;
        private List<string> _joinSegments = [];
        private StringBuilder _whereClause = new();
        private StringBuilder _groupByClause = new();
        private StringBuilder _havingClause = new();
        private StringBuilder _orderByClause = new();
        private StringBuilder _joinClause = new();
        protected Dictionary<Type, string> TypeToAliasMap = new Dictionary<Type, string>();
        protected Dictionary<Type, string> TypeToTableNameMap = [];
        private List<string> _selectFields = [];
        private StringBuilder _currentFieldBuilder = new();
        protected int AliasCount = 0;
        private string _activeContext = "WHERE";
        public List<DbParameter> Parameters { get; protected set; } = [];
        private readonly IAdoDao _dao;
        protected int ParamCount = 0;
        private bool _isNegated = false;
        protected long SkipRows
        {
            get; private set;
        } = 0;
        protected long PageSize
        {
            get; private set;
        } = 10;
        protected bool Pageable
        {
            get; private set;
        } = false;

        public SqlExpressionVisitor(IAdoDao dao)
        {
            this._dao = dao;
        }
        public void InheritContext(SqlExpressionVisitor parent)
        {
            AliasCount = parent.AliasCount;
            ParamCount = parent.ParamCount;
            Parameters = parent.Parameters;
            TypeToAliasMap = parent.TypeToAliasMap;
        }

        public void SynchronizeCounters(SqlExpressionVisitor child)
        {
            AliasCount = child.AliasCount;
            ParamCount = child.ParamCount;
        }

        public string TranslateSimple<T>(Expression expression, out string? tablesStr)
        {
            _tablesClause = string.Empty; _whereClause.Clear(); _groupByClause.Clear(); _havingClause.Clear();
            _selectFields.Clear(); TypeToAliasMap.Clear(); _currentFieldBuilder.Clear();
            AliasCount = 0; _activeContext = "WHERE";
            Type entityType = typeof(T);
            EntityContent content = EntityReflectUtils.GetEntityInfo(entityType);
            AliasCount++;
            string alias = GetLatestAlias();
            _tablesClause = $"{content.TableName}";
            Visit(expression);
            var sql = new StringBuilder();
            tablesStr = string.Join(", ", _tablesClause);
            if (_selectFields.Count > 0)
            {
                string selectStr = string.Join(", ", _selectFields);
                sql.Append($"{selectStr}");
            }
            if (_whereClause.Length > 0) sql.Append($"{_whereClause}");
            return sql.ToString();
        }
        public TranslationResult Translate(Expression expression)
        {
            _tablesClause = string.Empty; _whereClause.Clear(); _groupByClause.Clear(); _havingClause.Clear();
            _selectFields.Clear(); TypeToAliasMap.Clear(); _currentFieldBuilder.Clear();
            AliasCount = 0; _activeContext = "WHERE";

            Visit(expression);
            string? selectStr = null;
            if (_selectFields.Count == 0)
            {
                if (TypeToAliasMap.Count == 1)
                {
                    Type entityType = TypeToAliasMap.Keys.First();
                    SqlUtils.GetSelectFields(entityType, out selectStr);
                }
            }
            else
            {
                selectStr = string.Join(",", _selectFields);
            }

            string joinsStr = _joinSegments.Count > 0 ? " " + string.Join(" ", _joinSegments) : "";

            var sql = new StringBuilder($"SELECT {selectStr} FROM {_tablesClause}{joinsStr}");

            if (_whereClause.Length > 0) sql.Append($" WHERE {_whereClause}");
            if (_groupByClause.Length > 0) sql.Append($" GROUP BY {_groupByClause}");
            if (_havingClause.Length > 0) sql.Append($" HAVING {_havingClause}");
            if (_orderByClause.Length > 0)
            {
                _orderByClause.Remove(_orderByClause.Length - 1, 1);
                sql.Append($" ORDER BY {_orderByClause}");
            }

            return new TranslationResult() { CommandText = sql.ToString(), Parameters = Parameters, Pageable = Pageable, PageSize = PageSize, SkipRows = SkipRows };
        }
        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if (node.Body.NodeType == ExpressionType.MemberInit)
            {
                MemberInitExpression? memberInitExp = node.Body as MemberInitExpression;
                var bindings = memberInitExp?.Bindings;
                Type memberType = node.Body.Type;
                if (bindings != null && !bindings.IsNullOrEmpty())
                {
                    foreach (MemberBinding binding in bindings)
                    {
                        ParseAliasEntity(memberType);
                        if (TypeToAliasMap.TryGetValue(memberType, out string? alias))
                        {
                            Dictionary<string, FieldContent> fields = EntityReflectUtils.GetFieldsMap(memberType);
                            if (fields.TryGetValue(binding.Member.Name, out FieldContent? content))
                            {
                                _whereClause.Append($"{alias}.{content.FieldName}=");
                                Visit(((MemberAssignment)binding).Expression);
                                _whereClause.Append(",");

                            }
                        }
                    }
                }
                return node;
            }
            return base.VisitLambda(node);
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

            if (node.Method.Name == "Any" && node.Arguments.Count > 1)
            {
                var subVisitor = new SqlExpressionVisitor(_dao);
                subVisitor.InheritContext(this);

                // Visit inner data table context bounds (e.g. bCollection)
                subVisitor.Visit(node.Arguments[0]);
                var innerLambda = (LambdaExpression)UnwrapQuotes(node.Arguments[1]);
                Type modelType = innerLambda.Parameters[0].Type;
                subVisitor._activeContext = "WHERE";
                subVisitor.Visit(innerLambda.Body);
                SynchronizeCounters(subVisitor);
                string presenceOperator = _isNegated ? "NOT EXISTS " : "EXISTS ";
                subVisitor.TypeToAliasMap.TryGetValue(modelType, out string? alias);
                subVisitor.TypeToTableNameMap.TryGetValue(modelType, out string? tableName);
                Trace.Assert(!string.IsNullOrWhiteSpace(alias) && !string.IsNullOrWhiteSpace(tableName));
                AppendToActiveClause(presenceOperator);
                AppendToActiveClause($"(SELECT * FROM {tableName} as {alias} WHERE {subVisitor._whereClause})");

                return node;
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
                bool canReturn = true;
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
                    default:
                        canReturn = false;
                        break;
                }
                if (canReturn) return node;
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
                        ParamCount++;
                        string paramName = $"@p{ParamCount}";
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
                case "GroupJoin":
                case "Join":
                    Visit(node.Arguments[0]);
                    Visit(node.Arguments[1]);
                    Type rightModelType = node.Arguments[1].Type.GetGenericArguments().First();
                    if (!TypeToAliasMap.TryGetValue(rightModelType, out string? rightAlias) || !TypeToTableNameMap.TryGetValue(rightModelType, out string? rightTableName))
                    {
                        throw new BaseSqlException("entity type mismatch");
                    }
                    var outerKeyLambda = (LambdaExpression)UnwrapQuotes(node.Arguments[2]);
                    var innerKeyLambda = (LambdaExpression)UnwrapQuotes(node.Arguments[3]);

                    string joinOperator = node.Method.Name == "GroupJoin" ? " LEFT JOIN " : " INNER JOIN ";
                    string currentActive = _activeContext;
                    _activeContext = "JOIN";
                    Visit(outerKeyLambda.Body);
                    AppendToActiveClause(" = ");
                    Visit(innerKeyLambda.Body);
                    _activeContext = currentActive;
                    _joinSegments.Add($"{joinOperator} {rightTableName} {rightAlias} ON {_joinClause}");
                    break;
                case "Where":
                    Visit(node.Arguments[0]);
                    _activeContext = _groupByClause.Length > 0 ? "HAVING" : "WHERE";

                    var filterClause = _activeContext == "WHERE" ? _whereClause : _havingClause;
                    if (filterClause.Length > 0) AppendToActiveClause(" AND ");

                    Visit(UnwrapQuotes(node.Arguments[1]));
                    break;
                case "SelectMany":
                    Visit(node.Arguments[0]);
                    break;
                case "Select":
                    Visit(node.Arguments[0]);
                    _activeContext = "SELECT";
                    var lambda = (LambdaExpression)UnwrapQuotes(node.Arguments[1]);

                    if (lambda.Body is NewExpression newExp && newExp.Members != null)
                    {
                        for (int i = 0; i < newExp.Arguments.Count; i++)
                        {
                            _currentFieldBuilder.Clear();
                            Visit(newExp.Arguments[i]);
                            _selectFields.Add($"{_currentFieldBuilder} AS {newExp.Members[i].Name}");
                        }
                    }
                    else if (lambda.Body is MemberInitExpression)
                    {
                        MemberInitExpression? initExpression = lambda.Body as MemberInitExpression;
                        Trace.Assert(initExpression != null);
                        foreach (MemberBinding binding in initExpression.Bindings)
                        {
                            _currentFieldBuilder.Clear();
                            if (binding is MemberAssignment assignment)
                            {
                                string assignColumn = binding.Member.Name;
                                Visit(assignment.Expression);
                                _selectFields.Add($"{_currentFieldBuilder} as {assignColumn}");
                            }
                        }
                    }
                    else
                    {
                        _currentFieldBuilder.Clear();
                        Visit(lambda.Body);
                        _selectFields.Add(_currentFieldBuilder.ToString());
                    }
                    break;
                case "Sum":
                case "Max":
                case "Min":
                case "Average":
                    AppendToActiveClause($"{node.Method.Name.ToUpper()}(");
                    var lambdaArgument = node.Arguments.FirstOrDefault(arg => arg is LambdaExpression ||
                        (arg is UnaryExpression ue && ue.Operand is LambdaExpression));

                    if (lambdaArgument != null)
                    {
                        Visit(UnwrapQuotes(lambdaArgument));
                    }
                    else if (node.Method.Name == "Count")
                    {
                        // Default to * for bare g.Count() invocations
                        AppendToActiveClause("*");
                    }
                    else
                    {
                        var propertyArgument = node.Arguments.FirstOrDefault(arg => arg is MemberExpression);
                        if (propertyArgument != null)
                        {
                            Visit(propertyArgument);
                        }
                    }
                    AppendToActiveClause(")");
                    break;
                case "Equals":
                    Trace.Assert(node.Object != null);
                    Visit(UnwrapQuotes(node.Object));
                    AppendToActiveClause(" = ");
                    Visit(node.Arguments[0]);
                    break;
                case "OrderBy":
                case "OrderByDescending":
                case "ThenBy":
                case "ThenByDescending":
                    string oldAction = _activeContext;
                    Visit(node.Arguments[0]);
                    _activeContext = "ORDERBY";
                    Visit(UnwrapQuotes(node.Arguments[1]));
                    string sortDirection = node.Method.Name.Contains("Descending") ? " DESC" : " ASC";
                    AppendToActiveClause(sortDirection + ',');
                    _activeContext = oldAction;
                    break;
                case "Skip":
                    Pageable = true;
                    Visit(node.Arguments[0]);
                    var skipValue = EvaluateLocalExpression(node.Arguments[1]);
                    SkipRows = Convert.ToInt64(skipValue);
                    break;
                case "Take":
                    Pageable = true;
                    Visit(node.Arguments[0]);
                    var takeValue = EvaluateLocalExpression(node.Arguments[1]);
                    PageSize = Convert.ToInt64(takeValue);
                    break;
            }
            return node;
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {

                Type entityType = queryable.ElementType;
                Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)));
                ParseAliasEntity(entityType);
                if (string.IsNullOrWhiteSpace(_tablesClause) && TypeToAliasMap.TryGetValue(entityType, out string? alias) && TypeToTableNameMap.TryGetValue(entityType, out string? tableName))
                {
                    _tablesClause = $"{tableName} AS {alias}";
                }
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
                if (!memberType.IsSubclassOf(typeof(BaseEntity)))
                {
                    memberType = node.Type;
                }
                Trace.Assert(memberType != null);
                ParseAliasEntity(memberType);
                if (TypeToAliasMap.TryGetValue(memberType, out string? alias))
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

        private string GetLatestAlias() => $"t{AliasCount}";
        private Expression UnwrapQuotes(Expression exp)
        {
            while (exp.NodeType == ExpressionType.Quote) exp = ((UnaryExpression)exp).Operand;
            return exp;
        }
        private void AppendToActiveClause(string value)
        {
            if (_activeContext == "SELECT") _currentFieldBuilder.Append(value);
            else if (_activeContext == "WHERE")
            {
                _whereClause.Append(value);
            }
            else if (_activeContext == "GROUPBY") _groupByClause.Append(value);
            else if (_activeContext == "HAVING") _havingClause.Append(value);
            else if (_activeContext == "ORDERBY") _orderByClause.Append(value);
            else if (_activeContext == "JOIN") _joinClause.Append(value);
        }
        private void RegisterParameter(object? value)
        {
            if (value == null)
            {
                AppendToActiveClause("NULL");
                return;
            }
            ParamCount++;
            string parameterName = $"@p{ParamCount}";
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
        private void ParseAliasEntity(Type entityType)
        {
            if (!TypeToAliasMap.TryGetValue(entityType, out _))
            {
                AliasCount++;
                string alias = GetLatestAlias();
                EntityContent content = EntityReflectUtils.GetEntityInfo(entityType);
                TypeToAliasMap.TryAdd(entityType, alias);
                TypeToTableNameMap.TryAdd(entityType, content.GetTableName());
            }
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
