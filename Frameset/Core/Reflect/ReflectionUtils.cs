using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Frameset.Core.Reflect
{
    public static class ReflectionUtils
    {
        private static readonly ConcurrentDictionary<(Type, string), Func<object, object?>> _getters = new();
        private static readonly ConcurrentDictionary<(Type, string), Action<object, object?>> _setters = new();
        private const BindingFlags MemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static Func<object, object?> GetGetter(Type selType, string propName)
        {
            return _getters.GetOrAdd((selType, propName), CreateGetter);
        }
        public static Action<object, object?> GetSetter(Type selType, string propName)
        {
            return _setters.GetOrAdd((selType, propName), CreateSetter);
        }
        private static Func<object, object?> CreateGetter((Type selType, string Name) key)
        {
            MemberInfo member = GetMember(key.selType, key.Name);

            ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
            UnaryExpression castInstance = Expression.Convert(instanceParam, key.selType);

            // Works for both PropertyInfo and FieldInfo
            MemberExpression memberAccess = Expression.MakeMemberAccess(castInstance, member);
            UnaryExpression castResult = Expression.Convert(memberAccess, typeof(object));

            return Expression.Lambda<Func<object, object?>>(castResult, instanceParam).Compile();
        }

        private static Action<object, object?> CreateSetter((Type selType, string Name) key)
        {
            MemberInfo member = GetMember(key.selType, key.Name);
            Type memberType = GetMemberType(member);

            ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
            ParameterExpression valueParam = Expression.Parameter(typeof(object), "value");

            UnaryExpression castInstance = Expression.Convert(instanceParam, key.selType);
            UnaryExpression castValue = Expression.Convert(valueParam, memberType);

            MemberExpression memberAccess = Expression.MakeMemberAccess(castInstance, member);
            BinaryExpression assign = Expression.Assign(memberAccess, castValue);

            return Expression.Lambda<Action<object, object?>>(assign, instanceParam, valueParam).Compile();
        }
        private static MemberInfo GetMember(Type type, string name)
        {
            // Search properties first, then fields
            PropertyInfo? prop = type.GetProperty(name, MemberFlags);
            if (prop != null)
            {
                if (!prop.CanRead && !prop.CanWrite)
                    throw new InvalidOperationException($"Property '{name}' is inaccessible.");
                return prop;
            }

            FieldInfo? field = type.GetField(name, MemberFlags);
            if (field != null)
            {
                if (field.IsInitOnly || field.IsLiteral)
                    throw new InvalidOperationException($"Field '{name}' is readonly or const.");
                return field;
            }

            throw new ArgumentException($"Property or field '{name}' not found on {type.Name}.");
        }

        private static Type GetMemberType(MemberInfo member) => member switch
        {
            PropertyInfo p => p.PropertyType,
            FieldInfo f => f.FieldType,
            _ => throw new NotSupportedException()
        };
    }
}
