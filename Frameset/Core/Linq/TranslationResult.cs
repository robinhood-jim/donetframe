using System.Collections.Generic;
using System.Data.Common;

namespace Frameset.Core.Linq
{
    public class TranslationResult
    {
        public string CommandText { get; internal set; } = string.Empty;
        public List<DbParameter> Parameters { get; internal set; } = [];
        public long SkipRows
        {
            get; internal set;
        } = 0;
        public long PageSize
        {
            get; internal set;
        } = 10;
        public bool Pageable
        {
            get; internal set;
        } = false;

    }
}
