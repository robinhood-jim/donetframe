using FastMember;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Query;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;


namespace Frameset.Core.Dao.Meta
{
    public class OracleDialect : AbstractSqlDialect
    {
        internal OracleDialect()
        {

        }
        public override string AppendAutoIncrement()
        {
            throw new NotSupportedException("oracle not support increment");
        }
        public override string getVarcharFormat(FieldContent content)
        {
            return "VARCHAR2(" + content.Length + ")";
        }
        public override string GenerateSequenceScript(string sequenceName)
        {
            return sequenceName + ".nexval";
        }
        public override string AppendSequence(string sequenceName)
        {
            return ";selece " + sequenceName + ".CURRVAL from dual";
        }
        public override long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token, int batchSize = 10000)
        {
            IList<FieldContent> fields = EntityReflectUtils.GetFieldsContent(typeof(V));
            EntityContent entityContent = EntityReflectUtils.GetEntityInfo(typeof(V));

            using (OracleBulkCopy copy = new OracleBulkCopy((OracleConnection)connection))
            {
                copy.DestinationSchemaName = entityContent.Schema;
                copy.DestinationTableName = entityContent.TableName;
                List<string> columnNames = new();
                foreach (FieldContent content in fields)
                {
                    if (!content.IfIncrement)
                    {
                        columnNames.Add(content.PropertyName);
                    }

                }
                var reader = ObjectReader.Create(models, columnNames.ToArray());
                copy.BatchSize = batchSize;
                copy.WriteToServer(reader);
                return 0;
            }

        }
        public override DbConnection GetDbConnection(string connectStr)
        {
            return new OracleConnection(connectStr);
        }
        public override DbCommand GetDbCommand(DbConnection connection, string sql)
        {
            return new OracleCommand(sql, (OracleConnection)connection);
        }
        public override DbParameter WrapParameter(int pos, object value)
        {
            return new OracleParameter("@" + Convert.ToString(pos), value);
        }
        public override DbParameter WrapParameter(string column, object value)
        {
            return new OracleParameter(column, value);
        }
        public override string GeneratePageSql(string baseSql, PageQuery query)
        {
            if (query != null && query.PageSize > 0)
            {
                bool forUpdate = baseSql.ToLower().EndsWith("for update");
                Tuple<long, long> startEnds = GetStartEndRecord(query);
                StringBuilder pagingSelect = new StringBuilder();
                string rawsql = baseSql.Trim();
                bool hasOffset = startEnds.Item1 > 0;
                if (forUpdate)
                {
                    rawsql = rawsql.Substring(0, rawsql.Length - 11);
                }
                if (hasOffset)
                {
                    pagingSelect.Append("select * from ( select row_.*, rownum rownum_ from ( ");
                }
                else
                {
                    pagingSelect.Append("select * from ( ");
                }
                pagingSelect.Append(rawsql);
                long tonums = startEnds.Item2;
                if (hasOffset)
                {
                    pagingSelect.Append(" ) row_ ) where rownum_ <= ").Append(tonums).Append(" and rownum_ > ").Append(startEnds.Item1);
                }
                else
                {
                    pagingSelect.Append(" ) where rownum <= ").Append(query.PageSize);
                }
                if (forUpdate)
                {
                    pagingSelect.Append(" for update");
                }
                return pagingSelect.ToString();
            }
            else
            {
                return GetNoPageSql(baseSql, query);
            }
        }
        public override void AppendAdditionalScript(StringBuilder builder, Dictionary<string, object> paramMap)
        {
            if (!paramMap.IsNullOrEmpty())
            {

                object tableSapce;
                paramMap.TryGetValue("tablespace", out tableSapce);
                if (tableSapce != null && !tableSapce.ToString().IsNullOrEmpty())
                {
                    builder.Append(" TABLESPACE ").Append(tableSapce.ToString());
                }
                object storageMap;
                paramMap.TryGetValue("storageConfig", out storageMap);
                if (storageMap != null)
                {
                    builder.Append(" storage (\n");
                    Dictionary<string, string> storageCfgMap = storageMap as Dictionary<string, string>;
                    foreach (var entry in storageCfgMap)
                    {
                        builder.Append(entry.Key).Append(" ").Append(entry.Value).Append("\n");
                    }
                    builder.Append(")");
                }
            }
        }
    }

}
