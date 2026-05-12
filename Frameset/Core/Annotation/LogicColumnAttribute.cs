using Frameset.Core.Common;
using System;

namespace Frameset.Core.Annotation
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class LogicColumnAttribute : Attribute
    {

        public int ValidValue
        {
            get; set;
        } = Constants.VALID_INT;
        public int InvalidValue
        {
            get; set;
        } = Constants.INVALID_INT;
        public LogicColumnAttribute()
        {

        }


    }
}
