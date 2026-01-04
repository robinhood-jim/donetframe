using System;
using System.Collections.Generic;


namespace Frameset.Core.Model
{
    public class BaseEntity
    {
        private IList<string> dirtyProperties = new List<string>();
        private List<BaseEntity> _subEntitys = [];
       
        public void AddDirtys(string[] dirtyColumns)
        {
            if (dirtyColumns != null && dirtyColumns.Length > 0)
            {
                foreach (string column in dirtyColumns)
                {
                    if (!dirtyProperties.Contains(column))
                    {
                        dirtyProperties.Add(column);
                    }
                }
            }
        }

        public IList<string> GetDirties()
        {
            return dirtyProperties;
        }
        public List<BaseEntity> GetSubEntities()
        {
            return _subEntitys;
        }
        public void SetSubEntities(List<BaseEntity> subEntities)
        {
            _subEntitys = subEntities;
        }
    }
}
