using Frameset.Core.Service;
using Frameset.Web.Model.System;
using Frameset.WebMvc.Vo;

namespace Frameset.WebMvc.Services
{
    public interface IRegionService : IService<Region, string>
    {
        void DoRead();
        Dictionary<string, RegionVO> GetSubRegion(string parentCode);
        List<string> GetRegionNames(string districtCode);
    }
}
