using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Hooking;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;
using System.Runtime.InteropServices;
using static Reloaded.Hooks.Definitions.X64.FunctionAttribute;

namespace p3rpc.slplus.SocialLink
{
    // When fusing personas, the exp bonus for a particular arcana will be the maximum between all available social links that use that arcana
    public class VelvetRoomHooks : ModuleBase<SocialLinkContext>
    {
        private string UUICombineCalc_CalculateSocialLinkExpBonus_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 57 48 83 EC 20 48 8B EA 48 8B F9 E8 ?? ?? ?? ?? 48 8B D8 48 85 C0";
        private IHook<UUICombineCalc_CalculateSocialLinkExpBonus> _calcSocialLinkBonus;
        public unsafe delegate long UUICombineCalc_CalculateSocialLinkExpBonus(UUICombineCalc* self, FDatUnitPersonaEntry* persona);

        private string FDatUnitPersona_CalculateBonusExp_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 0F B7 DA";
        public FDatUnitPersona_CalculateBonusExp _calcBonusExp;
        public unsafe delegate int FDatUnitPersona_CalculateBonusExp(FDatUnitPersonaEntry* persona, ushort bonus);

        private string UUICombineCalc_CalculateResummonCost_SIG = "40 56 41 54 41 55 48 83 EC 50 48 8B 81 ?? ?? ?? ??";
        private IHook<UUICombineCalc_CalculateResummonCost> _calcResummonCost;
        public unsafe delegate long UUICombineCalc_CalculateResummonCost(UUICombineCalc* self, FDatUnitPersonaEntry* persona);

        private string UDatSkill_Instance_SIG = "48 89 05 ?? ?? ?? ?? EB ?? 4C 39 70 ?? 74 ?? 4C 39 70 ?? 74 ?? 4C 39 70 ?? 74 ?? 4C 39 70 ?? 75 ??";
        private unsafe UDatSkill** _datSkill;

        private string UDatPersona_Instance_SIG = "48 89 05 ?? ?? ?? ?? 8B 40 ?? 3B 05 ?? ?? ?? ?? 7D ?? 99 48 31 3D ?? ?? ?? ??";
        private unsafe UDatPersona** _datPersona;

        private string UUICombine_ShowSlBonus_SIG = "41 8B 4E ?? 41 89 86 ?? ?? ?? ?? 83 E9 04";
        private IAsmHook _uuiCombineShowSlBonus;
        private IReverseWrapper<UUICombine_ShowSlBonus> _uuiCombineShowSlBonusWrapper;

        private string APersonaStatus_SetSocialLinkBonus_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9 41 8B F0 0F B7 CA";
        private IHook<APersonaStatus_SetSocialLinkBonus> _setSocialLinkBonus;
        public unsafe delegate void APersonaStatus_SetSocialLinkBonus(APersonaStatus* self, ushort id, int points);

        [Function(FunctionAttribute.Register.r14, FunctionAttribute.Register.rax, false)]
        public unsafe delegate ushort UUICombine_ShowSlBonus(nint uiCombine);

        // FUN_1414804b0

        private SocialLinkManager _manager;
        private CommonHooks _common;

        public unsafe VelvetRoomHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UUICombineCalc_CalculateSocialLinkExpBonus_SIG, "UUICombineCalc::CalculateSocialLinkExpBonus", _context._utils.GetDirectAddress,
                addr => _calcSocialLinkBonus = _context._utils.MakeHooker<UUICombineCalc_CalculateSocialLinkExpBonus>(UUICombineCalc_CalculateSocialLinkExpBonusImpl, addr));

            _context._utils.SigScan(FDatUnitPersona_CalculateBonusExp_SIG, "FDatUnitPersona::CalculateBonusExp", _context._utils.GetDirectAddress, addr => _calcBonusExp = _context._utils.MakeWrapper<FDatUnitPersona_CalculateBonusExp>(addr));

            _context._utils.SigScan(UUICombineCalc_CalculateResummonCost_SIG, "UUICombineCalc::CalculateResummonCost", _context._utils.GetDirectAddress,
                addr => _calcResummonCost = _context._utils.MakeHooker<UUICombineCalc_CalculateResummonCost>(UUICombineCalc_CalculateResummonCostImpl, addr));

            _context._utils.SigScan(UDatSkill_Instance_SIG, "UDatSkill::Instance", _context._utils.GetIndirectAddressLong, addr => _datSkill = (UDatSkill**)addr);
            _context._utils.SigScan(UDatPersona_Instance_SIG, "UDatPersona::Instance", _context._utils.GetIndirectAddressLong, addr => _datPersona = (UDatPersona**)addr);

            _context._utils.SigScan(APersonaStatus_SetSocialLinkBonus_SIG, "APersonaStatus::SetSocialLinkBonus", _context._utils.GetDirectAddress,
                addr => _setSocialLinkBonus = _context._utils.MakeHooker<APersonaStatus_SetSocialLinkBonus>(APersonaStatus_SetSocialLinkBonusImpl, addr));

            _context._utils.SigScan(UUICombine_ShowSlBonus_SIG, "UUICombine::ShowSlBonus", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"{_context._utils.PreserveMicrosoftRegisters()}",
                    $"{_context._hooks.Utilities.GetAbsoluteCallMnemonics(UUICombine_ShowSlBonusImpl, out _uuiCombineShowSlBonusWrapper)}",
                    $"{_context._utils.RetrieveMicrosoftRegisters()}",
                };
                _uuiCombineShowSlBonus = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });
        }
        public override void Register()
        {
            _common = GetModule<CommonHooks>();
            _manager = GetModule<SocialLinkManager>();
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x4)]
        public unsafe struct FCommunityRankItem
        {
            [FieldOffset(0x0000)] public ushort Bonus;
            [FieldOffset(0x0002)] public ushort HighBonus;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x40)]
        public unsafe struct UCommunityRankDataAsset
        {
            //[FieldOffset(0x0000)] public UAppDataAsset baseObj;
            [FieldOffset(0x0030)] public TArray<FCommunityRankItem> Data;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x28)]
        public unsafe struct UUICombineCalcFunction
        {

        }

        [StructLayout(LayoutKind.Explicit, Size = 0xC0)]
        public unsafe struct UUICombineCalc
        {
            [FieldOffset(0x0000)] public UObject baseObj;
            [FieldOffset(0x0028)] public UAssetLoader* Loader_;
            //[FieldOffset(0x0030)] public UNormalSpreadDataAsset* NormalSpread_;
            //[FieldOffset(0x0038)] public USpecialSpreadDataAsset* SpecialSpread_;
            //[FieldOffset(0x0040)] public UPersonaLiftDataAsset* PersonaLift_;
            //[FieldOffset(0x0048)] public USkillAffinityDataAsset* SkillAffinity_;
            //[FieldOffset(0x0050)] public USkillLimitDataAsset* SkillLimit_;
            //[FieldOffset(0x0058)] public UPersonaConfigDataAsset* PersonaConfig_;
            [FieldOffset(0x0060)] public UCommunityRankDataAsset* CommunityRank_;
            //[FieldOffset(0x0068)] public UMoonAgeProbabilityDataAsset* MoonAgeProbability_;
            //[FieldOffset(0x0070)] public UCombineCounterDataAsset* CombineCounter_;
            //[FieldOffset(0x0078)] public USkillChangeDataAsset* SkillChange_;
            //[FieldOffset(0x0080)] public USkillPackDataAsset* SkillPack_;
            //[FieldOffset(0x0088)] public USkillPowerUpDataAsset* SkillPowerUp_;
            //[FieldOffset(0x0090)] public UCombineMiscDataAsset* CombineMisc_;
            [FieldOffset(0x0098)] public UObject* BPCombineCalc_;
            [FieldOffset(0x00A0)] public UUICombineCalcFunction* CombineCalcFunction_;
            //[FieldOffset(0x00A8)] public UDLCPersonaCombineBirthDataAsset* DLCPersonaCombineBirth_;
            //[FieldOffset(0x00B0)] public UWordSortDataAsset* WordSortDataAsset_;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x40)]
        public unsafe struct UDatSkillTable
        {
            [FieldOffset(0x0000)] public UDataAsset baseObj;
            [FieldOffset(0x0030)] public TArray<FDatSkillTableRecord> Data;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x50)]
        public unsafe struct UDatSkill
        {
            [FieldOffset(0x0000)] public UObject baseObj;
            [FieldOffset(0x0028)] public UAssetLoader* Loader;
            [FieldOffset(0x0030)] public UDataAsset* TableName;
            [FieldOffset(0x0038)] public UDatSkillTable* TableSkill;
            [FieldOffset(0x0040)] public UDataAsset* TableNormal;
            [FieldOffset(0x0048)] public UDataAsset* TableAttrName;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x3)]
        public unsafe struct FDatSkillTableRecord
        {
            [FieldOffset(0x0000)] public sbyte attr;
            [FieldOffset(0x0001)] public sbyte Type;
            [FieldOffset(0x0002)] public sbyte targetLv;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0xE)]
        public unsafe struct FDatPersonaDataRecord
        {
            [FieldOffset(0x0000)] public ushort flag;
            [FieldOffset(0x0002)] public byte Race;
            [FieldOffset(0x0003)] public byte Level;
            [FieldOffset(0x0004)] public byte Params;
            [FieldOffset(0x0009)] public byte breakage;
            [FieldOffset(0x000A)] public ushort succession;
            [FieldOffset(0x000C)] public byte conception;
            [FieldOffset(0x000D)] public byte Message;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x40)]
        public unsafe struct UDatPersonaTable
        {
            [FieldOffset(0x0000)] public UDataAsset baseObj;
            [FieldOffset(0x0030)] public TArray<FDatPersonaDataRecord> Data;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x58)]
        public unsafe struct UDatPersona
        {
            [FieldOffset(0x0000)] public UObject baseObj;
            [FieldOffset(0x0028)] public UAssetLoader* Loader;
            [FieldOffset(0x0030)] public UDatPersonaTable* TablePersona;
            [FieldOffset(0x0038)] public UDataAsset* TableAttr;
            [FieldOffset(0x0040)] public UDataAsset* TableName;
            [FieldOffset(0x0048)] public UDataAsset* TableGrowth;
            [FieldOffset(0x0050)] public UDataAsset* TableAllyGrowth;
        }

        private unsafe byte UDatPersona_GetPersonaArcana(ushort id) // FUN_1411ab6e0
            => (*_datPersona)->TablePersona->Data.allocator_instance[id].Race;

        // For the purposes of fusion, all social links belonging to a target arcana are treated as one, with the arcana's rank value
        // being the SL with the highest rank (note that elsewhere, two SLs of the same arcana are tracked separately, so we can't just
        // hook CmmGetRank)
        private unsafe int GetArcanaRankForVelvetRoom(UCommunityHandler* handle, int arcanaId)
        {
            var targetRank = (arcanaId <= SocialLinkManager.vanillaCmmLimit) ? _manager.UCommunityHandler_GetCmmEntryImpl(handle, arcanaId)->entry->Rank : 0;
            if (_manager.ArcanaIdToNewSL.TryGetValue((SocialLinkArcana)arcanaId, out var customSlsForArcana))
            {
                foreach (var customSlForArcana in customSlsForArcana)
                    targetRank = 8; // TODO: set it to the proper rank for the custom SL
            }
            return targetRank;
        }

        public unsafe long UUICombineCalc_CalculateSocialLinkExpBonusImpl(UUICombineCalc* self, FDatUnitPersonaEntry* persona)
        {
            var cmmHandle = _common._getUGlobalWork()->pCommunityWork->pCommunityHandle;
            if (cmmHandle != null)
            {
                var arcanaRank = GetArcanaRankForVelvetRoom(cmmHandle, UDatPersona_GetPersonaArcana(persona->Id));
                var expBonusDegree = _common._getUGlobalWork()->GetBitflag(0x3000002e) // BTL_SHUFFLE_LOVERS_BONUS
                        ? self->CommunityRank_->Data.allocator_instance[arcanaRank].HighBonus
                        : self->CommunityRank_->Data.allocator_instance[arcanaRank].Bonus;
                return _calcBonusExp.Invoke(persona, expBonusDegree);
            }
            return 0;
        }
        public unsafe long UUICombineCalc_CalculateResummonCostImpl(UUICombineCalc* self, FDatUnitPersonaEntry* persona)
        {
            if (self->CombineCalcFunction_ != null && *_datSkill != null)
            {
                var maxSkillLevel = 0;
                for (int i = 0; i < 8; i++)
                {
                    var currSkillLevel = (*_datSkill)->TableSkill->Data.allocator_instance[persona->GetSkill(i)].targetLv;
                    if (currSkillLevel > maxSkillLevel) maxSkillLevel = currSkillLevel;
                }
                var cmmHandle = _common._getUGlobalWork()->pCommunityWork->pCommunityHandle;
                var personaArcana = UDatPersona_GetPersonaArcana(persona->Id);
                var arcanaRank = (personaArcana <= SocialLinkManager.vanillaCmmLimit) ? _manager.UCommunityHandler_GetCmmEntryImpl(cmmHandle, personaArcana)->entry->Rank : 0;
                return _context._objectMethods.ProcessEvent<int>((UObject*)self->CombineCalcFunction_, "GetBookDrawOut",
                    ProcessEventParameterFactory.MakeIntParameter("power", persona->GetParamTotal(0)),
                    ProcessEventParameterFactory.MakeIntParameter("Magic", persona->GetParamTotal(1)),
                    ProcessEventParameterFactory.MakeIntParameter("Endurance", persona->GetParamTotal(2)),
                    ProcessEventParameterFactory.MakeIntParameter("Quick", persona->GetParamTotal(3)),
                    ProcessEventParameterFactory.MakeIntParameter("Luck", persona->GetParamTotal(4)),
                    ProcessEventParameterFactory.MakeIntParameter("CommuLevel", arcanaRank),
                    ProcessEventParameterFactory.MakeIntParameter("MaxSkillLevel", maxSkillLevel)
                );
            }
            return 0;
        }
        public unsafe ushort UUICombine_ShowSlBonusImpl(nint uiCombine)
        {
            var cmbCurrPersona = (FDatUnitPersonaEntry*)(uiCombine + 0x1c4);
            var cmbCurrPersonaArcana = UDatPersona_GetPersonaArcana(cmbCurrPersona->Id);
            var cmmHandle = _common._getUGlobalWork()->pCommunityWork->pCommunityHandle;
            return (ushort)GetArcanaRankForVelvetRoom(cmmHandle, cmbCurrPersonaArcana);
        }
        public unsafe void APersonaStatus_SetSocialLinkBonusImpl(APersonaStatus* self, ushort id, int points)
        { // FUN_1414804b0
            var personaStatusDraw = self->pPersonaStatusDraw;
            var cmmHandler = _common._getUGlobalWork()->pCommunityWork->pCommunityHandle;
            personaStatusDraw->ArcanaRank = GetArcanaRankForVelvetRoom(_common._getUGlobalWork()->pCommunityWork->pCommunityHandle, UDatPersona_GetPersonaArcana(id));
            personaStatusDraw->ExpBonusPoints = points;
        }
    }
}
