using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class MappingEntityAttribute : Attribute
    {
        public string TableName { get; set; }
        public string Schema { get; set; }
        public string DsName { get; set; } = "core";
        public bool IfExplicit { get; set; } = false;

        public MappingEntityAttribute()
        {

        }
        public MappingEntityAttribute(string tableName)
        {
            this.TableName = tableName;
        }
        public MappingEntityAttribute(string tableName, string schema)
        {
            this.TableName = tableName;
            this.Schema = schema;
        }

        public MappingEntityAttribute(string tableName, string schema, string dsName)
        {
            this.TableName = tableName;
            this.Schema = schema;
            this.DsName = dsName;
        }


    }
}