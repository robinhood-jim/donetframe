using Frameset.Core.Common;
using Frameset.Core.Utils;

using Frameset.Web.Model.System;
using Frameset.WebMvc.Common;
using System.Text.Json.Serialization;

namespace Frameset.WebMvc.Dto
{
    public class RouterDto
    {
        [JsonPropertyName("id")]
        public long Id
        {
            get;
            internal set;
        }
        [JsonPropertyName("name")]
        public string Name
        {
            get;
            internal set;
        } = string.Empty;
        [JsonPropertyName("path")]
        public string Path
        {
            get;
            internal set;
        } = string.Empty;
        [JsonPropertyName("seqNo")]
        public int SeqNo
        {
            get; internal set;
        }
        [JsonPropertyName("pid")]
        public long Pid
        {
            get; internal set;
        }
        [JsonPropertyName("hidden")]
        public bool Hidden
        {
            get; internal set;
        }
        [JsonPropertyName("ifFrame")]
        public bool IfFrame
        {
            get; internal set;
        }
        [JsonPropertyName("component")]
        public string Component
        {
            get; internal set;
        } = "Layout";
        [JsonPropertyName("assignType")]
        public string AssignType
        {
            get; internal set;
        } = WebConstants.RESOURCE_DENIED;
        [JsonPropertyName("children")]
        public List<RouterDto>? Children
        {
            get; internal set;
        }
        [JsonPropertyName("meta")]
        public MetaObj Meta
        {
            get; internal set;
        } = new();
        public static RouterDto FromVo(SysResource vo)
        {
            RouterDto dto = new RouterDto();
            BeanUtils.CopyProperties(vo, dto);
            dto.AssignType = WebConstants.RESOURCE_DEFAULT;
            dto.Meta.Title = vo.Name;
            dto.Meta.Icon = vo.Icon;
            dto.Path = vo.RouterPath;
            if (string.Equals(Constants.VALID, Convert.ToString(vo.LeafTag), StringComparison.OrdinalIgnoreCase))
            {
                dto.Component = vo.Url;
            }
            return dto;
        }
    }
    public class MetaObj
    {
        [JsonPropertyName("title")]
        public string Title
        {
            get; internal set;
        } = string.Empty;
        [JsonPropertyName("icon")]
        public string Icon
        {
            get; internal set;
        } = string.Empty;
    }
}
