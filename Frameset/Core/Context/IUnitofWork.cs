using System;
using System.Data.Common;

namespace Frameset.Core.Context
{
    public interface IUnitofWork : IDisposable
    {
        DbTransaction GetOrBeginTransaction();
        void Commit();
        void Rollback();
        DbConnection GetConnection();
    }
}
