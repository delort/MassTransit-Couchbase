namespace MassTransit.Persistence.Couchbase
{
    using System.Collections.Generic;
    using System.Globalization;

    using Automatonymous;

    using MassTransit.Persistence.Couchbase.Configuration;
    using MassTransit.Persistence.Couchbase.Converters;
    using MassTransit.Persistence.Couchbase.Resolvers;
    using MassTransit.Serialization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public class DefaultCouchbaseSagaRepositorySettings<TSaga>
        : ICouchbaseSagaRepositorySettings<TSaga>
        where TSaga : StateMachine
    {
        public DefaultCouchbaseSagaRepositorySettings(TSaga saga)
        {
            this.SerializationSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Auto,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    ContractResolver = new NotImplementedSafeContractResolver(),
                    DateParseHandling = DateParseHandling.None,
                    Converters = new List<JsonConverter>(new JsonConverter[]
                        {
                            new AutomatonymousStateJsonConverter<TSaga>(saga),
                            new ByteArrayConverter(), 
                            new IsoDateTimeConverter
                                {
                                    DateTimeStyles = DateTimeStyles.RoundtripKind
                                }
                        }),
                };

            this.DeserializationSettings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    ObjectCreationHandling = ObjectCreationHandling.Auto,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    ContractResolver = new NotImplementedSafeContractResolver(),
                    DateParseHandling = DateParseHandling.None,
                    Converters = new List<JsonConverter>(new JsonConverter[]
                        {
                            new AutomatonymousStateJsonConverter<TSaga>(saga),
                            new ByteArrayConverter(), 
                            new ListJsonConverter(),
                            new InterfaceProxyConverter(),
                            new StringDecimalConverter(),
                            new IsoDateTimeConverter
                                {
                                    DateTimeStyles = DateTimeStyles.RoundtripKind
                                }
                        })
                };

            this.ServerUsername = "Administrator";
            
            this.ServerPassword = "control";

            this.BucketName = "MassTransitSagas";

            this.BucketViewName = "AllSagas";

            this.DesignDocumentName = "MassTransit";

            this.DesignDocument = @"{
                ""views"": {
                    ""AllSagas"": {
                        ""map"": ""function (doc, meta) {\n  emit(meta.id, doc);\n}""
                    }
                }
            }";
        }

        public string ServerUsername { get; set; }

        public string ServerPassword { get; set; }
        
        public string BucketName { get; set; }

        public string BucketViewName { get; set; }

        public string DesignDocumentName { get; set; }
        
        public string DesignDocument { get; set; }
        
        public JsonSerializerSettings SerializationSettings { get; private set; }
        
        public JsonSerializerSettings DeserializationSettings { get; private set; }
    }
}
