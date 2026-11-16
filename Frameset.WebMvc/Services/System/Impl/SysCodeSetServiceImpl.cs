using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Reflect;
using Frameset.Core.Service;
using Frameset.Core.Utils;
using Frameset.Web.Model.System;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace Frameset.WebMvc.Services.System.Impl
{
    [Service]
    public class SysCodeSetServiceImpl : ServiceImpl<SysCodeset, long>, ISysCodesetService
    {
        private readonly WeakLruDictionary<string, List<SysCode>> dict = new();
        public List<SysCode> GetSysCode(string dictCode)
        {
            if (!dict.TryGetValue(dictCode, out List<SysCode> codes))
            {
                List<SysCodeset> codesets = dbContext.QueryModelsByField<SysCodeset>(nameof(SysCodeset.Code), Constants.SqlOperator.EQ, [dictCode]);
                if (!codesets.IsNullOrEmpty() && codesets.Count == 1)
                {
                    codes = codesets[0].Selections;
                    dict.TryAdd(dictCode, codes);
                }
            }
            return codes;

        }
        public void FilterListByCodeSet<V>(List<V> results, string codeName, string codeNo, string? defaultValue = null) where V : notnull
        {
            FilterListNewByCodeSet(results, codeName, codeName, codeNo, defaultValue);
        }

        public void FilterListNewByCodeSet<V>(List<V> results, string codeName, string newColumnName, string codeNo, string? defaultValue = null) where V : notnull
        {

            List<SysCode> codes = GetSysCode(codeNo);
            Dictionary<string, SysCode> codeMap = codes.ToDictionary(x => x.Value, x => x);
            bool valueTranfer = false;
            foreach (V vo in results)
            {
                if (typeof(V).IsAssignableFrom(typeof(Dictionary<string, object>)))
                {
                    Dictionary<string, object>? dict = vo as Dictionary<string, object>;
                    Trace.Assert(dict != null, "");
                    if (dict.TryGetValue(codeName, out object? getObj))
                    {
                        if (getObj != null)
                        {
                            string? getStr = getObj.ToString();
                            if (!string.IsNullOrWhiteSpace(getStr) && codeMap.TryGetValue(getStr, out SysCode? code))
                            {
                                dict[newColumnName] = code.Name;
                                valueTranfer = true;
                            }
                        }
                    }
                    if (!valueTranfer && !string.IsNullOrWhiteSpace(defaultValue))
                    {
                        dict[codeName] = defaultValue;
                    }
                }
                else
                {
                    Dictionary<string, MethodParam> methodParam = AnnotationUtils.ReflectObject(typeof(V));
                    if (methodParam.TryGetValue(codeName, out MethodParam? param) && methodParam.TryGetValue(newColumnName, out MethodParam? param1))
                    {
                        object? value = param.PropertyInfo.GetValue(vo);
                        if (value != null)
                        {
                            string? valueStr = value.ToString();
                            if (!string.IsNullOrWhiteSpace(valueStr) && codeMap.TryGetValue(valueStr, out SysCode? code))
                            {
                                param1.Setter(vo, code.Name);
                                valueTranfer = true;
                            }
                        }
                        if (!valueTranfer && !string.IsNullOrWhiteSpace(defaultValue))
                        {
                            param1.Setter(vo, defaultValue);
                        }
                    }
                }
            }
        }
    }
}
