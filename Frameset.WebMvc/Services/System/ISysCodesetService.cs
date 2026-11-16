using Frameset.Core.Service;
using Frameset.Web.Model.System;

namespace Frameset.WebMvc.Services.System
{
    public interface ISysCodesetService : IService<SysCodeset, long>
    {
        List<SysCode> GetSysCode(string dictCode);
        void FilterListByCodeSet<V>(List<V> results, string codeName, string codeNo, string? defaultValue = null) where V : notnull;
        void FilterListNewByCodeSet<V>(List<V> results, string codeName, string newColumnName, string codeNo, string? defaultValue = null) where V : notnull;
    }
}
