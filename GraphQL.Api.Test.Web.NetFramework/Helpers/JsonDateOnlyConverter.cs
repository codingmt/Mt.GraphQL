using Newtonsoft.Json;
using System;

namespace Mt.GraphQL.Api.Test.Web.NetFramework.Helpers
{
    public class JsonDateOnlyConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(DateTime);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
                writer.WriteNull();
            else if (value is DateTime dt)
                writer.WriteValue(dt.ToString("yyyy-MM-dd"));
        }
    }
}