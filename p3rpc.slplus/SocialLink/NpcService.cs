using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Hooking;
using Reloaded.Hooks.Definitions;
using System.Runtime.InteropServices;

namespace p3rpc.slplus.SocialLink
{
    public class NpcService : ModuleBase<SocialLinkContext>
    {
        private string UFldCommonData_LoadSpecialNpcTables_SIG = "4C 8B DC 55 53 57 41 55 49 8D AB ?? ?? ?? ?? 48 81 EC 78 03 00 00";
        private IHook<UFldCommonData_LoadSpecialNpcTables> _loadSpecialNpcTables;
        public unsafe delegate void UFldCommonData_LoadSpecialNpcTables(UFldCommonData* self, UArcAsset* fldBinary); //AF_FldBinaryData

        private CommonHooks _common;
        private SocialLinkUtilities _utils;
        private AssetLoader _assetLoader;

        public delegate bool TryGetKinId(FName kin, out int kinId);

        [StructLayout(LayoutKind.Explicit, Size = 0x38)]
        public unsafe struct FFldNpcNameTableRow
        {
            [FieldOffset(0x0)] public nint vtable;
            //[FieldOffset(0x0000)] public FTableRowBase baseObj;
            [FieldOffset(0x0008)] public FString Name;
            [FieldOffset(0x0018)] public FString flag;
            [FieldOffset(0x0028)] public FString Name2;
        }

        public enum EFldCmmNpcType
        {
            Cmm = 0,
            Normal = 1,
        };

        public enum EFldHitCharaIconType
        {
            None = 0,
            Normal = 1,
            Talk = 2,
            Cmm = 3,
            Cmm_Normal = 4,
            Cmm_Reverse = 5,
            Cmm_Object = 6,
            Quest = 7,
            MaleQuest = 8,
            MaleQuest_Object = 9,
            Dormitory = 10,
            Study = 11,
            Koromaru = 12,
            WORD_13 = 13,
            WORD_14 = 14,
            WORD_15 = 15,
            WORD_16 = 16,
            Max = 17,
            TargetOnly = 18,
            QuestHit = 19,
        };

        [StructLayout(LayoutKind.Explicit, Size = 0xC)]
        public unsafe struct FFldHitCharaIconParam
        {
            [FieldOffset(0x0000)] public FName mFlagName;
            [FieldOffset(0x0008)] public EFldHitCharaIconType mIconType;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0xA0)]
        public unsafe struct FFldCmmNpcLayoutTableRow
        {
            [FieldOffset(0x0)] public nint vtable;
            //[FieldOffset(0x0000)] public FTableRowBase baseObj;
            [FieldOffset(0x0008)] public int FieldMajor;
            [FieldOffset(0x000C)] public int FieldMinor;
            [FieldOffset(0x0010)] public int FieldParts;
            // 1: EarlyMorning, Morning, AM, Noon
            // 2: PM, After School
            // 3: Night, Midnight
            // 4: Shadow
            [FieldOffset(0x0014)] public int TimeType;
            [FieldOffset(0x0018)] public int KeyfreeEventID;
            [FieldOffset(0x001C)] public int UniqueId;
            [FieldOffset(0x0020)] public int ArcanaID;
            [FieldOffset(0x0024)] public EFldCmmNpcType Type;
            [FieldOffset(0x0025)] public EFldHitCharaIconType IconType;
            [FieldOffset(0x0028)] public TArray<FFldHitCharaIconParam> ChangeIcons;
            [FieldOffset(0x0038)] public int NameIndex;
            [FieldOffset(0x003C)] public FName OnFlagName;
            [FieldOffset(0x0044)] public FName OffFlagName;
            [FieldOffset(0x0050)] public TArray<FTransform> CharaTrans;
            [FieldOffset(0x0060)] public FTransform IconTran;
            [FieldOffset(0x0090)] public bool NotMapInfo;
        }
        public unsafe NpcService(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UFldCommonData_LoadSpecialNpcTables_SIG, "UFldCommonData::LoadSpecialNpcTables", _context._utils.GetDirectAddress,
                addr => _loadSpecialNpcTables = _context._utils.MakeHooker<UFldCommonData_LoadSpecialNpcTables>(UFldCommonData_LoadSpecialNpcTablesImpl, addr));
        }
        public override void Register()
        {
            _common = GetModule<CommonHooks>();
            _utils = GetModule<SocialLinkUtilities>();
            _assetLoader = GetModule<AssetLoader>();
        }

        public unsafe void PrintTableSize(UDataTable* dt, string name)
        {
            _context._utils.Log($"[UFldCommonData::LoadSpecialNpcTables] {name} (0x{(nint)dt:X}): {dt->RowMap.mapNum} entries, {dt->RowMap.mapMax} alloc");
        }
        // DT_FldNpcName
        public unsafe bool TryGetKinFldNpcName(FName kin, out int kinId)
        {
            var lastNpcNameKey = _context._objectMethods.GetFName(kin); // KIN_%08d
            var lastNpcNameKeyParts = lastNpcNameKey.Split("_");
            if (
                lastNpcNameKeyParts.Length == 2
                && lastNpcNameKeyParts[0] == "KIN"
                && lastNpcNameKeyParts[1].Length == 8
                && int.TryParse(lastNpcNameKeyParts[1], out kinId))
                return true;
            kinId = -1;
            _context._utils.Log($"[TryGetNextTargetKinFldNpcName] ERROR: Key \"{lastNpcNameKey}\" doesn't match template KIN_%08d (this should not happen - something is wrong with DT_FldNpcName)", System.Drawing.Color.Red, LogLevel.Error);
            return false;
        }
        public string MakeKinFldNpcName(int kinId) => $"KIN_{kinId:D8}";

        // DT_FldLmapNpcLayout, DT_FldLmapCmmNpcLayout
        public unsafe bool TryGetKinFldLmapNpcLayout(FName kin, out int kinId)
        {
            var lastCmmLayoutKey = _context._objectMethods.GetFName(kin); // KIN%05d
            var lastCmmLayoutKeyKin = lastCmmLayoutKey.Substring(0, 3);
            var lastCmmLayoutKeyId = lastCmmLayoutKey.Substring(3);
            if (
                lastCmmLayoutKeyKin == "KIN"
                && int.TryParse(lastCmmLayoutKeyId, out kinId
                ))
                return true;
            kinId = -1;
            _context._utils.Log($"[TryGetNextKinFldLmapCmmNpcLayout] ERROR: Key \"{lastCmmLayoutKey}\" doesn't match template KIN_%08d (this should not happen - something is wrong with DT_FldLmapCmmNpcLayout)", System.Drawing.Color.Red, LogLevel.Error);
            return false;
        }
        public string MakeKinFldLmapNpcLayout(int kinId) => $"KIN{kinId:D5}";
        public unsafe bool TryGetNextKin(UDataTable* tbl, TryGetKinId kinFn, out int nextKinId)
        {
            var lastElement = &tbl->RowMap.elements[tbl->RowMap.mapNum - 1];
            if (!kinFn(lastElement->Key, out nextKinId))
                return false;
            nextKinId++;
            return true;
        }

        // Pass in an existing name table to steal it's vtable
        // we'll need to call Unreal's allocator so it doesn't crash when it tries to free it
        public unsafe FFldNpcNameTableRow* MakeNewNpcNameTblTest(FFldNpcNameTableRow* existing)
        {
            var newNpcNameTable = _context._memoryMethods.FMemory_Malloc<FFldNpcNameTableRow>();
            NativeMemory.Clear(newNpcNameTable, (nuint)sizeof(FFldNpcNameTableRow));
            newNpcNameTable->vtable = existing->vtable;
            newNpcNameTable->Name = _utils.MakeFString("Saori Hasegawa");
            return newNpcNameTable;
        }

        public unsafe FFldCmmNpcLayoutTableRow* MakeNewCmmNpcLayoutTableRow(FFldCmmNpcLayoutTableRow* existing)
        {
            var newEntry = _context._memoryMethods.FMemory_Malloc<FFldCmmNpcLayoutTableRow>();
            NativeMemory.Clear(newEntry, (nuint)sizeof(FFldCmmNpcLayoutTableRow));
            newEntry->vtable = existing->vtable;
            newEntry->FieldMajor = 103; // Paulownia Mall
            //newEntry->FieldMajor = 101; // Gekkokuan High School
            //newEntry->FieldMajor = 105; // Iwatodai
            newEntry->FieldMinor = 101;
            //newEntry->FieldMinor = 102;
            newEntry->FieldParts = 1;
            newEntry->TimeType = 3;
            newEntry->KeyfreeEventID = 0;
            newEntry->UniqueId = 15;
            newEntry->ArcanaID = 2;
            newEntry->Type = EFldCmmNpcType.Cmm;
            newEntry->IconType = EFldHitCharaIconType.Cmm;
            newEntry->ChangeIcons = new TArray<FFldHitCharaIconParam>();
            newEntry->NameIndex = -1;
            newEntry->OnFlagName = _context._objectMethods.GetFName("CMM_15DEVIL______PRE_OPEN");
            newEntry->OffFlagName = _context._objectMethods.GetFName("CMM_15DEVIL______MAX");
            newEntry->CharaTrans = new TArray<FTransform>();
            FTransform charaTransEntry = new FTransform(
                new FVector4(0, 0, 0.7071057f, 1.707108f),
                new FVector(180, 310, -5),
                new FVector(2, 2, 2)
                );
            _context._memoryMethods.TArray_Insert(&newEntry->CharaTrans, charaTransEntry);
            newEntry->IconTran = new FTransform(
                new FVector4(0, 0, 0.7071057f, 1.707108f),
                new FVector(180, 310, -5),
                new FVector(2, 2, 2)
                );
            newEntry->NotMapInfo = false;
            return newEntry;
        }

        public unsafe void UFldCommonData_LoadSpecialNpcTablesImpl(UFldCommonData* self, UArcAsset* fldBinary)
        {
            var fldNpcName = self->GetDataTable(0xa);
            PrintTableSize(fldNpcName, "DT_FldNpcName");
            /*
            PrintTableSize(self->GetDataTable(0x8), "DT_FldDailyHitName");
            PrintTableSize(self->GetDataTable(0xa), "DT_FldNpcName");
            PrintTableSize(self->GetDataTable(0xf), "DT_FldLmapCmmNpcLayout");
            if (TryGetNextKin(self->GetDataTable(0xa), TryGetKinFldNpcName, out int newNpcKeyId))
                _context._utils.Log($"New key ID: {newNpcKeyId}");
            if (TryGetNextKin(self->GetDataTable(0xf), TryGetKinFldLmapNpcLayout, out int newCmmKeyId))
                _context._utils.Log($"New CMM key ID: {newCmmKeyId}");
            */
            //FFldNpcNameTableRow newNpcNameTable = new FFldNpcNameTableRow();
            if (TryGetNextKin(fldNpcName, TryGetKinFldNpcName, out int newNpcKeyId))
            {
                var newNpcRow = MakeNewNpcNameTblTest((FFldNpcNameTableRow*)fldNpcName->RowMap.elements[0].Value);
                var newNpcId = MakeKinFldNpcName(newNpcKeyId);
                _context._utils.Log($"{newNpcRow->Name} -> {newNpcId}");
                _context._memoryMethods.TMap_InsertNoInit(&fldNpcName->RowMap, _context._objectMethods.AddFName(newNpcId), (nint)newNpcRow);
                PrintTableSize(fldNpcName, "DT_FldNpcName");
            }
            var fldCmmNpc = self->GetDataTable(0xf);
            //((FFldCmmNpcLayoutTableRow*)fldCmmNpc->RowMap.elements[31].Value)->CharaTrans.allocator_instance[0].Translation.Y = 800;
            //((FFldCmmNpcLayoutTableRow*)fldCmmNpc->RowMap.elements[31].Value)->IconTran.Translation.Y = 800;
            if (TryGetNextKin(fldCmmNpc, TryGetKinFldLmapNpcLayout, out int newCmmKeyId))
            {
                PrintTableSize(fldCmmNpc, "DT_FldLmapCmmNpcLayout");
                /*
                ((FFldCmmNpcLayoutTableRow*)fldCmmNpc->RowMap.elements[31].Value)->TimeType = 2;
                var newCmmRow = MakeNewCmmNpcLayoutTableRow((FFldCmmNpcLayoutTableRow*)fldCmmNpc->RowMap.elements[0].Value);
                _context._utils.Log($"0x{(nint)newCmmRow:X}");
                _context._memoryMethods.TMap_InsertNoInit(&fldCmmNpc->RowMap, _context._objectMethods.AddFName(MakeKinFldLmapNpcLayout(newCmmKeyId)), (nint)newCmmRow);
                PrintTableSize(fldCmmNpc, "DT_FldLmapCmmNpcLayout");
                */
            }
            /*
            var pCmmExistTable = fldBinary->GetFile("CmmExistTable.fbd", out int existTableSize);
            if (pCmmExistTable != null)
            {
                var cmmExistTable = new FBD_CmmExistTable((nint)pCmmExistTable);
                //_context._utils.Log($"CmmExistTable: {existTableSize}");
            }
            */
            _loadSpecialNpcTables.OriginalFunction(self, fldBinary);
            var cmmExistEntries = *self->CmmExistEntry;
            /*
            FBD_CmmExistEntry newEntry = new FBD_CmmExistEntry();
            NativeMemory.Copy(&newEntry, &cmmExistEntries->allocator_instance[14], (nuint)sizeof(FBD_CmmExistEntry));
            newEntry.ArcanaId = 23;
            _context._memoryMethods.TArray_Insert(cmmExistEntries, newEntry);
            */
            // Force all social links to be active
            for (int i = 0; i < cmmExistEntries->arr_num; i++)
                for (int j = 0; j < 365; j++)
                    cmmExistEntries->allocator_instance[i].SetCmmAvailable(j, 1);
            /*
            for (int i = 0; i < 365; i++)
                cmmExistEntries->allocator_instance[14].SetCmmAvailable(i, 0);
            */
            //_context._utils.Log($"UFldCommonData: {(nint)self:X}");
        }
    }
}
