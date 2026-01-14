using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Frameset.Core.Utils
{
    public static class ExpressionUtils
    {
        private static readonly Dictionary<Type, Func<dynamic>> funcMap = [];
        public static Func<O> GetExpressionFunction<O>()
        {
            Type returnType = typeof(O);
            Func<O> retFun;
            if (!funcMap.TryGetValue(returnType, out Func<dynamic> func))
            {
                NewExpression constructExpression = Expression.New(typeof(O));
                Expression<Func<O>> lamadaExpression = Expression.Lambda<Func<O>>(constructExpression);
                retFun = lamadaExpression.Compile();
                funcMap.TryAdd(returnType, retFun as Func<dynamic>);
            }
            else
            {
                retFun = func as Func<O>;
            }
            return retFun;
        }
        
    }
}
