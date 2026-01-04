using Frameset.Core.Common;
using Frameset.Core.Exceptions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Frameset.Core.Dao.Utils
{
    public class JoinConditionBuilder
    {
        private readonly Dictionary<Type, Dictionary<string, FieldContent>> fieldsMap = [];
        private readonly Dictionary<Type, EntityContent> entityMap = [];
        private readonly Dictionary<Type, string> entityAliasMap = [];
        private readonly Dictionary<string, Type> entityNameMap = [];
        private readonly Dictionary<string, int> newColumns = [];
        private List<FilterCondition> filterConditions = [];


        private JoinConditionBuilder(Dictionary<Type, string> entityAliasMap)
        {
            this.entityAliasMap = entityAliasMap;
            if (!entityAliasMap.IsNullOrEmpty())
            {
                foreach (var entry in entityAliasMap)
                {
                    entityNameMap.TryAdd(entry.Key.Name, entry.Key);
                    EntityContent entityContent = EntityReflectUtils.GetEntityInfo(entry.Key);
                    entityMap.TryAdd(entry.Key, entityContent);
                    Dictionary<string, FieldContent> fields = EntityReflectUtils.GetFieldsMap(entry.Key);
                    fieldsMap.TryAdd(entry.Key, fields);
                }
            }
        }
        public static JoinConditionBuilder NewBuilder(Dictionary<Type, string> entityAliasMap)
        {
            return new JoinConditionBuilder(entityAliasMap);
        }
        public JoinConditionBuilder AddEq(string column, object value)
        {
            string columnName = null;
            Tuple<Type, EntityContent, FieldContent>? tuple = ParseColumn(column);
            if (tuple != null)
            {
                Trace.Assert(tuple.Item2 != null, "field " + column + " not found in entity");
                columnName = tuple.Item3.FieldName;
            }
            else if (newColumns.TryGetValue(column, out _))
            {
                columnName = column;
            }
            filterConditions.Add(new FilterCondition()
            {
                ColumnName = columnName,
                TargetEntity = tuple?.Item2,
                ColumnType = tuple?.Item3.ParamType,
                Values = [value]
            });
            return this;
        }
        public JoinConditionBuilder AddFilter(string column, Constants.SqlOperator sqlOperator, object[] objects)
        {
            string columnName = null;
            Tuple<Type, EntityContent, FieldContent>? tuple = ParseColumn(column);
            if (tuple != null)
            {
                Trace.Assert(tuple.Item2 != null, "field " + column + " not found in entity");
                columnName = tuple.Item3.FieldName;
            }
            else if (newColumns.TryGetValue(column, out _))
            {
                columnName = column;
            }
            filterConditions.Add(new FilterCondition()
            {
                ColumnName = columnName,
                Operator = sqlOperator,
                TargetEntity = tuple?.Item2,
                ColumnType = tuple?.Item3.ParamType,
                Values = objects.ToList()
            });
            return this;
        }
        public FilterCondition Eq(string column, object value)
        {
            string columnName = null;
            Tuple<Type, EntityContent, FieldContent>? tuple = ParseColumn(column);
            if (tuple != null)
            {
                Trace.Assert(tuple.Item2 != null, "field " + column + " not found in entity");
                columnName = tuple.Item3.FieldName;
            }
            else if (newColumns.TryGetValue(column, out _))
            {
                columnName = column;
            }
            return new FilterCondition()
            {
                ColumnName = columnName,
                TargetEntity = tuple?.Item2,
                ColumnType = tuple?.Item3.ParamType,
                Values = [value]
            };
        }
        public FilterCondition Filter(string column, Constants.SqlOperator sqlOperator, object[] values)
        {
            string columnName = null;
            Tuple<Type, EntityContent, FieldContent>? tuple = ParseColumn(column);
            if (tuple != null)
            {
                Trace.Assert(tuple.Item2 != null, "field " + column + " not found in entity");
                columnName = tuple.Item3.FieldName;
            }
            else if (newColumns.TryGetValue(column, out _))
            {
                columnName = column;
            }
            return new FilterCondition()
            {
                ColumnName = columnName,
                Operator = sqlOperator,
                TargetEntity = tuple?.Item2,
                ColumnType = tuple?.Item3.ParamType,
                Values = values.ToList()
            };
        }
        public FilterCondition Or(List<FilterCondition> conditions)
        {
            return new FilterCondition()
            {
                LinkOper = Constants.LINK_OR,
                Conditions = conditions
            };
        }
        public JoinConditionBuilder Add(FilterCondition condition)
        {
            filterConditions.Add(condition);
            return this;
        }
        public FilterCondition And(List<FilterCondition> conditions)
        {
            return new FilterCondition()
            {
                LinkOper = Constants.LINK_AND,
                Conditions = conditions
            };
        }
        public FilterCondition Build()
        {
            if (filterConditions.Count == 1)
            {
                return filterConditions[0];
            }
            else
            {
                return new()
                {
                    Conditions = filterConditions
                };
            }
        }

        private Tuple<Type, EntityContent, FieldContent>? ParseColumn(string column)
        {
            if (column.Contains('.'))
            {
                string[] selParts = column.Split('.');
                if (entityNameMap.TryGetValue(selParts[0], out Type targetType) && fieldsMap.TryGetValue(targetType, out Dictionary<string, FieldContent> fieldMap))
                {
                    if (fieldMap.TryGetValue(selParts[1], out FieldContent content) && entityMap.TryGetValue(targetType, out EntityContent entityContent))
                    {
                        return Tuple.Create(targetType, entityContent, content);
                    }
                    else
                    {
                        throw new BaseSqlException("column " + column + " not defined!");
                    }
                }
                else
                {
                    throw new BaseSqlException("column " + column + " not defined!");
                }
            }
            else if (newColumns.TryGetValue(column, out _))
            {
                return null;
            }
            else
            {
                throw new BaseSqlException("column " + column + " not defined in new Columns!");
            }
        }
    }
}
