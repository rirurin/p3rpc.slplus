using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;
using static p3rpc.slplus.Field.NewLevelRegistry;

namespace p3rpc.slplus.Messages
{
    public class MessageHooks : ModuleBase<SocialLinkContext>
    {
        // Handle hooking for creating BMD objects
        [StructLayout(LayoutKind.Explicit, Size = 0x218)]
        public unsafe struct FBmdWrapper
        {
        }

        private string FBmdWrapper_CreateBmdWrapper_SIG = "48 89 5C 24 ?? 57 48 83 EC 20 48 89 CF B9 18 02 00 00";
        public FBmdWrapper_CreateBmdWrapper _createBmdWrapper;
        public unsafe delegate FBmdWrapper* FBmdWrapper_CreateBmdWrapper(byte* mBuf);

        private string FBmdWrapper_FreeBmdWrapper_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 48 89 CD 48 8D 99 ?? ?? ?? ??";
        // private string FBmdWrapper_FreeBmdWrapper_SIG_EpAigis = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 89 CE 48 8D 99 ?? ?? ?? ?? 8B 3D ?? ?? ?? ??";
        private string FBmdWrapper_FreeBmdWrapper_SIG_EpAigis = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 89 CE 48 8D 99 ?? ?? ?? ?? BF 20 00 00 00";
        private MultiSignature freeBmdWarpperMS;
        public FBmdWrapper_FreeBmdWrapper _freeBmdWrapper;
        public unsafe delegate void FBmdWrapper_FreeBmdWrapper(FBmdWrapper* wrapper);

        private string FBmdPage_CreateBmdPage_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 63 D2";
        private string FBmdPage_CreateBmdPage_SIG_EpAigis = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 48 63 D2";
        private MultiSignature CreateBmdPageMS;
        public FBmdPage_CreateBmdPage _createBmdPage;
        public unsafe delegate FCurrentBmdPage* FBmdPage_CreateBmdPage(FBmdWrapper* warpper, int dialogId, int page, uint a4);

        private string FBmdPage_BmdSetTextSize_SIG = "E8 ?? ?? ?? ?? 48 8D 4C 24 ?? 4C 8B F8";
        public FBmdPage_BmdSetTextSize _setPageTextSize;
        public unsafe delegate FCurrentBmdPage* FBmdPage_BmdSetTextSize(FCurrentBmdPage* page, float a2, uint lineLimit, uint a4, byte a5, byte a6, nint a7);

        private string FBmdPage_FreeBmdPage_SIG = "48 89 74 24 ?? 57 48 83 EC 20 48 8B 41 ?? 48 89 CE 48 8B 78 ??";
        public FBmdPage_FreeBmdPage _freeBmdPage;
        public unsafe delegate void FBmdPage_FreeBmdPage(FCurrentBmdPage* bmdPage);

        private string BmdPageToFString_SIG = "4C 8B DC 55 57 41 54 49 8D AB ?? ?? ?? ?? 48 81 EC C0 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 45 33 E4 48 89 54 24 ??";
        public BmdPageToFString _bmdPageToFString;
        public unsafe delegate void BmdPageToFString(FString* str, nint firstChar);

        public nuint GetIndirectAddressShortThunk(int offset) => Utils.GetGlobalAddress((nint)_context._utils.GetIndirectAddressShort(offset) + 1);

        public unsafe MessageHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(FBmdWrapper_CreateBmdWrapper_SIG, "FBmdWrapper::CreateBmdWrapper", _context._utils.GetDirectAddress,
                addr => _createBmdWrapper = _context._utils.MakeWrapper<FBmdWrapper_CreateBmdWrapper>(addr));
            freeBmdWarpperMS = new MultiSignature();
            _context._utils.MultiSigScan(
                new[] { FBmdWrapper_FreeBmdWrapper_SIG, FBmdWrapper_FreeBmdWrapper_SIG_EpAigis },
                "FBmdWrapper::FreeBmdWrapper", _context._utils.GetDirectAddress,
                addr => _freeBmdWrapper = _context._utils.MakeWrapper<FBmdWrapper_FreeBmdWrapper>(addr),
                freeBmdWarpperMS
            );

            CreateBmdPageMS = new MultiSignature();
            _context._utils.MultiSigScan(
                new[] { FBmdPage_CreateBmdPage_SIG, FBmdPage_CreateBmdPage_SIG_EpAigis },
                "FBmdPage::CreateBmdPage", _context._utils.GetDirectAddress,
                addr => _createBmdPage = _context._utils.MakeWrapper<FBmdPage_CreateBmdPage>(addr),
                CreateBmdPageMS
            );
            _context._utils.SigScan(FBmdPage_FreeBmdPage_SIG, "FBmdPage::FreeBmdPage", _context._utils.GetDirectAddress,
                addr => _freeBmdPage = _context._utils.MakeWrapper<FBmdPage_FreeBmdPage>(addr));
            _context._utils.SigScan(FBmdPage_BmdSetTextSize_SIG, "FBmdPage::BmdSetTextSize", GetIndirectAddressShortThunk,
                addr => _setPageTextSize = _context._utils.MakeWrapper<FBmdPage_BmdSetTextSize>(addr));

            _context._utils.SigScan(BmdPageToFString_SIG, "BmdPageToFString", _context._utils.GetDirectAddress,
                addr => _bmdPageToFString = _context._utils.MakeWrapper<BmdPageToFString>(addr));
        }
        public override void Register()
        {
        }
    }
}
