using Frameset.Core.Dao;
using Frameset.Core.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Frameset.Core.Utils
{
    public class RepositoryContext
    {
        public static readonly Dictionary<Type, object> repositoryMap = new Dictionary<Type, object>();
        private static DAOFactory factory;
        public static void RegisterContext(string yamConfigPath)
        {
            factory = DAOFactory.DoInit(yamConfigPath);
        }

        public static void AddRepository(Type type, object repository)
        {
            Trace.Assert(type.IsGenericType && type.GetGenericArguments()[0] is BaseEntity);
            repositoryMap.TryAdd(type, repository);
        }
        public static object GetInstance(Type type)
        {
            Trace.Assert(type.IsGenericType && type.GetGenericArguments()[0] is BaseEntity);
            object repository = null;
            repositoryMap.TryGetValue(type, out repository);
            if (repository != null)
            {
                return repository;
            }
            return null;
        }

    }
}
