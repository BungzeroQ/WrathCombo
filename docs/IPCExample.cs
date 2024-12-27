using System;
using ECommons.EzIpcManager;
using ECommons.Logging;
using ECommons.Reflection;

namespace WrathCombo;

public static class YourCode
{
    public static void EnableWrathAuto()
    {
        if (!WrathIPC.IsEnabled) return;
        try
        {
            var lease = (Guid)WrathIPC.CurrentLease!;
            // enable Wrath Combo Auto-Rotation
            WrathIPC.SetAutoRotationState(lease, true);
            // make sure the job is ready for Auto-Rotation
            WrathIPC.SetCurrentJobAutoRotationReady(lease);
            // if the job is ready, all the user's settings are locked
            // if the job is not ready, it turns on the job's simple modes, or if those don't
            // exist, it turns on the job's advanced modes with all options enabled
        }
        catch (Exception e)
        {
            PluginLog.Error("Unknown Wrath IPC error," +
                            "probably inability to register a lease." +
                            "\n" + e.Message);
        }
    }

    public static void EnableWrathAutoAndConfigureIt()
    {
        if (!WrathIPC.IsEnabled) return;
        try
        {
            var lease = (Guid)WrathIPC.CurrentLease!;
            WrathIPC.SetAutoRotationState(lease, true);
            WrathIPC.SetCurrentJobAutoRotationReady(lease);
            WrathIPC.SetAutoRotationConfigState(lease,
                WrathIPC.AutoRotationConfigOption.InCombatOnly, false);
            WrathIPC.SetAutoRotationConfigState(lease,
                WrathIPC.AutoRotationConfigOption.AutoRez, true);
            WrathIPC.SetAutoRotationConfigState(lease,
                WrathIPC.AutoRotationConfigOption.SingleTargetHPP, 60);
        }
        catch (Exception e)
        {
            PluginLog.Error("Unknown Wrath IPC error," +
                            "probably inability to register a lease." +
                            "\n" + e.Message);
        }
    }
}

internal static class WrathIPC
{
    private static EzIPCDisposalToken[] _disposalTokens =
        EzIPC.Init(typeof(WrathIPC), "WrathCombo", SafeWrapper.IPCException);

    internal static bool IsEnabled =>
        DalamudReflector.TryGetDalamudPlugin("WrathCombo", out _, false, true);

    internal static Guid? CurrentLease
    {
        get
        {
            field ??= RegisterForLeaseWithCallback(
                "internalPluginName",
                "My Plugin's proper name",
                "myPluginIPCPrefixJustForWrath" // can be null, if your prefix=internal name
            );
            if (field is null)
                PluginLog.Warning("Failed to register for lease. " +
                                  "See logs from Wrath Como for why.");
            return field;
        }
    }

    [EzIPC] internal static readonly Func<string, string, Guid?> RegisterForLease;
    [EzIPC] internal static readonly Func<string, string, string?, Guid?>
        RegisterForLeaseWithCallback;
    [EzIPC] internal static readonly Action<Guid, bool> SetAutoRotationState;
    [EzIPC] internal static readonly Action<Guid> SetCurrentJobAutoRotationReady;
    [EzIPC] internal static readonly Action<Guid, AutoRotationConfigOption, object>
        SetAutoRotationConfigState;
    [EzIPC] internal static readonly Action<Guid> ReleaseControl;

    public enum AutoRotationConfigOption
    {
        InCombatOnly, //bool
        DPSRotationMode,
        HealerRotationMode,
        FATEPriority, //bool
        QuestPriority,//bool
        SingleTargetHPP,//int
        AoETargetHPP,//int
        SingleTargetRegenHPP,//int
        ManageKardia,//bool
        AutoRez,//bool
        AutoRezDPSJobs,//bool
        AutoCleanse,//bool
    }
}

public class MyIPC
{
    internal MyIPC()
    {
        EzIPC.Init(this, prefix: "myPluginIPCPrefixJustForWrath");
    }

    [EzIPC]
    public void WrathComboCallback(int reason, string additionalInfo)
    {
        PluginLog.Warning($"Lease was cancelled for reason {reason}. " +
                          $"Additional info: {additionalInfo}");

        if (reason == 0)
            PluginLog.Error("The user cancelled our lease." +
                            "We are suspended from creating a new lease for now.");

        // you can also convert the `reason` back to the `CancellationReason` enum.
        // you can copy this enum into your own class from:
        // https://github.com/PunishXIV/WrathCombo/blob/main/WrathCombo/Services/IPC/Enums.cs#L117
    }
}
