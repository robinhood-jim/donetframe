using Frameset.Core.Common;
using Frameset.Office.Core;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;

namespace Frameset.Office.Element
{
    public class DataValidation : IWritableElement
    {
        private string type;
        private bool allowBlank = true;
        private bool showInputMessage = true;
        private bool showErrorMessage = true;
        private IList<Formula> formulas = new List<Formula>();
        private string sqref;
        public DataValidation(string type, string sqref, IList<string> formulas)
        {
            this.type = type;
            this.sqref = sqref;
            foreach (string formula in formulas)
            {
                this.formulas.Add(new Formula(formula));
            }
        }

        public void WriteOut(XmlBufferWriter writer)
        {
            writer.Append("<dataValidation ")
               .Append("type=\"").Append(type).Append("\"")
               .Append("allowBlank=\"").Append(allowBlank ? Constants.VALID : Constants.INVALID).Append("\"")
               .Append("showInputMessage=\"").Append(showInputMessage ? Constants.VALID : Constants.INVALID).Append("\"")
               .Append("showErrorMessage=\"").Append(showErrorMessage ? Constants.VALID : Constants.INVALID).Append("\"")
               .Append("sqref=\"").Append(sqref).Append("\">");
            if (!formulas.IsNullOrEmpty())
            {
                for (int i = 1; i <= formulas.Count; i++)
                {
                    writer.Append("<formula").Append(i).Append(">");
                    if ("list".Equals(type))
                    {
                        writer.Append("\"");
                    }
                    writer.Append(formulas[i - 1].GetExpression());
                    if ("list".Equals(type))
                    {
                        writer.Append("\"");
                    }
                    writer.Append("</formula").Append(i).Append(">");
                }
            }
            writer.Append("</dataValidation>");
        }
    }
}
