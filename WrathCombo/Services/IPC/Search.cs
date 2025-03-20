#region

using ECommons.DalamudServices;
using ECommons.ExcelServices;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WrathCombo.Attributes;
using WrathCombo.Combos;
using WrathCombo.Core;
using WrathCombo.CustomComboNS.Functions;
using WrathCombo.Extensions;
using WrathCombo.Window.Tabs;

#endregion

namespace WrathCombo.Services.IPC;

public class Search(Leasing leasing)
{
    public Task? UpdatePresetCount;
    public CancellationTokenSource Cancel = new();

    /// <summary>
    ///     A shortcut for <see cref="StringComparison.CurrentCultureIgnoreCase" />.
    /// </summary>
    private const StringComparison ToLower =
        StringComparison.CurrentCultureIgnoreCase;

    private readonly Leasing _leasing = leasing;

    #region Aggregations of Leasing Configurations

    internal DateTime? LastCacheUpdateForAutoRotationConfigs;

    private Dictionary<AutoRotationConfigOption, Dictionary<string, int>>? _autoRotationConfigsField;
    internal Dictionary<AutoRotationConfigOption, Dictionary<string, int>> AllAutoRotationConfigsControlled
    {
        get
        {
            if (_autoRotationConfigsField is not null &&
                LastCacheUpdateForAutoRotationConfigs is not null &&
                _leasing.AutoRotationConfigsUpdated == LastCacheUpdateForAutoRotationConfigs)
                return _autoRotationConfigsField;

            _autoRotationConfigsField = _leasing.Registrations.Values
                .SelectMany(registration => registration
                    .AutoRotationConfigsControlled
                    .Select(pair => new
                    {
                        pair.Key,
                        registration.PluginName,
                        pair.Value,
                        registration.LastUpdated
                    }))
                .GroupBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.LastUpdated)
                        .ToDictionary(x => x.PluginName, x => x.Value)
                );

            LastCacheUpdateForAutoRotationConfigs = _leasing.AutoRotationConfigsUpdated;
            return _autoRotationConfigsField;
        }
    }

    internal DateTime? LastCacheUpdateForAllJobsControlled;

    private Dictionary<Job, Dictionary<string, bool>>? _jobsControlledField;
    internal Dictionary<Job, Dictionary<string, bool>> AllJobsControlled
    {
        get
        {
            if (_jobsControlledField is not null &&
                LastCacheUpdateForAllJobsControlled is not null &&
                _leasing.JobsUpdated == LastCacheUpdateForAllJobsControlled)
                return _jobsControlledField;

            _jobsControlledField = _leasing.Registrations.Values
                .SelectMany(registration => registration.JobsControlled
                    .Select(pair => new
                    {
                        pair.Key,
                        registration.PluginName,
                        pair.Value,
                        registration.LastUpdated
                    }))
                .GroupBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.LastUpdated)
                        .ToDictionary(x => x.PluginName, x => x.Value)
                );

            LastCacheUpdateForAllJobsControlled = _leasing.JobsUpdated;
            return _jobsControlledField;
        }
    }

    internal DateTime? LastCacheUpdateForAllPresetsControlled;

    private Dictionary<CustomComboPreset, Dictionary<string, (bool enabled, bool autoMode)>>? _presetsControlledField;
    internal Dictionary<CustomComboPreset, Dictionary<string, (bool enabled, bool autoMode)>> AllPresetsControlled
    {
        get
        {
            var presetsUpdated = (DateTime)(_leasing.CombosUpdated > _leasing.OptionsUpdated
                ? _leasing.CombosUpdated
                : _leasing.OptionsUpdated ?? DateTime.MinValue);

            if (_presetsControlledField is not null &&
                LastCacheUpdateForAllPresetsControlled is not null &&
                presetsUpdated == LastCacheUpdateForAllPresetsControlled)
                return _presetsControlledField;

            _presetsControlledField = _leasing.Registrations.Values
                .SelectMany(registration => registration.CombosControlled
                    .Select(pair => new
                    {
                        pair.Key,
                        registration.PluginName,
                        pair.Value.enabled,
                        pair.Value.autoMode,
                        registration.LastUpdated
                    }))
                .GroupBy(x => x.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.LastUpdated)
                        .ToDictionary(x => x.PluginName,
                            x => (x.enabled, x.autoMode))
                )
                .Concat(
                    _leasing.Registrations.Values
                        .SelectMany(registration => registration.OptionsControlled
                            .Select(pair => new
                            {
                                pair.Key,
                                registration.PluginName,
                                pair.Value,
                                registration.LastUpdated
                            }))
                        .GroupBy(x => x.Key)
                        .ToDictionary(
                            g => g.Key,
                            g => g.OrderByDescending(x => x.LastUpdated)
                                .ToDictionary(x => x.PluginName,
                                    x => (x.Value, false))
                        )
                )
                .DistinctBy(x => x.Key)
                .ToDictionary(pair => pair.Key, pair => pair.Value);

            LastCacheUpdateForAllPresetsControlled = presetsUpdated;
            return _presetsControlledField;
        }
    }

    #endregion

    #region Presets Information

    #region Cached Preset Info

    /// <summary>
    ///     The path to the configuration file for Wrath Combo.
    /// </summary>
    internal string ConfigFilePath
    {
        get
        {
            var pluginConfig = Svc.PluginInterface.GetPluginConfigDirectory();
            if (Path.EndsInDirectorySeparator(pluginConfig))
                pluginConfig = Path.TrimEndingDirectorySeparator(pluginConfig);
            pluginConfig = pluginConfig[..pluginConfig.LastIndexOf(Path.DirectorySeparatorChar)];
            pluginConfig = Path.Combine(pluginConfig, "WrathCombo.json");
            return pluginConfig;
        }
    }

    private DateTime _lastCacheUpdateForPresetStates = DateTime.MinValue;

    public CustomComboPreset GetRootParent(CustomComboPreset preset)
    {
        if (!Attribute.IsDefined(
                typeof(CustomComboPreset).GetField(preset.ToString())!,
                typeof(ParentComboAttribute)))
        {
            return preset;
        }

        var parentAttribute = (ParentComboAttribute)Attribute.GetCustomAttribute(
            typeof(CustomComboPreset).GetField(preset.ToString())!,
            typeof(ParentComboAttribute)
        )!;

        return GetRootParent(parentAttribute.ParentPreset);
    }

    private Dictionary<string, (Job Job, CustomComboPreset ID, CustomComboInfoAttribute Info, bool HasParentCombo, bool IsVariant, string ParentComboName)>? _presetsField;
    internal Dictionary<string, (Job Job, CustomComboPreset ID, CustomComboInfoAttribute Info, bool HasParentCombo, bool IsVariant, string ParentComboName)> Presets
    {
        get
        {
            return _presetsField ??= PresetStorage.AllPresets!
                .Cast<CustomComboPreset>()
                .Select(preset => new
                {
                    ID = preset,
                    JobId = (Job)preset.Attributes().CustomComboInfo.JobID,
                    InternalName = preset.ToString(),
                    Info = preset.Attributes().CustomComboInfo!,
                    HasParentCombo = preset.Attributes().Parent != null,
                    IsVariant = preset.Attributes().Variant != null,
                    ParentComboName = preset.Attributes().Parent != null
                        ? GetRootParent(preset).ToString()
                        : string.Empty
                })
                .Where(combo => !combo.InternalName.EndsWith("any", ToLower))
                .ToDictionary(
                    combo => combo.InternalName,
                    combo => (combo.JobId, combo.ID, combo.Info, combo.HasParentCombo, combo.IsVariant, combo.ParentComboName)
                );
        }
    }

    private Dictionary<string, Dictionary<ComboStateKeys, bool>>? _presetStatesField;
    internal Dictionary<string, Dictionary<ComboStateKeys, bool>> PresetStates
    {
        get
        {
            var presetsUpdated = (DateTime)(_leasing.CombosUpdated > _leasing.OptionsUpdated
                ? _leasing.CombosUpdated
                : _leasing.OptionsUpdated ?? DateTime.MinValue);

            if (!Debug.DebugConfig)
            {
                if (field != null &&
                    File.GetLastWriteTime(ConfigFilePath) <=
                    _lastCacheUpdateForPresetStates &&
                    presetsUpdated <= _lastCacheUpdateForPresetStates)
                    return field;
            }
            else
            {
                if (field != null &&
                    DateTime.Now.AddSeconds(-1) <=
                    _lastCacheUpdateForPresetStates &&
                    presetsUpdated <= _lastCacheUpdateForPresetStates)
                    return field;
            }

            _presetStatesField = Presets
                .ToDictionary(
                    preset => preset.Key,
                    preset =>
                    {
                        var isEnabled = CustomComboFunctions.IsEnabled(preset.Value.ID);
                        var ipcAutoMode = _leasing.CheckComboControlled(preset.Value.ID.ToString())?.autoMode ?? false;
                        var isAutoMode = Service.Configuration.AutoActions.TryGetValue(preset.Value.ID, out bool autoMode) &&
                            autoMode && preset.Value.ID.Attributes().AutoAction != null;
                        return new Dictionary<ComboStateKeys, bool>
                        {
                            { ComboStateKeys.Enabled, isEnabled },
                            { ComboStateKeys.AutoMode, isAutoMode || ipcAutoMode }
                        };
                    }
                );
            _lastCacheUpdateForPresetStates = DateTime.Now;
            UpdatePresetCount = Svc.Framework.RunOnTick(() => UpdateActiveJobPresets(), TimeSpan.FromSeconds(1), 0, Cancel.Token);
            return field;
        }
    }

    internal void UpdateActiveJobPresets()
    {
        ActiveJobPresets = Window.Functions.Presets.GetJobAutorots.Count;
    }

    internal int ActiveJobPresets = 0;

    #region Combo Information

    internal Dictionary<Job, List<string>> ComboNamesByJob =>
        Presets
            .Where(preset =>
                preset.Value is { IsVariant: false, HasParentCombo: false } &&
                !preset.Key.Contains("pvp", ToLower))
            .GroupBy(preset => preset.Value.Job)
            .ToDictionary(
                g => g.Key,
                g => g.Select(preset => preset.Key).ToList()
            );

    internal Dictionary<Job, Dictionary<string, Dictionary<ComboStateKeys, bool>>> ComboStatesByJob =>
        ComboNamesByJob
            .ToDictionary(
                job => job.Key,
                job => job.Value
                    .ToDictionary(
                        combo => combo,
                        combo => PresetStates[combo]
                    )
            );

    private DateTime _lastCacheUpdateForComboStatesByJobCategorized = DateTime.MinValue;

    private Dictionary<Job, Dictionary<ComboTargetTypeKeys, Dictionary<ComboSimplicityLevelKeys, Dictionary<string, Dictionary<ComboStateKeys, bool>>>>>? _comboStatesByJobCategorizedField;
    internal Dictionary<Job, Dictionary<ComboTargetTypeKeys, Dictionary<ComboSimplicityLevelKeys, Dictionary<string, Dictionary<ComboStateKeys, bool>>>>> ComboStatesByJobCategorized
    {
        get
        {
            if (File.GetLastWriteTime(ConfigFilePath) <=
                _lastCacheUpdateForComboStatesByJobCategorized)
                return field ?? [];

            Task.Run(() =>
            {
                field = Presets
                    .Where(preset =>
                        preset.Value is
                        { IsVariant: false, HasParentCombo: false } &&
                        !preset.Key.Contains("pvp", ToLower))
                    .SelectMany(preset => new[]
                    {
                        new
                        {
                            Job = (Job)preset.Value.Info.JobID,
                            Combo = preset.Key,
                            preset.Value.Info
                        }
                    })
                    .GroupBy(x => x.Job)
                    .ToDictionary(
                        g => g.Key,
                        g => g.GroupBy(x =>
                                x.Info.Name.Contains("heals - single", ToLower) ?
                                    ComboTargetTypeKeys.HealST :
                                    x.Info.Name.Contains("heals - aoe", ToLower) ?
                                        ComboTargetTypeKeys.HealMT :
                                        x.Info.Name.Contains("- aoe", ToLower) ||
                                        x.Info.Name.Contains("aoe dps feature",
                                            ToLower) ?
                                            ComboTargetTypeKeys.MultiTarget :
                                            x.Info.Name.Contains("- single target",
                                                ToLower) ||
                                            x.Info.Name.Contains(
                                                "single target dps feature",
                                                ToLower) ?
                                                ComboTargetTypeKeys.SingleTarget :
                                                ComboTargetTypeKeys.Other
                            )
                            .ToDictionary(
                                g2 => g2.Key,
                                g2 => g2.GroupBy(x =>
                                        x.Info.Name.Contains("advanced mode -",
                                            ToLower) ||
                                        x.Info.Name.Contains("dps feature",
                                            ToLower) ?
                                            ComboSimplicityLevelKeys.Advanced :
                                            x.Info.Name.Contains("simple mode -",
                                                ToLower) ?
                                                ComboSimplicityLevelKeys.Simple :
                                                ComboSimplicityLevelKeys.Other
                                    )
                                    .ToDictionary(
                                        g3 => g3.Key,
                                        g3 => g3.ToDictionary(
                                            x => x.Combo,
                                            x => ComboStatesByJob[x.Job][x.Combo]
                                        )
                                    )
                            )
                    );
                _lastCacheUpdateForComboStatesByJobCategorized = DateTime.Now;
            });

            return field ?? [];
        }
    }

    #endregion

    #region Options Information

    internal Dictionary<Job, Dictionary<string, List<string>>> OptionNamesByJob =>
        Presets
            .Where(preset =>
                preset.Value is { IsVariant: false, HasParentCombo: true } &&
                !preset.Key.Contains("pvp", ToLower))
            .GroupBy(preset => preset.Value.Job)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(preset => preset.Value.ParentComboName)
                    .ToDictionary(
                        g2 => g2.Key,
                        g2 => g2.Select(preset => preset.Key).ToList()
                    )
            );

    internal Dictionary<Job, Dictionary<string, Dictionary<string, Dictionary<ComboStateKeys, bool>>>> OptionStatesByJob =>
            OptionNamesByJob
                .ToDictionary(
                    job => job.Key,
                    job => job.Value
                        .ToDictionary(
                            parentCombo => parentCombo.Key,
                            parentCombo => parentCombo.Value
                                .ToDictionary(
                                    option => option,
                                    option => new Dictionary<ComboStateKeys, bool>
                                    {
                                    {
                                        ComboStateKeys.Enabled,
                                        PresetStates[option][ComboStateKeys.Enabled]
                                    }
                                    }
                                )
                        )
                );

    #endregion

    internal Dictionary<CustomComboPreset, bool> AutoActions =>
        PresetStates
            .ToDictionary(
                preset => Enum.Parse<CustomComboPreset>(preset.Key),
                preset => preset.Value[ComboStateKeys.AutoMode]
            );

    /// <summary>
    ///     A wrapper for <see cref="Core.PluginConfiguration.EnabledActions" /> with
    ///     IPC settings on top.
    /// </summary>
    internal HashSet<CustomComboPreset> EnabledActions =>
        PresetStates
            .Where(preset => preset.Value[ComboStateKeys.Enabled])
            .Select(preset => Enum.Parse<CustomComboPreset>(preset.Key))
            .ToHashSet();

    #endregion
    #endregion
}