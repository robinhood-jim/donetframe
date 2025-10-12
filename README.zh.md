# 通用.net框架
=========
#### 介绍
基于.Net5.0以上的 基础框架，目前包含基本ORM 框架，可支持EF Core的注解方式，整个框架小而精，集成了最小功能集合的ORM功能，未考虑微软的语法树等功能。代码直观易懂，整体代码都是原创，可通过阅读整体框架对ORM有深入的了解

##ORM 框架 说明
- 使用自定义的标签 MappingEntity MappingField
- 使用全局的DAOFactory 管理注册的所有数据库链接
- 同一数据源下唯一的数据访问层 dao JdbcDao
- 业务层 BaseRepository，带基本事务管理，可以扩展CRUD前后置操作，也可以完全自定义相应方法
- 类似Mybatis 的xml配置查询框架，配置支持js脚本语言
- 针对不同数据库，支持批量插入
- 支持对EF core的注解标注实体的支持

## Examples
- 数据库相关配置
```yaml
dataSource:
 core:
  host: localhost
  port: 3316
  dbType: Mysql
  userName: root
  password: root
  maxSize: 10
```
其中dbType 为数据库类型

- DAO/Repository 初始化
```cs
//工厂初始化
DAOFactory f = DAOFactory.init("f:/1.yaml");
//构造Repository
BaseRepository<TestModel,long> repository = new Builder<TestModel,long>().build();

```

- CRUD 操作
```cs
DAOFactory f = DAOFactory.init("f:/1.yaml");
BaseRepository<TestModel,long> repository = new Builder<TestModel,long>().build();
TestModel model = new TestModel();
...  
//新增
repository.SaveEntity(model);
//按主键获取
TestModel  model=repository.GetById(1);
//按单字段指定条件查询
IList<TestModel> list=repository.QueryModelsByField("name", Constants.SqlOperator.LIKE, new object[] { "t" });
//Update
repository.UpdateEntity(model);
//删除
repository.RemoveEntity(new long[] { 1 }.ToArray());
```
- 批量插入
```cs
DAOFactory f = DAOFactory.init("f:/1.yaml");
BaseRepository<TestModel,long> repository = new Builder<TestModel,long>().build();
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
```

- 类似Mybatis 的Mapper 操作 及 分页
```cs
DAOFactory f = DAOFactory.init("f:/1.yaml");
BaseRepository<TestModel,long> repository = new Builder<TestModel,long>().build();
//查询分页
PageQuery query = new PageQuery(4);
query.Parameters.Add("Name", "t%");
query.NameSpace = "Frameset.Test";
query.QueryId = "select1";
PageDTO<TestVO> list = repository.QueryPage<TestVO>(query);
//执行Sql
 TestVO vo = new TestVO();
 vo.Name = "test";
 vo.Description = "test";
 vo.CsId = 50;
 repository.ExecuteMapper("Frameset.Test", "insert1", vo);
```

- Mybatis 配置文件(基本兼容Mybatis语法，支持js脚本引擎)
```xml
<?xml version="1.0" encoding="UTF-8"?>
<mapper namespace="Frameset.Test">
    <resultMap id="rsMap1" type="Frametest.Dao.TestVO">
		<id property="Id"    column="id"    />
		<result property="Name"    column="name"    />
		<result property="CsId"    column="cs_id"    />
		<result property="CreateTime"    column="create_time"    />
		<result property="Description"    column="code_desc"    />
		
    </resultMap>
    <sql id="sqlpart1">
        id,name,code_desc,cs_id,create_time
    </sql>
    <sql id="sqlpart2">
        name,code_desc,cs_id,create_time
    </sql>
    <select id="select1" resultMap="rsMap1" parameterType="rsMap1">
        select
        <include refid="sqlpart1" />
         from t_test where 1=1
        <script lang="js" id="test1" resultMap="rsMap1">
            var returnstr="";
            if(Name!=null){
                returnstr+=" and name like @Name";
            }
            if(Description!=null){
                returnstr+=" and code_desc like @Description";
            }
            if(CsId!=0){
                returnstr+=" and cs_id=@CsId";
            }
            returnstr;
        </script>
    </select>
    <insert id="insert1" parameterType="rsMap1" useGeneratedKeys="true" keyProperty="Id">
        insert into t_test (
        <script lang="js" id="test2">
            var returnstr="";
            if(Name!=null){
                returnstr+="name,"
            }
            if(Description!=null){
                returnstr+="code_desc,"
            }
            if(CsId!=0){
                returnstr+="cs_id,"
            }
            returnstr+="create_time";
        </script>
        ) values (
        <script lang="js" id="test3">
            var returnstr="";
            if(Name!=null){
                returnstr+="@Name,";
            }
            if(Description!=null){
                returnstr+="@Description,";
            }if(CsId!=0){
                returnstr+="@CsId,";
            }
            returnstr+="sysdate()";
        </script>
        )
    </insert>
    <batch id="batch1" parameterType="rsMap1">
        insert into t_test
        <include refid="sqlpart2" />
        values (@Name,@Description,@CsId,sysdate())
    </batch>
    <update id="update1" parameterType="rsMap1">
        update t_test set
        <script lang="js" id="test4">
            var returnstr="";
            if(name!=null){
                returnstr+="name=@Name,";
            }
            if(description!=null){
                returnstr+="code_desc=@Description,";
            }
            if(csId!=0){
                returnstr+="cs_id=@CsId,";
            }
            returnstr.substr(0,returnstr.length-1);
        </script>
          where id=@Id
    </update>
    
</mapper>
```