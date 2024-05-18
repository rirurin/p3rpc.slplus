using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
namespace p3rpc.slplus.Hooking
{
    public class CommonHooks : ModuleBase<SocialLinkContext>
    {
        public ICommonMethods.GetUGlobalWork _getUGlobalWork;

        public unsafe CommonHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._sharedScans.CreateListener<ICommonMethods.GetUGlobalWork>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _getUGlobalWork = _context._utils.MakeWrapper<ICommonMethods.GetUGlobalWork>(addr)));
        }
        public override void Register()
        {
        }
    }
}
