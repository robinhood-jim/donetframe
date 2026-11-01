using Frameset.Core.Context;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Handler;
using Frameset.Web.Model;
using Frameset.Web.Utils;
using System.Reflection;

namespace Frameset.Web.Dao.Handler
{
    public class ColumnAutoFillHandler : MetaObjectHandler, IMetaObjectHandler
    {
        private ISecurityUtils securityUtils;
        public ColumnAutoFillHandler()
        {
            securityUtils = RegServiceContext.GetBean<ISecurityUtils>();
        }
        public override void InsertFill(MetaObject metaObject)
        {
            if (metaObject.HasSetter(nameof(AbstractModel.Creator)))
            {
                string? userId = securityUtils.GetLoginUser();
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    SetValue(nameof(AbstractModel.Creator), userId, metaObject);
                }
            }
            if (metaObject.HasProperty(nameof(AbstractModel.CreateTm), out PropertyInfo? propertyInfo))
            {
                propertyInfo?.SetValue(metaObject.GetOriginObj(), DateTime.Now);
            }
        }

        public override void UpdateFill(MetaObject metaObject)
        {
            if (metaObject.HasSetter(nameof(AbstractModel.Modifier)))
            {
                string? userId = securityUtils.GetLoginUser();
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    SetValue(nameof(AbstractModel.Modifier), userId, metaObject);
                }
            }
            if (metaObject.HasProperty(nameof(AbstractModel.ModifyTm), out PropertyInfo? propertyInfo))
            {
                propertyInfo?.SetValue(metaObject.GetOriginObj(), DateTime.Now);
            }
        }
    }
}
