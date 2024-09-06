using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Field;
using p3rpc.slplus.Parsing;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using System.Runtime.InteropServices;

namespace p3rpc.slplus.Event
{
    // Java programmers and they design patterns
    public class EvtPreDataService : ModuleBase<SocialLinkContext>
    {
        private string UAtlEvtSubsystem_GetEvtPreData_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 55 41 56 41 57 48 8D 6C 24 ?? 48 81 EC 90 00 00 00 45 0F B6 F8";
        private IHook<UAtlEvtSubsystem_GetEvtPreData> _getEvtPreData;
        public unsafe delegate FAtlEvtPreData* UAtlEvtSubsystem_GetEvtPreData(UAtlEvtSubsystem* self, FAtlEvtPreData* dataOut, EAtlEvtEventCategoryType category, uint MajorId, uint MinorId);

        private string UAtlEvtSubsystem_DoesLevelStreamingLevelExist_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 4C 89 C7";
        [Function(CallingConventions.Microsoft)]
        public unsafe delegate byte UAtlEvtSubsystem_DoesLevelStreamingLevelExist(UAtlEvtSubsystem* self, UWorld* worldOut, FString* pathOut);
        private IHook<UAtlEvtSubsystem_DoesLevelStreamingLevelExist> _doesLevelStreamingExist;

        private NewLevelRegistry _NewLevelRegistry;

        public unsafe byte UAtlEvtSubsystem_DoesLevelStreamingLevelExistImpl(UAtlEvtSubsystem* self, UWorld* BaseWorld, FString* StreamPath)
        {

            string StreamPathStr = StreamPath->ToString();
            _context._utils.Log($"UAtlEvtSubsystem::DoesLevelStreamingLevelExist: {StreamPathStr}");
            byte bInExistingLevelList = _doesLevelStreamingExist.OriginalFunction(self, BaseWorld, StreamPath);
            if (bInExistingLevelList == 0 && _NewLevelRegistry.NewLevels.TryGetValue(StreamPathStr, out _))
                bInExistingLevelList = 1;
            if (bInExistingLevelList == 0)
            {
                FVector OriginLocation = new FVector(0, 0, 0);
                FRotator OriginRotator = new FRotator(0, 0, 0);
                byte bSucceeded = 0;

                SocialLinkUtilities SlUtils = GetModule<SocialLinkUtilities>();
                FString* StreamPathCopy = SlUtils.MakeFStringRef(StreamPathStr);
                FString* LevelNameOverride = SlUtils.MakeFStringRef("");

                // LoadStreamingLevel doesn't work unless the level is added into the level hierachy (LV_Xrd777_P)
                ULevelStreaming* StreamedLevel = _NewLevelRegistry._loadLevelInstance.Invoke((UObject*)BaseWorld, StreamPathCopy, &OriginLocation, &OriginRotator, &bSucceeded, LevelNameOverride);
                _context._memoryMethods.FMemory_Free(StreamPathCopy);
                _context._memoryMethods.FMemory_Free(LevelNameOverride);
                if (bSucceeded == 1 && StreamedLevel != null)
                {
                    _context._logger.WriteLine($"Added level {StreamPathStr} to the level streaming registry: 0x{(nint)StreamedLevel:X}");
                    _NewLevelRegistry.NewLevels.Add(StreamPathStr, (nint)StreamedLevel);

                } else
                {
                    _context._logger.WriteLine($"LOADING LEVEL INSTANCE FAILED: {StreamPathStr}");
                }
                bInExistingLevelList = bSucceeded;
            }
            return bInExistingLevelList;
        }

        private Dictionary<uint, EvtPreDataModel> CustomEvtPreDataManaged = new();
        private Dictionary<uint, EvtPreDataNativeAdapter> CustomEvtPreDataAdapted = new();

        private bool HasPrinted = false;

        private string EvtCinemaBasePath = Path.Join("UnrealEssentials", "P3R", "Content", "Xrd777", "Events", "Cinema");
        public unsafe EvtPreDataService(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(UAtlEvtSubsystem_GetEvtPreData_SIG, "UAtlEvtSubsystem::GetEvtPreData", _context._utils.GetDirectAddress,
                addr => _getEvtPreData = _context._utils.MakeHooker<UAtlEvtSubsystem_GetEvtPreData>(UAtlEvtSubsystem_GetEvtPreDataImpl, addr));
            _context._utils.SigScan(UAtlEvtSubsystem_DoesLevelStreamingLevelExist_SIG, "UAtlEvtSubsystem::DoesLevelStreamingLevelExist", _context._utils.GetDirectAddress,
                addr => _doesLevelStreamingExist = _context._utils.MakeHooker<UAtlEvtSubsystem_DoesLevelStreamingLevelExist>(UAtlEvtSubsystem_DoesLevelStreamingLevelExistImpl, addr));
        }
        public override void Register()
        {
            _NewLevelRegistry = GetModule<NewLevelRegistry>();
        }

        // FString::FString(const FString& a1)
        private unsafe void FStringCtor(FString* copy, FString* src)
        {
            copy->text.allocator_instance = (nint*)_context._memoryMethods.FMemory_Malloc(sizeof(ushort) * src->text.arr_max, 8);
            NativeMemory.Copy(src->text.allocator_instance, copy->text.allocator_instance, (nuint)(sizeof(ushort) * src->text.arr_max));
            copy->text.arr_num = src->text.arr_num;
            copy->text.arr_max = src->text.arr_max;
        }

        private unsafe void CopyDungeonSublevelPreData(FAtlEvtPreDungeonSublevelData* copy, FAtlEvtPreDungeonSublevelData* src)
        {
            copy->EventBGFloorLevel = src->EventBGFloorLevel;
            copy->BGEnvironmentSubLevel = src->BGEnvironmentSubLevel;
        }

        private unsafe void CopySublevelsBGLevels(TArray<FString>* copy, TArray<FString>* src)
        {
            copy->allocator_instance = _context._memoryMethods.FMemory_MallocMultiple<FString>((uint)src->arr_max);
            copy->arr_num = src->arr_num;
            copy->arr_max = src->arr_max;
            for (int i = 0; i < copy->arr_num; i++)
                FStringCtor(&copy->allocator_instance[i], &src->allocator_instance[i]);
        }

        private unsafe void CopySublevelsPreData(TArray<FAtlEvtPreSublevelData>* copy, TArray<FAtlEvtPreSublevelData>* src)
        {
            copy->allocator_instance = _context._memoryMethods.FMemory_MallocMultiple<FAtlEvtPreSublevelData>((uint)src->arr_max);
            copy->arr_num = src->arr_num;
            copy->arr_max = src->arr_max;
            for (int i = 0; i < copy->arr_num; i++)
            {
                FAtlEvtPreSublevelData* copySub = &copy->allocator_instance[i];
                FAtlEvtPreSublevelData* ogSub = &src->allocator_instance[i];
                CopySublevelsBGLevels(&copySub->EventBGLevels, &ogSub->EventBGLevels);
                copySub->BGFieldMajorID = ogSub->BGFieldMajorID;
                copySub->BGFieldMinorID = ogSub->BGFieldMinorID;
                FStringCtor(&copySub->BGFieldSeasonSubLevel, &ogSub->BGFieldSeasonSubLevel);
                FStringCtor(&copySub->BGFieldSoundSubLevel, &ogSub->BGFieldSoundSubLevel);
            }
        }

        private unsafe void CopyLightScenarioSublevels(TArray<FName>* copy, TArray<FName>* src)
        {
            copy->allocator_instance = _context._memoryMethods.FMemory_MallocMultiple<FName>((uint)src->arr_max);
            copy->arr_num = src->arr_num;
            copy->arr_max = src->arr_max;
            for (int i = 0; i < copy->arr_num; i++)
                copy->allocator_instance[i] = src->allocator_instance[i];
        }

        public unsafe void ToNative(FAtlEvtPreData* copy, FAtlEvtPreData* original, EvtPreDataNativeAdapter? hook)
        {
            // These values would be the same anyway...
            copy->EventMajorID = (original != null) ? original->EventMajorID : hook.EventMajorID;
            copy->EventMinorID = (original != null) ? original->EventMinorID : hook.EventMinorID;
            copy->EventCategoryTypeID = (original != null) ? original->EventCategoryTypeID : hook.EventCategoryTypeID;
            copy->EventRank = (original != null) ? original->EventRank : hook.EventRank;
            copy->EventCategory = (original != null) ? original->EventCategory : hook.EventCategory;

            // begin hookable fields
            FStringCtor(&copy->EventLevel, (hook != null && hook.EventLevel != null) ? hook.EventLevel : &original->EventLevel);
            CopySublevelsPreData(&copy->EventSublevels, (hook != null && hook.EventSublevels != null) ? hook.EventSublevels : &original->EventSublevels);
            CopyLightScenarioSublevels(&copy->LightScenarioSublevels, (hook != null && hook.LightScenarioSublevels != null) ? hook.LightScenarioSublevels : &original->LightScenarioSublevels);
            /*
            CopyDungeonSublevelPreData(&copy->DungeonSublevel, (hook != null && hook.DungeonSublevel != null) ? hook.DungeonSublevel : &original->DungeonSublevel);
            copy->bDisableAutoLoadFirstLightingScenarioLevel = (hook != null && hook.bDisableAutoLoadFirstLightingScenarioLevel != null) ? hook.bDisableAutoLoadFirstLightingScenarioLevel.Value : original->bDisableAutoLoadFirstLightingScenarioLevel;
            copy->bForceDisableUseCurrentTimeZone = (hook != null && hook.bForceDisableUseCurrentTimeZone != null) ? hook.bForceDisableUseCurrentTimeZone.Value : original->bForceDisableUseCurrentTimeZone;
            copy->ForcedCldTimeZoneValue = (hook != null && hook.ForcedCldTimeZoneValue != null) ? hook.ForcedCldTimeZoneValue.Value : original->ForcedCldTimeZoneValue;
            copy->ForceMonth = (hook != null && hook.ForceMonth != null) ? hook.ForceMonth.Value : original->ForceMonth;
            copy->ForceDay = (hook != null && hook.ForceDay != null) ? hook.ForceDay.Value : original->ForceDay;
            */
        }

        private unsafe FAtlEvtPreData* UAtlEvtSubsystem_GetEvtPreDataImpl(UAtlEvtSubsystem* self, FAtlEvtPreData* dataOut, EAtlEvtEventCategoryType category, uint MajorId, uint MinorId)
        {
            _context._utils.Log($"[PRE_{category}_{MajorId:D3}_{MinorId:D3}] Get pre event data");
            var preHash = UAtlEvtSubsystem.GetEvtPreDataHash(category, MajorId, MinorId);
            var evtPreDataMapSearch = new TMapHashable<HashableInt, FAtlEvtPreData>((nint)(&self->EvtPreDataMap), 0x40, 0x48);
            FAtlEvtPreData* foundPreData = evtPreDataMapSearch.TryGetByHash(((int)preHash).AsHashable());
            NativeMemory.Fill(dataOut, (nuint)sizeof(FAtlEvtPreData), 0);
            if (!CustomEvtPreDataAdapted.TryGetValue(preHash, out EvtPreDataNativeAdapter preDataAdapted))
            {
                if (CustomEvtPreDataManaged.TryGetValue(preHash, out EvtPreDataModel preDataManaged))
                {
                    EvtPreDataNativeAdapter? preDataAdaptedMaybe = (foundPreData != null)
                        ? EvtPreDataNativeAdapter.HookFromYamlModel(GetModule<SocialLinkUtilities>(), preDataManaged)
                        : EvtPreDataNativeAdapter.NewFromYamlModel(GetModule<SocialLinkUtilities>(), preDataManaged);
                    if (preDataAdaptedMaybe != null)
                    {
                        CustomEvtPreDataAdapted.Add(preHash, preDataAdaptedMaybe);
                        ToNative(dataOut, foundPreData, preDataAdaptedMaybe);
                    } else ToNative(dataOut, foundPreData, null);
                } else
                {
                    if (foundPreData != null) ToNative(dataOut, foundPreData, null);
                    else dataOut->MakeInvalidEvent();
                }
            } else ToNative(dataOut, foundPreData, preDataAdapted);
            return dataOut;
        }

        private bool TryRegisterPreEventYaml(string path)
        {
            var leafDirectory = Path.GetDirectoryName(path).Split(Path.DirectorySeparatorChar)[^1];
            var fileNameComponents = Path.GetFileNameWithoutExtension(path).Split("_", 2);
            return fileNameComponents[0] == "PRE" && fileNameComponents[1] == leafDirectory;
        }

        private int GetEventCategoryTypeInt(string typeName)
        {
            return typeName switch
            {
                "Main" => 0,
                "Cmmu" => 1,
                "Qest" => 2,
                "Extr" => 3,
                "Fild" => 4,
                _ => throw new Exception($"Unimplemented event category type {typeName}")
            };
        }

        public void OnModLoaded(string modPath)
        {
            string EventsPath = Path.Join(modPath, EvtCinemaBasePath);
            if (!Path.Exists(EventsPath)) return;
            var preEvtFiles = Directory.EnumerateFiles(EventsPath, "*.*", SearchOption.AllDirectories).Where(
                x => Constants.YAML_EXTENSION.Contains(Path.GetExtension(x).Substring(1)) && TryRegisterPreEventYaml(x)
            );
            foreach (var preEvtFile in preEvtFiles)
            {
                // Get params stored in yml file, then generate anything that can be implied from file name
                // Params can be null if they're an event hook. New events must have all parameters defined to be accepted on GetEvtPreData
                _context._utils.Log($"Reading file {preEvtFile}");
                EvtPreDataModel preDataManaged = YamlSerializer.deserializer.Deserialize<EvtPreDataModel>(new StreamReader(preEvtFile));
                if (preDataManaged == null) { continue; }
                string[] preFileNameParts = Path.GetFileNameWithoutExtension(preEvtFile).Split("_"); // PRE_Event_Cmmu_120_100_C
                                                                                                     // [0]  [1]  [2]  [3] [4] [5]
                preDataManaged.EventMajorID = int.Parse(preFileNameParts[3]);
                preDataManaged.EventMinorID = int.Parse(preFileNameParts[4]);
                preDataManaged.EventCategoryTypeID = GetEventCategoryTypeInt(preFileNameParts[2]);
                preDataManaged.RecalculateHash();
                preDataManaged.EventRank = preFileNameParts[5];
                preDataManaged.EventCategory = preFileNameParts[2];
                // Validate each sublevel passed into EventSublevels.EventBGLevels
                foreach (var eventSublevel in preDataManaged.EventSublevels)
                {
                    foreach (var eventBgLevel in eventSublevel.EventBGLevels)
                    {
                        string[] eventBgLevelParts = Path.GetFileNameWithoutExtension(eventBgLevel).Split("_"); // LV_F101_141_001_BG
                                                                                                                // [0] [1] [2] [3] [4]
                        eventSublevel.BGFieldMajorID = int.Parse(eventBgLevelParts[1].Substring(1));
                        eventSublevel.BGFieldMinorID = int.Parse(eventBgLevelParts[2]);
                    }
                }
                var preDataHash = UAtlEvtSubsystem.GetEvtPreDataHash((EAtlEvtEventCategoryType)preDataManaged.EventCategoryTypeID, (uint)preDataManaged.EventMajorID, (uint)preDataManaged.EventMinorID);
                CustomEvtPreDataManaged.Add(preDataHash, preDataManaged);
                _context._utils.Log($"Registered pre event yaml {preDataManaged.EventCategory}_{preDataManaged.EventMajorID:D3}_{preDataManaged.EventMinorID:D3}_{preDataManaged.EventRank} (hash: 0x{preDataHash:X})");
                
            }
        }
    }
}
