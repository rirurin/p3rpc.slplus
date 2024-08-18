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
using static p3rpc.slplus.CommunityHooks;
using Reloaded.Hooks.Definitions.X64;
using p3rpc.slplus.Event;

namespace p3rpc.slplus.Modules
{
    // TODO: Get rid of this module.
    public class Core : ModuleAsmInlineColorEdit<SocialLinkContext>
    {
        private string AUICmpCommu_OverrideCheckSLStart_SIG = "84 C0 0F 84 ?? ?? ?? ?? 8B D3 44 89 64 24 ??";
        private string UCommunityHandler_CheckSocialLinkWasStarted_SIG = "E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 8B D3 44 89 64 24 ??";
        private string UCommunityHandler_AddSocialLinksToSLCampMenuNumber_SIG = "83 FB 16 0F 8E ?? ?? ?? ??";
        private string AUICmpCommu_GetCmmOutlineHelpDialog_SIG = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC B0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 49 8B C9";
        private IAsmHook _overrideCheckSlStart;
        private IHook<UCommunityHandler_CheckSocialLinkWasStarted> _checkSocialLinkStart;
        private IHook<AUICmpCommu_GetCmmOutlineHelpDialog> _getOutlineHelp;

        public ICommonMethods.GetUGlobalWork _getUGlobalWork;

        

        private string UCommunityHandler_ExpandCmmMemberFormatTable_SIG = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC E0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 8B 7A ??";
        private string UCommunityHandler_ExpandCmmNameFormatTable_SIG = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC E0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 8B 7A ??";
        [Function(FunctionAttribute.Register.rax, FunctionAttribute.Register.rax, false)]
        public unsafe delegate TArray<FName>* ExpandCmmFormatTable(TArray<FName>* original);

        private string AAtlEvtLevelSequenceActor_PreloadEvtDialogueTime_SIG = "4C 8B DC 55 41 57 49 8D 6B ?? 48 81 EC 38 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 00";
        public unsafe delegate void AAtlEvtLevelSequenceActor_PreloadEvtDialogueTime(nint self, UMovieSceneSequence* MovieSceneSequence, nint Subsection);
        public IHook<AAtlEvtLevelSequenceActor_PreloadEvtDialogueTime> _preloadEvtDialogueTime;

        public unsafe void AAtlEvtLevelSequenceActor_PreloadEvtDialogueTimeImpl(nint self, UMovieSceneSequence* MovieSceneSequence, nint Subsection)
        {
            if (MovieSceneSequence != null)
            {
                _context._utils.Log($"Scene Sequence GUID: {MovieSceneSequence->baseObj.Signature}");
                _preloadEvtDialogueTime.OriginalFunction(self, MovieSceneSequence, Subsection);
            }
        }

        private string AAtlEvtPlayObject_OnLoadFieldLevelStreaming_SIG = "40 53 48 81 EC A0 00 00 00 80 B9 ?? ?? ?? ?? 03";
        public unsafe delegate void AAtlEvtPlayObject_OnLoadFieldLevelStreaming(AAtlEvtPlayObject* self);
        private IHook<AAtlEvtPlayObject_OnLoadFieldLevelStreaming> _onLoadFieldLevelStreaming;

        public unsafe void AAtlEvtPlayObject_OnLoadFieldLevelStreamingImpl(AAtlEvtPlayObject* self)
        {
            _context._utils.Log($"AAtlEvtPlayObject::OnLoadFieldLevelStreaming: Loaded sublevel {self->LevelName.ToString()}", System.Drawing.Color.LimeGreen, LogLevel.Information);
            _onLoadFieldLevelStreaming.OriginalFunction(self);
        }

        private string UAtlEvtSubsystem_LoadStreamingSublevel_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 55 41 56 41 57 48 8B EC 48 83 EC 70 45 0F B6 F0";
        public unsafe delegate void UAtlEvtSubsystem_LoadStreamingSublevel(UObject* WorldContextObject, FName LevelName, byte a3, nint a4, nint a5);
        private IHook<UAtlEvtSubsystem_LoadStreamingSublevel> _loadStreamingSublevel;
        public unsafe void UAtlEvtSubsystem_LoadStreamingSublevelImpl(UObject* WorldContextObject, FName LevelName, byte a3, nint a4, nint a5)
        {
            _context._utils.Log($"UAtlEvtSubsystem::LoadStreamingSublevelImpl: Start loading {_context._objectMethods.GetFName(LevelName)}", System.Drawing.Color.Pink, LogLevel.Information);
            _loadStreamingSublevel.OriginalFunction(WorldContextObject, LevelName, a3, a4, a5);
        }

        private string UAtlEvtSubsystem_CallEvent_SpawnLoadSublevelActor_SIG = "41 8B 06 48 8D 15 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??";
        [Function(FunctionAttribute.Register.r15, FunctionAttribute.Register.rcx, false)]
        public unsafe delegate byte UAtlEvtSubsystem_CallEvent_SpawnLoadSublevelActor(FName* LevelName);
        private IAsmHook _evtSpawnLoadSublevelActor;
        private IReverseWrapper<UAtlEvtSubsystem_CallEvent_SpawnLoadSublevelActor> _evtSpawnLoadSublevelActorWrapper;
        public unsafe byte UAtlEvtSubsystem_CallEvent_SpawnLoadSublevelActorImpl(FName* LevelName)
        {
            _context._utils.Log($"SpawnLoadSublevelActor: {_context._objectMethods.GetFName(*LevelName)}");
            if (_context._objectMethods.GetFName(*LevelName) == "LV_Event_Cmmu_002_001_C")
            {
                return 1;
            }
            return 0;
        }

        private string ULevelStreaming_GetStreamingLevel_SIG = "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40";
        public unsafe delegate ULevelStreaming* ULevelStreaming_GetStreamingLevel(UObject* WorldContextObject, FName Name);
        private IHook<ULevelStreaming_GetStreamingLevel> _getStreamingLevel;
        public unsafe ULevelStreaming* ULevelStreaming_GetStreamingLevelImpl(UObject* WorldContextObject, FName Name)
        {
            ULevelStreaming* Result = _getStreamingLevel.OriginalFunction(WorldContextObject, Name);
            if (Result == null)
            {
                _context._utils.Log($"ULevelStreaming::GetStreamingLevel returned null! {_context._objectMethods.GetFName(Name)} , OVERRIDING");
                Result = GetModule<EvtPreDataService>().NEW_LEVEL;
            }
            return Result;
        }

        private string AAtlEvtPlayObject_OnLoadLevelStreaming_SIG = "48 89 4C 24 ?? 55 53 56 57 41 55 48 8D 6C 24 ?? 48 81 EC 90 00 00 00";
        public unsafe delegate void AAtlEvtPlayObject_OnLoadLevelStreaming(AAtlEvtPlayObject* self);
        private IHook<AAtlEvtPlayObject_OnLoadLevelStreaming> _onLoadLevelStreaming;

        public unsafe void AAtlEvtPlayObject_OnLoadLevelStreamingImpl(AAtlEvtPlayObject* self)
        {
            _context._utils.Log($"AAtlEvtPlayObject::OnLoadLevelStreaming: Loaded event level {self->LevelName.ToString()}", System.Drawing.Color.LimeGreen, LogLevel.Information);
            _onLoadLevelStreaming.OriginalFunction(self);
        }

        public unsafe Core(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            /*
            _context._utils.SigScan(AUICmpCommu_OverrideCheckSLStart_SIG, "AUICmpCommu::OverrideCheckSLStart", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"mov al, 1"
                };
                _overrideCheckSlStart = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            */
            _context._utils.SigScan(UCommunityHandler_AddSocialLinksToSLCampMenuNumber_SIG, "UCommunityHandler::AddSocialLinksToSLCampMenuNumber", _context._utils.GetDirectAddress, addr =>
            {
                _asmMemWrites.Add(new AddressToMemoryWrite(_context._memory, (nuint)addr, addr => _context._memory.Write(addr + 2, (byte)0x20)));
            });
            _context._utils.SigScan(AUICmpCommu_GetCmmOutlineHelpDialog_SIG, "AUICmpCommu::GetCmmOutlineHelpDialog", _context._utils.GetDirectAddress,
                addr => _getOutlineHelp = _context._utils.MakeHooker<AUICmpCommu_GetCmmOutlineHelpDialog>(AUICmpCommu_GetCmmOutlineHelpDialogImpl, addr));
            _context._utils.SigScan(UCommunityHandler_CheckSocialLinkWasStarted_SIG, "UCommunityHandler::CheckSocialLinkWasStarted", _context._utils.GetIndirectAddressShort, 
                addr => _checkSocialLinkStart = _context._utils.MakeHooker<UCommunityHandler_CheckSocialLinkWasStarted>(UCommunityHandler_CheckSocialLinkWasStartedImpl, addr));
            
            _context._sharedScans.CreateListener<ICommonMethods.GetUGlobalWork>(addr => _context._utils.AfterSigScan(addr, _context._utils.GetDirectAddress, addr => _getUGlobalWork = _context._utils.MakeWrapper<ICommonMethods.GetUGlobalWork>(addr)));

            _context._utils.SigScan(AAtlEvtLevelSequenceActor_PreloadEvtDialogueTime_SIG, "AAtlEvtLevelSequenceActor::PreloadEvtDialogueTime", _context._utils.GetDirectAddress,
                addr => _preloadEvtDialogueTime = _context._utils.MakeHooker<AAtlEvtLevelSequenceActor_PreloadEvtDialogueTime>(AAtlEvtLevelSequenceActor_PreloadEvtDialogueTimeImpl, addr));

            _context._utils.SigScan(AAtlEvtPlayObject_OnLoadLevelStreaming_SIG, "AAtlEvtPlayObject::OnLoadLevelStreaming", _context._utils.GetDirectAddress,
                addr => _onLoadLevelStreaming = _context._utils.MakeHooker<AAtlEvtPlayObject_OnLoadLevelStreaming>(AAtlEvtPlayObject_OnLoadLevelStreamingImpl, addr));

            _context._utils.SigScan(ULevelStreaming_GetStreamingLevel_SIG, "ULevelStreaming::GetStreamingLevel", _context._utils.GetDirectAddress,
                addr => _getStreamingLevel = _context._utils.MakeHooker<ULevelStreaming_GetStreamingLevel>(ULevelStreaming_GetStreamingLevelImpl, addr));

            /*
            _context._utils.SigScan(AAtlEvtPlayObject_OnLoadFieldLevelStreaming_SIG, "AAtlEvtPlayObject::OnLoadFieldLevelStreaming", _context._utils.GetDirectAddress,
                addr => _onLoadFieldLevelStreaming = _context._utils.MakeHooker<AAtlEvtPlayObject_OnLoadFieldLevelStreaming>(AAtlEvtPlayObject_OnLoadFieldLevelStreamingImpl, addr));
            _context._utils.SigScan(UAtlEvtSubsystem_LoadStreamingSublevel_SIG, "UAtlEvtSubsystem::LoadStreamingSublevelImpl", _context._utils.GetDirectAddress,
                addr => _loadStreamingSublevel = _context._utils.MakeHooker<UAtlEvtSubsystem_LoadStreamingSublevel>(UAtlEvtSubsystem_LoadStreamingSublevelImpl, addr));

            _context._utils.SigScan(UAtlEvtSubsystem_CallEvent_SpawnLoadSublevelActor_SIG, "UAtlEvtSubsystem::CallEvent_SpawnLoadSublevelActor", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"{_context._utils.PreserveMicrosoftRegisters()}",
                    $"{_context._hooks.Utilities.GetAbsoluteCallMnemonics(UAtlEvtSubsystem_CallEvent_SpawnLoadSublevelActorImpl, out _evtSpawnLoadSublevelActorWrapper)}",
                    $"cmp rcx, 0", // zero if the level exists in hierachy (LV_Xrd777_P, otherwise one)
                    $"xor rcx, rcx",
                    $"jz LevelExistsInHierachy",
                    $"{_context._utils.RetrieveMicrosoftRegisters()}",
                    $"mov rax, rsi", // move AAtlEvtPlayObject into return
                    $"add rsp, 0x90", // clean up the stack
                    $"pop r15",
                    $"pop rbp",
                    $"pop rbx",
                    $"ret",
                    $"label LevelExistsInHierachy",
                    $"{_context._utils.RetrieveMicrosoftRegisters()}",
                };
                _evtSpawnLoadSublevelActor = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
            */

        }
        public override void Register() {}

        public unsafe byte UCommunityHandler_CheckSocialLinkWasStartedImpl(UCommunityHandler* self, int id)
        {
            if (id < 0x17) return _checkSocialLinkStart.OriginalFunction(self, id);
            return 1;
        }
        public unsafe void AUICmpCommu_GetCmmOutlineHelpDialogImpl(nint self, int* outDialog, int* outPage, nint cmm)
        {
            _getOutlineHelp.OriginalFunction(self, outDialog, outPage, cmm);
            if (*outPage == -1) *outPage = 0; // force it to first page so it doesn't crash
        }
        private unsafe TArray<FName> UDataTable_GetRowNames(UDataTable* dt)
        {
            var rowNames = GetModule<SocialLinkUtilities>().MakeArray<FName>(dt->RowMap.mapNum);
            for (int i = 0; i < dt->RowMap.mapNum; i++)
                *rowNames.GetRef(i) = **(FName**)dt->RowMap.GetByIndex(i);
            return rowNames;
        }

        

        public unsafe delegate byte UCommunityHandler_CheckSocialLinkWasStarted(UCommunityHandler* self, int id);
        public unsafe delegate void AUICmpCommu_GetCmmOutlineHelpDialog(nint self, int* outDialog, int* outPage, nint cmm);
    }
}