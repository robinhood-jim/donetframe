using Frameset.Core.Common;
using Frameset.Core.Dao.Meta;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Exceptions;
using Frameset.Core.Mapper;
using Frameset.Core.Mapper.Segment;
using Frameset.Core.Model;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Reflect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;



namespace Frameset.Core.Dao
{
    public class JdbcDao : IJdbcDao
    {
        private string connectionStr;
        private string dbTypeStr = "Mysql";
        private AbstractSqlDialect dataMeta;
        private Constants.DbType dbType;
        private string schema;


        internal JdbcDao(string connectionStr)
        {
            this.connectionStr = connectionStr;

        }
        internal JdbcDao(string dbTypeStr, string connectionStr)
        {
            this.dbTypeStr = dbTypeStr;
            this.connectionStr = connectionStr;
            this.dbType = Constants.DbTypeOf(dbTypeStr);
            this.dataMeta = DbDialectFactory.GetInstance(dbType);
        }
        internal JdbcDao(string dbTypeStr, string schema, string connectionStr)
        {
            this.dbTypeStr = dbTypeStr;
            this.connectionStr = connectionStr;
            this.dbType = Constants.DbTypeOf(dbTypeStr);
            this.dataMeta = DbDialectFactory.GetInstance(dbType);
            this.schema = schema;
        }
        public int QueryByInt(DbCommand command)
        {
            try
            {
                return (int)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new BaseSqlException("", ex);
            }
        }

        public long QueryByLong(DbCommand command)
        {
            try
            {
                return (long)command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw new BaseSqlException("", ex);
            }
        }



        public IList<V> QueryModelsBySql<V>(Type modelType, DbCommand command, IList<DbParameter> parameters)
        {
            command.Parameters.AddRange(parameters.ToArray());
            IList<V> retList = new List<V>();
            Dictionary<string, FieldContent> map = EntityReflectUtils.GetFieldsMap(modelType);
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    V entity = System.Activator.CreateInstance<V>();
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        string propName = reader.GetName(col);
                        object value = reader[col];
                        FieldContent content = map[propName];
                        if (Convert.IsDBNull(value))
                        {
                            map.TryGetValue(propName.ToLower(), out content);
                        }
                        if (Convert.IsDBNull(value))
                        {
                            map.TryGetValue(propName.ToUpper(), out content);
                        }
                        if (!Convert.IsDBNull(value) && content != null)
                        {
                            content.SetMethold.Invoke(entity, new object[] { ConvertUtil.parseByType(content.GetMethold.ReturnType, value) });
                        }
                    }
                    retList.Add(entity);
                }
            }
            return retList;

        }
        public IList<Dictionary<string, object>> QueryBySql(DbCommand command, object[] objects)
        {
            DbParameter[] parameters = parseParameter(command, objects);
            command.Parameters.AddRange(parameters);
            IList<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
            using (DbDataReader reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    Dictionary<string, object> dict = new Dictionary<string, object>();
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        dict[reader.GetName(col)] = reader[col];
                    }
                    list.Add(dict);
                }

            }
            return list;
        }
        DbParameter[] parseParameter(DbCommand command, object[] obj)
        {
            if (obj != null && obj.Count() > 0)
            {
                DbParameter[] paramters = new DbParameter[obj.Length];
                for (int i = 0; i < obj.Length; i++)
                {
                    paramters[i] = dataMeta.WrapParameter(i, obj[i]);
                }
                return paramters;
            }
            return null;
        }
        void parseParameter(DbCommand command, Dictionary<string, object> paramMap)
        {
            if (!paramMap.IsNullOrEmpty())
            {
                foreach (var item in paramMap)
                {
                    command.Parameters.Add(dataMeta.WrapParameter(item.Key, item.Value));
                }
            }
        }


        public PageDTO<V> QueryPage<V>(DbCommand command, PageQuery query)
        {
            parseParameter(command, query.Parameters);
            string querySql = null;
            string countSql = null;
            ResultMap mappingMap = null;

            if (!query.QueryId.IsNullOrEmpty())
            {
                SqlSelectSegment segment = (SqlSelectSegment)SqlMapperConfigure.GetExecuteSegment(query.NameSpace, query.QueryId);
                querySql = segment.ReturnSqlPart(query.Parameters);
                mappingMap = SqlMapperConfigure.GetResultMap(query.NameSpace, segment.ResultMap);
            }
            else
            {
                querySql = query.QuerySql;
            }
            countSql = dataMeta.GenerateCountSql(querySql);
            command.CommandText = countSql;
            long count = QueryByLong(command);
            PageDTO<V> ret = new PageDTO<V>(count, query.PageSize, query.CurrentPage);
            string pageSql = dataMeta.GeneratePageSql(querySql, query);
            Type retType = typeof(V);
            bool ifRetMap = false;
            Dictionary<string, MethodParam> methodMap = null;

            if (retType.Equals(typeof(Dictionary<string, object>)))
            {
                ifRetMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }
            command.CommandText = pageSql;
            using (DbDataReader reader = command.ExecuteReader())
            {

                while (reader.Read())
                {
                    V entity = System.Activator.CreateInstance<V>();
                    Dictionary<string, object> dict = null;
                    if (ifRetMap)
                    {
                        dict = new Dictionary<string, object>();
                    }

                    MethodParam param = null;
                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        string name = reader.GetName(col);
                        if (ifRetMap)
                        {
                            dict[reader.GetName(col)] = reader[col];
                        }
                        string mappingColumn = null;

                        if (mappingMap == null || !mappingMap.MappingColumns.TryGetValue(name, out mappingColumn))
                        {
                            if (!query.MappingColumns.TryGetValue(name, out mappingColumn))
                            {
                                mappingColumn = Core.Utils.StringUtils.CamelCaseLowConvert(name);
                            }
                        }

                        if (methodMap.TryGetValue(mappingColumn, out param))
                        {
                            param.SetMethod.Invoke(entity, new object[] { ConvertUtil.parseByType(param.ParamType, reader[col]) });
                        }
                    }
                    if (ifRetMap)
                    {
                        dynamic tmp = Convert.ChangeType(dict, retType);
                        ret.Results.Add(tmp);
                    }
                    else
                    {
                        ret.Results.Add(entity);
                    }
                }
            }
            return ret;

        }


        public bool SaveEntity(DbCommand command, BaseEntity entity, InsertSegment segment)
        {
            command.Parameters.AddRange(segment.Parameters.ToArray());
            if (segment.Increment || segment.Sequence)
            {
                object genId = command.ExecuteScalar();
                if (genId != null)
                {
                    segment.GenKeyMethod.Invoke(entity, new object[] { ConvertUtil.parseByType(segment.GenKeyMethod.GetParameters()[0].ParameterType, genId) });
                }
            }
            else
            {
                command.ExecuteNonQuery();
            }
            return true;

        }
        public bool UpdateEntity(DbCommand command, BaseEntity entity, UpdateSegment segment)
        {
            command.Parameters.AddRange(segment.Parameters.ToArray());
            return command.ExecuteNonQuery() == 1;
        }
        public int Execute(DbCommand command, string sql, DbParameter[] parameters)
        {
            command.Parameters.AddRange(parameters);
            return command.ExecuteNonQuery();
        }
        public object QueryMapper(SqlSelectSegment sqlsegment, Dictionary<string, object> paramMap, string nameSpace, DbCommand command, object queryObject)
        {
            string rsMap = sqlsegment.ResultMap;
            ResultMap map = SqlMapperConfigure.GetResultMap(nameSpace, rsMap);
            Type retType = map != null ? map.ModelType : Type.GetType(rsMap);
            bool retMap = false;
            Dictionary<string, MethodParam> methodMap = null;
            if (retType == null && string.Equals(rsMap, "Map", StringComparison.OrdinalIgnoreCase))
            {
                retMap = true;
            }
            else
            {
                methodMap = AnnotationUtils.ReflectObject(retType);
            }


            if (!paramMap.IsNullOrEmpty())
            {
                foreach (var item in paramMap)
                {
                    command.Parameters.Add(dataMeta.WrapParameter(item.Key, item.Value));
                }
            }
            var listType = typeof(List<>).MakeGenericType(retType);

            dynamic retList = Activator.CreateInstance(listType);

            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    Dictionary<string, object> dict = null;
                    object ret = null;
                    MethodParam param = null;

                    if (retMap)
                    {
                        dict = new Dictionary<string, object>();
                    }
                    else
                    {
                        ret = Activator.CreateInstance(retType);
                    }

                    for (int col = 0; col < reader.FieldCount; col++)
                    {
                        string name = reader.GetName(col);
                        if (retMap)
                        {
                            dict[reader.GetName(col)] = reader[col];
                        }
                        if (methodMap.TryGetValue(map.MappingColumns[name], out param))
                        {
                            param.SetMethod.Invoke(ret, new object[] { ConvertUtil.parseByType(param.ParamType, reader[col]) });
                        }
                    }
                    if (retMap)
                    {
                        retList.Add(dict);
                    }
                    else
                    {
                        dynamic tmp = Convert.ChangeType(ret, retType);
                        retList.Add(tmp);
                    }
                }

            }
            return retList;
        }
        public int ExecuteMapper(CompositeSegment segment, string nameSpace, string executeId, DbCommand command, Dictionary<string, object> paramMap)
        {

            return command.ExecuteNonQuery();

        }
        public void DoWithQuery(string sql, object[] obj,Action<IDataReader> action)
        {
            using(DbConnection connection = dataMeta.GetDbConnection(connectionStr))
            {
                connection.Open();
                using (DbCommand command = dataMeta.GetDbCommand(connection, sql))
                {
                    DbParameter[] parameters = parseParameter(command, obj);
                    if (parameters != null && parameters.Count()>0)
                    {
                        command.Parameters.AddRange(parameters);
                    }
                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        action.Invoke(reader);
                    }
                }
            }
            
        }



        public string GetConnectString()
        {
            return connectionStr;
        }

        public string GetDbTypeStr()
        {
            return dbTypeStr;
        }
        public AbstractSqlDialect GetDialect()
        {
            return dataMeta;
        }
        public string GetCurrentSchema()
        {
            return schema;
        }
    }
}