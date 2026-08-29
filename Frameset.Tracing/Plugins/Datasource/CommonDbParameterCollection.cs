using System.Collections;
using System.Data.Common;

namespace Frameset.Tracing.Plugins.Datasource;

public class CommonDbParameterCollection : DbParameterCollection
{
    private List<DbParameter> parameters = [];
    private DbCommand dbCommand;
    private DbParameterCollection proxyParameterCollection;

    public CommonDbParameterCollection(DbCommand dbCommand)
    {
        this.dbCommand = dbCommand;
        proxyParameterCollection=dbCommand.Parameters;
    }
    
    public override int Add(object value)
    {
        parameters.Add((DbParameter)value);
        proxyParameterCollection.Add((DbParameter)value);
        return parameters.Count-1;
    }

    public override void Clear()
    {
        parameters.Clear();
        proxyParameterCollection.Clear();
    }

    public override bool Contains(object value)
    {
        return parameters.Contains((DbParameter)value);
    }

    public override int IndexOf(object value)
    {
        return parameters.IndexOf((DbParameter)value);
    }

    public override void Insert(int index, object value)
    {
        parameters.Insert(index,(DbParameter)value);
        proxyParameterCollection.Insert(index,(DbParameter)value);
    }

    public override void Remove(object value)
    {
        parameters.Remove((DbParameter)value);
        proxyParameterCollection.Remove((DbParameter)value);
    }

    public override void RemoveAt(int index)
    {
        parameters.RemoveAt(index);
        proxyParameterCollection.RemoveAt(index);
    }

    public override void RemoveAt(string parameterName)
    {
        int index = IndexOf(parameterName);
        if (index > 0)
        {
            parameters.RemoveAt(index);
            proxyParameterCollection.RemoveAt(index);
        }
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        parameters[index] = value;
        proxyParameterCollection[index] = value;
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        int index = IndexOf(parameterName);
        if (index < 0) throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");
        parameters[index] = value;
        proxyParameterCollection[index] = value;
    }

    public override int Count => parameters.Count;
    public override object SyncRoot => ((ICollection)parameters).SyncRoot;

    public override int IndexOf(string parameterName)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].ParameterName.Equals(parameterName)) return i;
        }

        return -1;
    }

    public override bool Contains(string value)
    {
        return IndexOf(value) > 0;
    }

    public override void CopyTo(Array array, int index)=> ((ICollection)parameters).CopyTo(array, index);

    public override IEnumerator GetEnumerator()
    {
        return parameters.GetEnumerator();
    }

    protected override DbParameter GetParameter(int index)
    {
        if (index < parameters.Count)
        {
            return parameters[index];
        }
        else
        {
            throw new ArgumentOutOfRangeException("parameter length is short than index");
        }
    }

    protected override DbParameter GetParameter(string parameterName)
    {
        int index = IndexOf(parameterName);
        if (index < 0) throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");
        return parameters[index];
    }

    public override void AddRange(Array values)
    {
        foreach (object value in values)
        {
            Add(value);
        }
    }

    public Dictionary<string, object> GetParametersByName()
    {
        Dictionary<string, object> parameterDict = [];
        for (int i = 0; i < parameters.Count; i++)
        {
            string paramName = string.IsNullOrEmpty(parameters[i].ParameterName) ? $"{i}" : parameters[i].ParameterName;
            string paramValue = parameters[i].Value == DBNull.Value || parameters[i].Value == null ? "NULL" : parameters[i].Value.ToString() ?? "";
            parameterDict.TryAdd(paramName, paramValue);
        }

        return parameterDict;
    }
    public Dictionary<int, object> GetParametersByPos()
    {
        Dictionary<int, object> parameterDict = [];
        for (int i = 0; i < parameters.Count; i++)
        {
            parameterDict.TryAdd(i, parameters[i].Value);
        }
        return parameterDict;
    }
}