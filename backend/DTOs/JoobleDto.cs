using System.Text.Json;
using System.Text.Json.Serialization;

namespace backend.DTOs
{
    public class JoobleRequestDto
    {
        public string Keywords { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public int Radius { get; set; } = 25;
        public int Page { get; set; } = 1;
    }

    public class JoobleResponseDto
    {
        public int TotalCount { get; set; }
        public List<JoobleJobDto> Jobs { get; set; } = new List<JoobleJobDto>();
    }

    public class JoobleJobDto
    {
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public string Salary { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Updated { get; set; } = string.Empty;
        
        [JsonConverter(typeof(FlexibleStringConverter))]
        public string Id { get; set; } = string.Empty;
    }

    public class FlexibleStringConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => reader.GetInt64().ToString(),
                JsonTokenType.True => "true",
                JsonTokenType.False => "false",
                JsonTokenType.Null => null,
                _ => reader.GetString()
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
