using p3rpc.commonmodutils;
using System.Security.Cryptography;
using System.Text;

namespace p3rpc.slplus
{
    public class SocialLinkImporter : ModuleBase<SocialLinkContext>
    {
        private SocialLinkManager? _manager;
        public unsafe SocialLinkImporter(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
        }
        public override void Register() 
        {
            _manager = GetModule<SocialLinkManager>();
        }

        public void RegisterSocialLinkFile(string modId, string path)
        {
            if (_manager == null)
            {
                _context._utils.Log($"Cannot load social link file - SocialLinkManager wasn't initialized");
                return;
            }

            var slId = $"{modId}.{Path.GetFileNameWithoutExtension(path)}";
            var slHash = BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(slId)));
            var newSl = YamlSerializer.deserializer.Deserialize<SocialLinkModel>(new StreamReader(path));
            _manager.RegisterSocialLink(slHash, newSl);
            
        }
    }
}