using p3rpc.slplus.Event;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace p3rpc.slplus
{
    internal static class YamlSerializer
    {
        public static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(EvtPreDataYamlConverter.Instance)
            .Build();
    }
}
