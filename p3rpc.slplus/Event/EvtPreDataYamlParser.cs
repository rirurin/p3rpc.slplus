using p3rpc.slplus.Parsing;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace p3rpc.slplus.Event
{
    public class EvtPreDataDeserializeRoot : YamlMappingParser<EvtPreDataModel>
    {
        public static readonly YamlMappingParser<EvtPreDataModel> Instance = new EvtPreDataDeserializeRoot();
        public EvtPreDataDeserializeRoot() : base()
        {
            ValueParsers.Add("EventLevel", ReadEventLevel);
            ValueParsers.Add("EventSublevels", ReadEventSublevels);
            ValueParsers.Add("LightScenarioSublevels", ReadLightScenarioSublevels);
            ValueParsers.Add("DungeonSublevel", ReadDungeonSublevel);
            ValueParsers.Add("bDisableAutoLoadFirstLightingScenarioLevel", ReadDisableAutoLoadFirstLightingScenarioLevel);
            ValueParsers.Add("bForceDisableUseCurrentTimeZone", ReadForceDisableUseCurrentTimeZone);
            ValueParsers.Add("ForcedCldTimeZoneValue", ReadForcedCldTimeZoneValue);
            ValueParsers.Add("ForceMonth", ReadForceMonth);
            ValueParsers.Add("ForceDay", ReadForceDay);
        }

        private void ReadEventLevel(IParser parser, EvtPreDataModel data) => data.EventLevel = parser.Consume<Scalar>().Value;
        private void ReadEventSublevels(IParser parser, EvtPreDataModel data)
        {
            data.EventSublevels = new();
            parser.Consume<SequenceStart>();
            while (parser.Accept<MappingStart>(out _))
                data.EventSublevels.Add(EvtPreDataSublevelConverter.Instance.ReadCurrentMapping(parser, new EvtPreDataSublevels()));
            parser.Consume<SequenceEnd>();
        }
        private void ReadLightScenarioSublevels(IParser parser, EvtPreDataModel data) => data.LightScenarioSublevels = ReadSequence(parser);
        private void ReadDungeonSublevel(IParser parser, EvtPreDataModel data) => data.DungeonSublevel = EvtPreDataDungeonSublevelConverter.Instance.ReadCurrentMapping(parser, new EvtPreDataDungeonSublevel());
        private void ReadDisableAutoLoadFirstLightingScenarioLevel(IParser parser, EvtPreDataModel data) => data.bDisableAutoLoadFirstLightingScenarioLevel = parser.Consume<Scalar>().Value == "true" ? true : false;
        private void ReadForceDisableUseCurrentTimeZone(IParser parser, EvtPreDataModel data) => data.bForceDisableUseCurrentTimeZone = parser.Consume<Scalar>().Value == "true" ? true : false;
        private void ReadForcedCldTimeZoneValue(IParser parser, EvtPreDataModel data) => data.ForcedCldTimeZoneValue = byte.Parse(parser.Consume<Scalar>().Value);
        private void ReadForceMonth(IParser parser, EvtPreDataModel data) => data.ForceMonth = int.Parse(parser.Consume<Scalar>().Value);
        private void ReadForceDay(IParser parser, EvtPreDataModel data) => data.ForceDay = int.Parse(parser.Consume<Scalar>().Value);
    }

    public class EvtPreDataSublevelConverter : YamlMappingParser<EvtPreDataSublevels>
    {
        public static readonly YamlMappingParser<EvtPreDataSublevels> Instance = new EvtPreDataSublevelConverter();
        public EvtPreDataSublevelConverter() : base()
        {
            ValueParsers.Add("EventBGLevels", ReadEventBGLevels);
            ValueParsers.Add("BGFieldSeasonSubLevel", ReadBGFieldSeasonSubLevel);
            ValueParsers.Add("BGFieldSoundSubLevel", ReadBGFieldSoundSublevel);
        }

        private void ReadEventBGLevels(IParser parser, EvtPreDataSublevels data) => data.EventBGLevels = ReadSequence(parser);
        private void ReadBGFieldSeasonSubLevel(IParser parser, EvtPreDataSublevels data) => data.BGFieldSeasonSubLevel = NullIfEmpty(parser.Consume<Scalar>().Value);
        private void ReadBGFieldSoundSublevel(IParser parser, EvtPreDataSublevels data) => data.BGFieldSoundSubLevel = NullIfEmpty(parser.Consume<Scalar>().Value);
    }

    public class EvtPreDataDungeonSublevelConverter : YamlMappingParser<EvtPreDataDungeonSublevel>
    {
        public static readonly YamlMappingParser<EvtPreDataDungeonSublevel> Instance = new EvtPreDataDungeonSublevelConverter();
        public EvtPreDataDungeonSublevelConverter() : base()
        {
            ValueParsers.Add("EventBGFloorLevel", ReadEventBGFloorLevel);
            ValueParsers.Add("BGEnvironmentSubLevel", ReadBGEnvironmentSubLevel);
        }
        private void ReadEventBGFloorLevel(IParser parser, EvtPreDataDungeonSublevel data) => data.EventBGFloorLevel = NullIfEmpty(parser.Consume<Scalar>().Value);
        private void ReadBGEnvironmentSubLevel(IParser parser, EvtPreDataDungeonSublevel data) => data.BGEnvironmentSubLevel = NullIfEmpty(parser.Consume<Scalar>().Value);
    }
    public class EvtPreDataYamlConverter : IYamlTypeConverter
    {
        public static readonly IYamlTypeConverter Instance = new EvtPreDataYamlConverter();
        public bool Accepts(Type type) => type == typeof(EvtPreDataModel);
        public object? ReadYaml(IParser parser, Type type) => EvtPreDataDeserializeRoot.Instance.ReadCurrentMapping(parser, new EvtPreDataModel());
        public void WriteYaml(IEmitter emitter, object? value, Type type) => throw new NotImplementedException();
    }
}
