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
        protected R repository;


        public AbstractController(R baseRepository)
        {
            repository = baseRepository;
        }
        public AbstractController()
        {
            repository = default(R);
        }
        public JsonResult SaveEntity(object input)
        {
            Trace.Assert(input != null);
            bool saveOk = false;
            if (input.GetType().Equals(typeof(V)))
            {
                saveOk = repository.SaveEntity((V)input);
            }
            else
            {
                saveOk = repository.SaveEntity(GetValueFrom(input));
            }
            return OutputMsg(saveOk);
        }
        public JsonResult UpdateEntity(object input)
        {
            Trace.Assert(input != null);
            bool saveOk = false;
            if (input.GetType().Equals(typeof(V)))
            {
                saveOk = repository.UpdateEntity((V)input);
            }
            else
            {
                saveOk = repository.UpdateEntity(GetValueFrom(input));
            }
            return OutputMsg(saveOk);
        }

        public PageDTO<T> QueryPage<T>(PageQuery query)
        {
            return repository.QueryPage<T>(query);
        }
        public JsonResult GetEntity(P id)
        {
            V vo = repository.GetById(id);
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
            return OutputMsg(repository.RemoveEntity([id]));
        }

        internal V GetValueFrom(object input)
        {
            V retObj = Activator.CreateInstance<V>();
            Dictionary<string, MethodParam> paramMap = AnnotationUtils.ReflectObject(typeof(V));
            if (input.GetType().Equals(typeof(Dictionary<string, object>)))
            {
                foreach (var entry in (Dictionary<string, object>)input)
                {
                    string key = StringUtils.Capitalize(entry.Key);
                    MethodParam? param = null;
                    paramMap.TryGetValue(key, out param);
                    param?.SetMethod.Invoke(retObj, new object[] { ConvertUtil.ParseByType(param.ParamType, entry.Value) });

                }
            }
            else
            {
                Dictionary<string, MethodParam> sourceMethodMap = AnnotationUtils.ReflectObject(input.GetType());
                foreach (var entry in sourceMethodMap)
                {
                    if (entry.Value != null)
                    {
                        paramMap.TryGetValue(entry.Key, out MethodParam? param);
                        param?.SetMethod.Invoke(retObj, new object[] { ConvertUtil.ParseByType(param.ParamType, entry.Value.GetMethod.Invoke(input, null)) });
                    }
                }
            }
            return retObj;

        }


    }
}
