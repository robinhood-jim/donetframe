using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

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
        public static ConstructorInfo GetConstructionInfo<V>(Type[] constructionTypes)
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
