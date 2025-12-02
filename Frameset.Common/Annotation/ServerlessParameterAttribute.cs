namespace Frameset.Common.Annotation
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.GenericParameter, AllowMultiple = false)]
    public class ServerlessParameterAttribute : Attribute
    {
        public string Value
        {
            get; set;
        }
        public ServerlessParameterAttribute(string value)
        {
            this.Value = value;
        }
    }
}
