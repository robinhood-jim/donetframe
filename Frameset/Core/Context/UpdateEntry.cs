using Frameset.Core.Model;
using System.Collections.Generic;
using System.Linq;

namespace Frameset.Core.Context
{
    public class UpdateEntry
    {
        public List<EffectEntry> EffectEntrys
        {
            get; set;
        } = [];

        public void Insert(BaseEntity entity)
        {
            EffectEntry effect = new EffectEntry(EFFECTTYPE.Insert);
            effect.Entity = entity;
            EffectEntrys.Add(effect);
        }
        public void Update(BaseEntity origin, BaseEntity update)
        {
            EffectEntry effect = new EffectEntry(EFFECTTYPE.Update);
            effect.Entity = update;
            effect.OriginEntity = origin;
            EffectEntrys.Add(effect);
        }
        public void Delete<V, P>(IList<P> pkList) where V : BaseEntity
        {
            EffectEntry effect = new EffectEntry(EFFECTTYPE.Delete);
            effect.PkList = pkList.Cast<object>().ToList();
            EffectEntrys.Add(effect);
        }

        public void DeleteLogic<V, P>(IList<P> pkList, string logicColumn, int status) where V : BaseEntity
        {
            EffectEntry effect = new EffectEntry(EFFECTTYPE.DeleteLogic);
            effect.PkList = pkList.Cast<object>().ToList();
            effect.UpdateStatus = status;
            effect.LogicColumn = logicColumn;
            EffectEntrys.Add(effect);
        }


    }
    public enum EFFECTTYPE
    {
        Insert,
        Update,
        Delete,
        DeleteLogic
    }
    public class EffectEntry
    {
        public EffectEntry(EFFECTTYPE effectType)
        {
            EffectType = effectType;
        }
        public EFFECTTYPE EffectType
        {
            get; set;
        }
        public BaseEntity Entity
        {
            get; set;
        }
        public BaseEntity OriginEntity
        {
            get; set;
        }
        public List<object> PkList
        {
            get; set;
        }
        public string LogicColumn
        {
            get; set;
        }
        public int UpdateStatus
        {
            get; set;
        }

    }
}
