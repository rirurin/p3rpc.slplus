using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.SocialLink;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;

namespace p3rpc.slplus.Field
{
    public class FldNpcActorHooks : ModuleBase<SocialLinkContext>
    {
        private SocialLinkManager _slManager;
        private SocialLinkUtilities _slUtils;

        private SocialLinkModel? _currSlModel;

        private string AFldCmmActor_CheckExistSpawnActor_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 41 54 41 56 48 83 EC 20 4D 63 E1";
        private IHook<AFldCmmActor_CheckExistSpawnActor> _checkExistSpawnActor;
        public unsafe delegate int AFldCmmActor_CheckExistSpawnActor(TArray<nint>* cmmExist, short uniqId, byte mType, int daysPassed);
        public unsafe int AFldCmmActor_CheckExistSpawnActorImpl(TArray<nint>* cmmExist, short uniqId, byte mType, int daysPassed)
        {
            // TODO: Write proper logic for this
            return 1;
            //_checkExistSpawnActor.OriginalFunction(cmmExist, uniqId, mType, daysPassed);
        }

        private string UCommunityHandler_FieldActorGetInteractName_SIG = "40 55 53 56 57 41 54 41 55 41 56 48 8D 6C 24 ?? 48 81 EC 00 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 45 33 E4";
        private IHook<UCommunityHandler_FieldActorGetInteractName> _fldActorGetInteractName;
        public unsafe delegate void UCommunityHandler_FieldActorGetInteractName(UCommunityHandler* self, nint a2, int id);
        // AFldCmmActor::FieldActorOnInteract adds +1 to mUniqueId, so subtract one for hash
        // AFldHitCharacter::vtable + 0x650
        public unsafe void UCommunityHandler_FieldActorGetInteractNameImpl(UCommunityHandler* self, nint a2, int id)
        {
            if (id < SocialLinkManager.vanillaCmmLimit)
                _fldActorGetInteractName.OriginalFunction(self, a2, id);
            else
            {
                NativeMemory.Clear((void*)(a2 + 8), 0x20);
                var idReal = id - 1;
                if (_slManager.activeSocialLinks.TryGetValue(idReal, out var slModel))
                {
                    var nameParts = slModel.NameKnown.Split(" ", 2);
                    _slUtils.MakeFStringFromExisting((FString*)(a2 + 0x8), nameParts[0]); // Saori
                    _slUtils.MakeFStringFromExisting((FString*)(a2 + 0x18), nameParts[1]); // Hasegawa
                    // this is called before UUIContactManager::GetMessage, while CmmInteractGetArcanaSprIdImpl is called after, so this should always work
                    _currSlModel = slModel;
                } else
                {
                    _slUtils.MakeFStringFromExisting((FString*)(a2 + 0x18), $"UNK UID {idReal:X}"); // Hasegawa
                }
            }
        }
        // AFldHitCharacter::vtable + 0x658
        // Can Interact

        private string AUIMiscCheckDraw_CmmInteractGetArcanaSprId_SIG = "48 89 5C 24 ?? 57 48 83 EC 30 48 8B D9 48 8B FA 8B 4A ??";
        private IHook<AUIMiscCheckDraw_CmmInteractGetArcanaSprId> _cmmInteractGetArcanaSprId;
        // self + 0x278
        public unsafe delegate byte AUIMiscCheckDraw_CmmInteractGetArcanaSprId(nint self /* + 0x278 */, UUIContactManagerPayload* payload);
        public unsafe byte AUIMiscCheckDraw_CmmInteractGetArcanaSprIdImpl(nint self, UUIContactManagerPayload* payload)
        {
            var checkDrawData = payload->Get<UICheckDrawPayload>();
            if (checkDrawData->arcana >= SocialLinkManager.vanillaCmmLimit)
            {
                checkDrawData->arcana = _currSlModel != null ? (int)_currSlModel.ArcanaId : 1;
                checkDrawData->rank = 4;
                checkDrawData->bIsCmmNpc = 1;
            }
            return _cmmInteractGetArcanaSprId.OriginalFunction(self, payload);
        }

        private string UUIContactManager_GetMessage_SIG = "40 55 53 56 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC A8 01 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 4D 8B E9";
        private IHook<UUIContactManager_GetMessage> _contactMngGetMessage;
        public unsafe delegate void UUIContactManager_GetMessage(nint self, EAppActorId type, int id, UUIContactManagerPayload* payload);
        public unsafe void UUIContactManager_GetMessageImpl(nint self, EAppActorId type, int id, UUIContactManagerPayload* payload)
        {
            _context._utils.Log($"[UUIContactManager::GetMessage] Type {type}, ID {id}, payload @ 0x{(nint)payload:X}");
            _contactMngGetMessage.OriginalFunction(self, type, id, payload);
        }

        public unsafe FldNpcActorHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(AFldCmmActor_CheckExistSpawnActor_SIG, "AFldCmmActor::CheckExistSpawnActor", _context._utils.GetDirectAddress,
                addr => _checkExistSpawnActor = _context._utils.MakeHooker<AFldCmmActor_CheckExistSpawnActor>(AFldCmmActor_CheckExistSpawnActorImpl, addr));
            _context._utils.SigScan(UCommunityHandler_FieldActorGetInteractName_SIG, "UCommunityHandler::FieldActorGetInteractName", _context._utils.GetDirectAddress,
                addr => _fldActorGetInteractName = _context._utils.MakeHooker<UCommunityHandler_FieldActorGetInteractName>(UCommunityHandler_FieldActorGetInteractNameImpl, addr));
            _context._utils.SigScan(AUIMiscCheckDraw_CmmInteractGetArcanaSprId_SIG, "AUIMiscCheckDraw::CmmInteractGetArcanaSprId", _context._utils.GetDirectAddress,
                addr => _cmmInteractGetArcanaSprId = _context._utils.MakeHooker<AUIMiscCheckDraw_CmmInteractGetArcanaSprId>(AUIMiscCheckDraw_CmmInteractGetArcanaSprIdImpl, addr));
            //_context._utils.SigScan(UUIContactManager_GetMessage_SIG, "UUIContactManager::GetMessage", _context._utils.GetDirectAddress,
            //    addr => _contactMngGetMessage = _context._utils.MakeHooker<UUIContactManager_GetMessage>(UUIContactManager_GetMessageImpl, addr));
        }
        public override void Register()
        {
            _slManager = GetModule<SocialLinkManager>();
            _slUtils = GetModule<SocialLinkUtilities>();
        }
    }
}
