> [!TIP]
> See [The Example C#](IPCExample.cs) for a barebones example of how to set up 
> the IPC in your plugin.

## Setting up the Wrath Combo IPC in your plugin

All examples use [ECommons'](https://github.com/NightmareXIV/ECommons/) EzIPC.

First you set up the IPC class for EzIPC to fill in abstract methods within:

```csharp
internal static class WrathIPC
{
    [EzIPC] private static readonly Func<string, string, string?, Guid?> RegisterForLeaseWithCallback;
    // + other methods signatures you want to use from Wrath's IPC
    // see https://github.com/PunishXIV/WrathCombo/blob/main/WrathCombo/Services/IPC/Provider.cs
    // and `ProvideAutoRotConfig.cs` next to it, for all the methods and their signatures
}
```
`Func` for methods with a return, last parameter being the return type.
`Action` for methods without a return.

Then you need to actually initiate EzIPC, adding a field to the same class:

```csharp
private static EzIPCDisposalToken[] _disposalTokens =
    EzIPC.Init(typeof(WrathIPC), "WrathCombo", SafeWrapper.IPCException);
```

You can also choose to add a property with Wrath's IPC status:
    
```csharp
internal static bool IsEnabled =>
    DalamudReflector.TryGetDalamudPlugin("WrathCombo", out _, false, true);
```

And since the IPC works on a lease system, it would make sense to add a property for
that as well: (done here as a property that tries to register a lease)

```csharp
internal static Guid? CurrentLease
{
    get
    {
        field ??= RegisterForLease("internalPluginName", "My Plugin's proper name");
        if (field is null)
            PluginLog.Warning("Failed to register for lease. " +
                              "See logs from Wrath Como for why.");
        return field;
    }
}
```

And the IPC is set up and ready to use.

### Setting up with a callback

If you would like to use a callback, to be notified when and why your lease was
cancelled, you can instead use the `RegisterForLeaseWithCallback` method:

```csharp
[EzIPC] private static readonly Func<string, string, string?, Guid?> RegisterForLeaseWithCallback;
```

But of course this needs you to provide a callback method, through your own IPC:
```csharp
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
```

And finally we would register our lease differently if we want that callback to be
used:

```csharp
internal static Guid? CurrentLease
{
    get
    {
        field ??= RegisterForLeaseWithCallback(
            "internalPluginName",
            "My Plugin's proper name",
            "myPluginIPCPrefixJustForWrath" // this can also be null, if the prefix
                                            // is the same as the internal plugin name
        );
        if (field is null)
            PluginLog.Warning("Failed to register for lease. " +
                              "See logs from Wrath Como for why.");
        return field;
    }
}
```

## How to use the setup IPC

Once you are set up to work with the Wrath Combo IPC, it can be very simple if all
you are trying to do is make sure the user is set up to for Auto-Rotation:

```csharp
if (WrathIPC.IsEnabled)
{
    // enable Wrath Combo Auto-Rotation
    WrathIPC.SetAutoRotationState(WrathIPC.CurrentLease, true);
    // make sure the job is ready for Auto-Rotation
    WrathIPC.SetCurrentJobAutoRotationReady(WrathIPC.CurrentLease);
    // if the job is ready, all the user's settings are locked
    // if the job is not ready, it turns on the job's simple modes, or if those don't
    // exist, it turns on the job's advanced modes with all options enabled
}
```

Or you can make it more advanced, making sure Auto-Rotation settings are as you want
them to be.

This does require you to copy over the [`AutoRotationConfigOption` enum](https://github.com/PunishXIV/WrathCombo/blob/main/WrathCombo/Services/IPC/Enums.cs#L117).

```csharp
if (WrathIPC.IsEnabled)
{
    WrathIPC.SetAutoRotationState(WrathIPC.CurrentLease, true);
    WrathIPC.SetCurrentJobAutoRotationReady(WrathIPC.CurrentLease);
    WrathIPC.SetAutoRotationConfigState(WrathIPC.CurrentLease,
        AutoRotationConfigOption.InCombatOnly, false);
    WrathIPC.SetAutoRotationConfigState(WrathIPC.CurrentLease,
        AutoRotationConfigOption.AutoRez, true);
    WrathIPC.SetAutoRotationConfigState(WrathIPC.CurrentLease,
        AutoRotationConfigOption.SingleTargetHPP, 50);
}
```
See how AutoDuty does this -and to what extent-
[here, in `SetAutoMode`](https://github.com/ffxivcode/AutoDuty/blob/master/AutoDuty/IPC/IPCSubscriber.cs#L448).

Lastly, you will need to release control when you are done, you are incentivized to
release control yourself so the user is not incentivized to revoke control from you:

```csharp
if (yourCodeNoLongerRequiresAutoRotation) // such as when the user disables your auto mode
    WrathIPC.ReleaseControl(WrathIPC.CurrentLease);
```

There are also a variety of `Get` methods in addition to `Set` ones shown here,
as well as many other `Set` methods such as for controlling individual Combo 
settings, however when `Set`ting, it also locks the value away from the user.
So in most cases, you will want to `Set`, without having to `Get` first.

For more information on the nuances of using the IPC, please see the extra resources
below.
#188 especially goes into detail on the times when your plugin may not be allowed
to register a lease, for example, and the Provider files provide significant detail
on any nuance with each method and how the limitation on a lease's `Set` count works.

Such resources will be consolidated, and kept updated, here at a later point.

## Other resources for setting up and using the IPC

- Provider files ([main](https://github.com/PunishXIV/WrathCombo/blob/main/WrathCombo/Services/IPC/Provider.cs),
  [auto-rot settings](https://github.com/PunishXIV/WrathCombo/blob/main/WrathCombo/Services/IPC/ProvideAutoRotConfig.cs)) -
  These are the actual IPC methods provided by Wrath and have verbose doc comments.
- #188 - this is where the IPC was introduced, and lists out the methods and some
  usage notes.
- ffxivcode/AutoDuty#714 - this is where the IPC was first implemented in another
  plugin (AutoDuty).

## Changelog

- #232 - Fixed capability to request a cancellation callback via your own IPC 
  method, `1.0.0.7`.
- #188 - Initial introduction of the IPC, `1.0.0.6`.
