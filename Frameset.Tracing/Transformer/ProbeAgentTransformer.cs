using System.Reflection;
using System.Runtime.Loader;
using Frameset.Tracing.Tracer;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Frameset.Tracing.Transformer;

public class ProbeAgentTransformer
{
    private readonly List<string> metionedNamespaces=[];
    public ProbeAgentTransformer(string enhancedNamespaces)
    {
        metionedNamespaces = new List<string>(enhancedNamespaces.Split(' '));
    }

    public void Initialize()
    {
        AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;
    }

    private Assembly OnAssemblyResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        string expectedPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName.Name}.dll");
        if (!File.Exists(expectedPath)) return null;
        using var testDefinition = AssemblyDefinition.ReadAssembly(expectedPath);
        bool hasTargetNamespace = testDefinition.MainModule.Types
            .Any(t => t.Namespace != null && CanNamespaceScan(metionedNamespaces,t.Namespace));
        if (!hasTargetNamespace)
        {
            return null;
        }
        byte[] originalBytes = File.ReadAllBytes(expectedPath);
        using var memoryStream = new MemoryStream(originalBytes);
        using var assemblyDef = AssemblyDefinition.ReadAssembly(memoryStream);
        var module = assemblyDef.MainModule;
        
        foreach (var type in module.Types.Where(t => t.Namespace != null && CanNamespaceScan(metionedNamespaces,t.Namespace)))
        {
            foreach (var method in type.Methods)
            {
                // 忽略沒有實體程式碼的方法（如抽象方法或介面）
                if (!method.HasBody) continue;

                // 進行 IL 增強：在每個方法開頭注入一條 Console.WriteLine 日誌
                EnhanceMethod(module, method);
            }
        }
        using var outputStream = new MemoryStream();
        assemblyDef.Write(outputStream);
        outputStream.Position = 0;

        // 6. 將記憶體中的 Byte 陣列載入至運行期（黑客只能靠 Memory Dump 破解）
        return context.LoadFromStream(outputStream);
    }
    private void EnhanceMethod(ModuleDefinition module, MethodDefinition method)
    {
        var ilProcessor = method.Body.GetILProcessor();
        var instructions = method.Body.Instructions;
        var firstInstruction = method.Body.Instructions.First();
        var activityVar = new VariableDefinition(method.Module.TypeSystem.Object);
        method.Body.Variables.Add(activityVar);
       
        var startMethod = typeof(GlobalTracer).GetMethod(nameof(GlobalTracer.StartTrace));
        var endMethod = typeof(GlobalTracer).GetMethod(nameof(GlobalTracer.EndTrace));
        var startMethodRef = module.ImportReference(startMethod);
        var endMethodRef = module.ImportReference(endMethod);
        // 建立 IL 指令
        var ldstr = ilProcessor.Create(OpCodes.Ldstr,  $"{method.DeclaringType.FullName}.{method.Name}");
        var callStartInst = ilProcessor.Create(OpCodes.Call, startMethodRef);
        var stlocInst = ilProcessor.Create(OpCodes.Stloc, activityVar);
        
        ilProcessor.InsertBefore(firstInstruction, ldstr);
        ilProcessor.InsertAfter(ldstr, callStartInst);
        ilProcessor.InsertAfter(callStartInst, stlocInst);
        var ldlocInst = ilProcessor.Create(OpCodes.Ldloc, activityVar);
        var callEndInst = ilProcessor.Create(OpCodes.Call, endMethodRef);
        var endfinallyInst = ilProcessor.Create(OpCodes.Endfinally);
        ilProcessor.Append(ldlocInst);
        ilProcessor.Append(callEndInst);
        ilProcessor.Append(endfinallyInst);
        var retInst = ilProcessor.Create(OpCodes.Ret);
        ilProcessor.Append(retInst);
        int originalCount = instructions.Count - 4;
        for (int i = 0; i < originalCount; i++)
        {
            var inst = instructions[i];
            if (inst.OpCode == OpCodes.Ret)
            {
                // 同步方法遇到 Return 時，必須改為 Leave，才會安全地先走 Finally 區塊
                inst.OpCode = OpCodes.Leave;
                inst.Operand = retInst;
            }
        }
        var handler = new ExceptionHandler(ExceptionHandlerType.Finally)
        {
            TryStart = firstInstruction,
            TryEnd = ldlocInst,       // Try 的範圍結束於 Finally 指令前
            HandlerStart = ldlocInst, // Finally 區塊從讀取 Local 變數開始
            HandlerEnd = retInst      // Finally 到最終 Ret 之前結束
        };

        method.Body.ExceptionHandlers.Add(handler);
        method.Body.SimplifyMacros();
        method.Body.OptimizeMacros();
    }

    private static bool CanNamespaceScan(List<string> mentionNamespaces, string namespaceString)
    {
        bool contains = false;
        foreach (string scannamespaces in mentionNamespaces)
        {
            if (namespaceString.StartsWith(scannamespaces))
            {
                contains = true;
                break;
            }
        }

        return contains;
    }
}