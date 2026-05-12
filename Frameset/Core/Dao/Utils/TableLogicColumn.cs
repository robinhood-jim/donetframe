namespace Frameset.Core.Dao.Utils
{
    public class TableLogicColumn
    {
        public string FieldName
        {
            get; internal set;
        }
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
