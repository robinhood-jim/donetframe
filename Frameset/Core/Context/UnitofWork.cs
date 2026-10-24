using Frameset.Core.Dao;
using Frameset.Core.Exceptions;
using Frameset.Core.Utils;
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;

namespace Frameset.Core.Context
{
    public class UnitofWork : IUnitofWork
    {
        private IAdoDao? dao;
        private DbTransaction? transaction;
        private DbConnection? connection;
        int _disposeCounter;
        public UnitofWork(IAdoDao dao)
        {
            this.dao = dao;
        }
        public void Commit()
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Commit();
                }
            }catch(Exception ex)
            {
                LogUtils.Error($"encounter error {ex.Message}");
                throw new BaseSqlException(ex.Message);
            }
            finally
            {
                DoClear();
            }
        }

        public DbTransaction GetOrBeginTransaction()
        {
            Trace.Assert(dao != null);
            if (transaction != null)
            {
                return transaction;
            }
            try
            {
                connection = dao.GetDialect().GetDbConnection(dao.GetConnectString());
                transaction = connection.BeginTransaction();
                return transaction;
            }
            catch (Exception ex)
            {
                throw new BaseSqlException(ex.Message);
            }
        }
        public DbConnection GetConnection()
        {
            if (connection != null)
            {
                return connection;
            }
            throw new ConfigIncorrectException("connection is null.you must call begintransaction first!");
        }
        public void Rollback()
        {
            try
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }catch(Exception ex)
            {
                LogUtils.Error($"encounter error {ex.Message}");
                throw new BaseSqlException(ex.Message);
            }
            finally
            {
                DoClear();
            }
            
        }
        internal void DoClear()
        {
            dao = null;
            transaction?.Dispose();
            transaction = null;
            if (connection != null)
            {
                if (connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
                connection.Dispose();
                connection = null;
            }

        }
        ~UnitofWork() => this.Dispose();

        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCounter) != 1) return;
            try
            {
                this.Rollback();
            }
            finally
            {
                GC.SuppressFinalize(this);
            }

        }
    }
}
