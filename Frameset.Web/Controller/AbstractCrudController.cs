using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Reflect;
using Frameset.Web.Model;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Frameset.Web.Controller
{
    public class AbstractCrudController<V, P> : AbstractControllerBase where V : BaseEntity
    {
        protected string contextName = DbContextFactory.CONTEXTDEFAULTNAME;
        protected IDbContext dbContext;
        protected Type modelType;
        protected bool isDefaultColumnModel;
        protected Dictionary<string, FieldContent> fieldMap = [];
        public AbstractCrudController()
        {
            modelType = typeof(V);
            fieldMap = EntityReflectUtils.GetFieldsMap(modelType);
            isDefaultColumnModel = modelType.IsSubclassOf(typeof(AbstractModel));
            dbContext = DbContextFactory.GetContext(contextName);
        }
        protected JsonResult SaveEntity(object input)
        {
            Trace.Assert(input != null);
            bool saveOk = false;
            if (input.GetType().Equals(typeof(V)))
            {
                V entity = (V)input;
                WrapEntityModelInsert(entity);
                saveOk = dbContext.SaveEntity<V>((V)input);
            }
            else
            {
                V entity = GetValueFrom(input);
                WrapEntityModelInsert(entity);
                saveOk = dbContext.SaveEntity<V>(entity);
            }
            return OutputMsg(saveOk);
        }
        protected JsonResult UpdateEntity(object input)
        {
            Trace.Assert(input != null);
            bool saveOk = false;
            if (input.GetType().Equals(typeof(V)))
            {
                V entity = (V)input;
                WrapEntityModelUpdate(entity);
                saveOk = dbContext.UpdateEntity<V, P>(entity);
            }
            else
            {
                V entity = GetValueFrom(input);
                WrapEntityModelUpdate(entity);
                saveOk = dbContext.UpdateEntity<V, P>(entity);
            }
            return OutputMsg(saveOk);
        }

        protected PageDTO<T> QueryPage<T>(PageQuery query)
        {
            return dbContext.QueryPage<V, T>(query);
        }
        protected JsonResult GetEntity(P id)
        {
            V vo = dbContext.GetById<V, P>(id);
            if (vo != null)
            {
                return OutputMsg(vo);
            }
            else
            {
                return OutputErrMsg("id not found!");
            }
        }
        protected JsonResult RemoveEntity(P id)
        {
            try
            {
                int delete = dbContext.RemoveEntity<V, P>([id]);
                if (delete > 0)
                {
                    return OutputMsg("success");
                }
                else
                {
                    return OutputErrMsg("failed");
                }
            }
            catch (Exception ex)
            {
                return OutputErrMsg(ex.Message);
            }
        }
        

        internal V GetValueFrom(object input)
        {
            V retObj = Activator.CreateInstance<V>();
            Dictionary<string, MethodParam> paramMap = AnnotationUtils.ReflectObject(typeof(V));
            if (input.GetType().Equals(typeof(Dictionary<string, object>)))
            {
                foreach (var entry in (Dictionary<string, object>)input)
                {
                    MethodParam? param = null;
                    paramMap.TryGetValue(entry.Key, out param);
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
        internal void WrapEntityModelUpdate(V entityModel)
        {
            if (isDefaultColumnModel)
            {
                fieldMap.TryGetValue(nameof(AbstractModel.CreateTm), out FieldContent createTmContent);
                if (Request.HttpContext.User.Identity?.IsAuthenticated ?? false)
                {
                    fieldMap.TryGetValue(nameof(AbstractModel.Modifier), out FieldContent fieldContent);
                    var user = Request.HttpContext.User;
                    var userId=user.Claims.First(x => x.Type.Equals("UserId")).Value;
                    fieldContent.SetMethod.Invoke(entityModel,[Convert.ToInt64(userId) ]);
                }
                fieldMap.TryGetValue(nameof(AbstractModel.ModifyTm), out FieldContent timeContent);
                timeContent.SetMethod.Invoke(entityModel, [DateTime.UtcNow]);
            }
        }
        internal void WrapEntityModelInsert(V entityModel)
        {
            if (isDefaultColumnModel)
            {
                if (Request.HttpContext.User.Identity?.IsAuthenticated ?? false)
                {
                    fieldMap.TryGetValue(nameof(AbstractModel.Creator), out FieldContent fieldContent);
                    var user = Request.HttpContext.User;
                    var userId = user.Claims.First(x => x.Type.Equals("UserId")).Value;
                    fieldContent.SetMethod.Invoke(entityModel, [Convert.ToInt64(userId)]);
                }
                fieldMap.TryGetValue(nameof(AbstractModel.Status), out FieldContent statusContent);
                var statusVal = statusContent.GetMethod.Invoke(entityModel, null);
                if (statusVal==null || string.IsNullOrWhiteSpace(statusVal.ToString()))
                {
                    statusContent.SetMethod.Invoke(entityModel, [Constants.VALID]);
                }
                fieldMap.TryGetValue(nameof(AbstractModel.CreateTm), out FieldContent createTmContent);
                createTmContent.SetMethod.Invoke(entityModel, [DateTime.UtcNow]);
            }
        }
    }
}
