using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Frameset.Core.Utils
{
    public static class ExpressionUtils
    {
        private static readonly WeakLruDictionary<Type, Func<dynamic>> funcMap = new(100);
        public static Func<O> GetExpressionFunction<O>()
        {
            Type returnType = typeof(O);
            Func<O>? retFun;
            if (!funcMap.TryGetValue(returnType, out Func<dynamic>? func))
            {
                NewExpression constructExpression = Expression.New(typeof(O));
                Expression<Func<O>> lamadaExpression = Expression.Lambda<Func<O>>(constructExpression);
                retFun = lamadaExpression.Compile();
                func = retFun as Func<dynamic>;
                Trace.Assert(func != null);
                funcMap.TryAdd(returnType, func);
            }
            retFun = func as Func<O>;
            Trace.Assert(retFun != null);
            return retFun;
        }
        public static ConstructorInfo? GetConstructionInfo<V>(Type[] constructionTypes)
        {
            return typeof(V).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null,
                   CallingConventions.HasThis,
                   constructionTypes,
                   new ParameterModifier[0]);
        }


        public static List<ParameterExpression> GetLambdaParameterExpressions(Type[] argumentTypes)
        {
            List<ParameterExpression> Expressions = new List<ParameterExpression>();
            for (int i = 0; i < argumentTypes.Length; i++)
            {
                Expressions.Add(Expression
                   .Parameter(argumentTypes[i], string.Concat("param", i)));
            }
            return Expressions;
        }


    }
}
