namespace p3rpc.slplus.SocialLink
{
    public class SocialLinkModel
    {
        public string CommuName { get; set; }
        public string NameUnknown { get; set; }
        public string NameKnown { get; set; }
        public string Arcana { get; set; }
        public SocialLinkArcana ArcanaId { get; set; }
        public string CommuBustup { get; set; }
        public string CommuHeader { get; set; }
        public string CmmOutlineBmd { get; set; }
        public string CmmProfileBmd { get; set; }
        public string RankUpName { get; set; }
        public string MailText { get; set; }
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

    public enum SocialLinkArcana : byte
    { // One indexed, based on DT_CommunityFormat_Name. Note that some arcana fields may be zero indexed (e.g fool is 0)
        UNUSED_00 = 0,
        FOOL,
        MAGICIAN,
        PRIESTESS,
        EMPRESS,
        EMPEROR,
        HIEROPHANT,
        LOVERS,
        CHARIOT,
        JUSTICE,
        HERMIT,
        FORTUNE,
        STRENGTH,
        HANGED,
        DEATH,
        TEMPERANCE,
        DEVIL,
        TOWER,
        STAR,
        MOON,
        SUN,
        JUDGEMENT,
        AEON // Judgement (Aigis)
        // WORLD (21) (we will ignore these lol)
        // UNIVERSE (21)
    }
}
