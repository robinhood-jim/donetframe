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
    public class AbstractCrudController<V, P> : AbstractControllerBase where V : BaseEntity where P : notnull
    {
        protected IDbContext dbContext;
        protected Type modelType;
        protected bool isDefaultColumnModel;
        protected Dictionary<string, FieldContent> fieldMap = [];
        protected EntityContent entityContent;
        public AbstractCrudController()
        {
            modelType = typeof(V);
            entityContent = EntityReflectUtils.GetEntityInfo(modelType);
            fieldMap = EntityReflectUtils.GetFieldsMap(modelType);
            isDefaultColumnModel = modelType.IsSubclassOf(typeof(AbstractModel));
            dbContext = DbContextFactory.GetContext(entityContent.DsName);
        }
        protected JsonResult SaveEntity(object input)
        {
            Trace.Assert(input != null);
            bool saveOk;
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
            bool saveOk;
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
                    if (paramMap.TryGetValue(entry.Key, out MethodParam? param))
                    {
                        param?.Setter(retObj,  ConvertUtil.ParseByType(param.ParamType, entry.Value));
                        retObj.AddDirtys(entry.Key);
                    }
                }
            }
            else
            {
                Dictionary<string, MethodParam> sourceMethodMap = AnnotationUtils.ReflectObject(input.GetType());
                foreach (var entry in sourceMethodMap)
                {
                    if (paramMap.TryGetValue(entry.Key, out MethodParam? param))
                    {
                        object? obj = entry.Value.Getter(input);
                        if (obj != null)
                        {
                            param.Setter(retObj, ConvertUtil.ParseByType(param.ParamType, obj));
                            retObj.AddDirtys(entry.Key);
                        }
                    }
                }
            }
            return retObj;
        }
        internal void WrapEntityModelUpdate(V entityModel)
        {
            if (isDefaultColumnModel)
            {
                fieldMap.TryGetValue(nameof(AbstractModel.CreateTm), out FieldContent? createTmContent);
                if (Request.HttpContext.User.Identity?.IsAuthenticated ?? false)
                {
                    if (fieldMap.TryGetValue(nameof(AbstractModel.Modifier), out FieldContent? fieldContent))
                    {
                        var user = Request.HttpContext.User;
                        var userId = user.Claims.First(x => x.Type.Equals("UserId")).Value;
                        fieldContent.Setter(entityModel, Convert.ToInt64(userId));
                        entityModel.AddDirtys(nameof(AbstractModel.Modifier));
                    }
                }
                if (fieldMap.TryGetValue(nameof(AbstractModel.ModifyTm), out FieldContent? timeContent))
                {
                    timeContent.Setter(entityModel, DateTime.UtcNow);
                }
                entityModel.AddDirtys(nameof(AbstractModel.ModifyTm));
            }
        }
        internal void WrapEntityModelInsert(V entityModel)
        {
            if (isDefaultColumnModel)
            {
                if (Request.HttpContext.User.Identity?.IsAuthenticated ?? false)
                {
                    if (fieldMap.TryGetValue(nameof(AbstractModel.Creator), out FieldContent? fieldContent))
                    {
                        var user = Request.HttpContext.User;
                        var userId = user.Claims.First(x => x.Type.Equals("UserId")).Value;
                        fieldContent.Setter(entityModel, Convert.ToInt64(userId));
                    }
                }

                if (fieldMap.TryGetValue(nameof(AbstractModel.Status), out FieldContent? statusContent))
                {
                    var statusVal = statusContent.Getter(entityModel);
                    if (statusVal == null || string.IsNullOrWhiteSpace(statusVal.ToString()))
                    {
                        statusContent.Setter(entityModel, Constants.VALID);
                    }
                }

                if (fieldMap.TryGetValue(nameof(AbstractModel.CreateTm), out FieldContent? createTmContent))
                {
                    createTmContent.Setter(entityModel, DateTime.UtcNow);
                }
            }
        }
    }
}
