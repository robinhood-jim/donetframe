using System.Data;
using System.Data.Common;

namespace Frameset.Tracing.Plugins.Datasource;

public class ProxyDbConnection : DbConnection
{
    private readonly DbConnection proxyConnection;
    private DbTransaction transaction;

    public ProxyDbConnection(DbConnection proxyConnection)
    {
        this.proxyConnection = proxyConnection;
    }
    
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
    {
        transaction=proxyConnection.BeginTransaction(isolationLevel);
        return transaction;
    }

    public override void ChangeDatabase(string databaseName)
    {
        proxyConnection.ChangeDatabase(databaseName);
    }

    public override void Close()
    {
        proxyConnection.Close();
    }

    public override void Open()
    {
        proxyConnection.Open();
    }

    public override string ConnectionString
    {
        get => proxyConnection.ConnectionString;
        set => proxyConnection.ConnectionString = value;
    }

    public override string Database => proxyConnection.Database;

    public override ConnectionState State => proxyConnection.State;
    public override string DataSource => proxyConnection.Database;
    public override string ServerVersion => proxyConnection.ServerVersion;

    protected override DbCommand CreateDbCommand()
    {
        return proxyConnection.CreateCommand();
    }

    public DbConnection GetTargetConnection()
    {
        return proxyConnection;
    }
}