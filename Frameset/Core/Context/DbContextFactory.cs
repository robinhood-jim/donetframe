using Frameset.Core.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Frameset.Core.Context
{
    public static class DbContextFactory
    {
        public static readonly string CONTEXTDEFAULTNAME = "DEFALUT";
        public static readonly Dictionary<string, IDbContext> contextMap = [];
        public static void Register(IDbContext context)
        {
            Trace.Assert(!string.IsNullOrWhiteSpace(context.GetContextName()), "Context Name must Exists!");
            contextMap.TryAdd(context.GetContextName(), context);
        }
        public static IDbContext GetContext(string contextName = "DEFALUT")
        {
            if (contextMap.TryGetValue(contextName, out IDbContext context))
            {
                return context;
            }
            throw new OperationFailedException("context not found!");
        }

        public static void Dispose()
        {
            foreach(var entry in contextMap)
            {
                entry.Value.Dispose();
            }
        }
    }
}
