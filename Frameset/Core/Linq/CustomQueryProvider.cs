using Frameset.Core.Dao;
using Frameset.Core.Utils;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Frameset.Core.Linq
{
    public class CustomQueryProvider : IQueryProvider
    {
        private readonly IAdoDao dao;
        public CustomQueryProvider(IAdoDao dao)
        {
            this.dao = dao;
        }
        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = expression.Type.GetGenericArguments().First();
            return Activator.CreateInstance(typeof(QueryTable<>).MakeGenericType(elementType), this, expression) as IQueryable;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new QueryTable<TElement>(this, dao, expression);
        }
        public TResult Execute<TResult>(Expression expression)
        {
            var translator = new SqlExpressionVisitor(dao);
            string sqlQuery = translator.Translate(expression);
            LogUtils.Debug($"execute LINQ Query:{sqlQuery}");
            Type returnType = typeof(TResult);
            Type itemType = returnType.IsGenericType ? returnType.GetGenericArguments().First() : returnType;
            object ret = dao.QueryBySql(itemType, sqlQuery, translator.Parameters);
            return (TResult)ret;

        }

        public object? Execute(Expression expression)
        {
            throw new NotImplementedException();
        }
    }
}
