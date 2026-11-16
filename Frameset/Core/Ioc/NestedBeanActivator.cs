using Frameset.Core.Annotation;
using Frameset.Core.Utils;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Frameset.Core.Ioc
{
    public static class NestedBeanActivator
    {
        private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> _ctorCache = new();
        private static readonly ConcurrentDictionary<Type, Func<object, IServiceProvider, object[]>> _memberInjectorCache = new();

        private static readonly MethodInfo GetServiceMethod =
            typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService), new[] { typeof(Type) })!;

        private static readonly MethodInfo GetRequiredServiceMethod =
            typeof(ServiceProviderServiceExtensions).GetMethod(
                nameof(ServiceProviderServiceExtensions.GetRequiredService),
                new[] { typeof(IServiceProvider), typeof(Type) })!;

        public static void PreCompile(Type implType)
        {
            _ctorCache.GetOrAdd(implType, BuildConstructor);
            _memberInjectorCache.GetOrAdd(implType, BuildMemberInjector);
        }

        /// <summary>
        /// Creates an instance (autowiring all constructor params) and recursively populates
        /// [AutoWired] properties/fields across the entire dependency graph.
        /// </summary>
        public static object CreateAndInjectGraph(IServiceProvider sp, Type implType)
        {
            var ctor = _ctorCache.GetOrAdd(implType, BuildConstructor);
            var instance = ctor(sp);

            InjectGraph(instance, sp);
            return instance;
        }

        /// <summary>
        /// Recursively populates [AutoWired] members on 'root' and every nested dependency it touches,
        /// using ReferenceEqualityComparer to safely break circular [AutoWired] graphs.
        /// </summary>
        public static void InjectGraph(object? root, IServiceProvider sp)
        {
            if (root is null) return;

            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            InjectRecursive(root, sp, visited);
        }

        private static void InjectRecursive(object current, IServiceProvider sp, HashSet<object> visited)
        {
            // Prevent infinite recursion on circular [AutoWired] references (A <-> B)
            if (!visited.Add(current))
            {
                LogUtils.Error($"Dependency cicrlar detected {StringUtils.PrintDepenencyTree(visited)}");
                return;
            }

            var injector = _memberInjectorCache.GetOrAdd(current.GetType(), BuildMemberInjector);

            // Populates [AutoWired] members on 'current' and returns the resolved child instances
            var children = injector(current, sp);

            // Recurse into every child dependency (B -> C -> D...)
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] is not null)
                {
                    InjectRecursive(children[i], sp, visited);
                }
            }
        }

        private static Func<IServiceProvider, object> BuildConstructor(Type implType)
        {
            var ctor = implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault()
                ?? throw new InvalidOperationException($"No public constructor found for {implType.FullName}");

            var providerParam = Expression.Parameter(typeof(IServiceProvider), "sp");

            var argExpressions = ctor.GetParameters().Select(param =>
            {
                var typeConst = Expression.Constant(param.ParameterType, typeof(Type));

                if (param.HasDefaultValue)
                {
                    var getService = Expression.Call(providerParam, GetServiceMethod, typeConst);
                    var defaultVal = Expression.Constant(param.DefaultValue, param.ParameterType);
                    return Expression.Coalesce(Expression.Convert(getService, param.ParameterType), defaultVal);
                }

                var getRequired = Expression.Call(GetRequiredServiceMethod, providerParam, typeConst);
                return (Expression)Expression.Convert(getRequired, param.ParameterType);
            });

            var newExpr = Expression.New(ctor, argExpressions);
            return Expression.Lambda<Func<IServiceProvider, object>>(
                Expression.Convert(newExpr, typeof(object)),
                providerParam).Compile();
        }

        private static Func<object, IServiceProvider, object[]> BuildMemberInjector(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var props = type.GetProperties(flags)
                .Where(p => p.CanWrite && p.IsDefined(typeof(AutoInjectAttribute), true))
                .ToArray();

            var fields = type.GetFields(flags)
                .Where(f => !f.IsInitOnly && f.IsDefined(typeof(AutoInjectAttribute), true))
                .ToArray();

            if (props.Length == 0 && fields.Length == 0)
                return static (_, _) => Array.Empty<object>();

            var targetParam = Expression.Parameter(typeof(object), "target");
            var providerParam = Expression.Parameter(typeof(IServiceProvider), "sp");

            var typedVar = Expression.Variable(type, "typed");
            var childrenVar = Expression.Variable(typeof(object[]), "children");

            int totalCount = props.Length + fields.Length;
            var statements = new List<Expression>
        {
            Expression.Assign(typedVar, Expression.Convert(targetParam, type)),
            Expression.Assign(childrenVar, Expression.NewArrayBounds(typeof(object), Expression.Constant(totalCount)))
        };

            int index = 0;

            foreach (var prop in props)
            {
                var depVar = Expression.Variable(prop.PropertyType, "dep");
                var resolve = Expression.Call(GetRequiredServiceMethod, providerParam, Expression.Constant(prop.PropertyType, typeof(Type)));

                var block = Expression.Block(
                    new[] { depVar },
                    Expression.Assign(depVar, Expression.Convert(resolve, prop.PropertyType)),
                    Expression.Assign(Expression.Property(typedVar, prop), depVar),
                    Expression.Assign(Expression.ArrayAccess(childrenVar, Expression.Constant(index++)), Expression.Convert(depVar, typeof(object)))
                );
                statements.Add(block);
            }

            foreach (var field in fields)
            {
                var depVar = Expression.Variable(field.FieldType, "dep");
                var resolve = Expression.Call(GetRequiredServiceMethod, providerParam, Expression.Constant(field.FieldType, typeof(Type)));

                var block = Expression.Block(
                    new[] { depVar },
                    Expression.Assign(depVar, Expression.Convert(resolve, field.FieldType)),
                    Expression.Assign(Expression.Field(typedVar, field), depVar),
                    Expression.Assign(Expression.ArrayAccess(childrenVar, Expression.Constant(index++)), Expression.Convert(depVar, typeof(object)))
                );
                statements.Add(block);
            }

            statements.Add(childrenVar);

            var body = Expression.Block(new[] { typedVar, childrenVar }, statements);
            return Expression.Lambda<Func<object, IServiceProvider, object[]>>(body, targetParam, providerParam).Compile();
        }
    }
}
