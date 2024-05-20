using p3rpc.commonmodutils;
using p3rpc.slplus.Configuration;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory;
using Reloaded.Mod.Interfaces;
using SharedScans.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reloaded.Hooks.Definitions;
using p3rpc.nativetypes.Interfaces;
using p3rpc.classconstructor.Interfaces;
using Unreal.AtlusScript.Interfaces;

namespace p3rpc.slplus
{
    public class SocialLinkContext : UnrealContext
    {
        public new Config _config { get; set; }
        public string ModName { get; init; }

        public IMemoryMethods _memoryMethods { get; init; }
        public IAtlusAssets _atlusAssets { get; init; }

        public SocialLinkContext(long baseAddress, IConfigurable config, ILogger logger, IStartupScanner startupScanner, IReloadedHooks hooks, string modLocation, Utils utils, Memory memory, 
            ISharedScans sharedScans, string modName, IClassMethods classMethods, IObjectMethods objectMethods, IMemoryMethods memoryMethods, IAtlusAssets atlusAssets)
            : base(baseAddress, config, logger, startupScanner, hooks, modLocation, utils, memory, sharedScans, classMethods, objectMethods)
        {
            _config = (Config)config;
            _memoryMethods = memoryMethods;
            ModName = modName;
            _atlusAssets = atlusAssets;
        }

        public override void OnConfigUpdated(IConfigurable newConfig) => _config = (Config)newConfig;
    }
}
