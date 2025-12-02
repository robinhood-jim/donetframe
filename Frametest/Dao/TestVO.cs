using Frameset.Core.Annotation;
using Frameset.Core.Model;

namespace Frametest.Dao
{
    [MappingEntity(TableName = "t_test", Schema = "test")]
    public class TestVO : BaseEntity
    {
        [MappingField(IfPrimary = true, IfIncrement = true)]
        public long Id
        {
            get; set;
        }
        public string Name
        {
            get; set;
        } = string.Empty;
        public int CsId
        {
            get; set;
        }
        public DateTime CreateTime
        {
            get; set;
        }
        public string Description
        {
            get; set;
        } = string.Empty;

    }
}
