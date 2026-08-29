using Newtonsoft.Json;

namespace Frameset.Web.Utils
{
    public class TokenModel
    {
        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }
    public class AuthenticationResult : TokenModel
    {

        public bool Success { get; set; }
        public IEnumerable<string> Errors { get; set; } = [];

    }
}
