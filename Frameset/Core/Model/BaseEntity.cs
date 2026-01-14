using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;


namespace Frameset.Core.Model
{
    public class BaseEntity
    {
        private IList<string> dirtyProperties = [];
        private List<BaseEntity> _subEntitys = [];

        public void AddDirtys(params string[] dirtyColumns)
        {
            if (!dirtyColumns.IsNullOrEmpty())
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
