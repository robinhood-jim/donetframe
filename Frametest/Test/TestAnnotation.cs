using Frameset.Core.Dao;
using Frameset.Core.Mapper;
using Frameset.Core.Repo;
using Frametest.Dao;
using Frametest.Models;
using Frametest.Office;
using Microsoft.Extensions.Configuration;
using Serilog;


namespace Frametest.Test
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
            //TestJdbc();
            //EntityContent content = EntityReflectUtils.GetEntityInfo(typeof(TestSimple));
            //Console.WriteLine(content);
            //ConstructDynamic();
            //TestWrite();
            //TestWriteModel();
            //TestQuery();
            //TestRead();
            //TestReadModel();
            //TestJdbc.TestQueryCondition();
            TestJdbc.TestQueryByFields();
        }
        private void TestJdbcOper()
        {
            DAOFactory.DoInit("f:/1.yaml");
            SqlMapperConfigure.DoInit("mapper");
            Type type = typeof(BaseRepository<,>);
            Type constructType = type.MakeGenericType([typeof(SysUser), typeof(long)]);
            BaseRepository<SysUser, long> repo = (BaseRepository<SysUser, long>)Activator.CreateInstance(constructType);
            SysUser user = repo.GetById(1);
            Dictionary<string, object> dict = [];
            dict.TryAdd("userId", 1);
            List<Dictionary<string, object>> list = (List<Dictionary<string, object>>)repo.QueryMapper("Frameset.Test", "getPermission", dict);
            List<Dictionary<string, object>> groupList = list.GroupBy(f => f["id"]).Select(group => { Dictionary<string, object> dict = []; dict.TryAdd("Key", group.Key); dict.TryAdd("Values", group.ToList()); return dict; }).ToList();
            List<string> permissions = [];
            foreach (Dictionary<string, object> group in groupList)
            {
                long id = Convert.ToInt64(group["Key"].ToString());
                List<Dictionary<string, object>> values = (List<Dictionary<string, object>>)group["Values"];
                var selected = values.OrderByDescending(f => f["id"] + "|" + f["assignType"]).First();
                selected.TryGetValue("assignType", out object assignType);
                if (!string.Equals(assignType, "2"))
                {
                    selected.TryGetValue("code", out object code);
                    permissions.Add(code.ToString());
                }
            }
            Console.WriteLine(permissions);
        }
        private void TestOfficeOper()
        {
            TestOffice.TestWordRead();
            //TestOffice.TestReadExcel();
        }

    }
}
