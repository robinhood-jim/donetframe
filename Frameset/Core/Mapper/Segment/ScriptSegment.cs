using Frameset.Core.Scripts;
using Microsoft.ClearScript.V8;
using System.Collections.Generic;
using System.Diagnostics;

namespace Frameset.Core.Mapper.Segment
{
    public class ScriptSegment : AbstractSegment
    {
        private V8ScriptEngine engine = null!;
        private V8Script script = null!;
        public ScriptSegment(string nameSpace, string id, string value) : base(nameSpace, id, value)
        {
            engine = ClearScriptUtils.GetEngine();
        }
        public string ScriptType
        {
            get; set;
        } = null!;
        public void Init()
        {
            script = engine.Compile(Value);
        }

        public override string ReturnSqlPart(Dictionary<string, object> map)
        {
            string? retStr = ClearScriptUtils.Eval(script, map).ToString();
            Trace.Assert(!string.IsNullOrWhiteSpace(retStr), "");
            return retStr;
        }
    }
}
