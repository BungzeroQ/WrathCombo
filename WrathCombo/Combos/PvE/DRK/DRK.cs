#region

using System.Linq;
using WrathCombo.Combos.PvE.Content;
using WrathCombo.CustomComboNS;
using WrathCombo.Data;

// ReSharper disable UnusedType.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

#endregion

namespace WrathCombo.Combos.PvE;

internal partial class DRK
{
    internal class DRK_ST_Combo : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } =
            CustomComboPreset.DRK_ST_Combo;

        protected override uint Invoke(uint actionID)
        {
            // Bail if not looking at the replaced action
            if (actionID is not HardSlash) return actionID;

            #region Variables

            const Combo comboFlags = Combo.ST | Combo.Adv;
            var newAction = HardSlash;
            var inManaPoolingContent =
                ContentCheck.IsInConfiguredContent(
                    Config.DRK_ST_ManaSpenderPoolingDifficulty,
                    Config.DRK_ST_ManaSpenderPoolingDifficultyListSet
                );
            var mpRemaining = inManaPoolingContent
                ? Config.DRK_ST_ManaSpenderPooling
                : 0;
            var hpRemainingShadow = Config.DRK_ST_LivingShadowThreshold;
            var hpRemainingDelirium = Config.DRK_ST_DeliriumThreshold;

            #endregion

            // Unmend Option
            if (IsEnabled(CustomComboPreset.DRK_ST_RangedUptime)
                && LevelChecked(Unmend)
                && !InMeleeRange()
                && HasBattleTarget())
                return Unmend;

            // Opener
            if (IsEnabled(CustomComboPreset.DRK_ST_BalanceOpener)
                && Opener().FullOpener(ref actionID))
                return actionID;

            // Bail if not in combat
            if (!InCombat()) return HardSlash;

            // Variant Abilities
            if (TryGetVariantAction(comboFlags, ref newAction))
                return newAction;

            // Cooldowns
            if (TryGetCooldownAction(comboFlags, ref newAction))
                return newAction;

            // oGCDs
            if (CanWeave() || CanDelayedWeave())
            {
                var inMitigationContent =
                    ContentCheck.IsInConfiguredContent(
                        Config.DRK_ST_MitDifficulty,
                        Config.DRK_ST_MitDifficultyListSet
                    );
                // Mitigation first
                if (IsEnabled(CustomComboPreset.DRK_ST_Mitigation) &&
                    inMitigationContent &&
                    TryGetMitigationAction(comboFlags, ref newAction))
                    return newAction;

                // Mana Spenders
                if (IsEnabled(CustomComboPreset.DRK_ST_ManaOvercap)
                    && CombatEngageDuration().TotalSeconds >= 5)
                {
                    // Spend mana to limit when not near even minute burst windows
                    if (IsEnabled(CustomComboPreset.DRK_ST_ManaSpenderPooling)
                        && GetCooldownRemainingTime(LivingShadow) >= 45
                        && LocalPlayer.CurrentMp > (mpRemaining + 3000)
                        && LevelChecked(EdgeOfDarkness))
                        return OriginalHook(EdgeOfDarkness);

                    // Keep Darkside up
                    if (LocalPlayer.CurrentMp > 8500
                        || (Gauge.DarksideTimeRemaining < 10000 &&
                            LocalPlayer.CurrentMp > (mpRemaining + 3000)))
                    {
                        // Return Edge of Darkness if available
                        if (LevelChecked(EdgeOfDarkness))
                            return OriginalHook(EdgeOfDarkness);
                        if (LevelChecked(FloodOfDarkness)
                            && !LevelChecked(EdgeOfDarkness))
                            return FloodOfDarkness;
                    }

                    // Spend Dark Arts
                    if (Gauge.HasDarkArts
                        && LevelChecked(EdgeOfDarkness)
                        && CombatEngageDuration().TotalSeconds >= 10
                        && (Gauge.ShadowTimeRemaining > 0 // In Burst
                            || (IsEnabled(CustomComboPreset
                                    .DRK_ST_DarkArtsDropPrevention)
                                && HasOwnTBN))) // TBN
                        return OriginalHook(EdgeOfDarkness);
                }

                // Bigger Cooldown Features
                if (Gauge.DarksideTimeRemaining > 1)
                {
                    // Living Shadow
                    var inLivingShadowThresholdContent =
                        ContentCheck.IsInConfiguredContent(
                            Config.DRK_ST_LivingShadowThresholdDifficulty,
                            Config.DRK_ST_LivingShadowThresholdDifficultyListSet
                        );
                    if (IsEnabled(CustomComboPreset.DRK_ST_CDs)
                        && IsEnabled(CustomComboPreset.DRK_ST_CD_Shadow)
                        && IsOffCooldown(LivingShadow)
                        && LevelChecked(LivingShadow)
                        && ((inLivingShadowThresholdContent
                             && GetTargetHPPercent() > hpRemainingShadow)
                            || !inLivingShadowThresholdContent))
                        return LivingShadow;

                    // Delirium
                    var inDeliriumThresholdContent =
                        ContentCheck.IsInConfiguredContent(
                            Config.DRK_ST_DeliriumThresholdDifficulty,
                            Config.DRK_ST_DeliriumThresholdDifficultyListSet
                        );
                    if (IsEnabled(CustomComboPreset.DRK_ST_Delirium)
                        && IsOffCooldown(BloodWeapon)
                        && LevelChecked(BloodWeapon)
                        && ((inDeliriumThresholdContent
                             && GetTargetHPPercent() > hpRemainingDelirium)
                            || !inDeliriumThresholdContent)
                        && CombatEngageDuration().TotalSeconds > 5)
                        return OriginalHook(Delirium);

                    // Big CDs
                    if (IsEnabled(CustomComboPreset.DRK_ST_CDs)
                        && CombatEngageDuration().TotalSeconds > 5)
                    {
                        // Salted Earth
                        if (IsEnabled(CustomComboPreset.DRK_ST_CD_Salt))
                        {
                            // Cast Salted Earth
                            if (!HasEffect(Buffs.SaltedEarth)
                                && ActionReady(SaltedEarth))
                                return SaltedEarth;
                            //Cast Salt and Darkness
                            if (HasEffect(Buffs.SaltedEarth)
                                && GetBuffRemainingTime(Buffs.SaltedEarth) < 7
                                && ActionReady(SaltAndDarkness))
                                return OriginalHook(SaltAndDarkness);
                        }

                        // Shadowbringer
                        if (LevelChecked(Shadowbringer)
                            && IsEnabled(CustomComboPreset.DRK_ST_CD_Bringer))
                        {
                            if ((GetRemainingCharges(Shadowbringer) > 0
                                 && IsNotEnabled(CustomComboPreset
                                     .DRK_ST_CD_BringerBurst)) // Dump
                                ||
                                (IsEnabled(CustomComboPreset
                                     .DRK_ST_CD_BringerBurst)
                                 && GetRemainingCharges(Shadowbringer) > 0
                                 && Gauge.ShadowTimeRemaining > 1
                                 && IsOnCooldown(LivingShadow)
                                 && !HasEffect(Buffs.Scorn))) // Burst
                                return Shadowbringer;
                        }

                        // Carve and Spit
                        if (IsEnabled(CustomComboPreset.DRK_ST_CD_Spit)
                            && IsOffCooldown(CarveAndSpit)
                            && LevelChecked(CarveAndSpit))
                            return CarveAndSpit;
                    }
                }
            }

            // Delirium Chain
            if (LevelChecked(Delirium)
                && LevelChecked(ScarletDelirium)
                && IsEnabled(CustomComboPreset.DRK_ST_Delirium_Chain)
                && HasEffect(Buffs.EnhancedDelirium)
                && Gauge.DarksideTimeRemaining > 0)
                return OriginalHook(Bloodspiller);

            //Delirium Features
            if (LevelChecked(Delirium)
                && IsEnabled(CustomComboPreset.DRK_ST_Bloodspiller))
            {
                //Bloodspiller under Delirium
                var deliriumBuff = TraitLevelChecked(Traits.EnhancedDelirium)
                    ? Buffs.EnhancedDelirium
                    : Buffs.Delirium;
                if (GetBuffStacks(deliriumBuff) > 0)
                    return Bloodspiller;

                //Blood management outside of Delirium
                if (IsEnabled(CustomComboPreset.DRK_ST_Delirium)
                    && ((Gauge.Blood >= 60 &&
                         GetCooldownRemainingTime(Delirium) is > 0
                             and < 3) // Prep for Delirium
                        || (Gauge.Blood >= 50 &&
                            GetCooldownRemainingTime(Delirium) >
                            37))) // Regular Bloodspiller
                    return Bloodspiller;
            }

            // 1-2-3 combo
            if (!(ComboTimer > 0)) return HardSlash;
            if (ComboAction == HardSlash && LevelChecked(SyphonStrike))
                return SyphonStrike;
            if (ComboAction == SyphonStrike && LevelChecked(Souleater))
            {
                // Blood management
                if (IsEnabled(CustomComboPreset.DRK_ST_BloodOvercap)
                    && LevelChecked(Bloodspiller) && Gauge.Blood >= 90)
                    return Bloodspiller;

                return Souleater;
            }

            return HardSlash;
        }
    }

    internal class DRK_AoE_Combo : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } =
            CustomComboPreset.DRK_AoE_Combo;

        protected override uint Invoke(uint actionID)
        {
            // Bail if not looking at the replaced action
            if (actionID is not Unleash) return actionID;

            #region Variables

            const Combo comboFlags = Combo.AoE | Combo.Adv;
            var newAction = Unleash;
            var hpRemainingShadow = Config.DRK_AoE_LivingShadowThreshold;
            var hpRemainingDelirium = Config.DRK_AoE_DeliriumThreshold;

            #endregion

            // Bail if not in combat
            if (!InCombat()) return Unleash;

            // Variant Abilities
            if (TryGetVariantAction(comboFlags, ref newAction))
                return newAction;

            // Disesteem
            if (LevelChecked(LivingShadow)
                && LevelChecked(Disesteem)
                && IsEnabled(CustomComboPreset.DRK_AoE_CD_Disesteem)
                && HasEffect(Buffs.Scorn)
                && (Gauge.DarksideTimeRemaining > 0 // Optimal usage
                    || GetBuffRemainingTime(Buffs.Scorn) < 5)) // Emergency usage
                return OriginalHook(Disesteem);

            // oGCDs
            if (CanWeave() || CanDelayedWeave())
            {
                // Mitigation first
                if (IsEnabled(CustomComboPreset.DRK_AoE_Mitigation) &&
                    TryGetMitigationAction(comboFlags, ref newAction))
                    return newAction;

                // Mana Features
                if (IsEnabled(CustomComboPreset.DRK_AoE_ManaOvercap)
                    && LevelChecked(FloodOfDarkness)
                    && (LocalPlayer.CurrentMp > 8500 ||
                        (Gauge.DarksideTimeRemaining < 10 &&
                         LocalPlayer.CurrentMp >= 3000)))
                    return OriginalHook(FloodOfDarkness);

                // Spend Dark Arts
                if (IsEnabled(CustomComboPreset.DRK_AoE_ManaOvercap)
                    && Gauge.HasDarkArts
                    && LevelChecked(FloodOfDarkness))
                    return OriginalHook(FloodOfDarkness);

                // Living Shadow
                var inLivingShadowThresholdContent =
                    ContentCheck.IsInConfiguredContent(
                        Config.DRK_AoE_LivingShadowThresholdDifficulty,
                        Config.DRK_AoE_LivingShadowThresholdDifficultyListSet
                    );
                if (IsEnabled(CustomComboPreset.DRK_AoE_CD_Shadow)
                    && IsOffCooldown(LivingShadow)
                    && LevelChecked(LivingShadow)
                    && ((inLivingShadowThresholdContent
                         && GetTargetHPPercent() > hpRemainingShadow)
                        || !inLivingShadowThresholdContent))
                    return LivingShadow;

                // Delirium
                var inDeliriumThresholdContent =
                    ContentCheck.IsInConfiguredContent(
                        Config.DRK_AoE_DeliriumThresholdDifficulty,
                        Config.DRK_AoE_DeliriumThresholdDifficultyListSet
                    );
                if (IsEnabled(CustomComboPreset.DRK_AoE_Delirium)
                    && IsOffCooldown(BloodWeapon)
                    && LevelChecked(BloodWeapon)
                    && ((inDeliriumThresholdContent
                         && GetTargetHPPercent() > hpRemainingDelirium)
                        || !inDeliriumThresholdContent))
                    return OriginalHook(Delirium);

                if (Gauge.DarksideTimeRemaining > 1)
                {
                    // Salted Earth
                    if (IsEnabled(CustomComboPreset.DRK_AoE_CD_Salt))
                    {
                        // Cast Salted Earth
                        if (!HasEffect(Buffs.SaltedEarth)
                            && ActionReady(SaltedEarth))
                            return SaltedEarth;
                        //Cast Salt and Darkness
                        if (HasEffect(Buffs.SaltedEarth)
                            && GetBuffRemainingTime(Buffs.SaltedEarth) < 9
                            && ActionReady(SaltAndDarkness))
                            return OriginalHook(SaltAndDarkness);
                    }

                    // Shadowbringer
                    if (IsEnabled(CustomComboPreset.DRK_AoE_CD_Bringer)
                        && LevelChecked(Shadowbringer)
                        && GetRemainingCharges(Shadowbringer) > 0)
                        return Shadowbringer;

                    // Abyssal Drain
                    if (IsEnabled(CustomComboPreset.DRK_AoE_CD_Drain)
                        && LevelChecked(AbyssalDrain)
                        && IsOffCooldown(AbyssalDrain)
                        && PlayerHealthPercentageHp() <= 60)
                        return AbyssalDrain;
                }
            }

            // Delirium Chain
            if (LevelChecked(Delirium)
                && LevelChecked(Impalement)
                && IsEnabled(CustomComboPreset.DRK_AoE_Delirium_Chain)
                && HasEffect(Buffs.EnhancedDelirium)
                && Gauge.DarksideTimeRemaining > 1)
                return OriginalHook(Quietus);

            // 1-2-3 combo
            if (!(ComboTimer > 0)) return Unleash;
            if (ComboAction == Unleash && LevelChecked(StalwartSoul))
            {
                if (IsEnabled(CustomComboPreset.DRK_AoE_BloodOvercap)
                    && Gauge.Blood >= 90
                    && LevelChecked(Quietus))
                    return Quietus;
                return StalwartSoul;
            }

            return Unleash;
        }
    }

    internal class DRK_oGCD : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } =
            CustomComboPreset.DRK_oGCD;

        protected override uint Invoke(uint actionID)
        {
            if (actionID is not (CarveAndSpit or AbyssalDrain)) return actionID;

            if (IsOffCooldown(LivingShadow)
                && LevelChecked(LivingShadow))
                return LivingShadow;

            if (IsOffCooldown(SaltedEarth)
                && LevelChecked(SaltedEarth))
                return SaltedEarth;

            if (IsOffCooldown(CarveAndSpit)
                && LevelChecked(AbyssalDrain))
                return actionID;

            if (IsOffCooldown(SaltAndDarkness)
                && HasEffect(Buffs.SaltedEarth)
                && LevelChecked(SaltAndDarkness))
                return SaltAndDarkness;

            if (IsEnabled(CustomComboPreset.DRK_Shadowbringer_oGCD)
                && GetCooldownRemainingTime(Shadowbringer) < 60
                && LevelChecked(Shadowbringer)
                && Gauge.DarksideTimeRemaining > 0)
                return Shadowbringer;

            return actionID;
        }
    }

    #region One-Button Mitigation
    internal class DRK_Mit_OneButton : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } =
            CustomComboPreset.DRK_Mit_OneButton;

        protected override uint Invoke(uint actionID)
        {
            if (actionID is not DarkMind) return actionID;

            if (IsEnabled(CustomComboPreset.DRK_Mit_LivingDead_Max) &&
                ActionReady(LivingDead) &&
                PlayerHealthPercentageHp() <= Config.DRK_Mit_LivingDead_Health &&
                ContentCheck.IsInConfiguredContent(
                    Config.DRK_Mit_EmergencyLivingDead_Difficulty,
                    Config.DRK_Mit_EmergencyLivingDead_DifficultyListSet
                ))
                return LivingDead;

            foreach (var priority in Config.DRK_Mit_Priorities.Items.OrderBy(x => x))
            {
                var index = Config.DRK_Mit_Priorities.IndexOf(priority);
                if (CheckMitigationConfigMeetsRequirements(index, out var action))
                    return action;
            }

            return actionID;
        }
    }
    #endregion
}
