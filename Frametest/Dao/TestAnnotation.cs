using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Mapper;
using Frameset.Core.Query;
using Frameset.Core.Query.Dto;
using Frameset.Core.Repo;
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
            DAOFactory f = DAOFactory.DoInit("f:/1.yaml");
            SqlMapperConfigure.DoInit("mapper");
            IJdbcDao dao = f.getJdbcDao("core");
            TestMeta(dao);
            BaseRepository<TestModel, long> repository = new Builder<TestModel, long>().Build();
            //TestQueryPage(repository);
            //TestVO vo = new TestVO();
            //vo.Name = "t%";
            //IList<TestVO> list=(List<TestVO>)repository.QueryMapper("Frameset.Test", "select1",vo);

            //TestSave(repository);
            //TestGet(repository,1);
            //TestBatch();
            //IList<TestModel> list = TestQuery(repository);
            //Console.WriteLine(list.Count);
            //Console.WriteLine(TestExcuteMapper(repository));

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
        private long TestExcuteMapper(BaseRepository<TestModel,long> repository)
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
