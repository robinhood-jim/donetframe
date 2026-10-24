using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Frameset.Core.Context
{
    public class CrudUtils
    {
        internal static void WrapSubEntity<V>(AbstractDbContext dbContext, Type entityType, List<V> retList, IList<FieldContent> fields) where V : BaseEntity
        {
            FieldContent pkColumn = EntityReflectUtils.GetPrimaryKey(entityType);
            Dictionary<string, FieldContent> fieldMap = EntityReflectUtils.GetFieldsMap(entityType);
            IEnumerable<FieldContent> manyToOnes = fields.Where(x => x.IsManyToOne);
            if (!manyToOnes.IsNullOrEmpty())
            {
                foreach (V entity in retList)
                {
                    List<FieldContent> contentList = manyToOnes.ToList();
                    foreach (FieldContent content in contentList)
                    {
                        Type parentType = content.ParentEntity;
                        string realtionColumn = content.RealtionColumn;

                        if (fieldMap.TryGetValue(realtionColumn, out FieldContent? relationContent))
                        {
                            object? relationId = relationContent.GetMethod.Invoke(entity, null);
                            Trace.Assert(relationId != null);
                            object parentVo = dbContext.GetByIdSimple(parentType, relationId);
                            content.SetMethod.Invoke(entity, [parentVo]);
                        }
                        else
                        {
                            throw new BaseSqlException("refrence column " + realtionColumn + " not exists!");
                        }

                    }
                }
            }
            IEnumerable<FieldContent> oneToManys = fields.Where(x => x.IsOneToMany);
            if (!oneToManys.IsNullOrEmpty())
            {
                foreach (V entity in retList)
                {
                    List<FieldContent> contentList = oneToManys.ToList();
                    foreach (FieldContent content in contentList)
                    {
                        Type subType = content.SubType;
                        object? id = pkColumn.GetMethod.Invoke(entity, null);
                        Trace.Assert(id != null);
                        if (EntityReflectUtils.GetRelationMap().TryGetValue(entityType, out Dictionary<Type, string>? subEntityMaps) && subEntityMaps.TryGetValue(subType, out string? relationColumn))
                        {
                            Dictionary<string, FieldContent> subFieldMap = EntityReflectUtils.GetFieldsMap(subType);
                            if (subFieldMap.TryGetValue(relationColumn, out FieldContent? relationContent))
                            {
                                List<BaseEntity> entities = dbContext.QueryModelsByFieldSimple(subType, relationColumn, Constants.SqlOperator.EQ, new object[] { id }, null);
                                object? obj = ConvertUtil.GetListsByType<BaseEntity>(typeof(List<>), subType, entities);
                                content.SetMethod.Invoke(entity, [obj]);
                            }
                            else
                            {
                                throw new BaseSqlException(" subColumn " + relationColumn + " is not defined in Model!");
                            }

                        }
                        else
                        {
                            throw new BaseSqlException(" type " + subType.Name + " is not defined subEntity as ManyToOne!");
                        }
                    }
                }
            }
        }
    }
}
