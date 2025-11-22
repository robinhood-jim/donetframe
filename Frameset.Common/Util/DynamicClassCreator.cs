using Frameset.Core.Dao;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;


namespace Frameset.Common.Util
{
    public class DynamicClassCreator
    {
        private static readonly AssemblyName assemblyName;
        private static readonly AssemblyBuilder assemblyBuilder;
        private static readonly ModuleBuilder moduleBuilder;
        static DynamicClassCreator()
        {
            assemblyName = new AssemblyName("DynamicAssembly");
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
        }


        public static Type CreateDynamicClass(string className, IList<DataSetColumnMeta> columns)
        {
            TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class);
            Trace.Assert(!columns.IsNullOrEmpty());
            foreach (DataSetColumnMeta column in columns)
            {
                Type propertyType = DataMetaUtils.GetValueType(column.ColumnType);
                FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + column.ColumnCode, propertyType, FieldAttributes.Private);
                PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(column.ColumnCode, PropertyAttributes.HasDefault, propertyType, null);
                MethodBuilder getMethodBuilder = typeBuilder.DefineMethod("get_" + column.ColumnCode, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, propertyType, Type.EmptyTypes);
                ILGenerator getIL = getMethodBuilder.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Ldfld, fieldBuilder);
                getIL.Emit(OpCodes.Ret);
                propertyBuilder.SetGetMethod(getMethodBuilder);
                MethodBuilder setMethodBuilder = typeBuilder.DefineMethod("set_" + column.ColumnCode, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, null, new Type[] { propertyType });
                ILGenerator setIL = setMethodBuilder.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Stfld, fieldBuilder);
                setIL.Emit(OpCodes.Ret);
                propertyBuilder.SetSetMethod(setMethodBuilder);
            }
            return typeBuilder.CreateType();
        }


    }
}
