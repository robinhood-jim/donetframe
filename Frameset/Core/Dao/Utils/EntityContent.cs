using Microsoft.IdentityModel.Tokens;
using System;

namespace Frameset.Core.Dao.Utils
{
    public class EntityContent
    {
        private string _tableName;
        private string _schema;
        private string _dsName;

        public string TableName
        {
            get => this._tableName;
            set
            {
                this._tableName = value?.ToString();
            }
        }
        public Type Clazz
        {
            get;
            set;
        }
        public string Schema
        {
            get => this._schema;
            set
            {
                this._schema = value?.ToString();
            }
        }
        public string DsName
        {
            get => this._dsName;
            set
            {
                this._dsName = value?.ToString();
            }
        }
        public bool IfExplicit
        {
            get; set;
        } = false;
        public EntityContent(Type className, string tableName, string schema, string dsName)
        {
            this.Clazz = className;
            this._tableName = tableName;
            this._schema = schema;
            if (!dsName.IsNullOrEmpty())
            {
                this._dsName = dsName;
            }
        }
    }
}
