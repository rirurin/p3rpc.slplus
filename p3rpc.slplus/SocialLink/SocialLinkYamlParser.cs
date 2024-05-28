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
            ValueParsers.Add("CommuBustup", ReadCommuBustup);
            ValueParsers.Add("CommuHeader", ReadCommuHeader);
            ValueParsers.Add("CmmOutlineBmd", ReadCmmOutlineBmd);
            ValueParsers.Add("CmmProfileBmd", ReadCmmProfileBmd);
            ValueParsers.Add("RankUpName", ReadRankUpName);
            ValueParsers.Add("MailText", ReadMailText);
        }

        private void ReadNameUnknown(IParser parser, SocialLinkModel data) => data.NameUnknown = parser.Consume<Scalar>().Value;
        private void ReadNameKnown(IParser parser, SocialLinkModel data) => data.NameKnown = parser.Consume<Scalar>().Value;
        private SocialLinkArcana ArcanaNameToId(string arcanaName)
        {
            return arcanaName switch
            {
                "Fool" => SocialLinkArcana.FOOL,
                "Magician" => SocialLinkArcana.MAGICIAN,
                "Priestess" => SocialLinkArcana.PRIESTESS,
                "Empress" => SocialLinkArcana.EMPRESS,
                "Empreor" => SocialLinkArcana.EMPEROR,
                "Hierophant" => SocialLinkArcana.HIEROPHANT,
                "Lovers" => SocialLinkArcana.LOVERS,
                "Chariot" => SocialLinkArcana.CHARIOT,
                "Justice" => SocialLinkArcana.JUSTICE,
                "Hermit" => SocialLinkArcana.HERMIT,
                "Fortune" => SocialLinkArcana.FORTUNE,
                "Strength" => SocialLinkArcana.STRENGTH,
                "Hanged" => SocialLinkArcana.HANGED,
                "Death" => SocialLinkArcana.DEATH,
                "Temperance" => SocialLinkArcana.TEMPERANCE,
                "Devil" => SocialLinkArcana.DEVIL,
                "Tower" => SocialLinkArcana.TOWER,
                "Star" => SocialLinkArcana.STAR,
                "Moon" => SocialLinkArcana.MOON,
                "Sun" => SocialLinkArcana.SUN,
                "Judgement" => SocialLinkArcana.JUDGEMENT,
                "Aeon" => SocialLinkArcana.AEON,
                _ => throw new Exception($"Unsupported arcana name {arcanaName}")
            };
        }
        private void ReadArcana(IParser parser, SocialLinkModel data)
        {
            data.Arcana = parser.Consume<Scalar>().Value;
            data.ArcanaId = ArcanaNameToId(data.Arcana);
        }
        private void ReadCommuBustup(IParser parser, SocialLinkModel data) => data.CommuBustup = parser.Consume<Scalar>().Value;
        private void ReadCommuHeader(IParser parser, SocialLinkModel data) => data.CommuHeader = parser.Consume<Scalar>().Value;
        private void ReadCmmOutlineBmd(IParser parser, SocialLinkModel data) => data.CmmOutlineBmd = parser.Consume<Scalar>().Value;
        private void ReadCmmProfileBmd(IParser parser, SocialLinkModel data) => data.CmmProfileBmd = parser.Consume<Scalar>().Value;
        private void ReadRankUpName(IParser parser, SocialLinkModel data) => data.RankUpName = parser.Consume<Scalar>().Value;
        private void ReadMailText(IParser parser, SocialLinkModel data) => data.MailText = parser.Consume<Scalar>().Value;
    }

    public class SocialLinkYamlConverter : IYamlTypeConverter
    {
        public static readonly IYamlTypeConverter Instance = new SocialLinkYamlConverter();
        public bool Accepts(Type type) => type == typeof(SocialLinkModel);
        public object? ReadYaml(IParser parser, Type type) => SocialLinkYamlParserRoot.Instance.ReadCurrentMapping(parser, new SocialLinkModel());
        public void WriteYaml(IEmitter emitter, object? value, Type type) => throw new NotImplementedException();
    }
}
