using Frameset.Core.Annotation;
using Frameset.Core.Common;
using Frameset.Core.Context;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Frameset.Core.Hardware;
using Frameset.Core.Mapper;
using Frameset.Core.Repo;
using Frameset.Core.Sql;
using Frameset.Core.Utils;
using Frameset.Office.Excel.Util;
using Frametest.Dto;
using Frametest.Models;
using Frametest.Office;
using Frametest.Repo;
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
            Dictionary<string, List<CellFormula>> formualMap = [];

            //String formnul1=CellUtils.ReturnFormulaWithPos(formualMap, "((A{P+2}+B{P-1})/AA{P+1})-DD{P-2}-10", 10);
            //String formnul2 = CellUtils.ReturnFormulaWithPos(formualMap, "((A{P+2}+B{P-1})/AA{P+1})-DD{P-2}-10", 20);
            //TestOffice.TestExcelWrite();
            //EntityContent content = EntityReflectUtils.GetEntityInfo(typeof(TestSimple));
            //Console.WriteLine(content);
            //ConstructDynamic();
            //TestWrite();
            //TestWriteModel();
            //TestQuery();
            //TestRead();
            //TestReadModel();
            //TestJdbc.TestQueryCondition();
            //TestJdbc.TestQueryByFields();
            Func<TestModel> func = ExpressionUtils.GetExpressionFunction<TestModel>();
            TestModel m1 = func();


            TestJdbcOper();
            //TestMachineOper();
            //TestMultipleJoin();
            //TestDbContext();
        }
        private void TestMachineOper()
        {
            string guid = MachineUtils.GetMachineId();
            string cpuSerial = MachineUtils.GetCpuSerial();
            string systemSerial = MachineUtils.GetSystemSerial();
            Console.WriteLine(guid);
            Console.WriteLine(cpuSerial);

        }
        private void TestDbContext()
        {
            DAOFactory.DoInit("res:config.yml");

            SqlMapperConfigure.DoInit("mapper");
            IDbContext context = new DbContext();
            DbContextFactory.Register(context);
            SysUser sysUser = context.GetById<SysUser, long>(1);
            Console.WriteLine(sysUser);
        }
        private void TestJdbcOper()
        {


            DAOFactory.DoInit("res:config.yml");

            SqlMapperConfigure.DoInit("mapper");
            IList<FieldContent> contents = EntityReflectUtils.GetFieldsContent(typeof(SysResourceUser));
            IDbContext context = new DbContext();
            DbContextFactory.Register(context);
            RegServiceContext.ScanServices(typeof(ServiceAttribute));
            SysUserRole sysUserRole = context.GetById<SysUserRole, long>(1);

            IBaseRepository<SysRole, long> roleRepo = RegServiceContext.GetBean<IBaseRepository<SysRole, long>>();
            //Type type = typeof(BaseRepository<,>);
            //Type constructType = type.MakeGenericType([typeof(SysUser), typeof(long)]);
            IBaseRepository<SysUser, long> repo = RegServiceContext.GetBean<IBaseRepository<SysUser, long>>();
            SysUser user = repo.GetById(1);
            IList<SysUser> users = repo.QueryModelsByField(nameof(SysUser.UserName), Constants.SqlOperator.LIKE, ["test"]);

            IUserOperService userOperService = RegServiceContext.GetBean<IUserOperService>();
            userOperService.InitDefault(1, "2,3,4");

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
                    selected.TryGetValue("code", out object? code);
                    permissions.Add(code?.ToString());
                }
            }
            Console.WriteLine(permissions);
        }
        private void TestOfficeOper()
        {
            TestOffice.TestWordRead();
            //TestOffice.TestReadExcel();
        }
        private void TestMultipleJoin()
        {
            Dictionary<Type, string> tableAliasMap = new() { { typeof(SysResource), "a" }, { typeof(SysResourceRole), "b" }, { typeof(SysRole), "c" }, { typeof(SysUserRole), "d" }, { typeof(SysResourceUser), "e" } };

            SqlBuilder sqlBuilder = SqlBuilder.NewBuilder();

            sqlBuilder.AliasEntity(tableAliasMap).From(typeof(SysResource))
                .Join("SysResource.Id", "SysResourceRole.ResId", Constants.JoinType.INNER)
                .Join("SysResourceRole.RoleId", "SysRole.Id", Constants.JoinType.INNER)
                .Join("SysRole.Id", "SysUserRole.RoleId", Constants.JoinType.INNER);

            sqlBuilder.Select("SysResource.Id", "SysResource.ResName", "SysResource.Url", "SysResource.IsLeaf", "SysResource.ResCode", "SysResource.Pid", "SysResource.SeqNo")
                .SelectAs("0", "AssignType");

            JoinConditionBuilder whereBuilder = JoinConditionBuilder.NewBuilder(tableAliasMap);
            whereBuilder.AddEq("SysResourceRole.Status", "1").AddEq("SysUserRole.Status", "1").AddEq("SysUserRole.UserId", 1);
            sqlBuilder.Filter(whereBuilder.Build());

            SqlBuilder sqlBuilder1 = SqlBuilder.NewBuilder();
            sqlBuilder1.AliasEntity(tableAliasMap);
            sqlBuilder1.From(typeof(SysResource))
                .Join("SysResource.Id", "SysResourceUser.ResId", Constants.JoinType.INNER)
                .Select("SysResource.Id", "SysResource.ResName", "SysResource.Url", "SysResource.IsLeaf", "SysResource.ResCode", "SysResource.Pid", "SysResource.SeqNo", "SysResourceUser.AssignType");
            JoinConditionBuilder whereBuilder1 = JoinConditionBuilder.NewBuilder(tableAliasMap);
            whereBuilder1.AddEq("SysResourceUser.Status", "1").AddEq("SysResourceUser.UserId", 1);
            sqlBuilder1.Filter(whereBuilder1.Build());
            //union
            sqlBuilder.Union(sqlBuilder1);

            string executeSql = sqlBuilder.Build();

            DAOFactory.DoInit("res:config.yml");
            RegServiceContext.ScanServices(typeof(ServiceAttribute));
            IBaseRepository<SysUser, long> repo = RegServiceContext.GetBean<IBaseRepository<SysUser, long>>();
            List<UserPermission> queryRs = repo.QueryByNamedParameter<UserPermission>(executeSql, sqlBuilder.QueryParamters.QueryParams);
            Console.WriteLine(queryRs);

        }
    }
}
