using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Frameset.Core.Linq;
using Frameset.Core.Model;
using Frameset.Core.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;


namespace Frameset.Core.Context
{

    public partial class DbContext
    {
        public class EntityExpression<T> where T : BaseEntity
        {

            public Expression<Func<T, bool>> ConditionExpression
            {
                get; internal set;
            } = null!;
            public Expression<Func<T>>? GenerateExpression
            {
                get; internal set;
            }

            public string Operation
            {
                get; internal set;
            } = string.Empty;

        }
        public override int Update<T>(Expression<Func<T, bool>> conditions, Expression<Func<T>> generateExpression)
        {
            var expression = new EntityExpression<T>
            {
                Operation = Constants.UPDATE,
                ConditionExpression = conditions,
                GenerateExpression = generateExpression,
            };
            return ExecuteExpression(expression);
        }
        public override int Count<T>(Expression<Func<T, bool>> conditions)
        {
            var expression = new EntityExpression<T>
            {
                Operation = Constants.COUNT,
                ConditionExpression = conditions
            };
            return ExecuteExpression(expression);
        }
        public int DeleteLogic<T>(Expression<Func<T, bool>> condititions, string logicColumn = "Status") where T : BaseEntity
        {
            var entityType = typeof(T);

            var ctor = entityType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderBy(c => c.GetParameters().Length)
                .FirstOrDefault();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            if (ctor != null && ctor.GetParameters().Length == 0)
            {
                var construct = Expression.New(ctor);
                var props = entityType.GetProperties(flags).Where(p => p.CanWrite && p.Name.Equals(logicColumn)).ToArray();
                if (props.Length != 1)
                {
                    throw new ConfigMissingException("logicColumn doen't exists in Properties");
                }
                ConstantExpression Value = Expression.Constant(Constants.VALID, typeof(string));
                MemberAssignment idBinding = Expression.Bind(props[0], Value);
                MemberInitExpression memberInit = Expression.MemberInit(construct, idBinding);
                Expression<Func<T>> memberFunc = Expression.Lambda<Func<T>>(memberInit);
                var expression = new EntityExpression<T>
                {
                    Operation = Constants.UPDATE,
                    ConditionExpression = condititions,
                    GenerateExpression = memberFunc,
                };
                return ExecuteExpression(expression);

            }
            throw new ConfigMissingException("entity type does't have default constructor");
        }

        public override int Delete<T>(Expression<Func<T, bool>> conditions)
        {
            var expression = new EntityExpression<T>
            {
                Operation = Constants.DELETE,
                ConditionExpression = conditions
            };
            return ExecuteExpression(expression);
        }
        private int ExecuteExpression<T>(EntityExpression<T> expression) where T : BaseEntity
        {
            SqlExpressionVisitor translate = new SqlExpressionVisitor(GetDao());
            StringBuilder executeSql = new();

            if (string.Equals(Constants.UPDATE, expression.Operation, StringComparison.OrdinalIgnoreCase))
            {
                Trace.Assert(expression.GenerateExpression != null);
                string updateStr = translate.TranslateSimple<T>(expression.GenerateExpression, out string? tableStr);
                executeSql.Append(Constants.UPDATE).Append(' ').Append(tableStr).Append(" set ");
                executeSql.Append(updateStr.AsSpan(0, updateStr.Length - 1)).Append(" WHERE ");
                string fieldStr = translate.TranslateSimple<T>(expression.ConditionExpression, out _);
                executeSql.Append(fieldStr);
            }
            else if (string.Equals(Constants.DELETE, expression.Operation, StringComparison.OrdinalIgnoreCase))
            {
                string fieldStr = translate.TranslateSimple<T>(expression.ConditionExpression, out string? tableStr);
                executeSql.Append(Constants.DELETE).Append(" from ").Append(tableStr).Append(" WHERE ");
                executeSql.Append(fieldStr);
            }
            else if (string.Equals(Constants.COUNT, expression.Operation, StringComparison.OrdinalIgnoreCase))
            {
                string fieldStr = translate.TranslateSimple<T>(expression.ConditionExpression, out string? tableStr);
                executeSql.Append(Constants.SQL_SELECT).Append(Constants.COUNT).Append("(*) from ").Append(tableStr).Append(" WHERE ");
                executeSql.Append(fieldStr);
            }
            LogUtils.Debug($"Execute {expression.Operation} Sql {executeSql}");
            return Execute(executeSql.ToString(), translate.Parameters.ToArray());
        }

    }
}
