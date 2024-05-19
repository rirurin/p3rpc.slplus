namespace p3rpc.slplus.SocialLink
{
    public class SocialLinkModel
    {
        public string NameUnknown { get; set; }
        public string NameKnown { get; set; }
        public string Arcana { get; set; }

        public int ArcanaId { get; set; }
    }

    public class SocialLinkBitflags
    {
        public uint IsOpen { get; private set; }
        public uint IsMax { get; private set; }
        public uint IsReverse { get; private set; }
        public uint IsBroken { get; private set; }
        public uint IsOk { get; private set; }
        public uint IsNpc { get; private set; }
        public uint IsPlayNewGame { get; private set; }
        public uint IsTalkedToToday { get; private set; }
    }
}
