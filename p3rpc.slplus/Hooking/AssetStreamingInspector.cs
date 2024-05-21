using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Hooks.Definitions.X64;

namespace p3rpc.slplus.Hooking
{
    // This will likely be moved to class constructor at some point
    // Inspect calls to UAssetLoader::LoadRequestedAssets. If we know that our asset loader instance has
    // a custom asset we've laoded, get it and make sure to mark it as root set so it can't be GC'd
    public class AssetStreamingInspector : ModuleBase<SocialLinkContext>
    {
        private string UAssetLoader_LoadRequestedAssets_SIG = "48 89 E0 48 83 EC 68 48 89 70 ??";
        private IHook<UAssetLoader_LoadRequestedAssets> _loadRequestedAssets;
        public unsafe delegate void UAssetLoader_LoadRequestedAssets(UAssetLoader* self);

        private string UAssetLoader_CheckStreamedAssets_SIG = "49 8D 4F ?? 48 89 5C 24 ?? 48 8D 54 24 ??";
        private IAsmHook _checkStreamedAssets;
        private IReverseWrapper<UAssetLoader_CheckStreamedAssets> _checkStreameedAssetsWrapper;
        [Function(FunctionAttribute.Register.r15, FunctionAttribute.Register.rax, false)]
        public unsafe delegate void UAssetLoader_CheckStreamedAssets(UAssetLoader* loader);

        public Dictionary<nint, Action<nint>> MemoryToNotify = new();
        public unsafe AssetStreamingInspector(SocialLinkContext context, Dictionary<string, ModuleBase<SocialLinkContext>> modules) : base(context, modules)
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
        }

        public override void Register()
        {
            //_manager = GetModule<SocialLinkManager>();
            //_utils = GetModule<SocialLinkUtilities>();
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
            //_context._utils.Log($"[UAssetLoader::CheckStreamedAssets] Using asset loader 0x{(nint)self:X} -> {loadedObjects.arr_num} objects loaded");
            for (int i = 0; i < loadedObjects.arr_num; i++)
            {
                if (MemoryToNotify.TryGetValue(loadedObjects.allocator_instance[i], out var onLoadedObjectCb))
                {
                    _context._utils.Log($"[UAssetLoader::CheckStreamedAssets] Notify for 0x{loadedObjects.allocator_instance[i]:X}", System.Drawing.Color.LimeGreen);
                    onLoadedObjectCb(loadedObjects.allocator_instance[i]);
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
    }
}
