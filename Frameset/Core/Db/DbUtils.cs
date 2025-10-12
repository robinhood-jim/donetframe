using Frameset.Core.Common;
using IBM.Data.Db2;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Spring.Util;
using System;
using System.Data;
using System.Data.Common;

namespace Frameset.Core.Db
{
    internal class DbUtils
    {
        public static DbConnection GetConnection(string connectStr, String dbType)
        {
            DbConnection connection = null;
            Constants.DbType dbTypes = Constants.dbTypeOf(dbType);

            switch (dbTypes)
            {
                case Constants.DbType.Mysql:
                    connection = new MySqlConnection(connectStr);
                    break;
                case Constants.DbType.Oracle:
                    connection = new OracleConnection(connectStr);
                    break;
                case Constants.DbType.Postgres:
                    connection = new NpgsqlConnection(connectStr);
                    break;
                case Constants.DbType.SqlServer:
                    connection = new SqlConnection(connectStr);
                    break;
                case Constants.DbType.DB2:
                    connection = new DB2Connection(connectStr);
                    break;
            }
            return connection;

        }
        public static DbCommand GetDbCommand(DbConnection connection, String dbType, String sql)
        {
            DbCommand command = null;
            Constants.DbType dbTypes = Constants.dbTypeOf(dbType);
            switch (dbTypes)
            {
                case Constants.DbType.Mysql:
                    AssertUtils.IsTrue(connection.GetType().Equals(typeof(MySqlConnection)));
                    command = new MySqlCommand(sql, (MySqlConnection)connection);
                    break;
                case Constants.DbType.Oracle:
                    AssertUtils.IsTrue(connection.GetType().Equals(typeof(OracleConnection)));
                    command = new OracleCommand(sql, (OracleConnection)connection);
                    break;
                case Constants.DbType.Postgres:
                    AssertUtils.IsTrue(connection.GetType().Equals(typeof(NpgsqlConnection)));
                    command = new NpgsqlCommand(sql, (NpgsqlConnection)connection);
                    break;
                case Constants.DbType.SqlServer:
                    AssertUtils.IsTrue(connection.GetType().Equals(typeof(SqlConnection)));
                    command = new SqlCommand(sql, (SqlConnection)connection);
                    break;
                case Constants.DbType.DB2:
                    AssertUtils.IsTrue(connection.GetType().Equals(typeof(DB2Connection)));
                    command = new DB2Command(sql, (DB2Connection)connection);
                    break;

            }
            return command;
        }
        public static DbParameter WrapParameter(string dbType, int pos, object value)
        {
            DbParameter parameter = null;
            Constants.DbType dbTypes = Constants.dbTypeOf(dbType);
            switch (dbTypes)
            {
                case Constants.DbType.Mysql:
                    parameter = new MySqlParameter("@" + Convert.ToString(pos), value);
                    break;
                case Constants.DbType.Oracle:
                    parameter = new OracleParameter("@" + Convert.ToString(pos), value);
                    break;
                case Constants.DbType.Postgres:
                    parameter = new NpgsqlParameter("@" + Convert.ToString(pos), value);
                    break;
                case Constants.DbType.SqlServer:
                    parameter = new SqlParameter("@" + Convert.ToString(pos), value);
                    break;
                case Constants.DbType.DB2:
                    parameter = new DB2Parameter("@" + Convert.ToString(pos), value);
                    break;
            }
            return parameter;

        }
        public static IDataAdapter WrapAdapater(string dbType)
        {
            IDataAdapter dataAdapter = null;
            Constants.DbType dbTypes = Constants.dbTypeOf(dbType);
            switch (dbTypes)
            {
                case Constants.DbType.Mysql:
                    dataAdapter = new MySqlDataAdapter();
                    break;
                case Constants.DbType.Oracle:
                    dataAdapter = new OracleDataAdapter();
                    break;
                case Constants.DbType.Postgres:
                    dataAdapter = new NpgsqlDataAdapter();
                    break;
                case Constants.DbType.SqlServer:
                    dataAdapter = new SqlDataAdapter();
                    break;
            }
            return dataAdapter;
        }
        public static DbParameter WrapParameter(string dbType, string column, object value)
        {
            DbParameter parameter = null;
            Constants.DbType dbTypes = Constants.dbTypeOf(dbType);
            switch (dbTypes)
            {
                case Constants.DbType.Mysql:
                    parameter = new MySqlParameter(column, value);
                    break;
                case Constants.DbType.Oracle:
                    parameter = new OracleParameter(column, value);
                    break;
                case Constants.DbType.Postgres:
                    parameter = new NpgsqlParameter(column, value);
                    break;
                case Constants.DbType.SqlServer:
                    parameter = new SqlParameter(column, value);
                    break;
                case Constants.DbType.DB2:
                    parameter = new DB2Parameter(column, value);
                    break;
            }
            return parameter;
        }



    }
}
