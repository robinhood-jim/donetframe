using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Frameset.Core.Utils;
using Frameset.Tracing.Tracer;

namespace Frameset.Tracing.Plugins.Datasource;

public class ProxyDbCommand : DbCommand
{
    private readonly DbCommand proxyCommand;
    private readonly DbConnection? dbConnection;
    private string proxyClassName;
    
    public ProxyDbCommand(DbConnection? connection, DbCommand proxyCommand)
    {
        this.proxyCommand = proxyCommand;
        this.dbConnection = connection;
        proxyClassName = this.proxyCommand.GetType().FullName;
    }


    public override void Cancel()
    {
        proxyCommand.Cancel();
    }

    protected override DbParameter CreateDbParameter()
    {
        return proxyCommand.CreateParameter();
    }

    public IDbDataParameter CreateParameter()
    {
        return proxyCommand.CreateParameter();
    }

    public override int ExecuteNonQuery()
    {
        
        Activity? activity=null;
        try
        {
            activity = (Activity)GlobalTracer.StartTrace(proxyClassName + ".ExecuteNonQuery");
            Trace(activity);
            int retValue = proxyCommand.ExecuteNonQuery();
            return retValue;
        }
        catch (Exception ex)
        {
            GlobalTracer.FailedTrace(activity, ex);
            LogUtils.Error(ex.Message);
        }
        finally
        {
            GlobalTracer.EndTrace(activity);
        }
        return -1;
    }

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        Activity? activity=null;
        try
        {
            activity = (Activity)GlobalTracer.StartTrace(proxyClassName + ".ExecuteReader");
            Trace(activity);

            DbDataReader reader = proxyCommand.ExecuteReader(behavior);
            return reader;
        }
        catch (Exception ex)
        {
            GlobalTracer.FailedTrace(activity, ex);
            LogUtils.Error(ex.Message);
        }
        finally
        {
            GlobalTracer.EndTrace(activity);
        }
        return null;
    }


    public IDataReader ExecuteReader()
    {
        return ExecuteDbDataReader(CommandBehavior.Default);
    }

    public IDataReader ExecuteReader(CommandBehavior behavior)
    {
        return ExecuteDbDataReader(behavior);
    }

    public override string CommandText
    {
        get => proxyCommand.CommandText;
        set => proxyCommand.CommandText = value; 
    }
    public override int CommandTimeout
    {
        get => proxyCommand.CommandTimeout;
        set => proxyCommand.CommandTimeout = value;
    }
    public override CommandType CommandType
    {
        get => proxyCommand.CommandType;
        set => proxyCommand.CommandType = value;
    }
    public override UpdateRowSource UpdatedRowSource
    {
        get => proxyCommand.UpdatedRowSource;
        set => proxyCommand.UpdatedRowSource = value;
    }
    protected override DbConnection? DbConnection
    {
        get =>dbConnection;
        set { throw new NotSupportedException("proxy connection can not set!"); }
    }

    protected override DbParameterCollection DbParameterCollection => proxyCommand.Parameters;
    protected override DbTransaction? DbTransaction
    {
        get => proxyCommand.Transaction;
        set => proxyCommand.Transaction = value;
    }
    public override bool DesignTimeVisible
    {
        get => proxyCommand.DesignTimeVisible;
        set => proxyCommand.DesignTimeVisible = value;
    }

    public override object? ExecuteScalar()
    {
        Activity? activity=null;
        try
        {
            activity = (Activity)GlobalTracer.StartTrace(proxyClassName + ".ExecuteScalar");
            Trace(activity);
            object value = proxyCommand.ExecuteScalar();
            return value;
        }
        catch (Exception ex)
        {
            GlobalTracer.FailedTrace(activity, ex);
            LogUtils.Error(ex.Message);
        }
        finally
        {
            GlobalTracer.EndTrace(activity);
        }
        return null;
    }

    public override void Prepare()
    {
        proxyCommand.Prepare();
    }


    public void Dispose()
    {
        proxyCommand.Dispose();
    }

    protected void Trace(Activity? activity)
    {
        activity?.AddTag("sql", proxyCommand.CommandText);
        if (proxyCommand.Parameters.Count > 0)
        {
            activity?.AddTag("params", JsonSerializer.Serialize(GetParametersByName()));
        }
    }
    protected Dictionary<string, object> GetParametersByName()
    {
        DbParameterCollection parameters = proxyCommand.Parameters;
        Dictionary<string, object> parameterDict = [];
        for (int i = 0; i < parameters.Count; i++)
        {
            string paramName = string.IsNullOrEmpty(parameters[i].ParameterName) ? $"{i}" : parameters[i].ParameterName;
            string paramValue = parameters[i].Value == DBNull.Value || parameters[i].Value == null ? "NULL" : parameters[i].Value.ToString() ?? "";
            parameterDict.TryAdd(paramName, paramValue);
        }

        return parameterDict;
    }
}