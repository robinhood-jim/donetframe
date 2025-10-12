using Frameset.Core.Model;
using Spring.Util;
using System.Collections.Generic;

namespace Frameset.Core.Dao.Utils
{
    public class EntityMappingUtils
    {
        public static void parseInsert(JdbcDao dao, BaseEntity entity)
        {
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(entity.GetType());
            AssertUtils.ArgumentNotNull(entityContent, "");
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(entity.GetType());
            if (!CollectionUtils.IsEmpty(fields))
            {

            }

        }
    }
}
