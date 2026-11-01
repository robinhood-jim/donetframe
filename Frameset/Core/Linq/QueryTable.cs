using Frameset.Core.Dao;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Frameset.Core.Linq
{
    public class QueryTable<T> : IQueryable<T>
    {
        public Expression Expression { get; }
        public IQueryProvider Provider { get; }
        public Type ElementType => typeof(T);
        private readonly IAdoDao dao;
        public QueryTable(IQueryProvider provider, IAdoDao dao, Expression expression)
        {
            Expression = expression;
            Provider = provider;
            this.dao = dao;
        }
        public QueryTable(IAdoDao dao)
        {
            this.dao = dao;
            Provider = new CustomQueryProvider(dao);
            Expression = Expression.Constant(this);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void And(bool condition, Expression expression)
        {
            if (condition)
            {
                Expression.And(Expression,expression);
            }
        }
    }
}
