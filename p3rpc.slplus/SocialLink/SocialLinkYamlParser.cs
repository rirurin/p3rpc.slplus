using p3rpc.slplus.Event;
using p3rpc.slplus.Parsing;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace p3rpc.slplus.SocialLink
{
    public class SocialLinkYamlParserRoot : YamlMappingParser<SocialLinkModel>
    {
        public static readonly YamlMappingParser<SocialLinkModel> Instance = new SocialLinkYamlParserRoot();

        public SocialLinkYamlParserRoot() : base()
        {
            ValueParsers.Add("NameUnknown", ReadNameUnknown);
            ValueParsers.Add("NameKnown", ReadNameKnown);
            ValueParsers.Add("Arcana", ReadArcana);
        }

        private void ReadNameUnknown(IParser parser, SocialLinkModel data) => data.NameUnknown = parser.Consume<Scalar>().Value;
        private void ReadNameKnown(IParser parser, SocialLinkModel data) => data.NameKnown = parser.Consume<Scalar>().Value;

        private int ArcanaNameToId(string arcanaName)
        {
            return arcanaName switch
            {
                "Fool" => 1,
                "Magician" => 2,
                "Priestess" => 3,
                "Empress" => 4,
                "Empreor" => 5,
                "Hierophant" => 6,
                "Lovers" => 7,
                "Chariot" => 8,
                "Justice" => 9,
                "Hermit" => 10,
                "Fortune" => 11,
                "Strength" => 12,
                "Hanged" => 13,
                "Death" => 14,
                "Temperance" => 15,
                "Devil" => 16,
                "Tower" => 17,
                "Star" => 18,
                "Moon" => 19,
                "Sun" => 20,
                "Judgement" => 21,
                "Aeon" => 22,
                _ => throw new Exception("Unsupported arcana ID")
            };
        }
        private void ReadArcana(IParser parser, SocialLinkModel data)
        {
            data.Arcana = parser.Consume<Scalar>().Value;
            data.ArcanaId = ArcanaNameToId(data.Arcana);
        }
    }

    public class SocialLinkYamlConverter : IYamlTypeConverter
    {
        public static readonly IYamlTypeConverter Instance = new SocialLinkYamlConverter();
        public bool Accepts(Type type) => type == typeof(SocialLinkModel);
        public object? ReadYaml(IParser parser, Type type) => SocialLinkYamlParserRoot.Instance.ReadCurrentMapping(parser, new SocialLinkModel());
        public void WriteYaml(IEmitter emitter, object? value, Type type) => throw new NotImplementedException();
    }
}
