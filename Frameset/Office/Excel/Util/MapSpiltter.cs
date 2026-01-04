using Frameset.Core.Common;
using Frameset.Core.Reflect;
using Frameset.Office.Element;
using Frameset.Office.Excel.Element;
using Frameset.Office.Excel.Meta;
using Spring.Context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Frameset.Office.Excel.Util
{
    public class MapSpiltter(WorkBook workBook, Stream stream, ExcelSheetProp sheetProp, bool reuseCurrent) : IEnumerable<Dictionary<string, object>>
    {
        private MapEnumerator enumerator = new MapEnumerator(workBook, stream, sheetProp, reuseCurrent);

        public IEnumerator<Dictionary<string, object>> GetEnumerator()
        {
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    public class MapEnumerator : AbstractCellEnumerator<Dictionary<string, object>>
    {

        public MapEnumerator(WorkBook workBook, Stream stream, ExcelSheetProp sheetProp, bool reuseCurrent = true) : base(workBook, stream, sheetProp, reuseCurrent)
        {

        }


        internal override void GetNext()
        {
            if (!"row".Equals(r.GetLocalName()))
            {
                throw new NoSuchMessageException();
            }
            int trackedColIndex = 0;
            while (r.GoTo(() => r.IsStartElement("c") || r.IsEndElement("row")))
            {
                if ("row".Equals(r.GetLocalName()))
                {
                    break;
                }
                ProcessCell(trackedColIndex++);
            }
            ConstructReturn();
        }


        public override void ConstructReturn()
        {
            Dictionary<string, object> dict = Current;
            dict.Clear();
            for (int i = 0; i < prop.CellProps.Count; i++)
            {
                if (cells[i] != null && cells[i].GetValue() != null)
                {
                    dict.TryAdd(prop.CellProps[i].ColumnCode, cells[i].GetValue());
                }
            }
        }

        public override void ProcessCell(int trackedColIndex)
        {
            Cell cell = ParseCell(trackedColIndex, false);
            CellAddress addr = cell.GetAddress();
            cells[addr.GetColumn()] = cell;
        }


    }
    public class EntityEnumerator<T> : AbstractCellEnumerator<T>
    {
        Dictionary<string, MethodParam> methodParam;

        public Action<T, EntityEnumerator<T>> ConstuctAction
        {
            get; set;
        }

        public EntityEnumerator(WorkBook workBook, Stream stream, ExcelSheetProp sheetProp, bool reuseCurrent = true) : base(workBook, stream, sheetProp, reuseCurrent)
        {
            methodParam = AnnotationUtils.ReflectObject(typeof(T));
        }

        public override void ConstructReturn()
        {
            if (ConstuctAction == null)
            {
                for (int i = 0; i < prop.CellProps.Count; i++)
                {
                    MethodParam param = null;
                    if (methodParam.TryGetValue(prop.CellProps[i].ColumnCode, out param))
                    {
                        if (cells[i] != null && cells[i].GetValue() != null)
                        {
                            param.SetMethod.Invoke(Current, new object[] { ConvertUtil.ParseByType(param.ParamType, cells[i].GetValue()) });
                        }
                        else
                        {
                            param.SetMethod.Invoke(Current, null);
                        }
                    }
                }
            }
            else
            {
                ConstuctAction.Invoke(Current, this);
            }
        }


        public override void ProcessCell(int trackedColIndex)
        {
            Cell cell = ParseCell(trackedColIndex, false);
            CellAddress addr = cell.GetAddress();
            cells[addr.GetColumn()] = cell;
        }

        internal override void GetNext()
        {
            if (!"row".Equals(r.GetLocalName()))
            {
                throw new NoSuchMessageException();
            }
            int trackedColIndex = 0;
            while (r.GoTo(() => r.IsStartElement("c") || r.IsEndElement("row")))
            {
                if ("row".Equals(r.GetLocalName()))
                {
                    break;
                }
                ProcessCell(trackedColIndex++);
            }
            ConstructReturn();
        }
    }
}
