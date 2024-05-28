using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Hooking;
using p3rpc.slplus.Parsing;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static p3rpc.slplus.SocialLink.CampMenuHooks;

namespace p3rpc.slplus.SocialLink
{
    public class SocialLinkManager : ModuleBase<SocialLinkContext>
    {
        public static readonly string SL_YAML_PATH = "Social";
        public static readonly int vanillaCmmLimit = 0x16;

        private int FirstFreeCmmIndex = vanillaCmmLimit + 1;
        public Dictionary<int, SocialLinkModel> activeSocialLinks = new();
        //private Dictionary<int, int> slHashToCmmIndex; // starting at 0x17
        public Dictionary<int, int> cmmIndexToSlHash = new();
        private Dictionary<int, nint> cmmIndexToCmmPtr = new(); // TODO: use key as hash, this is just for testing for now

        public Dictionary<int, uint> CmmIdToNameChangeBitflag;

        public Dictionary<SocialLinkArcana, List<SocialLinkModel>> ArcanaIdToNewSL = new();

        private CommonHooks _common;
        private SocialLinkUtilities _utils;
        private AssetLoader _assetLoader;

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

        private string UCommunityHandler_CmmCheckRomance_SIG = "48 89 5C 24 ?? 55 48 8D 6C 24 ?? 48 81 EC A0 00 00 00 48 8B 05 ?? ?? ?? ?? 48 31 E0";
        private IHook<UCommunityHandler_CmmCheckRomance> _cmmCheckRomance;
        public unsafe delegate byte UCommunityHandler_CmmCheckRomance(CmmPtr* cmm);

        private string UCmpCommu_Init_SIG = "48 89 5C 24 ?? 48 89 54 24 ?? 48 89 4C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC B0 00 00 00 BF 1C 00 00 00";
        private IHook<UCmpCommu_Init> _commuInit;
        public unsafe delegate void UCmpCommu_Init(UCmpCommu* self, UAssetLoader* loader);

        public unsafe SocialLinkManager(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            //_context._utils.SigScan(UCommunityHandler_ConvertToCommunityFormat_SIG, "UCommunityHandler::ConvertToCommunityFormat", _context._utils.GetDirectAddress,
            //    addr => _convertToCommunityFormat = _context._utils.MakeHooker<UCommunityHandler_ConvertToCommunityFormat>(UCommunityHandler_ConvertToCommunityFormatImpl, addr));

            _context._utils.SigScan(UCommunityHandler_GetCmmEntry_SIG, "UCommunityHandler::GetCmmEntry", _context._utils.GetIndirectAddressShort,
                addr => _getCmmEntry = _context._utils.MakeHooker<UCommunityHandler_GetCmmEntry>(UCommunityHandler_GetCmmEntryImpl, addr));
            _context._utils.SigScan(UCommunityHandler_GetSocialLinkNames_SIG, "UCommunityHandler::GetSocialLinkNames", _context._utils.GetDirectAddress,
                addr => _getSocialLinkNames = _context._utils.MakeHooker<UCommunityHandler_GetSocialLinkNames>(UCommunityHandler_GetSocialLinkNamesImpl, addr));
            _context._utils.SigScan(UCommunityHandler_ConvertToCommunityFormat_SIG, "UCommunityHandler::ConvertToCommunityFormat", _context._utils.GetDirectAddress,
                addr => _convertToCommunityFormat = _context._utils.MakeWrapper<UCommunityHandler_ConvertToCommunityFormat>(addr));
            _context._utils.SigScan(UCommunityHandler_GetCommunityNameFromId_SIG, "UCommunityHandler::GetCommunityNameFromId", _context._utils.GetDirectAddress,
                addr => _getCmmNameFromId = _context._utils.MakeHooker<UCommunityHandler_GetCommunityNameFromId>(UCommunityHandler_GetCommunityNameFromIdImpl, addr));
            _context._utils.SigScan(UCommunityHandler_CmmCheckReverse_SIG, "UCommunityHandler::CmmCheckReverse", _context._utils.GetDirectAddress,
                addr => _cmmCheckReverse = _context._utils.MakeHooker<UCommunityHandler_CmmCheckReverse>(UCommunityHandler_CmmCheckReverseImpl, addr));
            _context._utils.SigScan(UCommunityHandler_CmmCheckRomance_SIG, "UCommunityHandler::CmmCheckRomance", _context._utils.GetDirectAddress,
                addr => _cmmCheckRomance = _context._utils.MakeHooker<UCommunityHandler_CmmCheckRomance>(UCommunityHandler_CmmCheckRomanceImpl, addr));
            _context._utils.SigScan(UCmpCommu_Init_SIG, "UCmpCommu::Init", _context._utils.GetDirectAddress,
                addr => _commuInit = _context._utils.MakeHooker<UCmpCommu_Init>(UCmpCommu_InitImpl, addr));
            
            

            CmmIdToNameChangeBitflag = new()
            {
                { 13,  0x1000024b }, // CMM_12HANGEDMAN__NAME_MAIKO
                { 16,  0x1000027c }, // CMM_15DEVIL______NAME_TANAKA
                { 17,  0x10000289 }, // CMM_16TOWER______NAME_MUTATSU
                { 19,  0x100002ab }, // CMM_18MOON_______NAME_SUEMITSU
                { 20,  0x100002b6 }, // CMM_19SUN________NAME_KAMIKI
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

        [StructLayout(LayoutKind.Explicit, Size = 0x28)]
        public unsafe struct UCmpCommuExtendedEntry
        {
            [FieldOffset(0x0)] public UTexture2D* BustupTex;
            [FieldOffset(0x8)] public UTexture2D* HeaderTex;
            [FieldOffset(0x10)] public USprAsset* HeaderSpr;
            [FieldOffset(0x18)] public UBmdAsset* OutlineBmd;
            [FieldOffset(0x20)] public UBmdAsset* ProfileBmd;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x20)]
        public unsafe struct FCommunityFormattedName
        {
            [FieldOffset(0x0)] public int ModelId;
            [FieldOffset(0x4)] public int Flag;
            [FieldOffset(0x8)] public FString name;
            [FieldOffset(0x18)] public int state;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x18)]
        public unsafe struct CustomCmmData
        {
            [FieldOffset(0x0)] public UTexture2D* RankUpName;
            [FieldOffset(0x8)] public USprAsset* RankUpSpr;
            [FieldOffset(0x10)] public byte bRankUpNameLoading;
        }

        public override void Register()
        {
            _common = GetModule<CommonHooks>();
            _utils = GetModule<SocialLinkUtilities>();
            _assetLoader = GetModule<AssetLoader>();
        }

        // Social Link registry on startup (may move this to another file at a later point?)
        public void RegisterSocialLink(int key, SocialLinkModel newSl)
        {
            activeSocialLinks.Add(key, newSl);
            cmmIndexToSlHash.Add(FirstFreeCmmIndex, key);
            _context._utils.Log($"Registered new social link \"{newSl.NameKnown}\" for Arcana {newSl.Arcana} (ID {FirstFreeCmmIndex}, key 0x{key:X})");
            // add new arcana id -> custom sl
            if (!ArcanaIdToNewSL.ContainsKey(newSl.ArcanaId))
            {
                List<SocialLinkModel> pArcanaId = new() { newSl };
                ArcanaIdToNewSL.Add(newSl.ArcanaId, pArcanaId);
            }
            FirstFreeCmmIndex++;
        }

        private void TryRegisterMessageAssetHook(string assetName, string assetPath)
        {
            if (assetName != null)
            {
                var assetPathFull = Path.Combine(assetPath, $"{assetName}.{Constants.BMD_SOURCE_EXTENSION}");
                if (Path.Exists(assetPathFull))
                {
                    using (StreamReader reader = File.OpenText(assetPathFull))
                    {
                        string str = reader.ReadToEnd();
                        _context._atlusAssets.AddAsset(assetName, str, Unreal.AtlusScript.Interfaces.AssetType.BMD);
                    }
                } else
                {
                    _context._utils.Log($"ERROR: Message asset at {assetPathFull} wasn't found.", System.Drawing.Color.Red, LogLevel.Error);
                }
            }
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
                // deserialize yaml file
                var newSl = YamlSerializer.deserializer.Deserialize<SocialLinkModel>(new StreamReader(slFile));
                // hook message assets
                TryRegisterMessageAssetHook(newSl.CmmOutlineBmd, slplusPath);
                TryRegisterMessageAssetHook(newSl.CmmProfileBmd, slplusPath);
                RegisterSocialLink(slHash, newSl);
            }
            unsafe
            { // Extend UCmpCommu to store our custom bustup data without UE calling GC
                var newCmpCommuSize = (uint)sizeof(UCmpCommu) + (uint)(activeSocialLinks.Count * sizeof(UCmpCommuExtendedEntry));
                _context._utils.Log($"New size of UCmpCommu is 0x{newCmpCommuSize:X}");
                _context._classMethods.AddUnrealClassExtender("CmpCommu", newCmpCommuSize, null);
                // And extend UCommunityWork to fit everything else in
                var newCmmWorkSize = (uint)sizeof(UCommunityWork) + (uint)(activeSocialLinks.Count * sizeof(CustomCmmData));
                _context._utils.Log($"New size of UCommunityWork is 0x{newCmmWorkSize:X}");
                _context._classMethods.AddUnrealClassExtender("CommunityWork", newCmmWorkSize, x =>
                {
                    UCommunityWork* cmmWork = (UCommunityWork*)x;
                    // make sure this is zeroed out
                    // we'll be lazy loading these assets which requires checking that particular fields are null
                    NativeMemory.Clear(cmmWork + 1, (nuint)(activeSocialLinks.Count * sizeof(CustomCmmData)));
                });
                // TODO: Add ability to hook onto existing social links to change target assets for them
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
            if (id <= vanillaCmmLimit) _getSocialLinkNames.OriginalFunction(self, nameOut, id);
            /*
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
            } else
            {
                if (cmmIndexToSlHash.TryGetValue(id, out var slHash) && activeSocialLinks.TryGetValue(slHash, out var customSl))
                    _utils.MakeFStringFromExisting(nameOut, customSl.NameKnown);
                else
                    _utils.MakeFStringFromExisting(nameOut, $"CMM {id}");
            }
        }

        public unsafe byte UCommunityHandler_CmmCheckReverseImpl(CmmPtr* cmm)
        {
            // GetBitflag(CMM_00FOOL_______REVERSE - 1 + cmm->ArcanaId)
            if (cmm->ArcanaId <= vanillaCmmLimit) return _common._getUGlobalWork()->GetBitflag((uint)(cmm->ArcanaId + 0x1000003f)) ? (byte) 1 : (byte)0;
            return 0;
        }

        public unsafe byte UCommunityHandler_CmmCheckRomanceImpl(CmmPtr* cmm)
        {
            if (cmm->ArcanaId <= vanillaCmmLimit) return _cmmCheckRomance.OriginalFunction(cmm);
            return 0;
        }

        //public unsafe byte UCommunityHandler_ConvertToCommunityFormatImpl(UCommunityHandler* self, TArray<FCommunityFormattedName>* nameOut, int id, int mdlId, uint bitflag, FName nameGot)
        //    => _convertToCommunityFormat.OriginalFunction(self, nameOut, id, mdlId, bitflag, nameGot);

        public unsafe USprAsset* MakeCommuHeaderSprite(UTexture2D* headerTex)
        {
            USprAsset* newSpr = (USprAsset*)_context._objectMethods.SpawnObject("SprAsset", _context._objectMethods.GetEngineTransient());
            _context._objectMethods.MarkObjectAsRoot((UObject*)newSpr); // suppress GC
            _context._memoryMethods.TArray_Insert(&newSpr->mTexArray, (nint)headerTex);
            NativeMemory.Clear(&newSpr->SprDatas, 0x50);
            FSprDataArray newSprArr = new FSprDataArray();
            FSprData newSprEntry = new FSprData(512, 128, new FVector2D(0, 0), new FVector2D(1, 1), headerTex, uint.MaxValue, 0, 0);
            _context._memoryMethods.TArray_Insert(&newSprArr.SprDatas, newSprEntry);
            _context._memoryMethods.TMap_Insert(&newSpr->SprDatas, 0, newSprArr);
            _context._utils.Log($"{(nint)newSpr:X}");
            return newSpr;
        }

        public unsafe void UCmpCommu_InitImpl(UCmpCommu* self, UAssetLoader* loader)
        {
            // Load our added resources first so we can piggyback on UAssetLoader::LoadQueuedAssets
            _context._utils.Log($"[UCmpCommu::Init] instance: 0x{(nint)self:X}");
            foreach (var slIdToHash in cmmIndexToSlHash)
            {
                var pCustomCommuBustup = &((UCmpCommuExtendedEntry*)(self + 1))[slIdToHash.Key - vanillaCmmLimit - 1];
                if (activeSocialLinks.TryGetValue(slIdToHash.Value, out var customSl))
                {
                    _context._utils.Log($"[UCmpCommu::Init] {customSl.NameKnown}: 0x{(nint)pCustomCommuBustup:X}");
                    if (customSl.CommuBustup != null)
                    {
                        _assetLoader.LoadAsset(
                            loader, Constants.MakeAssetPath($"{Constants.CampCommuTextures}{customSl.CommuBustup}"),
                            (nint)(&pCustomCommuBustup->BustupTex), _assetLoader.MarkAssetAsRoot);
                    }
                    if (customSl.CommuHeader != null)
                    {
                        _assetLoader.LoadAsset(
                            loader, Constants.MakeAssetPath($"{Constants.CampCommuTextures}{customSl.CommuHeader}"),
                            (nint)(&pCustomCommuBustup->HeaderTex), x =>
                            {
                                _assetLoader.MarkAssetAsRoot(x);
                                UTexture2D* headerTex = *(UTexture2D**)x;
                                pCustomCommuBustup->HeaderSpr = MakeCommuHeaderSprite(headerTex);
                            });
                    }
                    if (customSl.CmmOutlineBmd != null)
                    {
                        _assetLoader.LoadAsset(
                            loader, Constants.MakeAssetPath($"{Constants.CampCommuBmds}{customSl.CmmOutlineBmd}"),
                            (nint)(&pCustomCommuBustup->OutlineBmd), _assetLoader.MarkAssetAsRoot);
                    }
                    if (customSl.CmmProfileBmd != null)
                    {
                        _assetLoader.LoadAsset(
                            loader, Constants.MakeAssetPath($"{Constants.CampCommuBmds}{customSl.CmmProfileBmd}"),
                            (nint)(&pCustomCommuBustup->ProfileBmd), _assetLoader.MarkAssetAsRoot);
                    }
                }
            }
            _commuInit.OriginalFunction(self, loader);
        }
    }
}
