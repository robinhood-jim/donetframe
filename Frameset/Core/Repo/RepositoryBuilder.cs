using Frameset.Core.Model;
using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Frameset.Core.Repo
{
    public class RepositoryBuilder<V, P> where V : BaseEntity
    {
        private readonly BaseRepository<V, P> repository;
        public RepositoryBuilder()
        {
            repository = new BaseRepository<V, P>();
        }
        public RepositoryBuilder<V, P> SaveFunction(Func<V, bool> insertFunction)
        {
            repository.saveFunc = insertFunction;
            return this;
        }
        public RepositoryBuilder<V, P> UpdateFunction(Func<V, bool> updateFunc)
        {
            repository.updateFunc = updateFunc;
            return this;
        }
        public RepositoryBuilder<V, P> SaveBeforeAction(Action<V> action)
        {
            repository.insertBeforeAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> UpdateBeforeAction(Action<V> action)
        {
            repository.updateBeforeAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> DeleteBeforeAction(Action<V> action)
        {
            repository.deleteBeforeAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> DeleteAfterAction(Action<DbCommand, V> action)
        {
            repository.deleteAfterAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> SaveAfterAction(Func<DbCommand, V, bool> action)
        {
            repository.insertAfterAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> UpdateAfterAction(Func<DbCommand, V, bool> action)
        {
            repository.updateAfterAction = action;
            return this;
        }
        public RepositoryBuilder<V, P> TransctionManager(Func<DbConnection, DbTransaction> func)
        {
            repository.transcationFunc = func;
            return this;
        }
        public RepositoryBuilder<V, P> DeleteFunc(Func<IList<P>, int> func)
        {
            repository.deleteFunc = func;
            return this;
        }
        public BaseRepository<V, P> Build()
        {
            return repository;
        }
    }
}
