using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Model;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace Frameset.Core.Sql
{
    public class SqlBuilder
    {
        private readonly Dictionary<Type, Tuple<EntityContent, Dictionary<string, FieldContent>>> entityTypeMap = [];
        private readonly Dictionary<Type, string> entityAliasMap = [];
        private readonly Dictionary<string, Type> entityNameMap = [];
        private readonly Dictionary<Type, Dictionary<string, string>> columnAliasMap = [];
        private readonly List<Tuple<string, string[]>> newColumns = [];
        private readonly Dictionary<string, int> newColumnMap = [];
        private readonly List<Tuple<Type, Constants.SqlOperator, FieldContent, SqlBuilder>> inSubQuerys = [];
        private readonly List<Tuple<Constants.SqlOperator, SqlBuilder>> subQuerys = [];
        private Type fromBaseType;
        private readonly List<TableJoin> joins = [];
        private readonly List<Tuple<Type, FieldContent>> selectFields = [];
        private readonly StringBuilder whereBuilder = new();
        private readonly StringBuilder joinBuilder = new();
        private readonly StringBuilder groupBuilder = new();
        private readonly StringBuilder orderByBuilder = new();
        private readonly StringBuilder sqlBuilder = new();
        private FilterCondition filterCondition;
        private FilterCondition havingConditon;
        public SqlParameter QueryParamters
        {
            get; protected set;
        }

        private SqlBuilder()
        {

        }
        public static SqlBuilder NewBuilder()
        {
            return new SqlBuilder();
        }

        public SqlBuilder AliasEntity(Type entityType, string aliasName)
        {
            Trace.Assert(entityType.IsSubclassOf(typeof(BaseEntity)), "class must be subClass of BaseEntity");
            if (!entityTypeMap.TryGetValue(entityType, out _))
            {
                EntityContent entityContent = EntityReflectUtils.GetEntityInfo(entityType);

                Dictionary<string, FieldContent> fields = EntityReflectUtils.GetFieldsMap(entityType);
                if (!fields.IsNullOrEmpty())
                {
                    entityTypeMap.TryAdd(entityType, Tuple.Create(entityContent, fields));
                    entityAliasMap.TryAdd(entityType, aliasName);
                    entityNameMap.TryAdd(entityType.Name, entityType);
                }
                else
                {
                    throw new BaseSqlException("get Fields by entity " + entityType.Name + " failed!");
                }

            }
            return this;
        }
        public SqlBuilder AliasEntity(Dictionary<Type, string> aliasMap)
        {
            foreach (var entry in aliasMap)
            {
                AliasEntity(entry.Key, entry.Value);
            }
            return this;
        }
        public SqlBuilder Select(params string[] columns)
        {
            if (!columns.IsNullOrEmpty())
            {
                foreach (string column in columns)
                {
                    if (column.Contains('.'))
                    {
                        string[] selParts = column.Split('.');
                        AliasColumn(selParts, AppendField);
                    }
                }
            }
            return this;
        }
        public SqlBuilder SelectAs(string column, string aliasName)
        {
            SelectAs([column], aliasName);
            return this;
        }
        public SqlBuilder SelectAs(string[] columns, string aliasName)
        {
            if (!columns.IsNullOrEmpty())
            {
                if (columns.Length > 1 || !columns[0].Contains('.'))
                {
                    newColumns.Add(Tuple.Create(aliasName, columns));
                    newColumnMap.TryAdd(aliasName, 1);
                }
                else
                {
                    if (columns[0].Contains('.'))
                    {
                        string[] selParts = columns[0].Split('.');
                        Tuple<Type, FieldContent> tuple = AliasColumn(selParts, AppendField);
                        if (columnAliasMap.TryGetValue(tuple.Item1, out Dictionary<string, string> aliasDict))
                        {
                            aliasDict.TryAdd(tuple.Item2.PropertyName, aliasName);
                        }
                        else
                        {
                            columnAliasMap.TryAdd(tuple.Item1, new() { { tuple.Item2.PropertyName, aliasName } });
                        }
                    }
                }
            }
            return this;
        }
        public SqlBuilder From(Type baseType)
        {
            fromBaseType = baseType;
            return this;
        }

        public SqlBuilder Join(string leftColumn, string rightColumn, Constants.JoinType joinType)
        {
            Tuple<Type, FieldContent> leftEntityTuple = AliasColumn(leftColumn.Split('.'));
            Tuple<Type, FieldContent> rightEntityTuple = AliasColumn(rightColumn.Split('.'));

            if (entityTypeMap.TryGetValue(leftEntityTuple.Item1, out Tuple<EntityContent, Dictionary<string, FieldContent>> tuple)
                && entityTypeMap.TryGetValue(rightEntityTuple.Item1, out Tuple<EntityContent, Dictionary<string, FieldContent>> tuple1))
            {
                joins.Add(new TableJoin(leftEntityTuple.Item1, rightEntityTuple.Item1, leftEntityTuple.Item2, rightEntityTuple.Item2, joinType));
            }
            return this;
        }

        public SqlBuilder InSubQuery(string column, SqlBuilder sqlBuilder)
        {
            Tuple<Type, FieldContent> tuple = AliasColumn(column.Split('.'));
            inSubQuerys.Add(Tuple.Create(tuple.Item1, Constants.SqlOperator.IN, tuple.Item2, sqlBuilder));
            return this;
        }
        public SqlBuilder NotInSubQuery(string column, SqlBuilder sqlBuilder)
        {
            Tuple<Type, FieldContent> tuple = AliasColumn(column.Split('.'));
            inSubQuerys.Add(Tuple.Create(tuple.Item1, Constants.SqlOperator.NOTIN, tuple.Item2, sqlBuilder));
            return this;
        }
        public SqlBuilder Exists(SqlBuilder sqlBuilder)
        {
            subQuerys.Add(Tuple.Create(Constants.SqlOperator.EXISTS, sqlBuilder));
            return this;
        }
        public SqlBuilder NotExists(SqlBuilder sqlBuilder)
        {
            subQuerys.Add(Tuple.Create(Constants.SqlOperator.NOTEXISTS, sqlBuilder));
            return this;
        }
        public SqlBuilder Union(SqlBuilder sqlBuilder)
        {
            subQuerys.Add(Tuple.Create(Constants.SqlOperator.UNION, sqlBuilder));
            return this;
        }
        public SqlBuilder UnionAll(SqlBuilder sqlBuilder)
        {
            subQuerys.Add(Tuple.Create(Constants.SqlOperator.UNIONALL, sqlBuilder));
            return this;
        }
        public SqlBuilder Filter(FilterCondition condition)
        {
            this.filterCondition = condition;
            return this;
        }


        public SqlBuilder SelectCase(string newColumnName, string[] caseColumns, Dictionary<string, object> caseDict)
        {
            StringBuilder builder = new StringBuilder();
            String operationColumn = GetCombinedColumn(caseColumns);
            builder.Append("CASE ").Append(operationColumn);
            caseDict.TryGetValue("_DEFAULT", out object defaultValue);
            caseDict.Remove("_DEFAULT");
            foreach (var entry in caseDict)
            {
                builder.Append(" WHEN ").Append(entry.Key).Append(" THEN ");
                if (entry.Value.GetType().Equals(typeof(List<string>)))
                {
                    builder.Append(GetCombinedColumn((entry.Value as List<string>).ToArray()));
                }
                else
                {
                    builder.Append(entry.Value.ToString());
                }
            }
            if (defaultValue != null)
            {
                builder.Append(" ELSE ").Append(defaultValue);
            }
            builder.Append(" END");
            newColumns.Add(Tuple.Create(newColumnName, new string[] { builder.ToString() }));
            newColumnMap.TryAdd(newColumnName, 1);
            return this;
        }
        public SqlBuilder GroupBy(string[] columns)
        {
            if (!columns.IsNullOrEmpty())
            {
                foreach (string column in columns)
                {
                    if (column.Contains('.'))
                    {
                        Tuple<Type, FieldContent> tuple = AliasColumn(column.Split('.'));
                        WrapField(groupBuilder, tuple);
                        groupBuilder.Append(',');
                    }
                    else
                    {
                        groupBuilder.Append(column).Append(",");
                    }
                }
                if (groupBuilder.Length > 0)
                {
                    groupBuilder.Remove(groupBuilder.Length - 1, 1);
                }
            }
            return this;
        }
        public SqlBuilder Having(FilterCondition havingCondition)
        {
            this.havingConditon = havingCondition;
            return this;
        }
        public SqlBuilder OrderBy(OrderedDictionary orders)
        {
            if (orders != null && orders.Count > 0)
            {
                foreach (DictionaryEntry entry in orders)
                {
                    string column = entry.Key.ToString();
                    bool ascDesc = bool.Parse(entry.Value.ToString());
                    if (column.Contains('.'))
                    {
                        Tuple<Type, FieldContent> tuple = AliasColumn(column.Split('.'));
                        WrapField(orderByBuilder, tuple);

                    }
                    else if (newColumnMap.TryGetValue(column, out _))
                    {
                        orderByBuilder.Append(column);
                    }
                    else
                    {
                        throw new BaseSqlException("order by column " + column + " not found!");
                    }
                    if (!ascDesc)
                    {
                        orderByBuilder.Append(" DESC");
                    }
                    orderByBuilder.Append(',');
                }
                if (orderByBuilder.Length > 0)
                {
                    orderByBuilder.Remove(orderByBuilder.Length - 1, 1);
                }
            }

            return this;
        }
        public string Build()
        {
            if (QueryParamters == null)
            {
                QueryParamters = new SqlParameter();
            }
            sqlBuilder.Append(Constants.SQL_SELECT);
            foreach (Tuple<Type, FieldContent> field in selectFields)
            {
                if (entityTypeMap.TryGetValue(field.Item1, out Tuple<EntityContent, Dictionary<string, FieldContent>> tuple) && entityAliasMap.TryGetValue(field.Item1, out string tabAlias))
                {
                    sqlBuilder.Append(tabAlias).Append('.').Append(field.Item2.FieldName).Append(Constants.SQL_AS);
                    if (columnAliasMap.TryGetValue(field.Item1, out Dictionary<string, string> aliasDict) && aliasDict.TryGetValue(field.Item2.PropertyName, out string aliasName))
                    {
                        sqlBuilder.Append(aliasName);
                    }
                    else
                    {
                        sqlBuilder.Append(field.Item2.PropertyName);
                    }
                    sqlBuilder.Append(',');
                }
            }
            if (!newColumns.IsNullOrEmpty())
            {
                foreach (Tuple<string, string[]> tuple in newColumns)
                {
                    if (tuple.Item2.Length > 1)
                    {
                        sqlBuilder.Append(GetCombinedColumn(tuple.Item2)).Append(Constants.SQL_AS).Append(tuple.Item1);
                    }
                    else
                    {
                        sqlBuilder.Append(tuple.Item2[0]).Append(Constants.SQL_AS).Append(tuple.Item1);
                    }
                    sqlBuilder.Append(',');
                }
            }
            sqlBuilder.Remove(sqlBuilder.Length - 1, 1);
            sqlBuilder.Append(Constants.SQL_FROM);
            if (fromBaseType != null)
            {
                if (entityTypeMap.TryGetValue(fromBaseType, out Tuple<EntityContent, Dictionary<string, FieldContent>> tuple) && entityAliasMap.TryGetValue(fromBaseType, out string tabAlias))
                {
                    sqlBuilder.Append(tuple.Item1.GetTableName()).Append(' ').Append(tabAlias);
                }
            }

            //join
            if (!joins.IsNullOrEmpty())
            {
                foreach (TableJoin join in joins)
                {
                    if (entityTypeMap.TryGetValue(join.LeftClass, out Tuple<EntityContent, Dictionary<string, FieldContent>> tuple) && entityTypeMap.TryGetValue(join.RightClass, out Tuple<EntityContent, Dictionary<string, FieldContent>> tuple1))
                    {
                        entityAliasMap.TryGetValue(join.LeftClass, out string leftAlias);
                        entityAliasMap.TryGetValue(join.RightClass, out string rightAlias);

                        joinBuilder.Append(Constants.GetJoinType(join.TabJoinType)).Append(Constants.SQL_JOIN);
                        joinBuilder.Append(tuple1.Item1.GetTableName()).Append(' ').Append(rightAlias);
                        joinBuilder.Append(Constants.SQL_ON).Append(leftAlias).Append('.').Append(join.LeftColumn.FieldName).Append(Constants.SQL_EQ)
                            .Append(rightAlias).Append('.').Append(join.RightColumn.FieldName).Append(' ');
                    }
                }
                sqlBuilder.Append(joinBuilder);
            }

            //where
            if (filterCondition != null)
            {
                whereBuilder.Append(filterCondition.GeneratePreparedSql(QueryParamters.QueryParams, QueryParamters.DuplicatedMap, entityAliasMap));
            }
            if (!inSubQuerys.IsNullOrEmpty())
            {
                foreach (Tuple<Type, Constants.SqlOperator, FieldContent, SqlBuilder> tuple in inSubQuerys)
                {
                    if (entityTypeMap.TryGetValue(tuple.Item1, out Tuple<EntityContent, Dictionary<string, FieldContent>> entityTuple))
                    {
                        entityAliasMap.TryGetValue(tuple.Item1, out string tabAlias);
                        whereBuilder.Append(Constants.SQL_AND).Append(tabAlias).Append('.').Append(tuple.Item3.FieldName).Append(Constants.OperatorValue(tuple.Item2)).Append("(");
                        tuple.Item4.QueryParamters = QueryParamters;
                        whereBuilder.Append(tuple.Item4.Build());
                        whereBuilder.Append(')');
                    }
                }
            }

            sqlBuilder.Append(Constants.SQL_WHERE).Append(whereBuilder);
            //group by
            if (groupBuilder.Length > 0)
            {
                sqlBuilder.Append(Constants.SQL_GROUPBY).Append(groupBuilder);
            }
            //having
            if (havingConditon != null)
            {
                sqlBuilder.Append(Constants.SQL_HAVING);
                sqlBuilder.Append(havingConditon.GeneratePreparedSql(QueryParamters.QueryParams, QueryParamters.DuplicatedMap, entityAliasMap));
            }
            //order by
            if (orderByBuilder.Length > 0)
            {
                sqlBuilder.Append(orderByBuilder);
            }
            if (!subQuerys.IsNullOrEmpty())
            {
                foreach (Tuple<Constants.SqlOperator, SqlBuilder> tuple in subQuerys)
                {
                    StringBuilder subBuilder = new StringBuilder();
                    subBuilder.Append(Constants.OperatorValue(tuple.Item1)).Append("(");
                    tuple.Item2.QueryParamters = QueryParamters;
                    subBuilder.Append(tuple.Item2.Build());
                    subBuilder.Append(')');
                    sqlBuilder.Append(subBuilder);
                }
            }
            return sqlBuilder.ToString();
        }

        private void AppendField(Type entityType, FieldContent field)
        {
            if (!selectFields.IsNullOrEmpty())
            {
                int count = selectFields.Count(t => t.Item1.Equals(entityType) && t.Item2.Equals(field));
                if (count > 0)
                {
                    throw new BaseSqlException("Field " + field.PropertyName + " already selected!");
                }
            }
            selectFields.Add(Tuple.Create(entityType, field));
        }
        private Tuple<Type, FieldContent> AliasColumn(string[] selParts, Action<Type, FieldContent> action = null)
        {
            if (entityNameMap.TryGetValue(selParts[0], out Type targetType) && entityTypeMap.TryGetValue(targetType, out Tuple<EntityContent, Dictionary<string, FieldContent>> tuple))
            {
                if (tuple.Item2.TryGetValue(selParts[1], out FieldContent content))
                {
                    if (action != null)
                    {
                        action.Invoke(targetType, content);
                    }
                    return Tuple.Create(targetType, content);
                }
                else
                {
                    throw new BaseSqlException("Field " + selParts[1] + " not defined in entity " + selParts[0] + "!");
                }

            }
            else
            {
                throw new BaseSqlException("entity " + selParts[0] + " not defined!");
            }
        }
        private string GetCombinedColumn(string[] columns)
        {
            StringBuilder columnBuilder = new StringBuilder();
            bool hasFunc = false;
            foreach (string column in columns)
            {
                if (column.Contains('.'))
                {
                    string[] selParts = column.Split('.');
                    Tuple<Type, FieldContent> selectColumnTuple = AliasColumn(selParts);
                    if (entityTypeMap.TryGetValue(selectColumnTuple.Item1, out Tuple<EntityContent, Dictionary<string, FieldContent>> tuple))
                    {
                        if (tuple.Item2.TryGetValue(selParts[1], out FieldContent content))
                        {
                            entityAliasMap.TryGetValue(selectColumnTuple.Item1, out string tabAlias);
                            columnBuilder.Append(tabAlias).Append('.').Append(content.FieldName);
                        }
                        else
                        {
                            throw new BaseSqlException("Field " + selParts[1] + " not defined in entity " + selParts[0] + "!");
                        }
                    }
                    else
                    {
                        EntityContent entityContent = EntityReflectUtils.GetEntityInfo(selectColumnTuple.Item1);
                        Dictionary<string, FieldContent> fieldDict = EntityReflectUtils.GetFieldsMap(selectColumnTuple.Item1);
                        fieldDict.TryGetValue(selParts[1], out FieldContent content);
                        Trace.Assert(content != null, "");
                        columnBuilder.Append(content.FieldName);
                    }

                }
                else if (Constants.SQLFUNCTIONS.Contains(column.ToUpper()))
                {
                    hasFunc = true;
                    columnBuilder.Append(column).Append("(");
                }
                else
                {
                    columnBuilder.Append(column);
                }
            }
            if (hasFunc)
            {
                columnBuilder.Append(")");
            }
            return columnBuilder.ToString();
        }


        private void WrapField(StringBuilder builder, Tuple<Type, FieldContent> field)
        {
            if (entityAliasMap.TryGetValue(field.Item1, out string tabAlias))
            {
                builder.Append(tabAlias).Append(".").Append(field.Item2.FieldName);
            }
        }
        public class SqlParameter
        {
            public Dictionary<string, object> QueryParams
            {
                get; protected set;
            } = [];
            public Dictionary<string, int> DuplicatedMap
            {
                get; protected set;
            } = [];
        }
        public class TableJoin
        {
            public Type LeftClass
            {
                get; protected set;
            }
            public Type RightClass
            {
                get; protected set;
            }
            public FieldContent LeftColumn
            {
                get; protected set;
            }
            public FieldContent RightColumn
            {
                get; protected set;
            }
            public Constants.JoinType TabJoinType
            {
                get; protected set;
            }
            public TableJoin(Type leftClass, Type rightClass, FieldContent leftColumn, FieldContent rightColumn, Constants.JoinType joinType)
            {
                this.LeftClass = leftClass;
                this.RightClass = rightClass;
                this.LeftColumn = leftColumn;
                this.RightColumn = rightColumn;
                this.TabJoinType = joinType;
            }
        }
    }
}
