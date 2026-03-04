namespace Frameset.Common.Annotation
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ProtoNumberAttribute : Attribute
    {
        public int Number
        {
            get; set;
        }
        public ProtoNumberAttribute(int inputNumber)
        {
            this.Number = inputNumber;
        }
    }
}
