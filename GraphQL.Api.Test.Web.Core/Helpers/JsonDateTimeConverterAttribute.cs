using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mt.GraphQL.Api.Test.Web.Core.Helpers
{
    public class JsonDateTimeConverterAttribute : JsonConverterAttribute
    {
        public string Format { get; set; } = "yyyy-MM-dd";

        public override JsonConverter CreateConverter(Type typeToConvert)
        {
            if (typeToConvert == typeof(DateTime))
                return new Converter { format = Format };
            if (typeToConvert == typeof(DateTime?))
                return new NullableConverter { format = Format };

            throw new NotImplementedException($"No converter for type {typeToConvert.Name}.");
        }

        private class Converter : JsonConverter<DateTime>
        {
            public string format;

            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                DateTime.ParseExact(reader.GetString()!, format, CultureInfo.InvariantCulture);

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) => 
                writer.WriteStringValue(value.ToString(format, CultureInfo.InvariantCulture));
        }

        private class NullableConverter : JsonConverter<DateTime?>
        {
            public string format;

            public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var s = reader.GetString();
                return string.IsNullOrEmpty(s) 
                    ? (DateTime?) null
                    : DateTime.ParseExact(s, format, CultureInfo.InvariantCulture);
            }

            public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
            {
                if (value.HasValue)
                    writer.WriteStringValue(value.Value.ToString(format, CultureInfo.InvariantCulture));
                else
                    writer.WriteNullValue();
            }
        }
    }
}
