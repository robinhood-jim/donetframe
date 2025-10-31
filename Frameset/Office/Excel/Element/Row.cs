using System;
using System.Collections.Generic;
using System.Text;

namespace Frameset.Office.Excel.Element
{
    public class Row
    {
        IList<Cell> cells;
        StringBuilder builder = new StringBuilder();
        public Row(IList<Cell> cells)
        {
            this.cells = cells;
        }
        public override String ToString()
        {
            if (builder.Length > 0)
            {
                builder.Clear();
            }
            builder.Append("{");
            for (int i = 0; i < cells.Count; i++)
            {
                builder.Append(i).Append(":").Append(cells[i].GetValue());
                if (i < cells.Count - 1)
                {
                    builder.Append(",");
                }
            }
            builder.Append("}");
            return builder.ToString();
        }
    }
}
