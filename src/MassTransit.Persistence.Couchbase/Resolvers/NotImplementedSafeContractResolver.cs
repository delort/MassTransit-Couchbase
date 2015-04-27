namespace MassTransit.Persistence.Couchbase.Resolvers
{
    using System;
    using System.Reflection;

    using MassTransit.Serialization;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    public class NotImplementedSafeContractResolver : JsonContractResolver
    {
        protected override JsonProperty CreateProperty(
            MemberInfo member,
            MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            property.ShouldSerialize = instance =>
                {
                    try
                    {
                        var propertyInfo = (PropertyInfo)member;
                        if (propertyInfo.CanRead)
                        {
                            propertyInfo.GetValue(instance, null);
                            return true;
                        }
                    }
                    catch (NotImplementedException)
                    {
                    }

                    return false;
                };

            return property;
        }
    }
}
