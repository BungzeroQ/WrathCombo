using System.Linq;
using WrathCombo.Combos.PvE.Content;
using WrathCombo.Core;
using WrathCombo.CustomComboNS;
using WrathCombo.Data;
using WrathCombo.Extensions;
namespace WrathCombo.Combos.PvE;

internal partial class SCH : Healer
{
    /*
     * SCH_Consolation
     * Even though Summon Seraph becomes Consolation,
     * This Feature also places Seraph's AoE heal+barrier ontop of the existing fairy AoE skill, Fey Blessing
     */
    internal class SCH_Consolation : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Consolation;
        protected override uint Invoke(uint actionID)
            => actionID is FeyBlessing && LevelChecked(SummonSeraph) && Gauge.SeraphTimer > 0 ? Consolation : actionID;
    }

    /*
     * SCH_Lustrate
     * Replaces Lustrate with Excogitation when Excogitation is ready.
     */
    internal class SCH_Lustrate : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Lustrate;
        protected override uint Invoke(uint actionID) =>
            actionID is Lustrate &&
            LevelChecked(Excogitation) && IsOffCooldown(Excogitation)
                ? Excogitation
                : actionID;
    }

    /*
     * SCH_Recitation
     * Replaces Recitation with selected one of its combo skills.
     */
    internal class SCH_Recitation : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Recitation;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not Recitation || !HasStatusEffect(Buffs.Recitation))
                return actionID;

            switch ((int)Config.SCH_Recitation_Mode)
            {
                case 0: return OriginalHook(Adloquium);
                case 1: return OriginalHook(Succor);
                case 2: return OriginalHook(Indomitability);
                case 3: return OriginalHook(Excogitation);
            }

            return actionID;
        }
    }

    /*
     * SCH_Aetherflow
     * Replaces all Energy Drain actions with Aetherflow when depleted, or just Energy Drain
     * Dissipation option to show if Aetherflow is on Cooldown
     * Recitation also an option
     */
    internal class SCH_Aetherflow : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Aetherflow;
        protected override uint Invoke(uint actionID)
        {
            if (!AetherflowList.Contains(actionID) || !LevelChecked(Aetherflow))
                return actionID;

            bool hasAetherFlows = HasAetherflow(); //False if Zero stacks

            if (IsEnabled(CustomComboPreset.SCH_Aetherflow_Recite) &&
                LevelChecked(Recitation) &&
                (IsOffCooldown(Recitation) || HasStatusEffect(Buffs.Recitation)))
            {
                //Recitation Indominability and Excogitation, with optional check against AF zero stack count
                bool alwaysShowReciteExcog = Config.SCH_Aetherflow_Recite_ExcogMode == 1;

                if (Config.SCH_Aetherflow_Recite_Excog &&
                    (alwaysShowReciteExcog ||
                     !alwaysShowReciteExcog && !hasAetherFlows) && actionID is Excogitation)
                {
                    //Do not merge this nested if with above. Won't procede with next set
                    return HasStatusEffect(Buffs.Recitation) && IsOffCooldown(Excogitation)
                        ? Excogitation
                        : Recitation;
                }

                bool alwaysShowReciteIndom = Config.SCH_Aetherflow_Recite_IndomMode == 1;

                if (Config.SCH_Aetherflow_Recite_Indom &&
                    (alwaysShowReciteIndom ||
                     !alwaysShowReciteIndom && !hasAetherFlows) && actionID is Indomitability)
                {
                    //Same as above, do not nest with above. It won't procede with the next set
                    return HasStatusEffect(Buffs.Recitation) && IsOffCooldown(Excogitation)
                        ? Indomitability
                        : Recitation;
                }
            }
            if (!hasAetherFlows)
            {
                bool showAetherflowOnAll = Config.SCH_Aetherflow_Display == 1;

                if ((actionID is EnergyDrain && !showAetherflowOnAll || showAetherflowOnAll) &&
                    IsOffCooldown(actionID))
                {
                    if (IsEnabled(CustomComboPreset.SCH_Aetherflow_Dissipation) &&
                        ActionReady(Dissipation) && IsOnCooldown(Aetherflow) && HasPetPresent())
                        //Dissipation requires fairy, can't seem to make it replace dissipation with fairy summon feature *shrug*
                        return Dissipation;

                    return Aetherflow;
                }
            }
            return actionID;
        }
    }

    /*
     * SCH_Raise (Swiftcast Raise combo)
     * Swiftcast changes to Raise when swiftcast is on cooldown
     */
    internal class SCH_Raise : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Raise;
        protected override uint Invoke(uint actionID) =>
            actionID == Role.Swiftcast && IsOnCooldown(Role.Swiftcast)
                ? IsEnabled(CustomComboPreset.SCH_Raise_Retarget)
                    ? Resurrection.Retarget(Role.Swiftcast,
                        SimpleTarget.Stack.AllyToRaise)
                    : Resurrection
                : actionID;
    }

    // Replaces Fairy abilities with Fairy summoning with Eos
    internal class SCH_FairyReminder : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_FairyReminder;
        protected override uint Invoke(uint actionID)
            => FairyList.Contains(actionID) && NeedToSummon ? SummonEos : actionID;
    }

    /*
     * SCH_DeploymentTactics
     * Combos Deployment Tactics with Adloquium by showing Adloquim when Deployment Tactics is ready,
     * Recitation is optional, if one wishes to Crit the shield first
     * Supports soft targetting and self as a fallback.
     */
    internal class SCH_DeploymentTactics : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_DeploymentTactics;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not DeploymentTactics || !ActionReady(DeploymentTactics))
                return actionID;

            //Grab our target
            var healTarget = OptionalTarget ?? SimpleTarget.Stack.AllyToHeal;

            //Check for the Galvanize shield buff. Start applying if it doesn't exist
            if (!HasStatusEffect(Buffs.Galvanize, healTarget)) 
            {
                if (IsEnabled(CustomComboPreset.SCH_DeploymentTactics_Recitation) && ActionReady(Recitation))
                    return Recitation;

                return OriginalHook(Adloquium);
            }
            return actionID;
        }
    }

    /*
     * SCH_DPS
     * Overrides main DPS ability family, The Broils (and Ruin 1)
     * Implements Ruin 2 as the movement option
     * Chain Stratagem has overlap protection
     */
    internal class SCH_DPS : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_DPS;

        internal static int BroilCount => ActionWatching.CombatActions.Count(x => x == OriginalHook(Broil));

        protected override uint Invoke(uint actionID)
        {
            bool actionFound;

            if (Config.SCH_ST_DPS_Adv && Config.SCH_ST_DPS_Adv_Actions.Count > 0)
            {
                bool onBroils = Config.SCH_ST_DPS_Adv_Actions[0] && BroilList.Contains(actionID);
                bool onBios = Config.SCH_ST_DPS_Adv_Actions[1] && BioList.ContainsKey(actionID);
                bool onRuinII = Config.SCH_ST_DPS_Adv_Actions[2] && actionID is Ruin2;
                actionFound = onBroils || onBios || onRuinII;
            }
            else
                actionFound = BroilList.Contains(actionID); //default handling

            // Return if action not found
            if (!actionFound)
                return actionID;

            if (IsEnabled(CustomComboPreset.SCH_DPS_FairyReminder) &&
                NeedToSummon)
                return SummonEos;

            if (Variant.CanRampart(CustomComboPreset.SCH_DPS_Variant_Rampart))
                return Variant.Rampart;

            if (OccultCrescent.ShouldUsePhantomActions())
                return OccultCrescent.BestPhantomAction();

            //Opener
            if (IsEnabled(CustomComboPreset.SCH_DPS_Balance_Opener) &&
                Opener().FullOpener(ref actionID))
                return actionID;

            // Aetherflow
            if (IsEnabled(CustomComboPreset.SCH_DPS_Aetherflow) &&
                !WasLastAction(Dissipation) && ActionReady(Aetherflow) &&
                !HasAetherflow() && InCombat() && CanSpellWeave())
                return Aetherflow;

            // Lucid Dreaming
            if (IsEnabled(CustomComboPreset.SCH_DPS_Lucid) &&
                Role.CanLucidDream(Config.SCH_ST_DPS_LucidOption))
                return Role.LucidDreaming;

            //Target based options
            if (HasBattleTarget())
            {
                // Energy Drain
                if (IsEnabled(CustomComboPreset.SCH_DPS_EnergyDrain))
                {
                    float edTime = Config.SCH_ST_DPS_EnergyDrain_Adv ? Config.SCH_ST_DPS_EnergyDrain : 10f;

                    if (LevelChecked(EnergyDrain) && InCombat() && CanSpellWeave() &&
                        HasAetherflow() && GetCooldownRemainingTime(Aetherflow) <= edTime &&
                        (!IsEnabled(CustomComboPreset.SCH_DPS_EnergyDrain_BurstSaver) ||
                         LevelChecked(ChainStratagem) && GetCooldownRemainingTime(ChainStratagem) > 10 ||
                         !ChainStratagem.LevelChecked()))
                        return EnergyDrain;
                }

                // Chain Stratagem
                if (IsEnabled(CustomComboPreset.SCH_DPS_ChainStrat) &&
                    (Config.SCH_ST_DPS_ChainStratagemSubOption == 0 ||
                     Config.SCH_ST_DPS_ChainStratagemSubOption == 1 && InBossEncounter()))
                {
                    // If CS is available and usable, or if the Impact Buff is on Player
                    if (ActionReady(ChainStratagem) && CanApplyStatus(CurrentTarget, Debuffs.ChainStratagem) &&
                        !HasStatusEffect(Debuffs.ChainStratagem, CurrentTarget, true) &&
                        GetTargetHPPercent() > Config.SCH_ST_DPS_ChainStratagemOption &&
                        InCombat() &&
                        CanSpellWeave())
                        return ChainStratagem;

                    if (LevelChecked(BanefulImpaction) &&
                        HasStatusEffect(Buffs.ImpactImminent) &&
                        InCombat() &&
                        CanSpellWeave())
                        return BanefulImpaction;
                    // Don't use OriginalHook(ChainStratagem), because player can disable ingame action replacement
                }

                //Bio/Biolysis
                if (IsEnabled(CustomComboPreset.SCH_DPS_Bio) && LevelChecked(Bio) && InCombat() &&
                    BioList.TryGetValue(OriginalHook(Bio), out ushort dotDebuffID))
                {
                    if (Variant.CanSpiritDart(CustomComboPreset.SCH_DPS_Variant_SpiritDart))
                        return Variant.SpiritDart;

                    float refreshTimer = Config.SCH_DPS_BioUptime_Threshold;
                    int hpThreshold = Config.SCH_DPS_BioSubOption == 1 || !InBossEncounter() ? Config.SCH_DPS_BioOption : 0;
                    if (GetStatusEffectRemainingTime(dotDebuffID, CurrentTarget) <= refreshTimer && CanApplyStatus(CurrentTarget, dotDebuffID) &&
                        GetTargetHPPercent() > hpThreshold)
                        return OriginalHook(Bio);
                }

                //Ruin 2 Movement
                if (IsEnabled(CustomComboPreset.SCH_DPS_Ruin2Movement) &&
                    LevelChecked(Ruin2) && IsMoving())
                    return OriginalHook(Ruin2);
            }
            return actionID;
        }
    }

    /*
     * SCH_AoE
     * Overrides main AoE DPS ability, Art of War
     * Lucid Dreaming and Aetherflow weave options
     */
    internal class SCH_AoE : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_AoE;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not (ArtOfWar or ArtOfWarII))
                return actionID;

            if (IsEnabled(CustomComboPreset.SCH_AoE_FairyReminder) &&
                NeedToSummon)
                return SummonEos;

            if (Variant.CanRampart(CustomComboPreset.SCH_DPS_Variant_Rampart))
                return Variant.Rampart;

            if (Variant.CanSpiritDart(CustomComboPreset.SCH_DPS_Variant_SpiritDart))
                return Variant.SpiritDart;

            if (OccultCrescent.ShouldUsePhantomActions())
                return OccultCrescent.BestPhantomAction();

            // Aetherflow
            if (IsEnabled(CustomComboPreset.SCH_AoE_Aetherflow) &&
                ActionReady(Aetherflow) && !HasAetherflow() &&
                InCombat())
                return Aetherflow;

            // Lucid Dreaming
            if (IsEnabled(CustomComboPreset.SCH_AoE_Lucid) &&
                Role.CanLucidDream(Config.SCH_AoE_LucidOption))
                return Role.LucidDreaming;

            return actionID;
        }
    }

    /*
     * SCH_AoE_Heal
     * Overrides main AoE Healing abiility, Succor
     * Lucid Dreaming and Atherflow weave options
     */
    internal class SCH_AoE_Heal : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_AoE_Heal;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not Succor)
                return actionID;

            // Aetherflow
            if (IsEnabled(CustomComboPreset.SCH_AoE_Heal_Aetherflow) &&
                ActionReady(Aetherflow) && !HasAetherflow() &&
                !(IsEnabled(CustomComboPreset.SCH_AoE_Heal_Aetherflow_Indomitability) && GetCooldownRemainingTime(Indomitability) <= 0.6f) &&
                InCombat())
                return Aetherflow;

            if (IsEnabled(CustomComboPreset.SCH_AoE_Heal_Dissipation)
                && ActionReady(Dissipation)
                && !HasAetherflow()
                && InCombat())
                return Dissipation;

            // Lucid Dreaming
            if (IsEnabled(CustomComboPreset.SCH_AoE_Heal_Lucid)
                && Role.CanLucidDream(Config.SCH_AoE_Heal_LucidOption))
                return Role.LucidDreaming;

            float averagePartyHP = GetPartyAvgHPPercent();
            for(int i = 0; i < Config.SCH_AoE_Heals_Priority.Count; i++)
            {
                int index = Config.SCH_AoE_Heals_Priority.IndexOf(i + 1);
                int config = GetMatchingConfigAoE(index, out uint spell, out bool enabled);
                bool onIdom = IsEnabled(CustomComboPreset.SCH_AoE_Heal_Recitation) && 
                              Config.SCH_AoE_Heal_Recitation_Actions[0] && spell is Indomitability;
                bool onSuccor = IsEnabled(CustomComboPreset.SCH_AoE_Heal_Recitation) && 
                                Config.SCH_AoE_Heal_Recitation_Actions[1] && spell is Succor or Concitation;

                if (enabled && averagePartyHP <= config && ActionReady(spell))
                     return ActionReady(Recitation) && (onIdom || onSuccor) ? 
                        Recitation :
                        spell;

            }

            return actionID;
        }
    }

    /*
     * SCH_Fairy_Combo
     * Overrides Whispering Dawn
     */
    internal class SCH_Fairy_Combo : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_Fairy_Combo;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not WhisperingDawn)
                return actionID;

            if (HasPetPresent())
            {
                // FeyIllumination
                if (ActionReady(FeyIllumination))
                    return OriginalHook(FeyIllumination);

                // FeyBlessing
                if (ActionReady(FeyBlessing) && !(Gauge.SeraphTimer > 0))
                    return OriginalHook(FeyBlessing);

                if (IsEnabled(CustomComboPreset.SCH_Fairy_Combo_Consolation) && ActionReady(WhisperingDawn))
                    return OriginalHook(actionID);

                if (IsEnabled(CustomComboPreset.SCH_Fairy_Combo_Consolation) && Gauge.SeraphTimer > 0 && GetRemainingCharges(Consolation) > 0)
                    return OriginalHook(Consolation);
            }

            return actionID;
        }
    }

    /*
     * SCH_ST_Heal
     * Overrides main AoE Healing abiility, Succor
     * Lucid Dreaming and Atherflow weave options
     */
    internal class SCH_ST_Heal : CustomCombo
    {
        protected internal override CustomComboPreset Preset { get; } = CustomComboPreset.SCH_ST_Heal;
        protected override uint Invoke(uint actionID)
        {
            if (actionID is not Physick)
                return actionID;

            // Aetherflow
            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Aetherflow) &&
                ActionReady(Aetherflow) && !HasAetherflow() &&
                InCombat() && CanSpellWeave())
                return Aetherflow;

            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Dissipation)
                && ActionReady(Dissipation)
                && !HasAetherflow()
                && InCombat())
                return Dissipation;

            // Lucid Dreaming
            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Lucid) &&
                Role.CanLucidDream(Config.SCH_ST_Heal_LucidOption))
                return Role.LucidDreaming;

            // Dissolve Union if needed
            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Aetherpact)
                && OriginalHook(Aetherpact) is DissolveUnion //Quick check to see if Fairy Aetherpact is Active
                && AetherPactTarget is not null //Null checking so GetTargetHPPercent doesn't fall back to CurrentTarget
                && GetTargetHPPercent(AetherPactTarget) >= Config.SCH_ST_Heal_AetherpactDissolveOption)
                return DissolveUnion;

            //Grab our target
            var healTarget = OptionalTarget ?? SimpleTarget.Stack.AllyToHeal;

            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Esuna) && ActionReady(Role.Esuna) &&
                GetTargetHPPercent(healTarget, Config.SCH_ST_Heal_IncludeShields) >= Config.SCH_ST_Heal_EsunaOption &&
                HasCleansableDebuff(healTarget))
                return Role.Esuna
                    .RetargetIfEnabled(OptionalTarget, Physick);

            for(int i = 0; i < Config.SCH_ST_Heals_Priority.Count; i++)
            {
                int index = Config.SCH_ST_Heals_Priority.IndexOf(i + 1);
                int config = GetMatchingConfigST(index, out uint spell, out bool enabled);

                if (enabled)
                {
                    if (GetTargetHPPercent(healTarget, Config.SCH_ST_Heal_IncludeShields) <= config &&
                        ActionReady(spell))
                        return spell
                            .RetargetIfEnabled(OptionalTarget, Physick);
                }
            }

            //Check for the Galvanize shield buff. Start applying if it doesn't exist or Target HP is below %
            if (IsEnabled(CustomComboPreset.SCH_ST_Heal_Adloquium) &&
                ActionReady(Adloquium) &&
                GetTargetHPPercent(healTarget, Config.SCH_ST_Heal_IncludeShields) <= Config.SCH_ST_Heal_AdloquiumOption)
            {
                if (Config.SCH_ST_Heal_AldoquimOpts[2] && ActionReady(EmergencyTactics))
                    return EmergencyTactics;

                if ((Config.SCH_ST_Heal_AldoquimOpts[0] || !HasStatusEffect(Buffs.Galvanize, healTarget, true)) && //Ignore existing shield check
                    (!Config.SCH_ST_Heal_AldoquimOpts[1] ||
                     !HasStatusEffect(SGE.Buffs.EukrasianDiagnosis, healTarget, true) && !HasStatusEffect(SGE.Buffs.EukrasianPrognosis, healTarget, true)
                    )) //Eukrasia Shield Check
                    return OriginalHook(Adloquium)
                        .RetargetIfEnabled(OptionalTarget, Physick);
            }

            return actionID
                .RetargetIfEnabled(OptionalTarget, Physick);
        }
    }
}
