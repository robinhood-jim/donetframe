using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Diagnostics;

namespace Frameset.Core.Dao.Utils
{
    public class EntityMappingUtils
    {
        public static void parseInsert(AdoDao dao, BaseEntity entity)
        {
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(entity.GetType());
            Trace.Assert(entityContent != null, "");
            List<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entity.GetType());
            if (!fields.IsNullOrEmpty())
            {

            }

        }
    }
}
