using p3rpc.slplus.Event;
using p3rpc.slplus.SocialLink;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace p3rpc.slplus.Parsing
{
    internal static class YamlSerializer
    {
        public static readonly IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .WithTypeConverter(EvtPreDataYamlConverter.Instance)
            .WithTypeConverter(SocialLinkYamlConverter.Instance)
            .Build();
    }
}
