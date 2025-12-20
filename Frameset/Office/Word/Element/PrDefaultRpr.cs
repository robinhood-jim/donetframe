using System.Collections.Generic;

namespace Frameset.Office.Word.Element
{
    public class PrDefaultRpr
    {
        public Dictionary<string, string> Fonts
        {
            get; set;
        } = [];
        public Dictionary<string, string> Langs
        {
            get; set;
        } = [];
        public string Sz
        {
            get; set;
        }
        public string SzCs
        {
            get; set;
        }
    }
}
