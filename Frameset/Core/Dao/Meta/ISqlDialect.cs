using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using Frameset.Core.Common;
using Frameset.Core.Dao.Utils;
using Frameset.Core.FileSystem;
using Frameset.Core.Query;

namespace Frameset.Core.Dao.Meta;

public interface ISqlDialect
{
    DbConnection GetDbConnection(string connectStr);
    DbCommand GetDbCommand(DbConnection connection, string sql);

    DbCommand GetDbCommand(DbConnection connection);
    DbParameter WrapParameter(int pos, object value);
    DbParameter WrapParameter(string column, object value);
    bool SupportAutoIncrement();
    bool SupportSequence();
    long QueryIdentityByTable(IJdbcDao dao, DbConnection connection, string schema, string tableName);
    long QuerySequenceValue(IJdbcDao dao, DbConnection connection, string sequenceName);
    string GetDecimalScript(int scale, int precise);
    string GetDecimalScript(FieldContent content);
    string GenerateSequenceFunc(string sequenceName);
    string GetSqlCurrentSequenceValue(string sequenceName);
    string GetSqlNextSequenceValue(string sequenceName);
    string GenerateFieldDefine(FieldContent content);
    string GenerateCountSql(string inputSql);
    string GeneratePageSql(string baseSql, PageQuery query);
    string getVarcharFormat(FieldContent content);
    string GetCharFormat(int length);
    string GetTimestampFormat(FieldContent content);

    long BatchInsert<V>(IJdbcDao dao, DbConnection connection, IEnumerable<V> models, CancellationToken token,int batchSize = 10000);

    long BatchInsert(IJdbcDao dao, DbConnection connection, string schema, string tableName,
        List<DataSetColumnMeta> metas, IEnumerable<Dictionary<string, object>> models, CancellationToken token,
        int batchSize = 10000);

    string GetIntegerFormat(FieldContent content);
    string GetShortFormat(FieldContent content);
    string GetLongFormat(FieldContent content);
    string GetFloatFormat(FieldContent content);
    string GetDoubleFormat(FieldContent content);
    string GetDateFormat(FieldContent content);
    string GetBlobFormat(FieldContent content);
    string GetNumericFormat(FieldContent content);
    string GetClobFormat(FieldContent content);
    string AppendKeyHolder();
    string AppendSequence(string sequenceName);
    string AppendAutoIncrement();
    string GetFieldDefineScript(FieldContent content);
    void AppendAdditionalScript(StringBuilder buidler, Dictionary<string, object> paramMap);
    void ExecuteNoQuery(DbConnection connection, string sql);
    Constants.DbType GetDbType();
}