using System;

namespace Frameset.Core.Common
{
    public static class ResourceConstants
    {
        public static readonly string CSVSPLITTER = "csv.splitter";
        public static readonly string CSVDEFAULTSPILTTER = ",";
        //Ftp Configuration
        public static readonly string DEFAULTHOST = "localhost";
        public static readonly string FTPHOST = "ftp.host";
        public static readonly string FTPUSERNAME = "ftp.userName";
        public static readonly string FTPPASSWD = "ftp.password";
        public static readonly string FTPPORT = "ftp.port";
        public static readonly int FTPDEFAULTPORT = 21;
        //SFTP configration
        public static readonly string SFTPHOST = "sftp.host";
        public static readonly string SFTPUSERNAME = "sftp.userName";
        public static readonly string SFTPPASSWD = "sftp.password";
        public static readonly string SFTPPORT = "sftp.port";
        public static readonly int SFTPDEFAULTPORT = 22;
        //HDFS configuration
        public static readonly string HDFSBASEURL = "dfs.baseUrl";
        public static readonly string HDFSUSERNAME = "dfs.userName";
        public static readonly string HDFSTOKEN = "dfs.token";
        public static readonly string HDFSAUTHTYPE = "dfs.authType";
        public static readonly string REUSECURRENT = "fs.reUseCurrent";
        public static readonly string STRINGENCODING = "fs.encoding";
        //dateformatter
        public static readonly string INPUTDATEFORMATTER = "intput.dateFormat";
        public static readonly string INPUTTIMESTAMPFORMATTER = "input.timestampFormat";
        public static readonly string OUTPUTDATEFORMATTER = "output.dateFormat";
        public static readonly string OUTPUTTIMESTAMPFORMATTER = "output.timestampFormat";
        public static readonly string DEFAULTDATEFORMAT = "yyyy-MM-dd";
        public static readonly string DEFAULTTIMESTAMPFORMAT = "yyyy-MM-dd HH:mm:ss";
        //orc
        public static readonly string DYNAMICCLASSPREFIX = "DynamicObject";
        public static readonly string DYNAMICORCCLASSNAME = "orc.dynamicClassName";
        //parquet
        public static readonly string PARQUETGROUPSIZE = "fs.parquetGroupSize";
        //xml
        public static readonly string XMLCOLLECTIONNAME = "xml.collectionName";
        public static readonly string XMLENTITYIONNAME = "xml.entityName";
        public static readonly string XMLDEFUALTCOLLECTIONAME = "Records";
        public static readonly string XMLDEFAULTENTITYNAME = "Record";
        //kafka
        public static readonly string KAFKABROKERURL = "kafka.brokerUrl";
        public static readonly string KAFKASERIALIZER = "kafka.serializer";
        public static readonly string KAFKACONSUMERGROUPID = "kafka.groupId";
        public static readonly string KAFKAQUEUENAME = "kafka.queueName";
        //rocketmq
        public static readonly string ROCKETMQBROKERURL = "rocketmq.brokerUrl";
        public static readonly string ROCKETMQSERIALIZER = "rocketmq.serializer";
        public static readonly string ROCKETMQCONSUMERGROUPID = "rocketmq.groupId";
        public static readonly string ROCKETMQUEUENAME = "rocketmq.queueName";
        //rabbitmq
        public static readonly string RABBITMQHOST = "rabbitmq.host";
        public static readonly string RABBITMQPORT = "rabbitmq.port";
        public static readonly string RABBITMQUSER = "rabbitmq.user";
        public static readonly string RABBITMQPASSWD = "rabbitmq.passwd";
        public static readonly string RABBITMQEXCHANGE = "rabbitmq.exchange";
        public static readonly string RABBITMQROUTINGKEY = "rabbitmq.routingKey";
        public static readonly string RABBITMQDEFAULTHOST = "localhost";
        public static readonly string RABBITMQQUEUENAE = "rabbitmq.queueName";
        public static readonly int RABBITMQDEFAULTPORT = 5672;
        //elasticsearch
        public static readonly string ELASTICENDPOINTS = "elastic.endpoints";
        public static readonly string ELASTICUSERNAME = "elastic.userName";
        public static readonly string ELASTICPASSWD = "elastic.passwd";
        //mongodb
        public static readonly string MONGODBURL = "mongodb.url";
        public static readonly string MONGODBDEFAULTURL = "mongodb://127.0.0.1:27017";
        //cassandra
        public static readonly string CASSANDRAURL = "cassandra.url";
        public static readonly string CASSANDRAKEYSAPCE = "cassandra.keySpace";
        public static readonly string CASSANDRAUSERNAME = "cassandra.userName";
        public static readonly string CASSANDRAPASSWD = "cassandra.passwd";
        public static readonly string CASSANDRASSLPATH = "cassandra.sslPath";
        //hbase
        public static readonly string HBASEQUORUMURL = "hbase.quorumUrl";
        public static readonly string HBASEPROTOBUFURL = "hbase.protoBufUrl";
        public static readonly string HBASEUSERNAME = "hbase.userName";
        public static readonly string HBASEPASSWD = "hbase.passwd";
        //CouchDb
        public static readonly string COUCHDBURL = "couchdb.url";
        public static readonly string COUCHDBNAME = "couchdb.name";
        public static readonly string COUCHDBCREDENTIAL = "couchdb.credential";
        //Redis
        public static readonly string REDISURL = "redis.url";
        public static readonly string REDISDBID = "redis.db";

        public enum SerializeType
        {
            JSON,
            XML,
            AVRO
        }
        public static SerializeType SerialzeTypeOf(string serialzeType)
        {
            SerializeType resType = SerializeType.JSON;
            foreach (SerializeType rtype in Enum.GetValues(typeof(SerializeType)))
            {
                if (rtype.ToString().ToUpper().Equals(serialzeType.ToUpper()))
                {
                    resType = rtype;
                    break;
                }
            }
            return resType;
        }

    }
}
