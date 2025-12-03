namespace Frameset.Common.Annotation
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ServerlessFuncAttribute : Attribute
    {
        public string Value
        {
            get; set;
        } = null!;
        public string? InitFunc
        {
            get; set;
        } 
        public string? InitParameter
        {
            get; set;
        }
        public string AllowMethods
        {
            get; set;
        } = null!;
        public ServerlessFuncAttribute()
        {

        }
        public ServerlessFuncAttribute(string value)
        {
            this.Value = value;
        }
        public ServerlessFuncAttribute(string value, string initFunc, string? initParam = null)
        {
            this.Value = value;
            this.InitFunc = initFunc;
            this.InitParameter = initParam;
        }
        public ServerlessFuncAttribute(string value, string allowMethods, string initFunc, string? initParam = null)
        {
            this.Value = value;
            this.InitFunc = initFunc;
            this.InitParameter = initParam;
            this.AllowMethods = allowMethods;
        }

    }
}
