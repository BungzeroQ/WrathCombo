using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Linq;

namespace WrathCombo.Extensions
{
    internal static class BattleCharaExtensions
    {
        public unsafe static uint RawShieldValue(this IBattleChara chara)
        {
            FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* baseVal = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)chara.Address;
            var value = baseVal->Character.CharacterData.ShieldValue;
            var rawValue = chara.MaxHp / 100 * value;

            return rawValue;
        }

        public unsafe static byte ShieldPercentage(this IBattleChara chara)
        {
            FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara* baseVal = (FFXIVClientStructs.FFXIV.Client.Game.Character.BattleChara*)chara.Address;
            var value = baseVal->Character.CharacterData.ShieldValue;

            return value;
        }

        public static bool HasShield(this IBattleChara chara) => chara.RawShieldValue() > 0;

        public static string GetInitials(this IBattleChara chara)
        {
            var ret = string.Concat(chara.Name.TextValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Where(x => x.Length >= 1 && char.IsLetter(x[0]))
                                          .Select(x => char.ToUpper(x[0])));

            return ret;
        }
    }
}
