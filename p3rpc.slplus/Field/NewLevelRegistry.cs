using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Event;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using static p3rpc.slplus.Field.NewLevelRegistry;
namespace p3rpc.slplus.Field
{
    public class NewLevelRegistry : ModuleAsmInlineColorEdit<SocialLinkContext>
    {
        public Dictionary<string, nint> NewLevels = new();

        private string ULevelStreamingDynamic_LoadLevelInstance_SIG = "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??";
        [Function(CallingConventions.Microsoft)]
        public unsafe delegate ULevelStreaming* ULevelStreamingDynamic_LoadLevelInstance(UObject* WorldContextObject, FString* LevelName, FVector* Location, FRotator* Rotation, byte* bOutSuccess, FString* OptionalLevelNameOverride);
        public ULevelStreamingDynamic_LoadLevelInstance? _loadLevelInstance;

        // UGameplayStatics::GetStreamingLevel
        private string ULevelStreaming_GetStreamingLevel_SIG = "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40";
        public unsafe delegate ULevelStreaming* ULevelStreaming_GetStreamingLevel(UObject* WorldContextObject, FName Name);
        private IHook<ULevelStreaming_GetStreamingLevel> _getStreamingLevel;

        // UGameplayStatics::UnloadStreamLevel
        private string UGameplayStatics_UnloadStreamLevel_SIG = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 54 24 ?? 56 48 83 EC 50";
        public unsafe delegate void UGameplayStatics_UnloadStreamLevel(UObject* WorldContextObject, FName Name, nint LatentInfo, bool bShouldBlockOnUnload);
        private IHook<UGameplayStatics_UnloadStreamLevel> _unloadStreamingLevel;

        // ULevelStreaming::SetShouldBeVisible
        private string ULevelStreaming_SetShouldBeVisible_SIG = "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40";
        public unsafe delegate void ULevelStreaming_SetShouldBeVisible(ULevelStreaming* Self, byte bVisible);
        private ULevelStreaming_SetShouldBeVisible? _setShouldBeVisible;

        private string UGameplayStatics_LoadStreamLevel_SIG = "48 89 54 24 ?? 53 55 41 56 48 83 EC 50";
        public unsafe delegate void UGameplayStatics_LoadStreamLevel(UObject* WorldContextObject, FName Name, bool bMakeVisibleAfterLoad, bool bShouldBlockOnLoad, nint LatentInfo);
        private IHook<UGameplayStatics_LoadStreamLevel> _loadStreamingLevel;

        private string GetNpcExistTables_TableArrayEntries_SIG = "48 8D 35 ?? ?? ?? ?? 44 89 F5 44 89 74 24 ??";
        private string GetNpcExistTables_TableArrayEntrySize_SIG = "83 FD 08 0F 82 ?? ?? ?? ?? 48 8B 8C 24 ?? ?? ?? ??";
        private string GetNpcExistTables_TableArrayEntrySize_SIG_EpAigis = "83 FD 0B 0F 82 ?? ?? ?? ?? 48 8B 8C 24 ?? ?? ?? ??";
        private MultiSignature GetNpcExistTablesMS;

        private string GetNpcExistTables_SIG = "48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 81 EC 70 03 00 00 48 8B 05 ?? ?? ?? ?? 48 31 E0";
        public unsafe delegate void GetNpcExistTables(nint a1, nint a2);
        private IHook<GetNpcExistTables> _getNpcExistTables;
        private unsafe FldNpcExistTableEntry* ExtendedExistTables;
        public unsafe void GetNpcExistTablesImpl(nint a1, nint a2)
        {

            if (ExtendedExistTables == null)
            {
                ExtendedExistTables = _context._redirector.MoveGlobal<FldNpcExistTableEntry>(null, "GetNpcExistTables_TableArrayEntries", 9);
                ExtendedExistTables[8] = new FldNpcExistTableEntry
                {
                    Major = 120,
                    Minor = 0,
                    pName = Marshal.StringToHGlobalAnsi($"F120_NpcExistTable.fbd")
                };
            }
            _getNpcExistTables.OriginalFunction(a1, a2);
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public unsafe struct FldNpcExistTableEntry
        {
            [FieldOffset(0x0)] public uint Major;
            [FieldOffset(0x4)] public uint Minor;
            [FieldOffset(0x8)] public nint pName;
        }
        public unsafe FldNpcExistTableEntry* _existTableEntry;
        public unsafe ULevelStreaming* ULevelStreaming_GetStreamingLevelImpl(UObject* WorldContextObject, FName Name)
        {
            if (Name.IsNone()) { return null; }
            ULevelStreaming* Result = _getStreamingLevel.OriginalFunction(WorldContextObject, Name);
            // string LevelPathFmt = _context._objectMethods.GetFName(Name);
            // _context._utils.Log($"ULevelStreaming::GetStreamingLevel: {LevelPathFmt}: {(nint)Result:X}");
            if (Result == null)
            {
                string LevelPath = _context._objectMethods.GetFName(Name);
                if (NewLevels.TryGetValue(LevelPath, out var NewLevel))
                {
                    Result = (ULevelStreaming*)NewLevel;
                } else
                {
                    // Try to create a new level...
                    FVector OriginLocation = new FVector(0, 0, 0);
                    FRotator OriginRotator = new FRotator(0, 0, 0);
                    byte bSucceeded = 0;

                    SocialLinkUtilities SlUtils = GetModule<SocialLinkUtilities>();
                    FString* StreamPathCopy = SlUtils.MakeFStringRef(LevelPath);
                    FString* LevelNameOverride = SlUtils.MakeFStringRef("");

                    Result = _loadLevelInstance!.Invoke(WorldContextObject, StreamPathCopy, &OriginLocation, &OriginRotator, &bSucceeded, LevelNameOverride);
                    _context._memoryMethods.FMemory_Free(StreamPathCopy);
                    _context._memoryMethods.FMemory_Free(LevelNameOverride);
                    if (bSucceeded == 1 && Result != null)
                    {
                        _context._logger.WriteLine($"Added level {LevelPath} to the level streaming registry: 0x{(nint)Result:X}");
                        NewLevels.Add(LevelPath, (nint)Result);
                    } else
                    {
                        _context._logger.WriteLine($"LOADING LEVEL INSTANCE FAILED: {LevelPath}");
                    }
                }
            }
            return Result;
        }

        public unsafe delegate void ULevelStreaming_SetShouldBeLoaded(ULevelStreaming* Self, byte bVisible); // vtable + 0x270
        public unsafe delegate bool ULevelStreaming_ShouldBeLoaded(ULevelStreaming* Self); // vtable + 0x278

        public unsafe void UGameplayStatics_UnloadStreamLevelImpl(UObject* WorldContextObject, FName Name, nint LatentInfo, bool bShouldBlockOnUnload)
        {
            string ExecutionFunction = _context._objectMethods.GetFName(*(FName*)(LatentInfo + 0x8));
            string LevelPath = _context._objectMethods.GetFName(Name);
            _context._utils.Log($"UGameplayStatics::UnloadStreamingLevel: {LevelPath} (EXEC: {ExecutionFunction})");
            _unloadStreamingLevel.OriginalFunction(WorldContextObject, Name, LatentInfo, bShouldBlockOnUnload);
            if (NewLevels.TryGetValue(LevelPath, out var NewLevel))
            {
                _context._utils.Log($"Force hide this! {LevelPath}");
                var level = (ULevelStreaming*)NewLevel;
                var setShouldLoad = _context._hooks.CreateWrapper<ULevelStreaming_SetShouldBeLoaded>(*(nint*)(*(nint*)level + 0x270), out _);
                setShouldLoad(level, 0);
                var shouldLoaded = _context._hooks.CreateWrapper<ULevelStreaming_ShouldBeLoaded>(*(nint*)(*(nint*)level + 0x278), out _);
                _context._utils.Log($"should be loaded: {shouldLoaded(level)}");
                //_setShouldBeVisible!((ULevelStreaming*)NewLevel, 0);
            }
        }
        public unsafe void UGameplayStatics_LoadStreamLevelImpl(UObject* WorldContextObject, FName Name, bool bMakeVisibleAfterLoad, bool bShouldBlockOnLoad, nint LatentInfo)
        {
            string LevelPathFmt = _context._objectMethods.GetFName(Name);
            _context._utils.Log($"UGameplayStatics::LoadStreamingLevel: {LevelPathFmt}");
            _loadStreamingLevel.OriginalFunction(WorldContextObject, Name, bMakeVisibleAfterLoad, bShouldBlockOnLoad, LatentInfo);
        }

        public unsafe NewLevelRegistry(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(ULevelStreamingDynamic_LoadLevelInstance_SIG, "ULevelStreamingDynamic::LoadLevelInstance", _context._utils.GetIndirectAddressShort,
                addr => _loadLevelInstance = _context._utils.MakeWrapper<ULevelStreamingDynamic_LoadLevelInstance>(addr));
            _context._utils.SigScan(ULevelStreaming_GetStreamingLevel_SIG, "ULevelStreaming::GetStreamingLevel", _context._utils.GetDirectAddress,
                addr => _getStreamingLevel = _context._utils.MakeHooker<ULevelStreaming_GetStreamingLevel>(ULevelStreaming_GetStreamingLevelImpl, addr));
            _context._utils.SigScan(UGameplayStatics_UnloadStreamLevel_SIG, "UGameplayStatics::UnloadStreamLevel", _context._utils.GetDirectAddress,
                addr => _unloadStreamingLevel = _context._utils.MakeHooker<UGameplayStatics_UnloadStreamLevel>(UGameplayStatics_UnloadStreamLevelImpl, addr));
            //_context._utils.SigScan(UGameplayStatics_LoadStreamLevel_SIG, "UGameplayStatics::LoadStreamLevel", _context._utils.GetDirectAddress,
            //    addr => _loadStreamingLevel = _context._utils.MakeHooker<UGameplayStatics_LoadStreamLevel>(UGameplayStatics_LoadStreamLevelImpl, addr));
            _context._utils.SigScan(ULevelStreaming_SetShouldBeVisible_SIG, " ULevelStreaming::SetShouldBeVisible", _context._utils.GetDirectAddress,
                addr => _setShouldBeVisible = _context._utils.MakeWrapper<ULevelStreaming_SetShouldBeVisible>(addr));
            _context._redirector.AddTargetRaw("GetNpcExistTables_TableArrayEntries", sizeof(FldNpcExistTableEntry) * 0xb, GetNpcExistTables_TableArrayEntries_SIG,
                x => _context._utils.GetIndirectAddressLong(x));
            GetNpcExistTablesMS = new MultiSignature();
            _context._utils.MultiSigScan(
                new[] { GetNpcExistTables_TableArrayEntrySize_SIG, GetNpcExistTables_TableArrayEntrySize_SIG },
                "GetNpcExistTables_TableArrayEntrySize", _context._utils.GetDirectAddress,
                addr => _asmMemWrites.Add(new AddressToMemoryWrite(_context._memory, (nuint)addr, addr =>
                    _context._memory.Write<byte>(addr + 2, (byte)(_context.bIsAigis ? 0xc : 9))
                )),
                GetNpcExistTablesMS
            );
            _context._utils.SigScan(GetNpcExistTables_SIG, "GetNpcExistTables", _context._utils.GetDirectAddress,
                addr => _getNpcExistTables = _context._utils.MakeHooker<GetNpcExistTables>(GetNpcExistTablesImpl, addr));
            /*
            _existTableEntry = (FldNpcExistTableEntry*)NativeMemory.AllocZeroed((nuint)(sizeof(FldNpcExistTableEntry) * 9));
            for (int i = 0; i < 8; i++)
            {
                _existTableEntry[i] = new FldNpcExistTableEntry {
                    Major = (uint)(101 + i), Minor = 0,
                    pName = Marshal.StringToHGlobalAnsi($"F{101 + i}_NpcExistTable.fbd")
                };
            }
            _existTableEntry[8] = new FldNpcExistTableEntry {
                Major = 120, Minor = 0,
                pName = Marshal.StringToHGlobalAnsi($"F120_NpcExistTable.fbd")
            };
            /*
            _context._utils.SigScan(GetNpcExistTables_TableArrayEntries_SIG, "GetNpcExistTables_TableArrayEntries", _context._utils.GetDirectAddress,
                addr => _asmMemWrites.Add(new AddressToMemoryWrite(_context._memory, (nuint)addr, addr => _context._memory.Write(addr + 3, (uint)((nint)_existTableEntry - ((nint)addr + 7))))));
            */
        }

        public override void Register() { }
    }
}
