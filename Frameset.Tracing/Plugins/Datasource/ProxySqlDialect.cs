using System.Data.Common;
using Frameset.Core.Common;
using Frameset.Core.Dao.Meta;
using Frameset.Core.Query;

namespace Frameset.Tracing.Plugins.Datasource;

public class ProxySqlDialect : AbstractSqlDialect,ISqlDialect
{
    private AbstractSqlDialect proxyDialect;

    public ProxySqlDialect(Constants.DbType dbType)
    {
        AbstractSqlDialect proxyDialect = DbDialectFactory.GetInstance(dbType);
        this.proxyDialect = proxyDialect;
    }

    public static ProxySqlDialect Init(string dbTypeStr)
    {
        ProxySqlDialect proxySqlDialect = new(dbTypeStr);
        return proxySqlDialect;
    }

    public ProxySqlDialect(string dbTypeStr)
    {
        Constants.DbType dbType = Constants.DbTypeOf(dbTypeStr);
        AbstractSqlDialect proxyDialect = DbDialectFactory.GetInstance(dbType);
        this.proxyDialect = proxyDialect;
    }

    public ProxySqlDialect(AbstractSqlDialect proxyDialect)
    {
        this.proxyDialect = proxyDialect;
    }


    public override string GeneratePageSql(string baseSql, PageQuery query)
    {
        return proxyDialect.GeneratePageSql(baseSql, query);
    }

    public override DbConnection GetDbConnection(string connectStr)
    {
        DbConnection connection = proxyDialect.GetDbConnection(connectStr);
        return new ProxyDbConnection(connection);
    }

    public override DbCommand GetDbCommand(DbConnection connection, string sql)
    {
        DbCommand command = proxyDialect.GetDbCommand(((ProxyDbConnection)connection).GetTargetConnection(),sql);
        return new ProxyDbCommand(connection, command);
    }
    public override DbCommand GetDbCommand(DbConnection connection, string sql,DbTransaction transaction)
    {
        DbCommand command = proxyDialect.GetDbCommand(((ProxyDbConnection)connection).GetTargetConnection(),sql,transaction);
        return new ProxyDbCommand(connection, command);
    }

    public override DbCommand GetDbCommand(DbConnection connection)
    {
        DbCommand command = proxyDialect.GetDbCommand(((ProxyDbConnection)connection).GetTargetConnection());
        return new ProxyDbCommand(connection, command);
    }

    public override DbParameter WrapParameter(int pos, object value)
    {
        DbParameter parameter= proxyDialect.WrapParameter(pos, value);
        return parameter;
    }

    public override DbParameter WrapParameter(string column, object value)
    {
        return proxyDialect.WrapParameter(column, value);
    }

    public override Constants.DbType GetDbType()
    {
        return proxyDialect.GetDbType();
    }

    public override DbDataAdapter GetDataAdapter()
    {
        throw new NotImplementedException();
    }
}