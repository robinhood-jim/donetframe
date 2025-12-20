using Frameset.Common.Data.Api;
using Frameset.Common.Data.Reader;
using Frameset.Common.Data.Writer;
using Frameset.Core.Common;
using Frameset.Core.FileSystem;
using Frameset.Core.Utils;
using Frametest.Dao;
using Serilog;

namespace Frametest.FileSystem
{
    public static class TestFileSystem
    {
        internal static void TestWrite()
        {
            DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
            builder
                .Path("e:/testlocal.csv.brotil").FsType(Constants.FileSystemType.LOCAL)
                //.Path("tmp/testminio.orc.gz").FsType(Constants.FileSystemType.MINIO).AddConfig(StorageConstants.CLOUDFSACCESSKEY, "jeason").AddConfig(StorageConstants.CLOUDFSSECRETKEY, "Jeason@1234").AddConfig(StorageConstants.CLOUDFSENDPOINT, "http://36.158.32.29:18889").AddConfig(StorageConstants.BUCKET_NAME, "test")
                //.Path("testftp.json.gz").FsType(Constants.FileSystemType.FTP).AddConfig(ResourceConstants.FTPUSERNAME, "test").AddConfig(ResourceConstants.FTPPASSWD, "test")
                .AddColumnDefine("id", Constants.MetaType.BIGINT).AddColumnDefine("name", Constants.MetaType.STRING)
                .AddColumnDefine("time", Constants.MetaType.TIMESTAMP).AddColumnDefine("amount", Constants.MetaType.INTEGER).AddColumnDefine("price", Constants.MetaType.DOUBLE);
            Dictionary<string, object> cachedMap = new Dictionary<string, object>();
            Random random = new Random(1231313);
            long startTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600 * 24 * 1000;
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            using (AbstractDataWriter<Dictionary<string, object>> writer = builder.Build().GetDataWriter<Dictionary<string, object>>())
            {
                for (int i = 0; i < 2000; i++)
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
        }
        internal static void TestWriteModel()
        {
            DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
            builder
            .Path("e:/testlocal.orc.gz").FsType(Constants.FileSystemType.LOCAL);
            //.Path("tmp/testminio.csv.gz").FsType(Constants.FileSystemType.MINIO).AddConfig(StorageConstants.CLOUDFSACCESSKEY,"jeason").AddConfig(StorageConstants.CLOUDFSSECRETKEY, "Jeason@1234").AddConfig(StorageConstants.CLOUDFSENDPOINT, "http://36.158.32.29:18889").AddConfig(StorageConstants.BUCKET_NAME,"test");
            //.Path("testftp.avro.bz2").FsType(Constants.FileSystemType.FTP).AddConfig(ResourceConstants.FTPUSERNAME, "test").AddConfig(ResourceConstants.FTPPASSWD, "test");
            DataCollectionDefine define = builder.Build();
            Random random = new Random(1231313);
            long startTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 3600 * 24 * 1000;
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            using (AbstractDataWriter<TestDataModel> writer = define.GetDataWriter<TestDataModel>())
            {
                for (int i = 0; i < 400000; i++)
                {
                    TestDataModel dataModel = new TestDataModel
                    {
                        Id = Convert.ToInt64(i),
                        Name = StringUtils.GenerateRandomChar(random, 12),
                        Time = dateTime.AddMilliseconds(startTs + i * 1000),
                        Amount = random.Next(1000) + 1,
                        Price = random.NextDouble() * 1000
                    };
                    writer.WriteRecord(dataModel);
                }
            }
        }
        internal static void TestRead()
        {
            DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
            builder
                .Path("e:/testlocal.csv.brotil").FsType(Constants.FileSystemType.LOCAL)
                //.Path("tmp/testminio.csv.gz").FsType(Constants.FileSystemType.MINIO).AddConfig(StorageConstants.CLOUDFSACCESSKEY,"jeason").AddConfig(StorageConstants.CLOUDFSSECRETKEY, "Jeason@1234").AddConfig(StorageConstants.CLOUDFSENDPOINT, "http://36.158.32.29:18889").AddConfig(StorageConstants.BUCKET_NAME,"test")
                //.Path("testftp.csv.gz").FsType(Constants.FileSystemType.FTP).AddConfig(ResourceConstants.FTPUSERNAME, "test").AddConfig(ResourceConstants.SFTPPASSWD, "test")
                .AddColumnDefine("id", Constants.MetaType.BIGINT).AddColumnDefine("name", Constants.MetaType.STRING)
                .AddColumnDefine("time", Constants.MetaType.TIMESTAMP).AddColumnDefine("amount", Constants.MetaType.INTEGER).AddColumnDefine("price", Constants.MetaType.DOUBLE);
            DataCollectionDefine define = builder.Build();
            using (AbstractDataIterator<Dictionary<string, object>> iterator = define.GetDataReader<Dictionary<string, object>>())
            {
                Log.Information("begin read {Path} with FileSystem {FsType} ", define.Path, define.FsType);
                int count = 0;
                while (iterator.MoveNext())
                {
                    Dictionary<string, object> valueMap = iterator.Current;
                    count++;
                    //Log.Information("{valueMap}", valueMap);
                }
                Log.Information("count= {Count}", count);
            }
        }
        internal static void TestQuery()
        {
            DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
            builder
                .Path("e:/testlocal1.json.gz").FsType(Constants.FileSystemType.LOCAL)
                //.Path("tmp/testminio.csv.gz").FsType(Constants.FileSystemType.MINIO).AddConfig(StorageConstants.CLOUDFSACCESSKEY,"jeason").AddConfig(StorageConstants.CLOUDFSSECRETKEY, "Jeason@1234").AddConfig(StorageConstants.CLOUDFSENDPOINT, "http://36.158.32.29:18889").AddConfig(StorageConstants.BUCKET_NAME,"test")
                //.Path("testftp.csv.gz").FsType(Constants.FileSystemType.FTP).AddConfig(ResourceConstants.FTPUSERNAME, "test").AddConfig(ResourceConstants.SFTPPASSWD, "test")
                .AddColumnDefine("id", Constants.MetaType.BIGINT).AddColumnDefine("name", Constants.MetaType.STRING)
                .AddColumnDefine("time", Constants.MetaType.TIMESTAMP).AddColumnDefine("amount", Constants.MetaType.INTEGER).AddColumnDefine("price", Constants.MetaType.DOUBLE);
            DataCollectionDefine define = builder.Build();
            IEnumerable<TestDataModel> dataModels = define.GetEnumerable<TestDataModel>();
            var items = from dataModel in dataModels where dataModel.Amount > 500 && dataModel.Price > 500.0 select dataModel;
            int count = 0;
            foreach (var item in items)
            {
                count++;
                //Log.Information("Get Object {Item}", item);
            }
            Log.Information("query total {Count}", count);
        }
        internal static void TestReadModel()
        {
            DataCollectionBuilder builder = DataCollectionBuilder.NewBuilder();
            builder
                .Path("e:/testlocal1.json.gz").FsType(Constants.FileSystemType.LOCAL);
            //.Path("tmp/testminio.csv.gz").FsType(Constants.FileSystemType.MINIO).AddConfig(StorageConstants.CLOUDFSACCESSKEY,"jeason").AddConfig(StorageConstants.CLOUDFSSECRETKEY, "Jeason@1234").AddConfig(StorageConstants.CLOUDFSENDPOINT, "http://36.158.32.29:18889").AddConfig(StorageConstants.BUCKET_NAME,"test")
            //.Path("testftp.csv.gz").FsType(Constants.FileSystemType.FTP).AddConfig(ResourceConstants.FTPUSERNAME, "test").AddConfig(ResourceConstants.SFTPPASSWD, "test")
            DataCollectionDefine define = builder.Build();
            using (AbstractDataIterator<TestDataModel> iterator = define.GetDataReader<TestDataModel>())
            {
                Log.Information("begin read {Path} with FileSystem {FsType} ", define.Path, define.FsType);
                int count = 0;
                while (iterator.MoveNext())
                {
                    TestDataModel dataModel = iterator.Current;
                    count++;
                    //Log.Information("Get Object {DataModel}", dataModel);
                }
                Log.Information("read total {Count}", count);
            }
        }
    }
}
