
using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Model;


namespace Frametest.Dao
{
    [MappingEntity(TableName = "testtablob", Schema = "test")]
    public class TestModel : BaseEntity
    {
        [MappingField(IfPrimary = true, IfIncrement = true)]
        public long id
        {
            get; set;
        }
        public string name
        {
            get; set;
        } = string.Empty;
        [MappingField(Field = "create_tm")]
        public DateTimeOffset createTime
        {
            get; set;
        }
        [MappingField]
        public float dataVal
        {
            get; set;
        }
        [MappingField(Exist = false)]
        public int status;
        [MappingField(DataType = Constants.MetaType.CLOB)]
        public string lob1
        {
            get; set;
        } = null!;
        [MappingField(DataType = Constants.MetaType.BLOB)]
        public byte[] lob2
        {
            get; set;
        } = null!;

    }
}
