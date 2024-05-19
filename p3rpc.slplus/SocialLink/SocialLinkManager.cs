using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Hooking;
using p3rpc.slplus.Parsing;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static p3rpc.slplus.Modules.Core;

namespace p3rpc.slplus.SocialLink
{
    public class SocialLinkManager : ModuleBase<SocialLinkContext>
    {
        public static readonly string SL_YAML_PATH = "Social";
        public static readonly int vanillaCmmLimit = 0x16;

        private int FirstFreeCmmIndex = vanillaCmmLimit + 1;
        private Dictionary<int, SocialLinkModel> activeSocialLinks = new();
        //private Dictionary<int, int> slHashToCmmIndex; // starting at 0x17
        private Dictionary<int, int> cmmIndexToSlHash = new();
        private Dictionary<int, nint> cmmIndexToCmmPtr = new(); // TODO: use key as hash, this is just for testing for now

        public Dictionary<int, uint> CmmIdToNameChangeBitflag;
        public Dictionary<int, uint> CmmIdToReverseBitflag;

        private CommonHooks _common;
        private SocialLinkUtilities _utils;

        private string UCommunityHandler_GetCmmEntry_SIG = "E8 ?? ?? ?? ?? 48 63 AE ?? ?? ?? ?? 48 89 44 24 ??";
        private IHook<UCommunityHandler_GetCmmEntry> _getCmmEntry;
        public unsafe delegate CmmPtr* UCommunityHandler_GetCmmEntry(UCommunityHandler* self, int id);

        private string UCommunityHandler_GetSocialLinkNames_SIG = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC E0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 8B 7A ??";
        private IHook<UCommunityHandler_GetSocialLinkNames> _getSocialLinkNames;
        public unsafe delegate void UCommunityHandler_GetSocialLinkNames(UCommunityHandler* self, TArray<FCommunityFormattedName>* nameOut, int id);

        private string UCommunityHandler_ConvertToCommunityFormat_SIG = "40 55 41 54 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC F0 01 00 00";
        //private IHook<UCommunityHandler_ConvertToCommunityFormat> _convertToCommunityFormat;
        private UCommunityHandler_ConvertToCommunityFormat _convertToCommunityFormat;
        public unsafe delegate byte UCommunityHandler_ConvertToCommunityFormat(UCommunityHandler* self, TArray<FCommunityFormattedName>* nameOut, int id, int mdlId, uint bitflag, FName nameGot);

        private string UCommunityHandler_GetCommunityNameFromId_SIG = "48 89 5C 24 ?? 57 48 83 EC 30 0F B6 FA 48 8B D9 E8 ?? ?? ?? ?? 45 33 C0";
        private IHook<UCommunityHandler_GetCommunityNameFromId> _getCmmNameFromId;
        public unsafe delegate void UCommunityHandler_GetCommunityNameFromId(FString* nameOut, byte id);

        private string UCommunityHandler_CmmCheckReverse_SIG = "48 89 5C 24 ?? 55 48 8D 6C 24 ?? 48 81 EC A0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 ?? 0F B7 01";
        private IHook<UCommunityHandler_CmmCheckReverse> _cmmCheckReverse;
        public unsafe delegate byte UCommunityHandler_CmmCheckReverse(CmmPtr* cmm);

        private unsafe FString* getNameTest = null;
        public unsafe SocialLinkManager(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UCommunityHandler_GetCmmEntry_SIG, "UCommunityHandler::GetCmmEntry", _context._utils.GetIndirectAddressShort,
                addr => _getCmmEntry = _context._utils.MakeHooker<UCommunityHandler_GetCmmEntry>(UCommunityHandler_GetCmmEntryImpl, addr));
            _context._utils.SigScan(UCommunityHandler_GetSocialLinkNames_SIG, "UCommunityHandler::GetSocialLinkNames", _context._utils.GetDirectAddress,
                addr => _getSocialLinkNames = _context._utils.MakeHooker<UCommunityHandler_GetSocialLinkNames>(UCommunityHandler_GetSocialLinkNamesImpl, addr));
            //_context._utils.SigScan(UCommunityHandler_ConvertToCommunityFormat_SIG, "UCommunityHandler::ConvertToCommunityFormat", _context._utils.GetDirectAddress,
            //    addr => _convertToCommunityFormat = _context._utils.MakeHooker<UCommunityHandler_ConvertToCommunityFormat>(UCommunityHandler_ConvertToCommunityFormatImpl, addr));
            _context._utils.SigScan(UCommunityHandler_ConvertToCommunityFormat_SIG, "UCommunityHandler::ConvertToCommunityFormat", _context._utils.GetDirectAddress,
                addr => _convertToCommunityFormat = _context._utils.MakeWrapper<UCommunityHandler_ConvertToCommunityFormat>(addr));
            //_context._utils.SigScan(UCommunityHandler_GetCommunityNameFromId_SIG, "UCommunityHandler::GetCommunityNameFromId", _context._utils.GetDirectAddress,
            //    addr => _getCmmNameFromId = _context._utils.MakeHooker<UCommunityHandler_GetCommunityNameFromId>(UCommunityHandler_GetCommunityNameFromIdImpl, addr));
            //_context._utils.SigScan(UCommunityHandler_CmmCheckReverse_SIG, "UCommunityHandler::CmmCheckReverse", _context._utils.GetDirectAddress,
            //    addr => _cmmCheckReverse = _context._utils.MakeHooker<UCommunityHandler_CmmCheckReverse>(UCommunityHandler_CmmCheckReverseImpl, addr));

            CmmIdToNameChangeBitflag = new()
            {
                { 13,  0x1000024b }, // CMM_12HANGEDMAN__NAME_MAIKO
                { 16,  0x1000027c }, // CMM_15DEVIL______NAME_TANAKA
                { 17,  0x10000289 }, // CMM_16TOWER______NAME_MUTATSU
                { 19,  0x100002ab }, // CMM_18MOON_______NAME_SUEMITSU
                { 20,  0x100002b6 }, // CMM_19SUN________NAME_KAMIKI
            };

            CmmIdToReverseBitflag = new()
            {
                { 01, 0x10000040 }, // CMM_00FOOL_______REVERSE
                { 02, 0x10000041 }, // CMM_01MAGICIAN___REVERSE
                { 03, 0x10000042 }, // CMM_02POPESS_____REVERSE
                { 04, 0x10000043 }, // CMM_03EMPRESS____REVERSE
                { 05, 0x10000044 }, // CMM_04EMPEROR____REVERSE
                { 06, 0x10000045 }, // CMM_05HIEROPHANT_REVERSE
                { 07, 0x10000046 }, // CMM_06LOVERS_____REVERSE
                { 08, 0x10000047 }, // CMM_07CHARIOT____REVERSE
                { 09, 0x10000048 }, // CMM_08JUSTICE____REVERSE
                { 10, 0x10000049 }, // CMM_09HERMIT_____REVERSE
                { 11, 0x1000004a }, // CMM_10WOFORTUNE__REVERSE
                { 12, 0x1000004b }, // CMM_11STRENGTH___REVERSE
                { 13, 0x1000004c }, // CMM_12HANGEDMAN__REVERSE
                { 14, 0x1000004d }, // CMM_13DEATH______REVERSE
                { 15, 0x1000004e }, // CMM_14TEMPERANCE_REVERSE
                { 16, 0x1000004f }, // CMM_15DEVIL______REVERSE
                { 17, 0x10000050 }, // CMM_16TOWER______REVERSE
                { 18, 0x10000051 }, // CMM_17STAR_______REVERSE
                { 19, 0x10000052 }, // CMM_18MOON_______REVERSE
                { 20, 0x10000053 }, // CMM_19SUN________REVERSE
                { 21, 0x10000054 }, // CMM_20JUDGEMENT__REVERSE
                { 22, 0x10000055 }, // CMM_21WORLD______REVERSE
                { 23, 0x10000056 }, // CMM_20THEAEON____REVERSE
                { 24, 0x10000057 }, // CMM_21UNIVERSE___REVERSE
            };
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x40)]
        public unsafe struct FCommunityNameFormat
        {
            //[FieldOffset(0x0000)] public FTableRowBase baseObj;
            [FieldOffset(0x0008)] public FName CommunityName;
            [FieldOffset(0x0010)] public FName CampDispCommunityCharacterNameA;
            [FieldOffset(0x0018)] public FName CampDispCommunityCharacterNameB;
            [FieldOffset(0x0020)] public FName NPCFirstNameA;
            [FieldOffset(0x0028)] public FName NPCLastNameA;
            [FieldOffset(0x0030)] public FName NPCFirstNameB;
            [FieldOffset(0x0038)] public FName NPCLastNameB;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x8)]
        public unsafe struct FCommunityMemberFormatEntry
        {
            [FieldOffset(0x0)] public int PcId;
            [FieldOffset(0x4)] public uint Flag;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x58)]
        public unsafe struct FCommunityMemberFormat
        {
            //[FieldOffset(0x0000)] public FTableRowBase baseObj;
            [FieldOffset(0x0008)] public int PCID1;
            [FieldOffset(0x000C)] public uint Flag1;
            [FieldOffset(0x0010)] public int PCID2;
            [FieldOffset(0x0014)] public uint Flag2;
            [FieldOffset(0x0018)] public int PCID3;
            [FieldOffset(0x001C)] public uint Flag3;
            [FieldOffset(0x0020)] public int PCID4;
            [FieldOffset(0x0024)] public uint Flag4;
            [FieldOffset(0x0028)] public int PCID5;
            [FieldOffset(0x002C)] public uint Flag5;
            [FieldOffset(0x0030)] public int PCID6;
            [FieldOffset(0x0034)] public uint Flag6;
            [FieldOffset(0x0038)] public int PCID7;
            [FieldOffset(0x003C)] public uint Flag7;
            [FieldOffset(0x0040)] public int PCID8;
            [FieldOffset(0x0044)] public uint Flag8;
            [FieldOffset(0x0048)] public int PCID9;
            [FieldOffset(0x004C)] public uint Flag9;
            [FieldOffset(0x0050)] public int PCID10;
            [FieldOffset(0x0054)] public uint Flag10;

            public FCommunityMemberFormatEntry* GetEntry(int id)
            {
                if (id < 1 || id > 10) return null;
                fixed (FCommunityMemberFormat* self = &this) { return &((FCommunityMemberFormatEntry*)self)[id]; }
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x20)]
        public unsafe struct FCommunityFormattedName
        {
            [FieldOffset(0x0)] public int ModelId;
            [FieldOffset(0x4)] public int Flag;
            [FieldOffset(0x8)] public FString name;
            [FieldOffset(0x18)] public int state;
        }
        public override void Register()
        {
            _common = GetModule<CommonHooks>();
            _utils = GetModule<SocialLinkUtilities>();
        }

        // Social Link registry on startup (may move this to another file at a later point?)
        public void RegisterSocialLink(int key, SocialLinkModel newSl)
        {
            activeSocialLinks.Add(key, newSl);
            cmmIndexToSlHash.Add(FirstFreeCmmIndex, key);
            _context._utils.Log($"Registered new social link \"{newSl.NameKnown}\" (ID {FirstFreeCmmIndex}, key 0x{key:X})");
            FirstFreeCmmIndex++;
        }

        public void OnModLoaded(string modPath, string modId)
        {
            var slplusPath = Path.Combine(modPath, SL_YAML_PATH);
            if (!Path.Exists(slplusPath)) return;
            _context._utils.Log($"Loaded mod with SL file at {slplusPath}");
            var slFiles = Directory.GetFiles(slplusPath).Where(x => Constants.YAML_EXTENSION.Contains(Path.GetExtension(x).Substring(1)));
            foreach (var slFile in slFiles)
            {
                var slId = $"{modId}.{Path.GetFileNameWithoutExtension(slFile)}";
                var slHash = BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(slId)));
                var newSl = YamlSerializer.deserializer.Deserialize<SocialLinkModel>(new StreamReader(slFile));
                RegisterSocialLink(slHash, newSl);
            }
        }

        // We have to lazy load this so we know that Unreal's memory allocator is active
        // Don't use this method in init code
        private unsafe CmmPtr* AddNativeCmmPtrData(int cmmIndex)
        {
            CmmPtr* newCmm = _context._memoryMethods.FMemory_Malloc<CmmPtr>();
            newCmm->entry = _context._memoryMethods.FMemory_Malloc<CmmEntry>();
            newCmm->entry->Rank = 1;
            newCmm->entry->Points = 0;
            newCmm->ArcanaId = (ushort)cmmIndex;
            cmmIndexToCmmPtr.Add(cmmIndex, (nint)newCmm);
            return newCmm;
        }

        public unsafe CmmPtr* UCommunityHandler_GetCmmEntryImpl(UCommunityHandler* self, int id)
        {
            if (id <= vanillaCmmLimit) return _getCmmEntry.OriginalFunction(self, id);
            if (!cmmIndexToCmmPtr.TryGetValue(id, out nint gotCmm))
                return AddNativeCmmPtrData(id);
            return (CmmPtr*)gotCmm;
        }

        public unsafe void UCommunityHandler_GetSocialLinkNamesImpl(UCommunityHandler* self, TArray<FCommunityFormattedName>* nameOut, int id)
        {
            // free any existing name entries if they exist (not sure if this is actually needed, but just to be safe)
            _context._utils.Log($"GetSocialLinkNamesImpl: called on cmm id {id}");
            for (int i = 0; i < nameOut->arr_num; i++)
            {
                FCommunityFormattedName* currCmmName = &nameOut->allocator_instance[i];
                var currCmmNameIn = currCmmName->name.text.allocator_instance;
                if (currCmmNameIn != null) _context._memoryMethods.FMemory_Free(currCmmNameIn);
            }
            _context._utils.Log($"GetSocialLinkNamesImpl: freed");
            nameOut->arr_num = 0;
            if (nameOut->arr_max < 10) // resize to 10
                _context._memoryMethods.FMemory_Realloc((nint)nameOut->allocator_instance, sizeof(FCommunityFormattedName) * 10, 8);
            _context._utils.Log($"GetSocialLinkNamesImpl: alloc'd");
            if (id <= vanillaCmmLimit)
            {
                var pCurrMemberFmt = (FCommunityMemberFormat*)self->pMemberFormatTable->RowMap.elements[id].Value;
                var pCurrNameFmt = (FCommunityNameFormat*)self->pNameFormatTable->RowMap.elements[id].Value;
                var nameGot = pCurrNameFmt->CampDispCommunityCharacterNameB;
                if (CmmIdToNameChangeBitflag.TryGetValue(id, out uint bitflag) && !_common._getUGlobalWork()->GetBitflag(bitflag))
                    nameGot = pCurrNameFmt->CampDispCommunityCharacterNameA;
                //_context._utils.Log(_context._objectMethods.GetFName(nameGot));
                for (int i = 1; i <= 10; i++)
                    if (_convertToCommunityFormat.Invoke(
                        self, nameOut, id, pCurrMemberFmt->GetEntry(i)->PcId,
                        pCurrMemberFmt->GetEntry(i)->Flag, nameGot) == 0)
                        break;
                _context._utils.Log($"GetSocialLinkNamesImpl: {nameOut->arr_num} entries");
            } /*else
            {
                if (getNameTest == null)
                    getNameTest = _utils.MakeFStringRef("Saori Hasegawa");
                FCommunityFormattedName* fakeCmmName = _context._memoryMethods.FMemory_Malloc<FCommunityFormattedName>();
                fakeCmmName->ModelId = 0;
                fakeCmmName->Flag = 0;
                fakeCmmName->name = *getNameTest;
                fakeCmmName->state = 0;
            }
            */
            // TODO: implement this for new SL entries
        }

        public unsafe void UCommunityHandler_GetCommunityNameFromIdImpl(FString* nameOut, byte id)
        {
            UCommunityHandler* cmmHandle = _common._getUGlobalWork()->pCommunityWork->pCommunityHandle;
            if (id <= vanillaCmmLimit)
            {
                var cmmNameFmt = (FCommunityNameFormat*)cmmHandle->pNameFormatTable->RowMap.elements[id].Value;
                _utils.MakeFStringFromExisting(nameOut, _context._objectMethods.GetFName(cmmNameFmt->CommunityName));
            } else _utils.MakeFStringFromExisting(nameOut, $"CMM {id}");
            _context._utils.Log($"{nameOut->ToString()}");
        }

        public unsafe byte UCommunityHandler_CmmCheckReverseImpl(CmmPtr* cmm)
            => (CmmIdToReverseBitflag.TryGetValue(cmm->ArcanaId, out uint bitflag) && _common._getUGlobalWork()->GetBitflag(bitflag)) ? (byte)1 : (byte)0;

        //public unsafe byte UCommunityHandler_ConvertToCommunityFormatImpl(UCommunityHandler* self, TArray<FCommunityFormattedName>* nameOut, int id, int mdlId, uint bitflag, FName nameGot)
        //    => _convertToCommunityFormat.OriginalFunction(self, nameOut, id, mdlId, bitflag, nameGot);
    }
}
