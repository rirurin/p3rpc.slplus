using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Hooking;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;

namespace p3rpc.slplus.SocialLink
{
    public class VelvetRoomHooks : ModuleBase<SocialLinkContext>
    {
        private string UUICombineCalc_CalculateSocialLinkExpBonus_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 57 48 83 EC 20 48 8B EA 48 8B F9 E8 ?? ?? ?? ?? 48 8B D8 48 85 C0";
        private IHook<UUICombineCalc_CalculateSocialLinkExpBonus> _calcSocialLinkBonus;
        public unsafe delegate long UUICombineCalc_CalculateSocialLinkExpBonus(UUICombineCalc* self, FDatUnitPersonaEntry* persona);

        private string FDatUnitPersona_CalculateBonusExp_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 20 0F B7 DA";
        public FDatUnitPersona_CalculateBonusExp _calcBonusExp;
        public unsafe delegate int FDatUnitPersona_CalculateBonusExp(FDatUnitPersonaEntry* persona, ushort bonus);

        private string UDatPersona_GetPersonaArcana_SIG = "E8 ?? ?? ?? ?? 0F B6 D0 48 8B CB E8 ?? ?? ?? ?? 48 85 C0 0F 84 ?? ?? ?? ??";
        public UDatPersona_GetPersonaArcana _getPersonaArcana;
        public unsafe delegate byte UDatPersona_GetPersonaArcana(uint personaId);

        private string UUICombineCalc_CalculateResummonCost_SIG = "40 56 41 54 41 55 48 83 EC 50 48 8B 81 ?? ?? ?? ??";
        private IHook<UUICombineCalc_CalculateResummonCost> _calcResummonCost;
        public unsafe delegate long UUICombineCalc_CalculateResummonCost(UUICombineCalc* self, FDatUnitPersonaEntry* persona);

        // FUN_1414804b0

        private CommonHooks _common;

        public unsafe VelvetRoomHooks(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UUICombineCalc_CalculateSocialLinkExpBonus_SIG, "UUICombineCalc::CalculateSocialLinkExpBonus", _context._utils.GetDirectAddress,
                addr => _calcSocialLinkBonus = _context._utils.MakeHooker<UUICombineCalc_CalculateSocialLinkExpBonus>(UUICombineCalc_CalculateSocialLinkExpBonusImpl, addr));

            _context._utils.SigScan(FDatUnitPersona_CalculateBonusExp_SIG, "FDatUnitPersona::CalculateBonusExp", _context._utils.GetDirectAddress, addr => _calcBonusExp = _context._utils.MakeWrapper<FDatUnitPersona_CalculateBonusExp>(addr));
            _context._utils.SigScan(UDatPersona_GetPersonaArcana_SIG, "UDatPersona::GetPersonaArcana", _context._utils.GetIndirectAddressShort, addr => _getPersonaArcana = _context._utils.MakeWrapper<UDatPersona_GetPersonaArcana>(addr));

            _context._utils.SigScan(UUICombineCalc_CalculateResummonCost_SIG, "UUICombineCalc::CalculateResummonCost", _context._utils.GetDirectAddress,
                addr => _calcResummonCost = _context._utils.MakeHooker<UUICombineCalc_CalculateResummonCost>(UUICombineCalc_CalculateResummonCostImpl, addr));
        }
        public override void Register()
        {
            _common = GetModule<CommonHooks>();
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

        public unsafe long UUICombineCalc_CalculateSocialLinkExpBonusImpl(UUICombineCalc* self, FDatUnitPersonaEntry* persona)
        {
            var cmmHandle = _common._getUGlobalWork()->pCommunityWork->pCommunityHandle;
            if (cmmHandle != null)
            {
                var targetArcana = _getPersonaArcana.Invoke(persona->Id);
                var targetCmmEntry = cmmHandle->GetCmmEntry(targetArcana); // TODO: Target highest level of a particular arcana
                if (targetCmmEntry != null)
                {
                    var expBonusDegree = _common._getUGlobalWork()->GetBitflag(0x3000002e) // BTL_SHUFFLE_LOVERS_BONUS
                        ? self->CommunityRank_->Data.allocator_instance[targetCmmEntry->entry->Rank].HighBonus
                        : self->CommunityRank_->Data.allocator_instance[targetCmmEntry->entry->Rank].Bonus;
                    return _calcBonusExp.Invoke(persona, expBonusDegree);
                }
            }
            return 0;
        }

        public unsafe long UUICombineCalc_CalculateResummonCostImpl(UUICombineCalc* self, FDatUnitPersonaEntry* persona)
        {
            if (self->CombineCalcFunction_ != null)
            {
                //_context._objectMethods.ProcessEvent<int>((UObject*)self->CombineCalcFunction_, "GetBookDrawOut", );
            }
            return 0;
        }
    }
}
