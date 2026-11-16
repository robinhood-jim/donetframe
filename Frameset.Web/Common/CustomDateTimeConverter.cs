using System.Text.Json;
using System.Text.Json.Serialization;

namespace Frameset.Web.Common
{
    public class CustomDateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _format;
        private ThreadLocal<string>? _tempFormat = null;

        // Default to a specific format if none is provided
        public CustomDateTimeConverter(string format = "yyyy-MM-dd HH:mm:ss")
        {
            _format = format;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.ParseExact(reader.GetString()!, _format, System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            string? formatStr = _tempFormat != null && _tempFormat.IsValueCreated ? _tempFormat.Value : _format;
            writer.WriteStringValue(value.ToString(formatStr));

        }
        public void SetDateTimeFormat(string format)
        {
            if (_tempFormat == null || !_tempFormat.IsValueCreated)
            {
                _tempFormat = new ThreadLocal<string>(() => format, true);
                string? value = _tempFormat.Value;
            }
        }
        public void ResetFormat()
        {
            if (_tempFormat != null && _tempFormat.IsValueCreated)
            {
                _tempFormat.Dispose();
                _tempFormat = null;
            }
        }

    }
}
