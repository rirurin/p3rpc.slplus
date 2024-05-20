using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
namespace p3rpc.slplus.Hooking
{
    public class CommonHooks : ModuleBase<SocialLinkContext>
    {
        public ICommonMethods.GetUGlobalWork _getUGlobalWork;
        public unsafe uint* _ActiveDrawTypeId;
        public IUIMethods.UIDraw_SetPresetBlendState _setPresetBlendState;
        public IUIMethods.GetSpriteItemMaskInstance _getSpriteItemMaskInstance;
        public IUIMethods.AUIDrawBaseActor_DrawPlg _drawPlg;

        public unsafe BPDrawSpr* GetDrawer() => (BPDrawSpr*)(_getSpriteItemMaskInstance() + 0x20);

        public unsafe CommonHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._sharedScans.CreateListener<ICommonMethods.GetUGlobalWork>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _getUGlobalWork = _context._utils.MakeWrapper<ICommonMethods.GetUGlobalWork>(addr)));
            _context._sharedScans.CreateListener("DrawComponentMask_ActiveDrawTypeId", addr => _context._utils.AfterSigScan(addr, _context._utils.GetIndirectAddressShort2, addr => _ActiveDrawTypeId = (uint*)addr));
            _context._sharedScans.CreateListener<IUIMethods.UIDraw_SetPresetBlendState>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _setPresetBlendState = _context._utils.MakeWrapper<IUIMethods.UIDraw_SetPresetBlendState>(addr)));
            _context._sharedScans.CreateListener<IUIMethods.GetSpriteItemMaskInstance>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetIndirectAddressShort, addr => _getSpriteItemMaskInstance = _context._utils.MakeWrapper<IUIMethods.GetSpriteItemMaskInstance>(addr)));
            _context._sharedScans.CreateListener<IUIMethods.AUIDrawBaseActor_DrawPlg>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetIndirectAddressShort, addr => _drawPlg = _context._utils.MakeWrapper<IUIMethods.AUIDrawBaseActor_DrawPlg>(addr)));
        }
        public override void Register()
        {
        }
    }
}
