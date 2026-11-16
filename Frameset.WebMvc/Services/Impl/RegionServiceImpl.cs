using Frameset.Core.Annotation;
using Frameset.Core.Service;
using Frameset.Web.Model.System;
using Frameset.WebMvc.Vo;
using Microsoft.IdentityModel.Tokens;

namespace Frameset.WebMvc.Services.Impl
{
    [Service]
    public class RegionServiceImpl : ServiceImpl<Region, string>, IRegionService
    {
        private Dictionary<string, RegionVO> dict = [];
        public void DoRead()
        {
            if (dict.IsNullOrEmpty())
            {
                List<Region> list = Query().ToList();
                dict = list.ToDictionary(x => x.Code, x => RegionVO.FromEntity(x));
            }
        }
        public RegionVO? GetRegion(string code)
        {
            DoRead();
            dict.TryGetValue(code, out RegionVO? vo);
            return vo;
        }
        public Dictionary<string, RegionVO> GetSubRegion(string parentCode)
        {
            DoRead();
            return dict.Where(x => x.Key.StartsWith(parentCode)).ToDictionary(x => x.Key, x => x.Value);
        }
        public List<string> GetRegionNames(string districtCode)
        {
            DoRead();
            List<string> rets = [];
            if (!string.IsNullOrWhiteSpace(districtCode) && districtCode.Length == 6)
            {
                if (dict.TryGetValue(districtCode.Substring(0, 2), out RegionVO? firstLevel))
                {
                    rets.Add(firstLevel.Name);
                }
                if (dict.TryGetValue(districtCode.Substring(0, 4), out RegionVO? secondLevel))
                {
                    rets.Add(secondLevel.Name);
                }
                if (dict.TryGetValue(districtCode, out RegionVO? currentLevel))
                {
                    rets.Add(currentLevel.Name);
                }
            }
            return rets;
        }
    }
}
