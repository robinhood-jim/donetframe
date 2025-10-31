using Frameset.Office.Core;
using Frameset.Office.Element;
using System.Collections.Generic;

namespace Frameset.Office.Meta
{
    public class RelationShips : AbstractWritableElement
    {
        internal IList<RelationShip> relationShips = new List<RelationShip>();
        public void AddRelationShip(RelationShip relationShip)
        {
            relationShips.Add(relationShip);
        }
        public IList<RelationShip> GetRelationShips()
        {
            return relationShips;
        }

        public override void WriteOut(XmlBufferWriter writer)
        {
            BeginPart(writer, "");
        }
    }
    public class RelationShip
    {
        public string Id
        {
            get; internal set;
        }
        public string Target
        {
            get; internal set;
        }
        public string RType
        {
            get; internal set;
        }
        public string TargetMode
        {
            get; internal set;
        }
        public RelationShip(string id, string target, string type)
        {
            this.Id = id;
            this.Target = target;
            this.RType = type;
        }
        public RelationShip(string id, string type, string target, string targetMode)
        {
            this.Id = id;
            this.RType = type;
            this.Target = target;
            this.TargetMode = targetMode;
        }
    }
}
