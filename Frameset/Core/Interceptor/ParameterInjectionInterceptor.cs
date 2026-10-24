using Castle.DynamicProxy;
using Frameset.Core.Annotation;
using Frameset.Core.Context;
using System.Reflection;

namespace Frameset.Core.Interceptor
{
    public class ParameterInjectionInterceptor : IInterceptor
    {

        public void Intercept(IInvocation invocation)
        {
            var parameters = invocation.Method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (param.GetCustomAttribute<InjectAttribute>() != null)
                {
                    invocation.Arguments[i] = RegServiceContext.GetBean(param.ParameterType);
                }
            }
            invocation.Proceed();
        }
    }
}
