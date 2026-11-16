using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Reflect;
using Frameset.Core.Service;
using Frameset.Core.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Frameset.Web.Controller
{
    public class AbstractController<R, V, P> : AbstractControllerBase where R : IService<V, P> where V : BaseEntity where P : notnull
    {
        [AutoInject]
        protected R _service = default!;

        protected AbstractController(R baseService)
        {
            _service = baseService;
        }
        public AbstractController()
        {
            //_service = RegServiceContext.GetBean<R>();
        }
        public JsonResult SaveEntity(object input)
        {
            Trace.Assert(input != null && _service != null);
            bool saveOk = false;
            if (input.GetType().Equals(typeof(V)))
            {
                saveOk = _service.SaveEntity((V)input);
            }
            else
            {
                saveOk = _service.SaveEntity(GetValueFrom(input));
            }
            return OutputMsg(saveOk);
        }
        public JsonResult UpdateEntity(object input)
        {
            Trace.Assert(input != null && _service != null);
            bool saveOk = false;
            if (input.GetType().Equals(typeof(V)))
            {
                saveOk = _service.UpdateEntity((V)input);
            }
            else
            {
                saveOk = _service.UpdateEntity(GetValueFrom(input));
            }
            return OutputMsg(saveOk);
        }

        public PageDTO<T> QueryPage<T>(PageQuery query)
        {
            Trace.Assert(_service != null);
            return _service.QueryPage<T>(query);
        }
        public JsonResult GetEntity(P id)
        {
            Trace.Assert(_service != null);
            V vo = _service.GetById(id);
            if (vo != null)
            {
                return OutputMsg(vo);
            }
            else
            {
                return OutputErrMsg("id not found!");
            }
        }
        public JsonResult RemoveEntity(P id)
        {
            Trace.Assert(_service != null);
            return OutputMsg(_service.RemoveEntity([id]));
        }
        public JsonResult RemoveEnities(string ids)
        {
            List<P> idsArr = [];
            string[] idsStrArr = ids.Split(',');
            foreach (var idStr in idsStrArr)
            {
                idsArr.Add((P)Convert.ChangeType(idStr, typeof(P)));
            }
            return OutputMsg(_service.RemoveEntity(idsArr));
        }

        protected V GetValueFrom(object input)
        {
            V retObj = Activator.CreateInstance<V>();
            Dictionary<string, MethodParam> paramMap = AnnotationUtils.ReflectObject(typeof(V));
            if (input.GetType().IsTypeDictionary())
            {
                foreach (var entry in (Dictionary<string, object>)input)
                {
                    string key = StringUtils.Capitalize(entry.Key);
                    if (paramMap.TryGetValue(key, out MethodParam? param))
                    {
                        param.Setter(retObj,  ConvertUtil.ParseByType(param.ParamType, entry.Value));
                    }
                }
            }
            else
            {
                Dictionary<string, MethodParam> sourceMethodMap = AnnotationUtils.ReflectObject(input.GetType());
                foreach (var entry in sourceMethodMap)
                {
                    if (entry.Value != null && paramMap.TryGetValue(entry.Key, out MethodParam? param))
                    {
                        object? obj = entry.Value.Getter(input);
                        if(obj!=null)
                            param.Setter(retObj, ConvertUtil.ParseByType(param.ParamType,obj));
                    }
                }
            }
            return retObj;

        }


    }
}
