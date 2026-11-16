# dotnetframe 通用 .NET 框架

[![DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/robinhood-jim/donetframe)

## 介绍

dotnetframe 是一个基于 .NET 8.0 框架开发的通用应用开发基础框架，由个人独立开发。框架旨在简化具体开发中的重复繁琐的 CRUD 等劳动，同时对若干常用的业务场景进行抽象，提供开箱即用的企业级应用开发能力。

## 特性

### 数据访问层 (ORM)
- 统一的 DAO 层设计，一个数据源对应一个 DAO
- Repository 模式封装，支持单表基本操作（增删改查）
- 支持各种前后置操作钩子
- 支持逻辑删除
- 线程安全的 DbContext 操作，基于 ThreadLocal 实现

### 配置管理
- 基于 YAML 的统一配置管理
- 内置 License 许可证管理能力
- 支持多数据源配置

### 依赖注入 (IoC)
- 参考 Spring 的实现方式
- 支持构造函数注入
- 支持字段属性注入（`[Resource]` 特性）
- 支持多级依赖逐级创建
- 支持依赖递归注入

### 文件处理
- **Excel/Word**: 高效的大文件读写功能
- **多种格式支持**: CSV、JSON、XML、Avro、Parquet 等
- **文件系统**: 本地文件系统、FTP/SFTP、HDFS
- **云存储**: S3、阿里云 OSS、腾讯云 COS、七牛云等
- **压缩文件**: 支持多种压缩格式（无需解压直接读取）

### 服务端无 Serverless
- 支持 DLL 动态加载和卸载
- 支持函数热插拔

### SQL 构建
- Mybatis 风格配置文件支持
- 基于 Builder 的动态 SQL 构建
- 支持 JavaScript 脚本引擎动态解析

### 大数据能力
- NoSQL 数据库封装：MongoDB、Cassandra、CouchDB、HBase
- 图数据库支持：HugeGraph
- 全文搜索：Elasticsearch

### 服务发现与路由
- 支持 Consul 服务注册与发现
- 支持 Nacos 服务注册与发现
- 配置与 Spring Cloud Gateway 兼容

### 链路追踪
- 支持对应 Namespace 的 Assembly Bytecode 增强
- 记录 DAO 层的 SQL 及参数
- 全链路追踪能力

### API 网关
- 统一请求路由
- 负载均衡
- 请求过滤与拦截

## 快速开始

### 配置数据源

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

### 初始化 DAO 工厂

```csharp
// 工厂初始化
DAOFactory f = DAOFactory.init("f:/1.yaml");
// 构造 Repository
BaseRepository<TestModel, long> repository = new Builder<TestModel, long>().build();
```

### 基本 CRUD 操作

```csharp
DAOFactory f = DAOFactory.init("f:/1.yaml");
BaseRepository<TestModel, long> repository = new Builder<TestModel, long>().build();
TestModel model = new TestModel();
// ... 设置属性

// 新增
repository.SaveEntity(model);

// 按主键获取
TestModel model = repository.GetById(1);

// 按单字段条件查询
IList<TestModel> list = repository.QueryModelsByField("name", Constants.SqlOperator.LIKE, new object[] { "t" });

// 更新
repository.UpdateEntity(model);

// 删除
repository.RemoveEntity(new long[] { 1 });
```

### 批量插入

```csharp
IList<TestSimple> list = new List<TestSimple>();
for (int i = 0; i < 1000; i++)
{
    TestSimple simple = new TestSimple();
    simple.name = "col" + Convert.ToString(i);
    simple.tValue = i;
    simple.dTime = DateTime.Now;
    list.Add(simple);
}
repository.InsertBatch(list);
```

### Mybatis 风格 Mapper 操作

```csharp
// 查询分页
PageQuery query = new PageQuery(4);
query.Parameters.Add("Name", "t%");
query.NameSpace = "Frameset.Test";
query.QueryId = "select1";
PageDTO<TestVO> list = repository.QueryPage<TestVO>(query);

// 执行 Sql
TestVO vo = new TestVO();
vo.Name = "test";
vo.Description = "test";
vo.CsId = 50;
repository.ExecuteMapper("Frameset.Test", "insert1", vo);
```

### Mybatis XML 配置示例

```xml
<?xml version="1.0" encoding="UTF-8"?>
<mapper namespace="Frameset.Test">
    <select id="select1" resultMap="rsMap1" parameterType="rsMap1">
        select id, name, code_desc, cs_id, create_time
        from t_test where 1=1
        <script lang="js" id="test1" resultMap="rsMap1">
            var returnstr="";
            if(Name!=null){
                returnstr+=" and name like @Name";
            }
            returnstr;
        </script>
    </select>
</mapper>
```

### 自定义 IoC 注入

```csharp
RegServiceContext.ScanServices(typeof(ServiceAttribute));

[Service]
public class UserOperService : IUserOperService
{
    [Resource]
    private ISysUserRepository _userRepository;
    
    [Resource]
    private IBaseRepository<SysRole, long> _roleRepository;
}
```

### 文件读写

```csharp
// 读取 JSON 文件
DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
builder.Path("e:/1.json.gz").FsType(Constants.FileSystemType.LOCAL);

using (AbstractDataIterator<Dictionary<string, object>> iterator = builder.Build().GetDataReader<Dictionary<string,object>>())
{
    while (iterator.MoveNext())
    {
        Dictionary<string, object> valueMap = iterator.Current;
    }
}

// 写入 Parquet 文件
DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
builder.Path("e:/1.parquet")
    .AddColumnDefine("id", Constants.MetaType.BIGINT)
    .AddColumnDefine("name", Constants.MetaType.STRING)
    .AddColumnDefine("time", Constants.MetaType.TIMESTAMP);

using (AbstractDataWriter<Dictionary<string, object>> writer = DataFileExporter.GetDataWriter<Dictionary<string, object>>(builder.Build()))
{
    for (int i = 0; i < 1000; i++)
    {
        Dictionary<string, object> cachedMap = new Dictionary<string, object>();
        cachedMap.TryAdd("id", i);
        cachedMap.TryAdd("name", "value");
        writer.WriteRecord(cachedMap);
    }
}
```

### 服务注册发现

框架支持通过 Consul 或 Nacos 进行服务注册与发现，配置方式与 Spring Cloud Gateway 兼容。

### 链路追踪

```csharp
// 初始化追踪
DbOperationHacker.Initialize(httpContextAccessor);
```

## 项目结构

```
dotnetframe/
├── Frameset/                    # 核心框架
│   ├── Core/                    # 核心功能
│   ├── Annotation/              # 特性定义
│   ├── Dao/                     # 数据访问
│   ├── Repo/                    # 仓储层
│   └── Service/                 # 服务层
├── Frameset.Bigdata/            # 大数据组件
│   ├── MongoDb/
│   ├── Elastic/
│   ├── Cassandra/
│   ├── Hbase/
│   └── Graph/
├── Frameset.Common/             # 通用工具
│   ├── Data/                    # 数据处理
│   ├── FileSystem/              # 文件系统
│   ├── Compress/                # 压缩解压
│   └── Streaming/               # 消息队列
├── Frameset.Gateway/            # API 网关
├── Frameset.Web/                # Web 基础
├── Frameset.WebMvc/             # MVC 控制器
└── ConsoleTest/                 # 测试示例
```

## 技术栈

- **.NET 8.0**
- **数据库**: MySQL, PostgreSQL, Oracle, SQL Server, ClickHouse, DB2
- **NoSQL**: MongoDB, Cassandra, CouchDB, HBase, Redis
- **搜索**: Elasticsearch
- **消息队列**: Kafka, RabbitMQ
- **文件存储**: S3, HDFS, OSS, COS, 七牛云

## 许可证

本项目为个人独立开发项目，使用请遵循相关许可证协议。

## 贡献

欢迎提交 Issue 和 Pull Request。