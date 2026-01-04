using Frameset.Core.Common;
using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MappingFieldAttribute : Attribute
    {
        private string _field;
        private string _sequenceName;
        private int _precise;
        private int _scale;

        public string Field
        {
            get => this._field;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("fieldName must not be null!");
                }

                this._field = value;
            }
        }
        public bool IfPrimary { get; set; } = false;
        public bool IfIncrement { get; set; } = false;

        public string SequenceName
        {
            get => this._sequenceName;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("sequenceName must not be null!");
                }
                this._sequenceName = value;
            }
        }
        public bool IfRequired { get; set; } = false;

        public Constants.MetaType DataType
        {
            get;
            set;
        }

        public int Precise
        {
            get => this._precise;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("precise not less than zero!");
                }

                _precise = value;
            }
        }


        public int Scale
        {
            get => this._scale;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("scale no less than zero!");
                }

                _scale = value;
            }
        }

        public bool Exist { get; set; } = true;

        public MappingFieldAttribute(string field)
        {
            this.Field = field;
        }

        public MappingFieldAttribute()
        {

        }

        public MappingFieldAttribute(bool ifPrimary)
        {
            this.IfPrimary = ifPrimary;
        }
        public MappingFieldAttribute(bool ifPrimary, bool ifIncrement)
        {
            this.IfPrimary = ifPrimary;
            this.IfIncrement = IfIncrement;
        }


        public MappingFieldAttribute(string field, bool ifPrimary)
        {
            this.Field = field;
            this.IfPrimary = ifPrimary;
        }



    }
}