# Generic Net framewortk
=========
#### Introduction
Based on Net Frame 8.Now including basic ORM frame support (including EF Core).This may be most simplify code to intergrate ORM ability


## Specification
- Customer Annotation MappingEntity MappingField
- Global DAOFactory Manager Datasource;
- Single JdbcDao per DataSource;
- Service Layer BaseRepository，with basic CRUD operator,can enhace or override;
- Mybatis xml query ability,with js script enabled;
- Integarte with propluar Database system;
- Support EF core Annotation

## Examples
- DataSource Configration
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


- DAO/Repository initialize
```cs

DAOFactory f = DAOFactory.init("f:/1.yaml");
//Construct Repository
BaseRepository<TestModel,long> repository = new Builder<TestModel,long>().build();

```

- CRUD Operator
```cs
DAOFactory f = DAOFactory.init("f:/1.yaml");
BaseRepository<TestModel,long> repository = new Builder<TestModel,long>().build();
TestModel model = new TestModel();
...  
//New Entity
repository.SaveEntity(model);
//
TestModel  model=repository.GetById(1);
//return models with single column Query
IList<TestModel> list=repository.QueryModelsByField("name", Constants.SqlOperator.LIKE, new object[] { "t" });
//Update
repository.UpdateEntity(model);
//Delete
repository.RemoveEntity(new long[] { 1 }.ToArray());
```
- Batch Insert
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

- Mybatis like Query
```cs
DAOFactory f = DAOFactory.init("f:/1.yaml");
BaseRepository<TestModel,long> repository = new Builder<TestModel,long>().build();

PageQuery query = new PageQuery(4);
query.Parameters.Add("Name", "t%");
query.NameSpace = "Frameset.Test";
query.QueryId = "select1";
PageDTO<TestVO> list = repository.QueryPage<TestVO>(query);

TestVO vo = new TestVO();
vo.Name = "test";
vo.Description = "test";
vo.CsId = 50;
repository.ExecuteMapper("Frameset.Test", "insert1", vo);
```

- Mybatis config xml (compatiable with  Mybatis，support js script)
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
- Read/Write File format(CSV/XML/JSON/AVRO/PARQUET) using FileSystem(Local/FTP/SFTP/WebHDFS/AmazonS3)
Read From FileSystem
```java
 DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
 //using FileSystem Local
 builder.Path("e:/1.parquet").FsType(Constants.FileSystemType.LOCAL);

 using (AbstractDataIterator<Dictionary<string, object>> iterator = DataFileImporter.GetDataReader<Dictionary<string,object>>(builder.Build()))
 {
     while (iterator.MoveNext())
     {
         Dictionary<string, object> valueMap = iterator.Current;
         Log.Information("{valueMap}", valueMap);
     }
 }
```
Write to target FileSystem
```java
DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
//assign Path and column metadata define
builder.Path("e:/1.parquet").AddColumnDefine("id", Constants.MetaType.BIGINT).AddColumnDefine("name", Constants.MetaType.STRING)
    .AddColumnDefine("time", Constants.MetaType.TIMESTAMP).AddColumnDefine("amount", Constants.MetaType.INTEGER).AddColumnDefine("price", Constants.MetaType.DOUBLE);
Dictionary<string, object> cachedMap = new Dictionary<string, object>();
Random random = new Random(1231313);
long startTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600 * 24 * 1000;
DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
using (AbstractDataWriter<Dictionary<string, object>> writer = DataFileExporter.GetDataWriter<Dictionary<string, object>>(builder.Build()))
{
    for (int i = 0; i < 1000; i++)
    {
        cachedMap.Clear();
        cachedMap.TryAdd("name", StringUtils.GenerateRandomChar(random, 12));
        cachedMap.TryAdd("time", dateTime.AddMilliseconds(startTs + i * 1000));
        cachedMap.TryAdd("amount", random.Next(1000) + 1);
        cachedMap.TryAdd("price", random.NextDouble() * 1000);
        cachedMap.TryAdd("id", Convert.ToInt64(i));
        writer.WriteRecord(cachedMap);
    }
}
```