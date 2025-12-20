using Frameset.Office.Annotation;

namespace Frametest.Office
{
    [ExcelSheet("测试")]
    public class TestExcelMode
    {
        [ExcelColumnName("名称")]
        public string Name
        {
            get; set;
        } = string.Empty;
        [ExcelColumn(ColumnName = "日期", Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime Time
        {
            get; set;
        }
        [ExcelColumnName("分类")]
        public int GroupId
        {
            get; set;
        }
        [ExcelColumnName("值1")]
        public double IndValue1
        {
            get; set;
        }
        [ExcelColumnName("值2")]
        public double IndValue2
        {
            get; set;
        }
        [ExcelColumn(ColumnName = "差异", Formula = "(D{P}-E{P})/C{P}")]
        public double Diff
        {
            get; set;
        }
    }
}
