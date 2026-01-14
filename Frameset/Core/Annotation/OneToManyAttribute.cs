using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class OneToManyAttribute : Attribute
    {
        public Type SubType
        {
            get; set;
        }
        public string JoinColumn
        {
            get; set;
        }
        public CascadeType Cascade
        {
            get; set;
        } = CascadeType.DETACH;
        public OneToManyAttribute(Type subType)
        {
            this.SubType = subType;
        }
        public OneToManyAttribute(Type subType, string joinColumn)
        {
            this.SubType = subType;
            this.JoinColumn = joinColumn;
        }
    }
    public enum CascadeType
    {
        PERSIST,
        MERGE,
        REMOVE,
        REFRESH,
        DETACH,
        ALL
    }
}
