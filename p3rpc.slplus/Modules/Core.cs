using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Memory.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using YamlDotNet.Core.Tokens;

namespace p3rpc.slplus.Modules
{
    public class Core : ModuleAsmInlineColorEdit<SocialLinkContext>
    {
        private string AUICmpCommu_OverrideCheckSLStart_SIG = "84 C0 0F 84 ?? ?? ?? ?? 8B D3 44 89 64 24 ??";
        private string UCommunityHandler_CheckSocialLinkWasStarted_SIG = "E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 8B D3 44 89 64 24 ??";
        private string UCommunityHandler_AddSocialLinksToSLCampMenuNumber_SIG = "83 FB 16 0F 8E ?? ?? ?? ??";
        private string AUICmpCommu_GetCmmOutlineHelpDialog_SIG = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC B0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 49 8B C9";
        private string FNameHelper_ParseNumber_SIG = "48 89 5C 24 ?? 48 89 7C 24 ?? 4C 63 1A";
        private string UClass_DeferredRegister_SIG = "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 48 8B 93 ?? ?? ?? ?? 48 8D 4C 24 ??";
        private IAsmHook _overrideCheckSlStart;
        private IHook<UCommunityHandler_CheckSocialLinkWasStarted> _checkSocialLinkStart;
        private IHook<AUICmpCommu_GetCmmOutlineHelpDialog> _getOutlineHelp;

        public ICommonMethods.GetUGlobalWork _getUGlobalWork;
        private IHook<FNameHelper_ParseNumber> _parseNumber;
        private IHook<UClass_DeferredRegister> _deferRegister;
        public unsafe Core(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(AUICmpCommu_OverrideCheckSLStart_SIG, "AUICmpCommu::OverrideCheckSLStart", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"mov al, 1"
                };
                _overrideCheckSlStart = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            _context._utils.SigScan(UCommunityHandler_CheckSocialLinkWasStarted_SIG, "UCommunityHandler::CheckSocialLinkWasStarted", _context._utils.GetIndirectAddressShort, 
                addr => _checkSocialLinkStart = _context._utils.MakeHooker<UCommunityHandler_CheckSocialLinkWasStarted>(UCommunityHandler_CheckSocialLinkWasStartedImpl, addr));
            _context._utils.SigScan(UCommunityHandler_AddSocialLinksToSLCampMenuNumber_SIG, "UCommunityHandler::AddSocialLinksToSLCampMenuNumber", _context._utils.GetDirectAddress, addr =>
            {
                _asmMemWrites.Add(new AddressToMemoryWrite(_context._memory, (nuint)addr, addr => _context._memory.Write(addr + 2, (byte)0x20)));
            });
            _context._utils.SigScan(AUICmpCommu_GetCmmOutlineHelpDialog_SIG, "AUICmpCommu::GetCmmOutlineHelpDialog", _context._utils.GetDirectAddress,
                addr => _getOutlineHelp = _context._utils.MakeHooker<AUICmpCommu_GetCmmOutlineHelpDialog>(AUICmpCommu_GetCmmOutlineHelpDialogImpl, addr));
            _context._sharedScans.CreateListener<ICommonMethods.GetUGlobalWork>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _getUGlobalWork = _context._utils.MakeWrapper<ICommonMethods.GetUGlobalWork>(addr)));
        }
        public override void Register() {}

        public unsafe byte UCommunityHandler_CheckSocialLinkWasStartedImpl(UCommunityHandler* self, int id)
        {
            return _checkSocialLinkStart.OriginalFunction(self, id);
        }
        public unsafe void AUICmpCommu_GetCmmOutlineHelpDialogImpl(nint self, int* outDialog, int* outPage, nint cmm)
        {
            _getOutlineHelp.OriginalFunction(self, outDialog, outPage, cmm);
            if (*outPage == -1) *outPage = 0; // force it to first page so it doesn't crash
        }
        public unsafe int FNameHelper_ParseNumberImpl(nint text, int* len)
        {
            int value = _parseNumber.OriginalFunction(text, len);
            _context._utils.Log($"FNameHelper::ParseNumber: got {value} from {Marshal.PtrToStringUni(text)}");
            return value;
        }
        public unsafe void UClass_DeferredRegisterImpl(UClass* self, UClass* type, nint packageName, nint name)
        {
            _context._utils.Log($"UClass::DeferredRegister was called on {Marshal.PtrToStringUni(name)}");
            _deferRegister.OriginalFunction(self, type, packageName, name);
        }
        public unsafe delegate byte UCommunityHandler_CheckSocialLinkWasStarted(UCommunityHandler* self, int id);
        public unsafe delegate void AUICmpCommu_GetCmmOutlineHelpDialog(nint self, int* outDialog, int* outPage, nint cmm);
        public unsafe delegate int FNameHelper_ParseNumber(nint text, int* len);
        public unsafe delegate void UClass_DeferredRegister(UClass* self, UClass* type, nint packageName, nint name);
    }
}
;