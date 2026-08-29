using System.Reflection;
using System.Runtime.Loader;
using Frameset.Tracing.Tracer;
using Microsoft.IdentityModel.Tokens;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Frameset.Tracing.Transformer;

public class ProbeDbTransformer
{
    
    public ProbeDbTransformer()
    {
        
    }
    public void Initialize()
    {
        Console.WriteLine("--- begin to add hook -----");
        AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;
    }

    private Assembly OnAssemblyResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        string detectedName = assemblyName.Name ?? "";
        
        if (!IsAssmblyDb(detectedName))
        {
            return null;
        }
        string assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName.Name}.dll");
        if (!File.Exists(assemblyPath)) return null;
        using var testDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
        if (InjectADOTracing(testDefinition.MainModule))
        {
            testDefinition.Name.HasPublicKey = false;
            testDefinition.Name.PublicKey = Array.Empty<byte>();
            testDefinition.MainModule.Attributes &= ~ModuleAttributes.StrongNameSigned;
            using var outputStream = new MemoryStream();
            testDefinition.Write(outputStream);
            outputStream.Position = 0;
            return context.LoadFromStream(outputStream);
        }
        return null;
    }

    private bool InjectADOTracing(ModuleDefinition module)
    {
        bool modified = false;
        
        var dbCommandTypes = module.Types
            .Where(t => t.BaseType != null && t.BaseType.FullName == "System.Data.Common.DbCommand").ToList();
        if (!dbCommandTypes.IsNullOrEmpty())
        {
            var activityType = module.ImportReference(typeof(System.Diagnostics.Activity));
            var startMethod = module.ImportReference(typeof(DbTracer).GetMethod("StartTrace"));
            var endMethod = module.ImportReference(typeof(DbTracer).GetMethod("EndTrace"));
            var errorMethod = module.ImportReference(typeof(DbTracer).GetMethod("LogException"));
            var exceptionType = module.ImportReference(typeof(Exception));

            foreach (var type in dbCommandTypes)
            {
                var targets = type.Methods.Where(m =>
                    m.Name == "ExecuteReader" || m.Name == "ExecuteNonQuery" || m.Name == "ExecuteScalar").ToList();

                foreach (var method in targets)
                {
                    if (!method.HasBody) continue;

                    var processor = method.Body.GetILProcessor();
                     // 宣告區域變數：index 0 為 Activity, index 1 為 Exception
                    var activityVar = new VariableDefinition(activityType);
                    var exceptionVar = new VariableDefinition(exceptionType);
                    method.Body.Variables.Add(activityVar);
                    method.Body.Variables.Add(exceptionVar);

                    // --- 1. 方法最開頭：Activity = StartTrace(this) ---
                    var startFirst = method.Body.Instructions.First();
                    processor.InsertBefore(startFirst, processor.Create(OpCodes.Ldarg_0));
                    processor.InsertBefore(startFirst, processor.Create(OpCodes.Call, startMethod));
                    processor.InsertBefore(startFirst, processor.Create(OpCodes.Stloc, activityVar));

                    // --- 2. 建立 Catch 區塊的指令 ---
                    // Catch 開頭：將 runtime 丟出的 exception 存入變數
                    var catchStart = processor.Create(OpCodes.Stloc, exceptionVar);
                    // 呼叫 LogException(activity, exception)
                    var callError = processor.Create(OpCodes.Ldloc, activityVar);
                    var callError2 = processor.Create(OpCodes.Ldloc, exceptionVar);
                    //var callError3 = module.ImportReference(typeof(DbTracer).GetMethod("LogException"));
                    var callErrorFinal = processor.Create(OpCodes.Call, errorMethod);
                    // 重新拋出異常 (rethrow)
                    var rethrow = processor.Create(OpCodes.Rethrow);

                    // 將 Catch 指令群附加到原本方法的最後面
                    processor.Append(catchStart);
                    processor.Append(callError);
                    processor.Append(callError2);
                    processor.Append(callErrorFinal);
                    processor.Append(rethrow);

                    // --- 3. 修改原本的所有 Ret (Return) 指令，回傳前補呼叫 EndTrace ---
                    // 注意：排除我們剛剛加在末尾的指令，只尋找原本的 ret
                    var origRetInstructions = method.Body.Instructions
                        .Where(i => i.OpCode == OpCodes.Ret && i != catchStart && i != callError && i != callError2 && i != callErrorFinal && i != rethrow)
                        .ToList();

                    foreach (var ret in origRetInstructions)
                    {
                        processor.InsertBefore(ret, processor.Create(OpCodes.Ldloc, activityVar));
                        processor.InsertBefore(ret, processor.Create(OpCodes.Call, endMethod));
                    }

                    // --- 4. 關鍵：織入 Try-Catch 邊界保護 (ExceptionHandler) ---
                    var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
                    {
                        // Try 區塊範圍：從原本的第一條指令，到 Catch 區塊開頭的前一條
                        TryStart = startFirst,
                        TryEnd = catchStart,
                        // Catch 區塊範圍：從 catchStart 到 rethrow 的下一條 (也就是方法結束)
                        HandlerStart = catchStart,
                        HandlerEnd = method.Body.Instructions.Last().Next,
                        CatchType = exceptionType
                    };
                    method.Body.ExceptionHandlers.Add(handler); 
                }
            }
            modified = true;
        }

        return modified;
    }

    private bool IsAssmblyDb(string name)
    {
        return name.Equals("Oracle.ManagedDataAccess", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Oracle.ManagedDataAccess.Core", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("MySql.Data", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("MySqlConnector", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Npgsql", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("ClickHouse.Client", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Microsoft.Data.SqlClient", StringComparison.CurrentCultureIgnoreCase) ||
               name.Equals("Net.IBM.Data.Db2-lnx", StringComparison.OrdinalIgnoreCase);

    }
}