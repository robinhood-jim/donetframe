using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;

namespace Frameset.Core.Scripts
{
    public sealed class CSScriptExpression
    {
        private static ConcurrentDictionary<string, ScriptRunner<object>> scriptCache = [];
        private static readonly ConcurrentDictionary<string, SchemaCompilationData> _compiledTypeCache = new();
        private static AssemblyLoadContext loadContext;
        private static readonly object _lockObject = new();
        private static bool _isDisposed;
        public class SchemaCompilationData
        {
            public Type ContextType { get; set; }
            public byte[] AssemblyBytes { get; set; }
            public Assembly CompiledAssembly { get; set; }
            public string ClassName
            {
                get; set;
            }
            public string Key { get; set; }
        }

        static CSScriptExpression()
        {
            loadContext = new AssemblyLoadContext("GlobalScriptSandbox", true);
            loadContext.Resolving += (context, assemblyName) =>
            {
                return loadContext.Assemblies
                    .FirstOrDefault(a => a.FullName == assemblyName.FullName);
            };
            AssemblyLoadContext.Default.Resolving += DefaultContext_Resolving;
            AppDomain.CurrentDomain.ProcessExit += (sender, e) => ShutdownAndCleanMemory();
        }
        private static Assembly DefaultContext_Resolving(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            // Search through our static cache items to find the loaded binary assembly matching the runtime request name
            var matchedSchema = _compiledTypeCache.Values
                .FirstOrDefault(data => data.CompiledAssembly != null && data.CompiledAssembly.FullName == assemblyName.FullName);

            if (matchedSchema != null)
            {
                return matchedSchema.CompiledAssembly;
            }

            // Fallback: search by name inside the context
            return loadContext?.Assemblies
                .FirstOrDefault(a => a.FullName == assemblyName.FullName);
        }
        public static SchemaCompilationData ConstructDynamicType(IList<DataSetColumnMeta> columnMetas, string expression)
        {
            lock (_lockObject)
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(CSharpExtensions));
            }
            string schemaKey = string.Join("|", columnMetas.OrderBy(k => k.ColumnCode)
           .Select(k => $"{k.ColumnCode}:{k.ColumnType.ToString()}"));

            string className = "Context_" + Guid.NewGuid().ToString("N");
            Trace.Assert(!columnMetas.IsNullOrEmpty(), "meta is null");
            SchemaCompilationData runtimeType = _compiledTypeCache.GetOrAdd(schemaKey + "=>" + expression, _ =>
            {
                //return DynamicClassCreator.CreateDynamicFieldClass(className, columnMetas);
                StringBuilder builder = new StringBuilder("using System;\npublic class " + className + " {\n");
                foreach (DataSetColumnMeta meta in columnMetas)
                {
                    string typeName = GetTypeStrByMeta(meta);
                    builder.Append("    public ").Append(typeName).Append(' ').Append(meta.ColumnCode).AppendLine(";");
                }
                builder.AppendLine("    public object Run() {");
                builder.Append($"       return {expression};");
                builder.AppendLine("    }");
                builder.AppendLine("}");
                var option = ScriptOptions.Default.WithImports("System").AddImports("System.Collections.Generic");
                var classScript = CSharpScript.Create(builder.ToString(), option);
                var compilation = classScript.GetCompilation();
                using (var stream = new MemoryStream())
                {
                    var emitResult = compilation.Emit(stream);
                    if (!emitResult.Success)
                    {
                        var errors = string.Join("\n", emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.GetMessage()));
                        LogUtils.Error("compiled error ${errors}");
                        throw new Exception(errors);
                    }
                    byte[] rawByte = stream.ToArray();
                    stream.Seek(0, SeekOrigin.Begin);
                    var assembly = loadContext.LoadFromStream(stream);
                    Type[] types = assembly.GetTypes();
                    Type retType = null;
                    foreach (Type selType in types)
                    {
                        if (selType.Name.Equals(className))
                        {
                            retType = selType;
                            break;
                        }
                    }
                    if (retType == null)
                    {
                        throw new Exception("execption in generate dynamic " + schemaKey);
                    }

                    return new SchemaCompilationData
                    {
                        ContextType = retType,
                        AssemblyBytes = rawByte,
                        CompiledAssembly = assembly,
                    };
                }
            });
            runtimeType.ClassName = className;
            runtimeType.Key = schemaKey;
            return runtimeType;
        }
        public static ScriptRunner<object> ConsturctScriptRunner(SchemaCompilationData data, string expression)
        {
            lock (_lockObject)
            {
                if (_isDisposed) throw new ObjectDisposedException(nameof(CSharpExtensions));
            }
            return scriptCache.GetOrAdd(data.Key + expression, _ =>
            {
                var assemblyReference = MetadataReference.CreateFromImage(data.AssemblyBytes);
                var option = ScriptOptions.Default.WithImports("System", "System.Collections.Generic").AddReferences(assemblyReference);

                var classScript = CSharpScript.Create(expression, option, globalsType: data.ContextType);
                classScript.Compile();
                return classScript.CreateDelegate();
            });
        }


        public static object Eval(IList<DataSetColumnMeta> columnMetas, Dictionary<string, object> valueDict, Type dynamicType, ScriptRunner<object> script)
        {
            var invokeObj = Activator.CreateInstance(dynamicType);
            foreach (DataSetColumnMeta meta in columnMetas)
            {
                var field = dynamicType.GetField(meta.ColumnCode);
                if (field != null && valueDict.TryGetValue(meta.ColumnCode, out object value) && value != null)
                {
                    field.SetValue(invokeObj, ConvertUtil.ConvertStringToTargetObject(value, meta));
                }
            }
            return script(invokeObj).GetAwaiter().GetResult();
        }
        private static void ShutdownAndCleanMemory()
        {
            lock (_lockObject)
            {
                if (_isDisposed) return;
                _compiledTypeCache.Clear();
                scriptCache.Clear();

                if (loadContext != null)
                {
                    loadContext.Unload();
                    loadContext = null;
                }
                _isDisposed = true;
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        internal static string GetTypeStrByMeta(DataSetColumnMeta dataSetColumn)
        {
            string typeStr = string.Empty;

            switch (dataSetColumn.ColumnType)
            {
                case Constants.MetaType.SHORT:
                    typeStr = "short";
                    break;
                case Constants.MetaType.INTEGER:
                    typeStr = "int";
                    break;
                case Constants.MetaType.LONG:
                    typeStr = "long";
                    break;
                case Constants.MetaType.FLOAT:
                    typeStr = "float";
                    break;
                case Constants.MetaType.DOUBLE:
                    typeStr = "double";
                    break;
                case Constants.MetaType.TIMESTAMP:
                    typeStr = "System.DateTime";
                    break;
                case Constants.MetaType.DATE:
                    typeStr = "System.DateTime";
                    break;
                case Constants.MetaType.BOOLEAN:
                    typeStr = "bool";
                    break;
                case Constants.MetaType.BLOB:
                    typeStr = "byte[]";
                    break;
                default:
                    typeStr = "string";
                    break;
            }
            return typeStr;
        }
    }
}
