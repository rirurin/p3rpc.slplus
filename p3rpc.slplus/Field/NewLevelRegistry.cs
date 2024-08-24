using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Event;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using System;
namespace p3rpc.slplus.Field
{
    public class NewLevelRegistry : ModuleAsmInlineColorEdit<SocialLinkContext>
    {
        public Dictionary<string, nint> NewLevels = new();

        private string ULevelStreamingDynamic_LoadLevelInstance_SIG = "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??";
        [Function(CallingConventions.Microsoft)]
        public unsafe delegate ULevelStreaming* ULevelStreamingDynamic_LoadLevelInstance(UObject* WorldContextObject, FString* LevelName, FVector* Location, FRotator* Rotation, byte* bOutSuccess, FString* OptionalLevelNameOverride);
        public ULevelStreamingDynamic_LoadLevelInstance? _loadLevelInstance;

        private string ULevelStreaming_GetStreamingLevel_SIG = "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40";
        public unsafe delegate ULevelStreaming* ULevelStreaming_GetStreamingLevel(UObject* WorldContextObject, FName Name);
        private IHook<ULevelStreaming_GetStreamingLevel> _getStreamingLevel;
        public unsafe ULevelStreaming* ULevelStreaming_GetStreamingLevelImpl(UObject* WorldContextObject, FName Name)
        {
            ULevelStreaming* Result = _getStreamingLevel.OriginalFunction(WorldContextObject, Name);
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

                    Result = _loadLevelInstance.Invoke(WorldContextObject, StreamPathCopy, &OriginLocation, &OriginRotator, &bSucceeded, LevelNameOverride);
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
        public unsafe NewLevelRegistry(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            _context._utils.SigScan(ULevelStreamingDynamic_LoadLevelInstance_SIG, "ULevelStreamingDynamic::LoadLevelInstance", _context._utils.GetIndirectAddressShort,
                addr => _loadLevelInstance = _context._utils.MakeWrapper<ULevelStreamingDynamic_LoadLevelInstance>(addr));
            _context._utils.SigScan(ULevelStreaming_GetStreamingLevel_SIG, "ULevelStreaming::GetStreamingLevel", _context._utils.GetDirectAddress,
                addr => _getStreamingLevel = _context._utils.MakeHooker<ULevelStreaming_GetStreamingLevel>(ULevelStreaming_GetStreamingLevelImpl, addr));
        }

        public override void Register() { }
    }
}
