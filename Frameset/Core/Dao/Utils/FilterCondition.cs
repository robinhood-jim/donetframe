using Frameset.Core.Common;
using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Frameset.Core.Dao.Utils
{
    public class FilterCondition
    {
        public Constants.SqlOperator Operator
        {
            get; set;
        } = Constants.SqlOperator.EQ;
        public string ColumnName
        {
            get; set;
        }
        public List<object> Values
        {
            get; set;
        }
        public Type ColumnType
        {
            get; set;
        }
        public List<FilterCondition> Conditions
        {
            get; set;
        } = [];
        public string LinkOper
        {
            get; set;
        } = Constants.LINK_AND;
        public string LeftArithmetic
        {
            get; set;
        }
        public string RightArithmetic
        {
            get; set;
        }
        public bool LeftArith
        {
            get; set;
        } = false;

        public bool AllArith
        {
            get; set;
        } = false;
        public EntityContent TargetEntity
        {
            get; set;
        }
        public string GroupBy
        {
            get; set;
        }
        public string Having
        {
            get; set;
        }
        public string OrderBy
        {
            get; set;
        }
        public string SelectParts
        {
            get; set;
        }
        public List<string> NewColumns
        {
            get; set;
        }
        public Dictionary<string, string> PropFieldMap
        {
            get; set;
        }



        public string GeneratePreparedSql(Dictionary<string, object> valueMap, Dictionary<string, int> duplicatedMap, Dictionary<Type, string> entityAliasMap)
        {
            StringBuilder builder = new StringBuilder(0);

            if (!Conditions.IsNullOrEmpty())
            {
                for (int i = 0; i < Conditions.Count; i++)
                {
                    FilterCondition condition = Conditions[i];
                    if (string.Equals(Constants.LINK_OR, condition.LinkOper, StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Append("(");
                    }
                    builder.Append(condition.GeneratePreparedSql(valueMap, duplicatedMap, entityAliasMap));
                    if (string.Equals(Constants.LINK_OR, condition.LinkOper, StringComparison.OrdinalIgnoreCase))
                    {
                        builder.Append(")");
                    }
                    if (i < Conditions.Count - 1)
                    {
                        builder.Append(" ").Append(LinkOper).Append(" ");
                    }
                }
                return builder.ToString();
            }
            else
            {

                SqlUtils.AppendPreparedSql(this, valueMap, builder, duplicatedMap, entityAliasMap);

                if (!GroupBy.IsNullOrEmpty())
                {
                    builder.Append(" GROUP BY ").Append(GroupBy);
                }
                if (!Having.IsNullOrEmpty())
                {
                    builder.Append(" HAVING ").Append(Having);
                }
                if (!OrderBy.IsNullOrEmpty())
                {
                    builder.Append(" ORDER BY ").Append(OrderBy);
                }

                return builder.ToString();
            }

        }

    }
    public class SingleFilterConditionBuilder
    {
        private List<FilterCondition> filterConditions = [];
        private readonly Type modelType;
        private readonly Dictionary<string, FieldContent> fieldMap;
        private EntityContent targetEntity;
        private string _selectParts;
        private string _groupBy;
        private string _having;
        private string _orderBy;
        private SingleFilterConditionBuilder(Type modelType)
        {
            Trace.Assert(modelType.IsSubclassOf(typeof(BaseEntity)), "must use BaseEntity");
            this.modelType = modelType;
            targetEntity = EntityReflectUtils.GetEntityInfo(modelType);
            fieldMap = EntityReflectUtils.GetFieldsMap(modelType);
        }
        public static SingleFilterConditionBuilder NewBuilder<T>()
        {
            return new SingleFilterConditionBuilder(typeof(T));
        }
        public SingleFilterConditionBuilder AddEq(string columnName, object value)
        {
            fieldMap.TryGetValue(columnName, out FieldContent content);
            Trace.Assert(content != null, "field " + columnName + " not found in entity");
            filterConditions.Add(new FilterCondition()
            {
                ColumnName = content.FieldName,
                TargetEntity = targetEntity,
                ColumnType = content.ParamType,
                Values = [value]
            });
            return this;
        }
        public SingleFilterConditionBuilder AddFilter(string columnName, Constants.SqlOperator sqlOperator, object[] objects)
        {
            fieldMap.TryGetValue(columnName, out FieldContent content);
            Trace.Assert(content != null, "field " + columnName + " not found in entity");
            filterConditions.Add(new FilterCondition()
            {
                ColumnName = content.FieldName,
                TargetEntity = targetEntity,
                ColumnType = content.ParamType,
                Operator = sqlOperator,
                Values = [objects]
            });
            return this;
        }
        public SingleFilterConditionBuilder LeftArithmetic(string arithColumn, Type valueType, Constants.SqlOperator sqlOperator, object[] objects)
        {
            filterConditions.Add(new FilterCondition()
            {
                ColumnName = arithColumn,
                TargetEntity = targetEntity,
                LeftArith = true,
                ColumnType = valueType,
                Operator = sqlOperator,
                Values = [objects]
            });
            return this;
        }
        public SingleFilterConditionBuilder AllArithmetic(string arithLeftColumn, string arithRightColumn, Constants.SqlOperator sqlOperator)
        {
            filterConditions.Add(new FilterCondition()
            {
                ColumnName = arithLeftColumn,
                TargetEntity = targetEntity,
                AllArith = true,
                Operator = sqlOperator,
                Values = [arithRightColumn]
            });
            return this;
        }
        public FilterCondition Eq(string columnName, object value)
        {
            fieldMap.TryGetValue(columnName, out FieldContent content);
            Trace.Assert(content != null, "field " + columnName + " not found in entity");
            return new FilterCondition()
            {
                ColumnName = content.FieldName,
                ColumnType = content.ParamType,
                Values = [value]
            };
        }
        public FilterCondition Filter(string columnName, Constants.SqlOperator sqlOperator, object[] objects)
        {
            fieldMap.TryGetValue(columnName, out FieldContent content);
            Trace.Assert(content != null, "field " + columnName + " not found in entity");
            return new FilterCondition()
            {
                ColumnName = content.FieldName,
                Operator = sqlOperator,
                ColumnType = content.ParamType,
                Values = [objects]
            };
        }
        public FilterCondition Combine(Func<SingleFilterConditionBuilder, FilterCondition> action)
        {
            FilterCondition condition = action.Invoke(this);
            return condition;
        }
        public SingleFilterConditionBuilder Or(Action<SingleFilterConditionBuilder> action)
        {
            action.Invoke(this);
            return this;
        }
        public FilterCondition Or(List<FilterCondition> conditions)
        {
            return new FilterCondition()
            {
                LinkOper = Constants.LINK_OR,
                Conditions = conditions
            };
        }
        public FilterCondition Or(Dictionary<string, string> paramDict)
        {
            List<FilterCondition> condition = [];
            foreach (var entry in paramDict)
            {
                fieldMap.TryGetValue(entry.Key, out FieldContent content);
                Trace.Assert(content != null, "field " + entry.Key + " not found in entity");
                string[] parameters = entry.Value.Split('|');
                string[] values = [];
                Constants.SqlOperator sqlOperator = Constants.SqlOperator.EQ;
                if (parameters.Length == 1)
                {
                    values = parameters[0].Split(',');
                }
                else
                {
                    sqlOperator = Constants.Parse(parameters[0]);
                    values = parameters[1].Split(',');
                }
                condition.Add(new FilterCondition()
                {
                    ColumnName = content.FieldName,
                    ColumnType = content.GetMethod.ReturnType,
                    Operator = sqlOperator,
                    Values = values.ToList().ConvertAll(it => (object)it)
                });
            }
            return new()
            {
                Conditions = condition,
                LinkOper = Constants.LINK_OR
            };
        }
        public FilterCondition And(Dictionary<string, string> paramDict)
        {
            List<FilterCondition> condition = [];
            foreach (var entry in paramDict)
            {
                fieldMap.TryGetValue(entry.Key, out FieldContent content);
                Trace.Assert(content != null, "field " + entry.Key + " not found in entity");
                string[] parameters = entry.Value.Split('|');
                string[] values = [];
                Constants.SqlOperator sqlOperator = Constants.SqlOperator.EQ;
                if (parameters.Length == 1)
                {
                    values = parameters[0].Split(',');
                }
                else
                {
                    sqlOperator = Constants.Parse(parameters[0]);
                    values = parameters[1].Split(',');
                }

                condition.Add(new FilterCondition()
                {
                    ColumnName = content.FieldName,
                    ColumnType = content.GetMethod.ReturnType,
                    Operator = sqlOperator,
                    Values = values.ToList().ConvertAll(it => (object)it)
                });
            }
            return new()
            {
                Conditions = condition,
                LinkOper = Constants.LINK_AND
            };
        }
        public SingleFilterConditionBuilder Add(FilterCondition condition)
        {
            filterConditions.Add(condition);
            return this;
        }
        public SingleFilterConditionBuilder And(List<FilterCondition> conditions)
        {
            filterConditions.Add(new FilterCondition()
            {
                LinkOper = Constants.LINK_AND,
                Conditions = conditions
            });
            return this;
        }
        public SingleFilterConditionBuilder SelectParts(string selectParts)
        {
            _selectParts = selectParts;
            return this;
        }
        public SingleFilterConditionBuilder GroupBy(string groupBy)
        {
            _groupBy = groupBy;
            return this;
        }
        public SingleFilterConditionBuilder Having(string having)
        {
            _having = having;
            return this;
        }
        public SingleFilterConditionBuilder OrderBy(string orderBy)
        {
            _orderBy = orderBy;
            return this;
        }
        public FilterCondition Build()
        {
            if (filterConditions.Count == 1)
            {
                FilterCondition condition = filterConditions[0];
                condition.SelectParts = _selectParts;
                condition.GroupBy = _groupBy;
                condition.Having = _having;
                condition.OrderBy = _orderBy;
                return condition;
            }
            else
            {
                return new()
                {

                    Conditions = filterConditions
                };
            }
        }


    }
}
