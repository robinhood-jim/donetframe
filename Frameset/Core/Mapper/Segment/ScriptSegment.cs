using Frameset.Core.Scripts;
using Microsoft.ClearScript.V8;
using System.Collections.Generic;

namespace Frameset.Core.Mapper.Segment
{
    public class ScriptSegment : AbstractSegment
    {
        private V8ScriptEngine engine;
        private V8Script script;
        public ScriptSegment(string nameSpace, string id, string value) : base(nameSpace, id, value)
        {
            engine = ClearScriptUtils.GetEngine();

        }
        public string ScriptType
        {
            get; set;
        }
        public void Init()
        {
            script = engine.Compile(Value);
        }

        public override string ReturnSqlPart(Dictionary<string, object> map)
        {
            return ClearScriptUtils.Eval(engine, script, map).ToString();
        }
    }
}
