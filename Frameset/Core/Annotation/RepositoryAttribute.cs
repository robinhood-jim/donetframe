using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RepositoryAttribute : Attribute
    {
        public string DsName
        {
            get; set;
        }
        public RepositoryAttribute(string dsName)
        {
            DsName = dsName;
        }
    }
}
