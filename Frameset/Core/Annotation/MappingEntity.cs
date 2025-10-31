using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MappingEntity : Attribute
    {
        public string TableName { get; set; }
        public string Schema { get; set; }
        public string DsName { get; set; }
        public bool IfExplicit { get; set; } = false;
        public MappingEntity()
        {

        }
        public MappingEntity(string tableName)
        {
            this.TableName = tableName;
        }
        public MappingEntity(string tableName, string schema)
        {
            this.TableName = tableName;
            this.Schema = schema;
        }

        public MappingEntity(string tableName, string schema, string dsName)
        {
            this.TableName = tableName;
            this.Schema = schema;
            this.DsName = dsName;
        }


    }
}