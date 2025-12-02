using Frameset.Core.Common;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Reflect;
using Frameset.Core.Repo;
using Microsoft.AspNetCore.Mvc;
using Spring.Globalization.Formatters;
using System.Diagnostics;

namespace Frameset.Web.Controller
{
    public class AbstractController<R, V, P> : AbstractControllerBase where R : IBaseRepository<V, P> where V : BaseEntity
    {
        protected readonly R repository;
        private DateTimeFormatter timeFormat = new DateTimeFormatter("yyyy-MM-dd HH:mm:ss");
        public AbstractController(R baseRepository)
        {
            repository = baseRepository;
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
            return new JsonResult(vo);
        }


        internal V GetValueFrom(object input)
        {
            V retObj = System.Activator.CreateInstance<V>();
            Dictionary<string, MethodParam> paramMap = AnnotationUtils.ReflectObject(typeof(V));
            if (input.GetType().Equals(typeof(Dictionary<string, object>)))
            {
                foreach (var entry in (Dictionary<string, object>)input)
                {
                    MethodParam? param = null;
                    paramMap.TryGetValue(entry.Key, out param);
                    param?.SetMethod.Invoke(retObj, new object[] { ConvertUtil.ParseByType(param.GetMethod.ReturnType, entry.Value) });

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
                        param?.SetMethod.Invoke(retObj, new object[] { ConvertUtil.ParseByType(param.GetMethod.ReturnType, entry.Value.GetMethod.Invoke(input, null)) });
                    }
                }
            }
            return retObj;

        }


    }
}
