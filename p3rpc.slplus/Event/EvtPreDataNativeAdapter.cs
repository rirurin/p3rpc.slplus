using p3rpc.commonmodutils;
using p3rpc.nativetypes.Interfaces;
using System.Runtime.InteropServices;

namespace p3rpc.slplus.Event
{
    public class EvtPreDataNativeAdapter
    {
        public int EventMajorID { get; set; }
        public int EventMinorID { get; set; }
        public int EventCategoryTypeID { get; set; }
        public FName EventRank { get; set; }
        public FName EventCategory { get; set; }
        public unsafe FString* EventLevel { get; set; } = null;
        public unsafe TArray<FAtlEvtPreSublevelData>* EventSublevels { get; set; } = null;
        public unsafe TArray<FName>* LightScenarioSublevels { get; set; } = null;
        public unsafe FAtlEvtPreDungeonSublevelData* DungeonSublevel { get; set; } = null;
        public bool? bDisableAutoLoadFirstLightingScenarioLevel { get; set; } = null;
        public bool? bForceDisableUseCurrentTimeZone { get; set; } = null;
        public byte? ForcedCldTimeZoneValue { get; set; } = null;
        public int? ForceMonth { get; set; } = null;
        public int? ForceDay { get; set; } = null;
        /*
        public unsafe FAtlEvtPreData* ToNative(FAtlEvtPreData* preData)
        {
            preData->EventMajorID = EventMajorID;
            preData->EventMinorID = EventMinorID;
            preData->EventCategoryTypeID = EventCategoryTypeID;
            preData->EventRank = EventRank;
            preData->EventCategory = EventCategory;
            if (EventLevel != null) preData->EventLevel = *EventLevel;
            if (EventSublevels != null) preData->EventSublevels = EventSublevels.Value;
            if (LightScenarioSublevels != null) preData->LightScenarioSublevels = LightScenarioSublevels.Value;
            if (DungeonSublevel != null) preData->DungeonSublevel = DungeonSublevel.Value;
            if (bDisableAutoLoadFirstLightingScenarioLevel != null) preData->bDisableAutoLoadFirstLightingScenarioLevel = bDisableAutoLoadFirstLightingScenarioLevel.Value;
            if (bForceDisableUseCurrentTimeZone != null) preData->bForceDisableUseCurrentTimeZone = bForceDisableUseCurrentTimeZone.Value;
            if (ForcedCldTimeZoneValue != null) preData->ForcedCldTimeZoneValue = ForcedCldTimeZoneValue.Value;
            if (ForceMonth != null) preData->ForceMonth = ForceMonth.Value;
            if (ForceDay != null) preData->ForceDay = ForceDay.Value;
            return preData;
        }
        */

        public void FromYamlModelCommon(SocialLinkUtilities _context, EvtPreDataModel model)
        {
            EventMajorID = model.EventMajorID;
            EventMinorID = model.EventMinorID;
            EventCategoryTypeID = model.EventCategoryTypeID;
            EventRank = _context.GetFName(model.EventRank);
            EventCategory = _context.GetFName(model.EventCategory);
        }

        private unsafe void SublevelEntryFromYamlModel(FAtlEvtPreSublevelData* entry, SocialLinkUtilities _context, EvtPreDataSublevels model)
        {
            entry->EventBGLevels = *_context.MakeArray<FString>(model.EventBGLevels.Count());
            for (int i = 0; i < model.EventBGLevels.Count(); i++)
                entry->EventBGLevels.allocator_instance[i] = *_context.MakeFString(model.EventBGLevels[i]);
            entry->BGFieldMajorID = model.BGFieldMajorID;
            entry->BGFieldMinorID = model.BGFieldMinorID;
            entry->BGFieldSeasonSubLevel = *_context.MakeFString(model.BGFieldSeasonSubLevel);
            entry->BGFieldSoundSubLevel = *_context.MakeFString(model.BGFieldSoundSubLevel);
        }
        private unsafe TArray<FAtlEvtPreSublevelData>* SublevelHookFromYamlModel(SocialLinkUtilities _context, EvtPreDataModel model)
        {
            if (model.EventSublevels == null)
            {
                _context.Log("model.EventSublevels == null in SublevelHookFromYamlModel (this should not happen");
                throw new Exception();
            }
            var sublevels = _context.MakeArray<FAtlEvtPreSublevelData>(model.EventSublevels.Count());
            for (int i = 0; i < sublevels->arr_num; i++)
                SublevelEntryFromYamlModel(&sublevels->allocator_instance[i], _context, model.EventSublevels[i]);
            return sublevels;
        }

        private static bool HasSublevels(EvtPreDataModel model) => model.EventSublevels != null && model.EventSublevels.Count > 0;

        private unsafe TArray<FName>* LightScenarioFromYamlModel(SocialLinkUtilities _context, EvtPreDataModel model)
        {
            if (model.LightScenarioSublevels == null) return null;
            var lightScenarios = _context.MakeArray<FName>(model.LightScenarioSublevels.Count());
            for (int i = 0; i < lightScenarios->arr_num; i++)
                lightScenarios->allocator_instance[i] = _context.GetFName(model.LightScenarioSublevels[i]);
            return lightScenarios;
        }

        private unsafe FAtlEvtPreDungeonSublevelData* DungeonSublevelHookFromYamlModel(SocialLinkUtilities _context, EvtPreDataDungeonSublevel model)
        {
            var dungeonSublevel = _context.Malloc<FAtlEvtPreDungeonSublevelData>();
            dungeonSublevel->EventBGFloorLevel = _context.GetFName(model.EventBGFloorLevel);
            dungeonSublevel->BGEnvironmentSubLevel = _context.GetFName(model.BGEnvironmentSubLevel);
            return dungeonSublevel;
        }

        public static EvtPreDataNativeAdapter? HookFromYamlModel(SocialLinkUtilities _context, EvtPreDataModel model)
        {
            var newAdapter = new EvtPreDataNativeAdapter();
            newAdapter.FromYamlModelCommon(_context, model);
            unsafe
            {
                newAdapter.EventLevel = (model.EventLevel != null) ? _context.MakeFString(model.EventLevel) : null;
                newAdapter.EventSublevels = (HasSublevels(model)) ? newAdapter.SublevelHookFromYamlModel(_context, model) : null;
                newAdapter.LightScenarioSublevels = newAdapter.LightScenarioFromYamlModel(_context, model);
                newAdapter.DungeonSublevel = (model.DungeonSublevel != null) ? newAdapter.DungeonSublevelHookFromYamlModel(_context, model.DungeonSublevel) : null;
            }
            newAdapter.bDisableAutoLoadFirstLightingScenarioLevel = model.bDisableAutoLoadFirstLightingScenarioLevel;
            newAdapter.bForceDisableUseCurrentTimeZone = model.bForceDisableUseCurrentTimeZone;
            newAdapter.ForcedCldTimeZoneValue = model.ForcedCldTimeZoneValue;
            newAdapter.ForceMonth = model.ForceMonth;
            newAdapter.ForceDay = model.ForceDay;
            return newAdapter;
        }

        public static EvtPreDataNativeAdapter? NewFromYamlModel(SocialLinkUtilities _context, EvtPreDataModel model)
        {
            return null;
            /*
            var newAdapter = new EvtPreDataNativeAdapter(); 
            var errorReporter = new EvtPreDataNativeAdapterErrors(newAdapter);
            newAdapter.FromYamlModelCommon(_context, model);
            if (model.EventLevel != null) newAdapter.EventLevel = _context.MakeFString(model.EventLevel); else errorReporter.MissingParameters.Add("EventLevel");
            if (model.EventSublevels != null) newAdapter.EventSublevels = newAdapter.SublevelHookFromYamlModel(_context, model); else errorReporter.MissingParameters.Add("EventSublevels");
            if (model.LightScenarioSublevels != null) newAdapter.LightScenarioSublevels = newAdapter.LightScenarioFromYamlModel(_context, model); else errorReporter.MissingParameters.Add("LightScenarioSublevels");
            if (model.DungeonSublevel != null) newAdapter.DungeonSublevel = newAdapter.DungeonSublevelHookFromYamlModel(_context, model.DungeonSublevel); else errorReporter.MissingParameters.Add("DungeonSublevel");
            if (model.bDisableAutoLoadFirstLightingScenarioLevel != null) newAdapter.bDisableAutoLoadFirstLightingScenarioLevel = model.bDisableAutoLoadFirstLightingScenarioLevel; else errorReporter.MissingParameters.Add("bDisableAutoLoadFirstLightingScenarioLevel");
            if (model.bForceDisableUseCurrentTimeZone != null) newAdapter.bForceDisableUseCurrentTimeZone = model.bForceDisableUseCurrentTimeZone; else errorReporter.MissingParameters.Add("bForceDisableUseCurrentTimeZone");
            if (model.ForcedCldTimeZoneValue != null) newAdapter.ForcedCldTimeZoneValue = model.ForcedCldTimeZoneValue; else errorReporter.MissingParameters.Add("ForcedCldTimeZoneValue");
            if (model.ForceMonth != null) newAdapter.ForceMonth = model.ForceMonth; else errorReporter.MissingParameters.Add("ForceMonth");
            if (model.ForceDay != null) newAdapter.ForceDay = model.ForceDay; else errorReporter.MissingParameters.Add("ForceDay");
            errorReporter.ReportErrors(_context);
            return (errorReporter.MissingParameters.Count == 0) ? newAdapter : null;
            */
        }
    }

    public class EvtPreDataNativeAdapterErrors
    {
        public EvtPreDataNativeAdapter Owner;
        public List<string> MissingParameters { get; private set; } = new();
        public EvtPreDataNativeAdapterErrors(EvtPreDataNativeAdapter owner) { Owner = owner; }
        public void ReportErrors(SocialLinkUtilities _context)
        {
            foreach (var missingParam in MissingParameters)
                _context.LogError(
                    $"[PRE_{_context.GetFName(Owner.EventCategory)}_{Owner.EventMajorID:D3}_{Owner.EventMinorID:D3}]: " +
                    $"Missing parameter {missingParam}. Event won't be loaded."
                );
        }
    }
}
