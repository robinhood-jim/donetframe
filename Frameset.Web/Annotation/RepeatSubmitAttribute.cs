namespace Frameset.Web.Annotation
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RepeatSubmitAttribute : Attribute
    {
        public RepeatSubmitAttribute()
        {

        }
        public bool BannIp
        {
            get; set;
        } = false;
        public long AllowInterval
        {
            get; set;
        } = 5000;
        public int AllowAccessNum
        {
            get; set;
        } = 0;
    }
}
