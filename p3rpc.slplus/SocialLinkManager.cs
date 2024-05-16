using p3rpc.commonmodutils;

namespace p3rpc.slplus
{
    public class SocialLinkManager : ModuleBase<SocialLinkContext>
    {
        private int FirstFreeCmmIndex = 0x17;
        private Dictionary<int, SocialLinkModel> activeSocialLinks = new();
        //private Dictionary<int, int> slHashToCmmIndex; // starting at 0x17
        private Dictionary<int, int> cmmIndexToSlHash = new();
        public unsafe SocialLinkManager(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {}
        public override void Register()
        {
        }

        public void RegisterSocialLink(int key, SocialLinkModel newSl)
        {
            activeSocialLinks.Add(key, newSl);
            cmmIndexToSlHash.Add(FirstFreeCmmIndex, key);
            _context._utils.Log($"Registered new social link \"{newSl.nameKnown}\" (ID {FirstFreeCmmIndex}, key 0x{key:X})");
            FirstFreeCmmIndex++;
        }
    }
}
