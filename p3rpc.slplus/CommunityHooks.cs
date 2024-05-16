using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static p3rpc.slplus.Modules.Core;

namespace p3rpc.slplus
{
    public class CommunityHooks : ModuleBase<SocialLinkContext>
    {
        private string UCommunityHandler_GetCmmEntry_SIG = "E8 ?? ?? ?? ?? 48 63 AE ?? ?? ?? ?? 48 89 44 24 ??";
        public unsafe CommunityHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            //_context._utils.SigScan(UCommunityHandler_CheckSocialLinkWasStarted_SIG, "UCommunityHandler::CheckSocialLinkWasStarted", _context._utils.GetIndirectAddressShort,
            //    addr => _checkSocialLinkStart = _context._utils.MakeHooker<UCommunityHandler_CheckSocialLinkWasStarted>(UCommunityHandler_CheckSocialLinkWasStartedImpl, addr));
        }
        public override void Register()
        {
            
        }
    }
}
