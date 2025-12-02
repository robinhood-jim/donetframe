using Frameset.Core.Annotation;
using Frameset.Core.Model;
namespace Frametest.Dao
{
    [MappingEntity(TableName = "t_simple", Schema = "test")]
    public class TestSimple : BaseEntity
    {
        [MappingField(IfIncrement = true, IfPrimary = true)]
        public long id
        {
            get;
            set;
        } = -1;
        public string name
        {
            get; set;
        } = string.Empty;
        [MappingField(Field = "t_value")]
        public int tValue
        {
            get; set;
        } = 0;
        public DateTime dTime
        {
            get; set;
        }
    }
}
