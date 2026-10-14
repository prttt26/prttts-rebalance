using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Regent;

/// <summary>
/// 淬炼刀刃 (RefineBlade) — op: modify，储君普通
///   原效果：1费，铸造 9(13)，下个回合获得 1 费
///   新效果：2费，铸造 12(16)，**若本回合铸造过则立刻获得 2 费，否则下个回合获得 2 费**
///
/// 从「无脑铺垫」变成「铸造顺序有讲究」：先打别的铸造牌，这张就能当场回本。
/// </summary>
public static class RefineBladePatch
{
    private const int NewCost = 2;

    /// <summary>
    /// 记录每个玩家最近一次铸造发生在哪个「玩家回合序号」。
    ///
    /// 游戏没有铸造的战斗历史条目（Entries 里只有抽/弃/消耗/打出/受伤等），
    /// 也没有现成的 ForgedThisTurn，所以只能自己记。
    /// 键用 PlayerCombatState.TurnNumber——CombatHistoryEntry.HappenedThisTurn 内部
    /// 也是拿它判定「同一回合」，口径一致。
    /// 用 ConditionalWeakTable：键是弱引用，战斗结束后自动消失，不用挂清理钩子。
    /// </summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Player, int[]> LastForgeTurn = new();

    private static bool ForgedThisTurn(Player player)
    {
        int? now = player.PlayerCombatState?.TurnNumber;
        return now != null && LastForgeTurn.TryGetValue(player, out var last) && last[0] == now.Value;
    }

    /// <summary>所有铸造都会经过 ForgeCmd.Forge，所以在这里统一记录。</summary>
    [HarmonyPatch(typeof(ForgeCmd), "Forge")]
    private static class TrackForge
    {
        [HarmonyPostfix]
        private static void Postfix(Player player)
        {
            int? turn = player.PlayerCombatState?.TurnNumber;
            if (turn == null) return;

            LastForgeTurn.Remove(player);
            LastForgeTurn.Add(player, [turn.Value]);
        }
    }

    [HarmonyPatch(typeof(RefineBlade), "CanonicalVars", MethodType.Getter)]
    private static class Vars
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<DynamicVar> __result) =>
            __result = [new ForgeVar(12), new EnergyVar(2)];
    }

    [HarmonyPatch(typeof(RefineBlade), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.DynamicVars.Forge.UpgradeValueBy(4m); // 12 -> 16
            return false; // 能量不再随升级变化（固定 2）
        }
    }

    [HarmonyPatch(typeof(RefineBlade), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance, PlayerChoiceContext choiceContext, ref Task __result)
        {
            __result = NewOnPlay(__instance, choiceContext);
            return false;
        }

        private static async Task NewOnPlay(CardModel card, PlayerChoiceContext choiceContext)
        {
            await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);

            // 先判定再铸造：本卡自己的铸造不该让条件成立，否则条件永远为真。
            bool forgedEarlier = ForgedThisTurn(card.Owner);

            await ForgeCmd.Forge(card.DynamicVars.Forge.IntValue, card.Owner, card);

            decimal energy = card.DynamicVars.Energy.BaseValue;
            if (forgedEarlier)
                await PlayerCmd.GainEnergy(energy, card.Owner);
            else
                await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, card.Owner.Creature, energy, card.Owner.Creature, card);
        }
    }

    /// <summary>1费 → 2费。同彩虹：不能用 SetCustomBaseCost（对规范实例会抛异常）。</summary>
    [HarmonyPatch(typeof(CardEnergyCost), MethodType.Constructor, typeof(CardModel), typeof(int), typeof(bool))]
    private static class Cost
    {
        private static readonly System.Reflection.FieldInfo BaseField =
            AccessTools.Field(typeof(CardEnergyCost), "_base");
        private static readonly System.Reflection.FieldInfo CanonicalField =
            AccessTools.Field(typeof(CardEnergyCost), "<Canonical>k__BackingField");

        [HarmonyPostfix]
        private static void Postfix(CardEnergyCost __instance, CardModel card)
        {
            if (card is not RefineBlade) return;
            CanonicalField?.SetValue(__instance, NewCost);
            BaseField?.SetValue(__instance, NewCost);
        }
    }

    [HarmonyPatch(typeof(ModelDb), "Init")]
    private static class Loc
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            var table = LocManager.Instance.GetTable("cards");
            if (AccessTools.Field(typeof(LocTable), "_translations").GetValue(table)
                is not Dictionary<string, string> translations) return;

            string key = ModelDb.Card<RefineBlade>().Id.Entry + ".description";
            translations[key] = SimpleLoc.TrySimplify(LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "#铸造 {Forge}。若你本回合铸造过，获得 [E?]；否则在下个回合获得 [E?]。",
                _ =>
                    "#Forge {Forge}. If you have Forged this turn, gain [E?]; otherwise gain [E?] next turn.",
            });
        }
    }
}
