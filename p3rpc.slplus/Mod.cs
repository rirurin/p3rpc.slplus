﻿using p3rpc.classconstructor.Interfaces;
using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using p3rpc.slplus.Configuration;
using p3rpc.slplus.Event;
using p3rpc.slplus.Field;
using p3rpc.slplus.Hooking;
using p3rpc.slplus.Interfaces;
using p3rpc.slplus.Messages;
using p3rpc.slplus.Modules;
using p3rpc.slplus.SocialLink;
using p3rpc.slplus.Template;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.Sigscan.Definitions;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using Reloaded.Mod.Interfaces.Internal;
using riri.globalredirector.Interfaces;
using SharedScans.Interfaces;
using System.Diagnostics;
using Unreal.AtlusScript.Interfaces;

namespace p3rpc.slplus
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase, IExports
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private SocialLinkContext _context;
        private ModuleRuntime<SocialLinkContext> _runtime;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;
#if DEBUG
            //Debugger.Launch();
#endif

            var process = Process.GetCurrentProcess();
            if (process == null || process.MainModule == null) throw new Exception($"[{_modConfig.ModName}] Process is null");
            var baseAddress = process.MainModule.BaseAddress;
            if (_hooks == null) throw new Exception($"[{_modConfig.ModName}] Could not get controller for Reloaded hooks");
            var startupScanner = GetDependency<IStartupScanner>("Reloaded Startup Scanner");
            var sharedScans = GetDependency<ISharedScans>("Shared Scans");
            var memoryMethods = GetDependency<IMemoryMethods>("P3RE Native Types (Memory Methods)");

            var classMethods = GetDependency<IClassMethods>("Class Constructor (Class Methods)");
            var objectMethods = GetDependency<IObjectMethods>("Class Constructor (Object Methods)");
            var atlusAssets = GetDependency<IAtlusAssets>("Unreal Atlus Script");
            //var redirectorApi = GetDependency<IRedirectorApi>("Global Redirector");

            // Check what version of the game is being used

            var scannerFactory = GetDependency<IScannerFactory>("Scanner Factory");
            var scanner = scannerFactory.CreateScanner(process, process.MainModule);
            var res = scanner.FindPattern("48 8B C4 48 89 48 ?? 55 41 54 48 8D 68 ?? 48 81 EC 48 01 00 00");
            bool bIsAigis = false;
            if (!res.Found)
            {
                _logger.WriteLine("Error! Couldn't find the pattern for UAppCharacterComp::Update! We'll assume that this is Episode Aigis.", System.Drawing.Color.Red);
                bIsAigis = true;
            }
            unsafe
            {
                if (*(byte*)(baseAddress + res.Offset + 0x254) == 0x75)
                {
                    _logger.WriteLine("Set hooks to use Episode Aigis.");
                    bIsAigis = true;
                }
            }

            Utils utils = new(startupScanner, _logger, _hooks, baseAddress, _modConfig.ModName, System.Drawing.Color.PaleTurquoise, _configuration.LogLevel);
            _context = new(
                baseAddress, _configuration, _logger, startupScanner, _hooks, 
                _modLoader.GetDirectoryForModId(_modConfig.ModId), utils, 
                new Reloaded.Memory.Memory(), sharedScans, _modConfig.ModId, 
                classMethods, objectMethods, memoryMethods, atlusAssets,
                bIsAigis);
            //redirectorApi, bIsAigis);
            _runtime = new(_context);
            
            _runtime.AddModule<Core>();
            _runtime.AddModule<CommonHooks>();
            _runtime.AddModule<SocialLinkUtilities>();
            
            _runtime.AddModule<SocialLinkManager>();
            _runtime.AddModule<CommunityHooks>();
            _runtime.AddModule<CampMenuHooks>();
            _runtime.AddModule<VelvetRoomHooks>();
            _runtime.AddModule<AssetLoader>();
            _runtime.AddModule<MessageHooks>();
            _runtime.AddModule<RankUpHooks>();
            _runtime.AddModule<MailService>();
            _runtime.AddModule<NpcService>();
            _runtime.AddModule<EvtPreDataService>();
            _runtime.AddModule<FldNpcActorHooks>();
            _runtime.AddModule<NewLevelRegistry>();
            
            _runtime.RegisterModules();

            var camp = _runtime.TryGetModule<CampMenuHooks>();
            if (camp != null) { _modLoader.AddOrReplaceController<ICommuListColors>(_owner, camp.listColors); }

            _modLoader.OnModLoaderInitialized += OnLoaderInit;
            _modLoader.ModLoading += OnModLoading;
        }

        private void OnLoaderInit()
        {
            _modLoader.OnModLoaderInitialized -= OnLoaderInit;
            _modLoader.ModLoading -= OnModLoading;
        }

        private IControllerType GetDependency<IControllerType>(string modName) where IControllerType : class
        {
            var controller = _modLoader.GetController<IControllerType>();
            if (controller == null || !controller.TryGetTarget(out var target))
                throw new Exception($"[{_modConfig.ModName}] Could not get controller for \"{modName}\". This depedency is likely missing.");
            return target;
        }

        private void OnModLoading(IModV1 mod, IModConfigV1 conf)
        {
            if (!conf.ModDependencies.Contains(_modConfig.ModId)) return;
            var service = _runtime.TryGetModule<EvtPreDataService>();
            if (service != null) {  service.OnModLoaded(_modLoader.GetDirectoryForModId(conf.ModId)); }
            var manager = _runtime.TryGetModule<SocialLinkManager>();
            if (manager != null) { manager.OnModLoaded(_modLoader.GetDirectoryForModId(conf.ModId), conf.ModId); }
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
            _runtime.UpdateConfiguration(configuration);
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
        public Type[] GetTypes() => new[] { typeof(ICommuListColors) };
    }
}