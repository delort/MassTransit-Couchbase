namespace MassTransit.Persistence.Couchbase
{
    using System;
    using System.Globalization;

    using Automatonymous;

    using Newtonsoft.Json;

    public class AutomatonymousStateJsonConverter<TSaga> : JsonConverter
        where TSaga : StateMachine
    {
        private readonly TSaga saga;

        public AutomatonymousStateJsonConverter(TSaga machine)
        {
            this.saga = machine;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        ////TODO: Looks like this Serialization is not get called from Couchbase client.
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                throw new JsonReaderException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Error reading State. Expected an Object but got {0}.",
                        new object[] { reader.TokenType }));
            }

            var dynamicValue = serializer.Deserialize<dynamic>(reader);
            var stateText = dynamicValue.name.ToString();

            return string.IsNullOrWhiteSpace(stateText)
                ? default(State)
                : this.saga.GetState(stateText);
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(State).IsAssignableFrom(objectType);
        }
    }
}