using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Necrobinder;

/// <summary>
/// 增生 (Spur) — op: modify，亡灵契约师罕见
///   原效果：1费，召唤 3(5)，奥斯提回复 5(7) 生命
///   新效果：1费，召唤 3(5)，奥斯提回复 **9(12)** 生命，且本回合奥斯提优先于你的格挡承受攻击伤害
///
/// 召唤量不变，回血大幅提高。设计意图有两种用法：
///   1. 让奥斯提快速吃满伤害后死去，启动「忧郁」（每死一个单位降费，含奥斯提）
///   2. 奥斯提血上限极高时，本卡提供的等效防御值超模
///
/// 「奥斯提优先于你的格挡承受伤害」由 SpurPower 承载（挂在玩家身上），机制说明见那里。
/// 这个效果**不叠加持续时间**：一回合内无论打出多少张增生，都只持续到你的下个回合开始。
/// </summary>
public static class SpurPatch
{
    [HarmonyPatch(typeof(Spur), "CanonicalVars", MethodType.Getter)]
    private static class Vars
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<DynamicVar> __result) =>
            __result = [new SummonVar(3m), new HealVar(9m)];
    }

    [HarmonyPatch(typeof(Spur), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.DynamicVars.Summon.UpgradeValueBy(2m); // 3 -> 5（同原版）
            __instance.DynamicVars.Heal.UpgradeValueBy(3m);   // 9 -> 12（原版是 5 -> 7）
            return false;
        }
    }

    /// <summary>原版 OnPlay（召唤、回血）保留，跑完之后挂上 SpurPower。async 的 postfix 要包 Task，见 UpMySleevePatch。</summary>
    [HarmonyPatch(typeof(Spur), "OnPlay")]
    private static class Play
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, PlayerChoiceContext choiceContext, ref Task __result) =>
            __result = ThenGuard(__result, __instance, choiceContext);

        private static async Task ThenGuard(Task original, CardModel card, PlayerChoiceContext choiceContext)
        {
            await original;
            await PowerCmd.Apply<SpurPower>(choiceContext, card.Owner.Creature, 1m, card.Owner.Creature, card);
        }
    }

    /// <summary>
    /// 扣格挡前，把奥斯提要先吃掉的那部分从伤害里拿走——格挡只去挡奥斯提吃不完的余量。
    /// 没被格挡的伤害随后走原版的转移和溢出逻辑，这里不碰。
    /// 是否适用、拿走多少都由 SpurPower 在 BeforeDamageReceived 里定好，这里只取结果。
    /// </summary>
    [HarmonyPatch(typeof(Creature), nameof(Creature.DamageBlockInternal))]
    private static class BlockAfterOsty
    {
        [HarmonyPrefix]
        private static void Prefix(Creature __instance, ref decimal amount)
        {
            var power = __instance.GetPower<SpurPower>();
            if (power != null) amount -= power.ConsumePendingAbsorb(amount);
        }
    }

    [HarmonyPatch(typeof(Spur), "ExtraHoverTips", MethodType.Getter)]
    private static class Tips
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<IHoverTip> __result) =>
            __result = __result.Append(HoverTipFactory.FromPower<SpurPower>());
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

            // 沿用原版条目的写法，只在末尾加一句。
            string key = ModelDb.Card<Spur>().Id.Entry + ".description";
            translations[key] = LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "[gold]召唤[/gold]{Summon:diff()}。\n[gold]奥斯提[/gold]回复{Heal:diff()}点生命。\n本回合，[gold]奥斯提[/gold]会先于你的[gold]格挡[/gold]承受攻击伤害。",
                _ =>
                    "[gold]Summon[/gold] {Summon:diff()}.\n[gold]Osty[/gold] heals {Heal:diff()} HP.\nThis turn, [gold]Osty[/gold] absorbs attack damage before your [gold]Block[/gold].",
            };
        }
    }
}
