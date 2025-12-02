using Microsoft.IdentityModel.Tokens;
using System.Reflection;

namespace Frameset.Web.Util
{
    public class DynamicFunction
    {
        public string FuncName
        {
            get; protected set;
        }
        public List<ServerlessParameter> Parameters
        {
            get; set;
        }
        public List<string> AllowMethods
        {
            get; protected set;
        } = [];
        public bool IsStatic
        {
            get; protected set;
        } = false;
        public MethodInfo TargetMethod
        {
            get; protected set;
        }
        public Type TaregetType
        {
            get; protected set;
        }
        public string InitFunc
        {
            get; set;
        }
        public string InitParam
        {
            get; set;
        }
        public DynamicFunction(string funcName, Type targetType, MethodInfo info, string allowMethods, bool isStatic)
        {
            FuncName = funcName;
            TaregetType = targetType;
            TargetMethod = info;
            AllowMethods = new(allowMethods.Split(','));
            IsStatic = isStatic;
        }
        public bool CanAccess(string callMethod)
        {
            return AllowMethods.IsNullOrEmpty() || AllowMethods.Contains(callMethod.ToUpper());
        }
    }
}
