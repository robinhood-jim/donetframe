using Frameset.Web.Model.System;

namespace Frameset.WebMvc.Vo
{
    public class RegionVO
    {
        public string Code
        {
            get; set;
        } = string.Empty;
        public string Name
        {
            get; set;
        } = string.Empty;
        public string Pcode
        {
            get; set;
        } = string.Empty;
        public int Level
        {
            get; set;
        }
        public static RegionVO FromEntity(Region region)
        {
            return new RegionVO
            {
                Code = region.Code,
                Name = region.Name,
                Pcode = region.Pcode,
                Level = region.Level
            };
        }
    }
}
