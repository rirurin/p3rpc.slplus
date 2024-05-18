using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
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
       
        
        public unsafe CommunityHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            
        }
        public override void Register()
        {

        }
    }
}
