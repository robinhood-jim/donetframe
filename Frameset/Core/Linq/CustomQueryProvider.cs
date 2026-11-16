using Frameset.Core.Dao;
using Frameset.Core.Exceptions;
using Frameset.Core.Query;
using Frameset.Core.Utils;
using System;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace Frameset.Core.Linq
{
    public class CustomQueryProvider : IQueryProvider
    {
        private readonly IAdoDao dao;
        public long QueryCount
        {
            get;
            internal set;
        }
        public CustomQueryProvider(IAdoDao dao)
        {
            this.dao = dao;
        }
        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = expression.Type.GetGenericArguments().First();
            bool isOrdered = typeof(IOrderedQueryable).IsAssignableFrom(expression.Type);
            Type queryableType = isOrdered
                ? typeof(CustomOrderedQueryable<>).MakeGenericType(elementType)
                : typeof(CustomQueryable<>).MakeGenericType(elementType);
            return Activator.CreateInstance(queryableType, this, dao, expression) as IQueryable;
        }
        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (typeof(IOrderedQueryable).IsAssignableFrom(expression.Type))
            {
                return new CustomOrderedQueryable<TElement>(this, dao, expression);
            }
            return new CustomQueryable<TElement>(this, dao, expression);
        }


        public TResult Execute<TResult>(Expression expression)
        {
            var translator = new SqlExpressionVisitor(dao);
            TranslationResult sqlQuery = translator.Translate(expression);
            LogUtils.Debug($"execute LINQ Query:{sqlQuery.CommandText}");
            Type returnType = typeof(TResult);
            Type itemType = returnType.IsGenericType ? returnType.GetGenericArguments().First() : returnType;
            object? ret = null;
            using DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString());
            try
            {
                connection.Open();
                using DbCommand command = dao.GetDialect().GetDbCommand(connection, sqlQuery.CommandText);
                if (!sqlQuery.Pageable)
                {
                    ret = dao.QueryBySql(command, itemType, sqlQuery.CommandText, sqlQuery.Parameters);
                }
                else
                {
                    string countSql = dao.GetDialect().GenerateCountSql(sqlQuery.CommandText);
                    command.CommandText = countSql;
                    long count = dao.QueryByLong(command, sqlQuery.Parameters);
                    QueryCount = count;
                    if (sqlQuery.SkipRows > 0 && sqlQuery.SkipRows % sqlQuery.PageSize != 0)
                    {
                        throw new BaseSqlException("SkipRow is not times of PageSize!Query may not accurate!!!");
                    }
                    long pageNo = sqlQuery.SkipRows > 0 ? sqlQuery.SkipRows / sqlQuery.PageSize : 1;
                    PageQuery query = new PageQuery(sqlQuery.PageSize, pageNo, count);
                    string pageSql = dao.GetDialect().GeneratePageSql(sqlQuery.CommandText, query);
                    command.CommandText = pageSql;
                    ret = dao.QueryBySql(command, itemType, pageSql, null);
                }
            }
            catch (Exception ex)
            {
                throw new BaseSqlException(ex.Message, ex);
            }

            Trace.Assert(ret != null);
            return (TResult)ret;

        }

        public object? Execute(Expression expression)
        {
            throw new NotImplementedException();
        }
    }
}
