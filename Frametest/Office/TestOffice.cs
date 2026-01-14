using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Utils;
using Frameset.Office.Excel;
using Frameset.Office.Excel.Meta;
using Frameset.Office.Excel.Util;
using Frameset.Office.Word;
using Frameset.Office.Word.Element;
using Frameset.Office.Word.Util;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Frametest.Office
{
    public static class TestOffice
    {
        public static void TestExcelModelWrite()
        {

            Random random = new Random(1231313);
            using (SingleWorkBook workBook = new SingleWorkBook(File.Create("d:/testModel.xlsx"), true, typeof(TestExcelMode)))
            {
                long startTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600 * 24 * 1000;
                workBook.BeginWrite();
                for (int j = 0; j < 1200; j++)
                {
                    TestExcelMode m = new TestExcelMode();
                    m.Name = StringUtils.GenerateRandomChar(random, 12);
                    m.Time = DateTimeOffset.FromUnixTimeMilliseconds(startTs + j * 1000).LocalDateTime;
                    m.GroupId = random.Next(100) + 1;
                    m.IndValue1 = random.NextDouble() * 1000;
                    m.IndValue2 = random.NextDouble() * 500;
                    workBook.WriteEntity(m);
                }
            }
        }

        public static void TestExcelReaderWrite(IJdbcDao dao)
        {
            dao.DoWithQuery("select * from t_batch_test", null, (reader) =>
            {
                using (SingleWorkBook workBook = new SingleWorkBook(new FileStream("d:/testReader.xlsx", FileMode.Create), true, reader))
                {
                    workBook.BeginWrite();
                    workBook.WriteRecords(reader);
                }
            });
        }
        public static bool TestExcelWrite()
        {
            SheetPropBuilder builder = SheetPropBuilder.NewBuilder();
            builder.AddCellProp("名称", "name", Constants.MetaType.STRING, false)
                   .AddCellProp("时间", "time", Constants.MetaType.TIMESTAMP, false)
                   .AddCellProp("整形", "intcol", Constants.MetaType.INTEGER, false)
                   .AddCellProp("数值1", "dval", Constants.MetaType.DOUBLE, false)
                   .AddCellProp("数值2", "dval2", Constants.MetaType.DOUBLE, false)
                   .AddCellProp(new ExcelCellProp("diff", "diff", Constants.MetaType.FORMULA, "(D{P}-E{P})/C{P}"));
            ExcelSheetProp prop = builder.Build();
            Random random = new Random(1231313);
            Dictionary<string, object> cachedMap = new Dictionary<string, object>();
            using (SingleWorkBook workBook = new SingleWorkBook(File.Create("d:/testnet1.xlsx"), true, prop))
            {
                workBook.MaxRows = 500000;
                long startTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600 * 24 * 1000;
                workBook.BeginWrite();
                for (int j = 0; j < 1200000; j++)
                {
                    cachedMap.Clear();
                    cachedMap.TryAdd("name", StringUtils.GenerateRandomChar(random, 12));
                    cachedMap.TryAdd("time", Convert.ToString(startTs + j * 1000));
                    cachedMap.TryAdd("intcol", Convert.ToString(random.Next(1000) + 1));
                    cachedMap.TryAdd("dval", Convert.ToString(random.NextDouble() * 1000));
                    cachedMap.TryAdd("dval2", Convert.ToString(random.NextDouble() * 500));
                    workBook.WriteRow(cachedMap);
                }
            }
            Console.ReadLine();
            return true;
        }
        public static void TestReadExcel()
        {
            SheetPropBuilder builder = SheetPropBuilder.NewBuilder();
            builder.AddCellProp("name", "name", Constants.MetaType.STRING, false)
                   .AddCellProp("time", "time", Constants.MetaType.TIMESTAMP, false)
                   .AddCellProp("intcol", "intcol", Constants.MetaType.INTEGER, false)
                   .AddCellProp("dval", "dval", Constants.MetaType.DOUBLE, false)
                   .AddCellProp("dval2", "dval2", Constants.MetaType.DOUBLE, false)
                   .AddCellProp(new ExcelCellProp("diff", "diff", Constants.MetaType.FORMULA, "(D{P}-E{P})/C{P}"));
            ExcelSheetProp prop = builder.Build();
            using (SingleWorkBook workBook = new SingleWorkBook(File.Open("d:/testnet1.xlsx", FileMode.Open), false, prop))
            {
                for (int i = 0; i < workBook.GetSheetNum(); i++)
                {
                    int count = 0;
                    Log.Information("process read sheet " + i);
                    using (MapEnumerator enumerator = workBook.GetMapEnumerator(workBook.GetSheet(i), prop))
                    {
                        while (enumerator.MoveNext())
                        {
                            Dictionary<string, object> valueMap = enumerator.Current;
                            //Console.WriteLine(valueMap);
                            count++;
                        }
                        Log.Information("end read sheet TotalCount:" + count);
                    }
                }

            }
        }
        public static void TestWordRead()
        {
            using (Stream fileStream = File.OpenRead("f:\\test123.docx"))
            {
                using (Document document = new Document(fileStream))
                {
                    BodyElementEnumerator enumerator = document.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        IBodyElement element = enumerator.Current;
                        if (element is ParagraphElement)
                        {
                            Console.WriteLine("---Paragraph begin");
                            foreach (XRunElement run in (element as ParagraphElement).Elements)
                            {
                                Console.WriteLine("---XRun begin");
                                Console.WriteLine(run.Content);
                                if (!run.PictureDatas.IsNullOrEmpty())
                                {
                                    run.PictureDatas.ForEach(f => Console.WriteLine("pic " + f.Rid + " path " + f.Path));
                                }
                                Console.WriteLine("---XRun end!");
                            }
                            Console.WriteLine("---Paragraph end!");
                        }
                        else if (element is TableElement)
                        {
                            TableElement element1 = element as TableElement;
                            Console.WriteLine("---Table begin");
                            Log.Information("{Headers}", element1.Headers);
                            Console.WriteLine("---Table end!");
                            //Console.WriteLine(element1.Values);
                        }
                    }

                }
            }
        }
    }
}
