# Robin.Frameset

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)
![Platform](https://img.shields.io/badge/Platform-AnyCPU%20%7C%20x64-lightgrey)
[![DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/robinhood-jim/donetframe)

=========
## Introduction
A comprehensive enterprise-grade **.NET 8** framework solution providing ORM, unified file system, multi-format data processing, microservice gateway, serverless computing, distributed tracing, and AI integration capabilities.

## 📋 Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Core Features](#core-features)
- [Module Architecture](#module-architecture)
  - [Core Modules](#core-modules)
  - [Common Modules](#common-modules)
  - [Infrastructure Modules](#infrastructure-modules)
- [Usage Examples](#usage-examples)
  - [ORM & Data Access](#orm--data-access)
  - [Unified File System](#unified-file-system)
  - [Serverless Functions](#serverless-functions)
- [Dependencies Matrix](#dependencies-matrix)
- [Project Structure](#project-structure)


---

## Overview

**Robin.Frameset** is a modular .NET framework designed for enterprise application development. It provides a complete suite of tools and libraries covering:

| Capability | Description |
|------------|-------------|
| **Data Access** | Full-featured ORM with annotation-based mapping, supporting MySQL, SQL Server, Oracle, PostgreSQL, DB2, and SQLite |
| **File System** | Unified abstraction for LOCAL, FTP, SFTP, WebHDFS, Amazon S3, MinIO, and Alibaba Cloud OSS |
| **Data Processing** | Stream readers/writers for CSV, JSON, XML, Avro, Parquet, ORC, and Excel formats |
| **Web Framework** | ASP.NET Core based MVC with built-in authentication, authorization, and CRUD scaffolding |
| **Microservices** | API Gateway with Consul service discovery, distributed tracing via OpenTelemetry |
| **Serverless** | Function-as-a-Service runtime with annotation-based endpoint definition |
| **AI Integration** | Extensible AI module for intelligent application development |
| **Message Queue** | Multi-broker support for Kafka, RabbitMQ, and RocketMQ |

---

## Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Visual Studio 2022 / JetBrains Rider / VS Code


```bash
# Clone the repository
git clone https://gitee.com/robinhoodjim/donetframe.git
cd donetframe

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release

# Run tests
dotnet test Frametest/Frametest.csproj
NuGet Packages
```

The framework is published as NuGet packages. Reference them in your project:

```xml 
<ItemGroup>
  <PackageReference Include="Frameset.Core" Version="1.2.2" />
  <PackageReference Include="Frameset.Common" Version="1.2.2" />
</ItemGroup>
```
## Core Features

  - 🗄️ Multi-Database ORM - Annotation-driven entity mapping with support for complex relationships (ManyToOne, OneToMany, ManyToMany)
  -  📁 Unified File Abstraction - Single API for local and cloud storage providers
  -  📊 Big Data Formats - Native support for Apache Arrow, Avro, Parquet, ORC, and Protocol Buffers
  -  🔐 Security & Auth - JWT Bearer authentication, role-based access control, SSH/SFTP support
  -  🌐 Microservice Ready - Consul-based service registry, API Gateway with dynamic routing
  -  📡 Distributed Tracing - OpenTelemetry integration with Zipkin and OTLP exporters
  -  ⚡ Serverless Runtime - Lightweight function hosting with parameter injection
  -  🤖 AI Extension - Pluggable AI capabilities for intelligent workflows
  -  📦 Message Integration - Kafka, RabbitMQ, and RocketMQ client abstractions
  -  📝 Document Processing - Streaming Excel (xlsx) and Word file I/O

## Module Architecture
### Core Modules

🎯 Frameset (v1.2.2)

Assembly: Frameset.Core

The foundational ORM and data access layer.

Key Features:

   - Annotation-based entity mapping ([MappingEntity], [MappingField])
   - Relationship mapping ([ManyToOne], [OneToMany], [ManyToMany])
   - SQL query builder and execution engine
   - Formula columns ([ColumnFormula])
   - Logic columns ([LogicColumn])
   - Dependency injection ([Inject])
   - Repository pattern ([Repository])
   - Pre-authorization ([PreAuthorization])
   - Streaming Excel and Word file processing
   - Code analysis and scripting (Roslyn integration)

Supported Databases: MySQL, SQL Server, Oracle, PostgreSQL, IBM DB2, SQLite

🌐 Frameset.Web

ASP.NET Core web framework extension.

Key Features:

  -  AbstractController<R, V, P> - Generic controller with service injection
  -  AbstractCrudController<V, P> - Ready-to-use CRUD endpoints
  -  JWT Bearer authentication
  -  Role-based authorization
  -  Built-in system models (User, Role, Resource)
  -  Custom middleware stack

📄 Frameset.WebMvc

MVC pattern implementation for web applications.
☁️ Frameset.Serverless

Serverless function runtime.

Key Features:

   - [ServerlessFunc] attribute for endpoint definition
   - [ServerlessParameter] for parameter binding
   - Lightweight execution model
   - Stateless function hosting

### Common Modules

🛠️ Frameset.Common (v1.2.1)

Assembly: Frameset.Common

Unified utilities and integrations.

Key Features:

Unified File System:

   - Local file system
   - FTP / SFTP (FluentFTP, SSH.NET)
   - WebHDFS
   - Amazon S3 (AWSSDK.S3)
   - MinIO (Minio SDK)
   - Alibaba Cloud OSS (Aliyun.OSS.SDK)
   - Tencent Cloud COS (Tencent.QCloud.Cos.Sdk)

Data Format Readers/Writers:

   - CSV (with configurable delimiters)
   - JSON / JSON Lines
   - XML
   - Apache Avro (with BZip2, Snappy, XZ, Zstandard compression)
   - Apache Parquet
   - Apache ORC
   - Apache Arrow (streaming)
   - Protocol Buffers (Google.Protobuf)

Message Queue:

   - Apache Kafka (Confluent.Kafka)
   - RabbitMQ (RabbitMQ.Client)
   - RocketMQ (RocketMQ.Client)

Compression & Encoding:

   - LZ4 (K4os.Compression.LZ4)
   - LZMA (LZMA-SDK)
   - SharpCompress
   - SharpZipLib
   - iText 8 (PDF generation with Asian fonts)

📊 Frameset.Bigdata

Big data processing utilities and connectors.
⛏️ Frameset.Dataming

Data mining and analytics tools.
Infrastructure Modules

🚪 Frameset.Gateway

API Gateway with service mesh capabilities.

Key Features:

  -  Consul service discovery and registration
  -  Dynamic routing configuration
  -  Request forwarding and load balancing
  -  Gateway middleware pipeline
  -  Service health monitoring

🔍 Frameset.Tracing

Distributed tracing and observability.

Key Features:

   - OpenTelemetry SDK integration
   - Exporters: Console, Zipkin, OTLP, OpenTelemetryProtocol
   - Lib.Harmony for AOP instrumentation
   - HTTP correlation tracking
   - Identity token propagation

🌉 Frameset.NioGateway

High-performance NIO (Non-blocking I/O) gateway implementation.



## Usage Examples
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

## ORM & Data Access
- DAO/Repository initialize
Each Dao present as ADO Provider for one datasource
```cs

DAOFactory f = DAOFactory.init("f:/1.yaml");
//Construct Repository
BaseRepository<TestModel,long> repository = new Builder<TestModel,long>().build();

```
- Customer IDbContext
Thread safe DbContext,SaveChanges operation separate with ManagedThreadId,can switch dataSource with dsName at runtime.
```cs

IDbContext context = new DbContext();
DbContextFactory.Register(context);

```

- Customer Ioc Support

```cs
RegServiceContext.ScanServices(typeof(ServiceAttribute));
```
Auto register Class with Attribute ServiceAttribute and recusive AutoWrie service with Attribute ResourceAttribute
- 

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
    ...
    
</mapper>
```
## Unified File System

Read From FileSystem
```cs
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
```cs
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
## Serverless Functions
- Serverless Dynamic Dll support

1.With webapi project add in appsetting.json,serverlessPrefix Parameter
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "serverlessPrefix": "/serverless",
  
}
```
Add Class with Method with Attribute ServerlessFunc,and pack to DLL
```cs
 public class TestService
    {
        [ServerlessFunc]
        public static object GetRole(HttpRequest request, HttpResponse response, long id, IBaseRepository<SysRole, long> repository)
        {
```
then use DynamicFunctionLoader RegisterFunction to register at runtime
you can call through /serverless/{functionName} to call DLL function dynamic

## Dependencies Matrix

|Module|Key Dependencies|
|------------|-------------|
|Frameset |AutoFac, ClickHouse, Dapper, Serilog, SqlParserCS, Net.IBM.Data.Db2, Oracle.ManagedDataAccess, MySqlConnector, Npgsql, Obfuscar, YamlDotNet, dotnet-etcd, Google.Protobuf, Microsoft.IdentityModel.Tokens|
|Frameset.Common |	Aliyun.OSS.SDK, Apache Arrow, Avro, AWSSDK.S3, Confluent.Kafka, FluentFTP, Google.Protobuf, iText 8, LZ4, LZMA-SDK, MessagePack, Minio, Mono.Cecil, Parquet.Net, RabbitMQ.Client, RocketMQ.Client, SharpCompress, SharpZipLib, Snapper, SqlParserCS, SSH.NET, Tencent.QCloud.Cos.Sdk, ZstdSharp, OpenTelemetry, Serilog|
|Frameset.Web |	BCrypt.Net-Next, Microsoft.AspNetCore.Authentication.JwtBearer, Newtonsoft.Json, Google.Protobuf, Microsoft.IdentityModel.Tokens, System.IdentityModel.Tokens.Jwt|
|Frameset.Tracing |	Lib.Harmony, Mono.Cecil, OpenTelemetry (Console, Zipkin, OTLP exporters)

## Project Structure
```
donetframe/
├── Frameset/                    # Core ORM and data access layer (v1.2.0)
├── Frameset.Common/             # Unified utilities and integrations (v1.2.1)
├── Frameset.Web/                # ASP.NET Core web framework
├── Frameset.WebMvc/             # MVC pattern implementation
├── Frameset.Serverless/         # Serverless function runtime
├── Frameset.Gateway/            # API Gateway with Consul
├── Frameset.NioGateway/         # NIO-based gateway
├── Frameset.Tracing/            # OpenTelemetry distributed tracing
├── Frameset.Bigdata/            # Big data processing utilities
├── Frameset.Dataming/           # Data mining tools
├── Frameset.AI/                 # AI integration module
├── Frameset.License/            # License management
├── Frameset.Vue/                # Vue.js frontend project
├── Frametest/                   # Unit and integration tests
├── ConsoleTest/                 # Console application for testing
├── dotnetframe.sln              # Visual Studio solution file
├── README.md                    # Chinese documentation
├── README.en.md                 # English documentation
├── LICENSE                      # Apache License 2.0
└── .
```

## Contact

   - Author: robinhoodjim
   - Repository: https://github.com/robinhood-jim/donetframe
   - Mirror: https://gitee.com/robinhoodjim/donetframe
