using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Text;

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
        public Type EntityType
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
       
        public List<Type> ParentEntitys
        {
            get; set;
        } = [];
        public bool IfExplicit
        {
            get; set;
        } = false;
        public EntityContent(Type className,string tableName, string schema, string dsName)
        {
            this.EntityType = className;
            this._tableName = tableName;
            this._schema = schema;
            if (!dsName.IsNullOrEmpty())
            {
                this._dsName = dsName;
            }
        }
        public string GetTableName()
        {
            StringBuilder builder = new StringBuilder();
            if (!Schema.IsNullOrEmpty())
            {
                builder.Append(Schema).Append(".");
            }
            builder.Append(TableName);
            return builder.ToString();
        }
    }
}
