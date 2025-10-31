using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Mapper;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Repo;
using Frameset.Core.Utils;
using Frameset.Office.Excel;
using Frameset.Office.Excel.Meta;
using Frameset.Office.Excel.Util;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Data.Common;


namespace Frametest.Dao
{
    [TestClass]
    public class TestAnnotation
    {
        public TestAnnotation()
        {

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .CreateLogger();
        }

        [TestMethod]
        public void DoAnnotation()
        {
            //TestExcelWrite();
            TestExcelModelWrite();
            //TestReadExcel();
            DAOFactory f = DAOFactory.DoInit("f:/1.yaml");
            SqlMapperConfigure.DoInit("mapper");
            IJdbcDao dao = f.getJdbcDao("core");
            //TestExcelReaderWrite(dao);
            //TestMeta(dao);
            //BaseRepository<TestModel, long> repository = new Builder<TestModel, long>().Build();
            

        }
        private void TestExcelModelWrite()
        {
           
            Random random = new Random(1231313);
            using(SingleWorkBook workBook=new SingleWorkBook(File.Create("d:/testModel.xlsx"), true, typeof(TestExcelMode)))
            {
                long startTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600 * 24 * 1000;
                workBook.BeginWrite();
                for (int j = 0; j < 1200; j++)
                {
                    TestExcelMode m = new TestExcelMode();
                    m.Name = StringUtils.GenerateRandomChar(random, 12);
                    m.Time = DateTimeOffset.FromUnixTimeMilliseconds(startTs + j*1000).LocalDateTime;
                    m.GroupId = random.Next(100)+1;
                    m.IndValue1 = random.NextDouble() * 1000;
                    m.IndValue2 = random.NextDouble() * 500;
                    workBook.WriteEntity(m);
                }
            }
        }
        private void TestExcelReaderWrite(IJdbcDao dao)
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
        private bool TestExcelWrite()
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
                    cachedMap.TryAdd("intcol", Convert.ToString(random.Next(1000)+1));
                    cachedMap.TryAdd("dval", Convert.ToString(random.NextDouble() * 1000));
                    cachedMap.TryAdd("dval2", Convert.ToString(random.NextDouble() * 500));
                    workBook.WriteRow(cachedMap);
                }
            }
            return true;
        }
        public void TestReadExcel()
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
        private bool TestSave(BaseRepository<TestModel, long> repository)
        {
            TestModel model = new TestModel();
            model.name = "test212";
            model.createTime = DateTimeOffset.Now;
            model.dataVal = 3;
            return repository.SavenEntity(model);
        }
        private bool TestGet(BaseRepository<TestModel, long> repository, long id)
        {
            TestModel tmodel = repository.GetById(1);
            FileStream fs = new FileStream("f:/test.png", FileMode.OpenOrCreate);
            BinaryWriter writer = new BinaryWriter(fs);
            writer.Write(tmodel.lob2);
            writer.Close();
            fs.Close();
            return true;
        }
        private long TestExcuteMapper(BaseRepository<TestModel, long> repository)
        {
            TestVO vo = new TestVO();
            vo.Name = "test99900";
            vo.Description = "dettt";
            vo.CsId = 50;
            repository.ExecuteMapper(AppConfigurtaionServices.Configuration["DefaultNameSpace"], "insert1", vo);
            return vo.Id;
        }
        private int TestBatch()
        {
            BaseRepository<TestSimple, long> repository = new Builder<TestSimple, long>().Build();
            IList<TestSimple> list = new List<TestSimple>();
            for (int i = 0; i < 1000; i++)
            {
                TestSimple simple = new TestSimple();
                simple.name = "col" + Convert.ToString(i);
                simple.tValue = i;
                simple.dTime = DateTime.Now;
                list.Add(simple);
            }
            return repository.InsertBatch(list);
        }
        private IList<TestModel> TestQuery(BaseRepository<TestModel, long> repository)
        {

            repository.QueryModelsByField(typeof(TestVO).GetProperty("Name"), Constants.SqlOperator.LIKE, new object[] { "t" });
            return repository.QueryModelsByField("name", Constants.SqlOperator.LIKE, new object[] { "t" });
        }
        private void TestQueryPage(BaseRepository<TestModel, long> repository)
        {
            PageQuery query = new PageQuery(4);
            query.Parameters.Add("Name", "t%");
            query.Parameters.Add("CsId", 0);
            query.Parameters.Add("Description", null);
            query.NameSpace = AppConfigurtaionServices.Configuration["DefaultNameSpace"];
            query.QueryId = "select1";
            PageDTO<TestVO> list = repository.QueryPage<TestVO>(query);
            Console.WriteLine(list.Count);
        }
        public void TestMeta(IJdbcDao dao)
        {

            using (DbConnection connection = dao.GetDialect().GetDbConnection(dao.GetConnectString()))
            {
                connection.Open();
                IList<TableMeta> tables = DataMetaUtils.GetTables(connection, "test");
                Console.WriteLine(tables);
                IList<ColumnMeta> columns = DataMetaUtils.GetTableColumns(connection, null, "t_simple");
                Console.WriteLine(columns);
            }
        }
    }
}
