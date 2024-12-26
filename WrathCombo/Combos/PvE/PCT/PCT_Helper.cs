using Dalamud.Game.ClientState.JobGauge.Types;
using System;
using System.Collections.Generic;
using WrathCombo.CustomComboNS;
using WrathCombo.CustomComboNS.Functions;
using static WrathCombo.CustomComboNS.Functions.CustomComboFunctions;


namespace WrathCombo.Combos.PvE;

internal partial class PCT
{
    internal static PCTopenerMaxLevel1 Opener1 = new();
    internal static PCTopenerMaxLevel2 Opener2 = new();

    private static PCTGauge Gauge = GetJobGauge<PCTGauge>();

    internal static WrathOpener Opener()
    {
        if (Opener1.LevelChecked && Config.PCT_Opener_Choice == 0) return Opener1;
        if (Opener2.LevelChecked && Config.PCT_Opener_Choice == 1) return Opener2;

        return WrathOpener.Dummy;
    }

    public static bool HasMotifs()
    {

        if (!Gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Pom))
            return false;

        if (!Gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Weapon))
            return false;

        if (!Gauge.CanvasFlags.HasFlag(Dalamud.Game.ClientState.JobGauge.Enums.CanvasFlags.Landscape))
            return false;

        return true;
    }

    internal class PCTopenerMaxLevel1 : WrathOpener
    {
        //2nd GCD Starry Opener
        public override int MinOpenerLevel => 100;

        public override int MaxOpenerLevel => 109;
        public override List<uint> OpenerActions { get; set; } =
        [
            RainbowDrip,
            PomMuse,
            StrikingMuse,
            WingMotif,
            StarryMuse,
            HammerStamp,
            SubtractivePalette,
            BlizzardinCyan,
            StoneinYellow,
            ThunderinMagenta,
            CometinBlack,
            WingedMuse,
            MogoftheAges,
            StarPrism,
            HammerBrush,
            PolishingHammer,
            RainbowDrip,
            All.Swiftcast,
            ClawMotif,
            ClawedMuse,
        ];
        internal override UserData? ContentCheckConfig => Config.PCT_Balance_Content;

        public override List<(int[] Steps, uint NewAction, Func<bool> Condition)> SubstitutionSteps { get; set; } =
[
            ([8, 9, 10], BlizzardinCyan, () => OriginalHook(BlizzardinCyan) == BlizzardinCyan),
            ([8, 9, 10], StoneinYellow, () => OriginalHook(BlizzardinCyan) == StoneinYellow),
            ([8, 9, 10], ThunderinMagenta, () => OriginalHook(BlizzardinCyan) == ThunderinMagenta),
            ([11], HolyInWhite, () => !HasEffect(Buffs.MonochromeTones)),
        ];

        public override bool HasCooldowns()
        {
            if (!ActionReady(StarryMuse))
                return false;

            if (GetRemainingCharges(LivingMuse) < 3)
                return false;

            if (GetRemainingCharges(SteelMuse) < 2)
                return false;

            if (!HasMotifs())
                return false;

            if (HasEffect(Buffs.SubtractivePalette))
                return false;

            if (IsOnCooldown(All.Swiftcast))
                return false;

            return true;
        }

    }

    internal class PCTopenerMaxLevel2 : WrathOpener
    {
        //3rd GCD Starry Opener
        public override int MinOpenerLevel => 100;

        public override int MaxOpenerLevel => 109;
        public override List<uint> OpenerActions { get; set; } =
        [
            RainbowDrip,
            StrikingMuse,
            HolyInWhite,
            PomMuse,
            WingMotif,
            StarryMuse,
            HammerStamp,
            SubtractivePalette,
            BlizzardinCyan,
            BlizzardinCyan,
            BlizzardinCyan,
            CometinBlack,
            WingedMuse,
            MogoftheAges,
            StarPrism,
            HammerBrush,
            PolishingHammer,
            FireInRed,
            All.Swiftcast,
            ClawMotif,
            ClawedMuse
        ];
        internal override UserData? ContentCheckConfig => Config.PCT_Balance_Content;

        public override List<(int[] Steps, uint NewAction, Func<bool> Condition)> SubstitutionSteps { get; set; } =
        [
            ([3], CometinBlack, () => HasEffect(Buffs.MonochromeTones)),
            ([9, 10, 11], BlizzardinCyan, () => OriginalHook(BlizzardinCyan) == BlizzardinCyan),
             ([9, 10, 11], StoneinYellow, () => OriginalHook(BlizzardinCyan) == StoneinYellow),
            ([9, 10, 11], ThunderinMagenta, () => OriginalHook(BlizzardinCyan) == ThunderinMagenta),
            ([12], HolyInWhite, () => !HasEffect(Buffs.MonochromeTones)),
        ];

        public override bool HasCooldowns()
        {
            if (!ActionReady(StarryMuse))
                return false;

            if (GetRemainingCharges(LivingMuse) < 3)
                return false;

            if (GetRemainingCharges(SteelMuse) < 2)
                return false;

            if (!HasMotifs())
                return false;

            if (HasEffect(Buffs.SubtractivePalette))
                return false;

            if (IsOnCooldown(All.Swiftcast))
                return false;

            return true;
        }

    }
}