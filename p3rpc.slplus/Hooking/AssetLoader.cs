using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;

namespace p3rpc.slplus.Hooking
{
    // This will likely be moved to class constructor at some point
    // Inspect calls to UAssetLoader::LoadRequestedAssets. If we know that our asset loader instance has
    // a custom asset we've loaded, get it and make sure to mark it as root set so it can't be GC'd
    public class AssetLoader : ModuleBase<SocialLinkContext>
    {
        private string UAssetLoader_LoadRequestedAssets_SIG = "48 89 E0 48 83 EC 68 48 89 70 ??";
        private IHook<UAssetLoader_LoadRequestedAssets> _loadRequestedAssets;
        public unsafe delegate void UAssetLoader_LoadRequestedAssets(UAssetLoader* self);

        private string UAssetLoader_CheckStreamedAssets_SIG = "49 8D 4F ?? 48 89 5C 24 ?? 48 8D 54 24 ??";
        private IAsmHook _checkStreamedAssets;
        private IReverseWrapper<UAssetLoader_CheckStreamedAssets> _checkStreameedAssetsWrapper;
        [Function(FunctionAttribute.Register.r15, FunctionAttribute.Register.rax, false)]
        public unsafe delegate void UAssetLoader_CheckStreamedAssets(UAssetLoader* loader);

        private string UAssetLoader_LoadTargetAsset_SIG = "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 41 56 48 83 EC 50 48 8B 02";
        private UAssetLoader_LoadTargetAsset _loadTargetAsset;
        public unsafe delegate void UAssetLoader_LoadTargetAsset(UAssetLoader* self, FString* name, nint dest);

        private string UAssetLoader_LoadQueuedAssets_SIG = "48 89 E0 41 56 48 81 EC 90 00 00 00";
        public UAssetLoader_LoadQueuedAssets _loadQueuedAssets;
        public unsafe delegate void UAssetLoader_LoadQueuedAssets(UAssetLoader* self);

        public Dictionary<nint, (Action<nint> onLoadCb, string fileName)> MemoryToNotify = new();

        private SocialLinkUtilities _utils;
        public unsafe AssetLoader(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
        {
            //_context._utils.SigScan(UAssetLoader_LoadRequestedAssets_SIG, "UAssetLoader::LoadRequestedAssets", _context._utils.GetDirectAddress,
            //    addr => _loadRequestedAssets = _context._utils.MakeHooker<UAssetLoader_LoadRequestedAssets>(UAssetLoader_LoadRequestedAssetsImpl, addr));

            _context._utils.SigScan(UAssetLoader_CheckStreamedAssets_SIG, "UAssetLoader::CheckStreamedAssets", _context._utils.GetDirectAddress, addr =>
            {
                string[] function =
                {
                    "use64",
                    $"{_context._utils.PreserveMicrosoftRegisters()}",
                    $"{_context._hooks.Utilities.GetAbsoluteCallMnemonics(UAssetLoader_CheckStreamedAssetsImpl, out _checkStreameedAssetsWrapper)}",
                    $"{_context._utils.RetrieveMicrosoftRegisters()}",
                };
                _checkStreamedAssets = _context._hooks.CreateAsmHook(function, addr, AsmHookBehaviour.ExecuteFirst).Activate();
            });

            _context._utils.SigScan(UAssetLoader_LoadTargetAsset_SIG, "UAssetLoader::LoadTargetAsset", _context._utils.GetDirectAddress,
                addr => _loadTargetAsset = _context._utils.MakeWrapper<UAssetLoader_LoadTargetAsset>(addr));

            _context._utils.SigScan(UAssetLoader_LoadQueuedAssets_SIG, "UAssetLoader::LoadQueuedAssets", _context._utils.GetDirectAddress,
                addr => _loadQueuedAssets = _context._utils.MakeWrapper<UAssetLoader_LoadQueuedAssets>(addr));
        }

        public override void Register()
        {
            //_manager = GetModule<SocialLinkManager>();
            _utils = GetModule<SocialLinkUtilities>();
            //_common = GetModule<CommonHooks>();
        }

        public unsafe void UAssetLoader_LoadRequestedAssetsImpl(UAssetLoader* self)
        {
            _context._utils.Log($"Using Asset Loader 0x{(nint)self:X}, (handle 0x{self->StreamHandle:X})");
            _loadRequestedAssets.OriginalFunction(self);
        }
        public unsafe void UAssetLoader_CheckStreamedAssetsImpl(UAssetLoader* self)
        {
            var loadedObjects = self->ObjectReferences;
            for (int i = 0; i < loadedObjects.arr_num; i++)
            {
                if (MemoryToNotify.TryGetValue(loadedObjects.allocator_instance[i], out var memoryNotification))
                {
                    if (*(UObject**)loadedObjects.allocator_instance[i] != null)
                    {
                        _context._utils.Log($"[UAssetLoader::CheckStreamedAssets] Loaded file \"{memoryNotification.fileName}\" into 0x{loadedObjects.allocator_instance[i]:X}", System.Drawing.Color.LimeGreen);
                        memoryNotification.onLoadCb(loadedObjects.allocator_instance[i]);
                    } else
                        _context._utils.Log($"[UAssetLoader::CheckStreamedAssets] ERROR: File \"{memoryNotification.fileName}\" could not be found. The file is likely missing from your mod.", System.Drawing.Color.Red, LogLevel.Error);
                    MemoryToNotify.Remove(loadedObjects.allocator_instance[i]);
                }
            }
        }

        // Make the target object immune to Unreal's garbage collector
        public unsafe void MarkAssetAsRoot(nint ppObject)
        {
            UObject* pObject = *(UObject**)ppObject;
            _context._objectMethods.MarkObjectAsRoot(pObject);
        }

        public unsafe void LoadAsset(UAssetLoader* loader, string path, nint target, Action<nint> onLoadedCb)
        {
            FString assetNameFString = _utils.MakeFString(path);
            MemoryToNotify.Add(target, (onLoadedCb, path));
            _loadTargetAsset(loader, &assetNameFString, target);
        }
    }
}
