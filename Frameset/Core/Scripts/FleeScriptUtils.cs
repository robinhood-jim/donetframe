using System;
using System.Collections.Generic;
using Flee.PublicTypes;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.Core.Scripts;

public class FleeScriptUtils
{
    public static IDynamicExpression Compile(IList<DataSetColumnMeta> colmetas,string expression,Type[] addPackages,out ExpressionContext context)
    {
        context = new ExpressionContext();
        foreach (DataSetColumnMeta columnMeta in colmetas)
        {
            context.Variables[columnMeta.ColumnCode] = GetDefaultValue(columnMeta);
        }

        if (!addPackages.IsNullOrEmpty())
        {
            foreach (Type packageType in addPackages)
            {
                context.Imports.AddType(packageType);
            }
        }
        return context.CompileDynamic(expression);
    }

    public static IDynamicExpression Compile<T>(string expression, Type[] addPackages, out ExpressionContext context,string objectParameter="o")
    {
        Type modelType = typeof(T);
        context = new ExpressionContext();
        context.Imports.AddType(modelType);
        context.Variables[objectParameter] = Activator.CreateInstance<T>();
        if (!addPackages.IsNullOrEmpty())
        {
            foreach (Type packageType in addPackages)
            {
                context.Imports.AddType(packageType);
            }
        }
        return context.CompileDynamic(expression);
    }

    public static object EvalDict(IList<DataSetColumnMeta> colmetas,Dictionary<string, object> input, ExpressionContext context,
        IDynamicExpression expression)
    {
        foreach (DataSetColumnMeta columnMeta in colmetas)
        {
            if (input.TryGetValue(columnMeta.ColumnCode, out object value))
            {
                context.Variables[columnMeta.ColumnCode] = value;
            }
            else
            {
                context.Variables[columnMeta.ColumnCode] = null;
            }
        }
        return expression.Evaluate();
    }

    public static object Eval<T>(T input, ExpressionContext context,
        IDynamicExpression expression, string objectParameter = "o")
    {
        context.Variables[objectParameter] = input;
        return expression.Evaluate();
    }

    protected static object GetDefaultValue(DataSetColumnMeta columnMeta)
    {
        return columnMeta.ColumnType switch
        {
            Constants.MetaType.LONG => Convert.ToInt64('1'),
            Constants.MetaType.SHORT => Convert.ToInt16('1'),
            Constants.MetaType.INTEGER => 1,
            Constants.MetaType.FLOAT => Convert.ToSingle("1.0"),
            Constants.MetaType.DOUBLE => 1.0,
            Constants.MetaType.DATE => DateTime.Now,
            Constants.MetaType.TIMESTAMP => DateTime.Now,
            Constants.MetaType.NUMERIC => Convert.ToDecimal("1.0"),
            _ => string.Empty
        };
    }
    
}