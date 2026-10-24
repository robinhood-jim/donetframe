namespace Frameset.Core.Dao.Utils
{
    public class TableLogicColumn
    {
        public string FieldName
        {
            get; internal set;
        } = null!;
        public int ValidValue
        {
            get; internal set;
        }
        public int InvalidValue
        {
            get; internal set;
        }
    }
}
