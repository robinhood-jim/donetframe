using Frameset.Core.Common;
using Frameset.Core.Dao;
using Frameset.Core.Dao.Utils;
using Spring.Util;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Frameset.Core.Repo
{
    public static class ParameterHelper
    {
        internal static IList<DbParameter> AddQueryParam(IJdbcDao dao, FieldContent content, StringBuilder whereBuilder, int paramStartPos, out int parameterSize, Constants.SqlOperator oper, object[] values)
        {
            IList<DbParameter> parameters = new List<DbParameter>();
            AssertUtils.IsTrue(values.Count() > 0, "");
            parameterSize = 0;
            switch (oper)
            {
                case Constants.SqlOperator.EQ:
                case Constants.SqlOperator.NE:
                case Constants.SqlOperator.GT:
                case Constants.SqlOperator.LT:
                case Constants.SqlOperator.GE:
                case Constants.SqlOperator.LE:
                    whereBuilder.Append(content.FieldName).Append(GetOperator(oper)).Append("@").Append(Convert.ToString(paramStartPos));
                    parameters.Add(dao.GetDialect().WrapParameter(paramStartPos, values[0]));
                    parameterSize = 1;
                    break;
                case Constants.SqlOperator.BT:
                    AssertUtils.IsTrue(values.Count() >= 2, "");
                    whereBuilder.Append(content.FieldName).Append(GetOperator(oper)).Append(" @").Append(Convert.ToString(paramStartPos)).Append(" AND ").Append("@").Append(Convert.ToString(paramStartPos + 1));
                    parameters.Add(dao.GetDialect().WrapParameter(paramStartPos, values[0]));
                    parameters.Add(dao.GetDialect().WrapParameter(paramStartPos + 1, values[1]));
                    parameterSize = 2;
                    break;
                case Constants.SqlOperator.LIKE:
                case Constants.SqlOperator.RLIKE:
                case Constants.SqlOperator.LLIKE:
                    whereBuilder.Append(content.FieldName).Append(GetOperator(oper)).Append("@").Append(Convert.ToString(paramStartPos));
                    string likeValue = "%" + values[0].ToString() + "%";
                    if (oper != Constants.SqlOperator.LIKE)
                    {
                        likeValue = Constants.SqlOperator.LLIKE.Equals(oper) ? "%" + values[0].ToString() : values[0].ToString() + "%";
                    }
                    parameters.Add(dao.GetDialect().WrapParameter(paramStartPos, likeValue));
                    parameterSize = 1;
                    break;
                case Constants.SqlOperator.IN:
                    whereBuilder.Append(content.FieldName).Append(GetOperator(oper)).Append("(");
                    parameterSize = values.Length;
                    List<object> inValues = values[0].ToString().Split(',').AsEnumerable<string>().Select(input => ConvertUtil.ParseByType(content.GetMethod.ReflectedType, input)).ToList();
                    inValues.ForEach(o => parameters.Add(dao.GetDialect().WrapParameter(paramStartPos++, o)));
                    parameterSize = inValues.Count;
                    break;

            }
            return parameters;
        }
        public static string GetOperator(Constants.SqlOperator oper)
        {
            string retOper = null;
            switch (oper)
            {
                case Constants.SqlOperator.EQ:
                    retOper = "=";
                    break;
                case Constants.SqlOperator.NE:
                    retOper = "!=";
                    break;
                case Constants.SqlOperator.GT:
                    retOper = ">";
                    break;
                case Constants.SqlOperator.LT:
                    retOper = "<";
                    break;
                case Constants.SqlOperator.GE:
                    retOper = ">=";
                    break;
                case Constants.SqlOperator.LE:
                    retOper = "<=";
                    break;
                case Constants.SqlOperator.BT:
                    retOper = " BETWEEN ";
                    break;
                case Constants.SqlOperator.LIKE:
                case Constants.SqlOperator.LLIKE:
                case Constants.SqlOperator.RLIKE:
                    retOper = " LIKE ";
                    break;
                case Constants.SqlOperator.IN:
                    retOper = " IN ";
                    break;
                case Constants.SqlOperator.NOTIN:
                    retOper = " NOT IN ";
                    break;

            }
            return retOper;

        }
    }
}
