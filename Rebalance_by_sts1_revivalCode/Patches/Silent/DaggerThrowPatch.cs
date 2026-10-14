using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Silent;

/// <summary>
/// 投掷匕首 (DaggerThrow) — op: modify，猎人普通
///   原效果：1费，造成 9(12) 点伤害，抽 1 张牌，然后弃 1 张牌
///   新效果：1费，造成 9(11) 点伤害，抽 1(2) 张牌，然后弃 1(2) 张牌
///
/// 升级收益从「纯 +3 伤害」改成「+2 伤害 + 抽弃各翻倍」——把它从单纯的数值升级
/// 变成过牌/弃牌流的构筑件（猎人有大量奇巧和弃牌收益）。
///
/// 数值锚点是战士的剑柄打击 PommelStrike：同为 1 费、基础 9 伤，升级后 10 伤 + 抽 2，**无代价**。
/// 本卡升级后同费同抽牌但要弃 2 张，所以多给 1 点伤害（11）作为代价的对价。
///
/// DaggerThrow 自己声明了 OnPlay / OnUpgrade，所以这里可以直接指向它，
/// 不用像 Rarity / GainsBlock 那样退回 CardModel。
/// </summary>
public static class DaggerThrowPatch
{
    private const decimal UpgradeDamageBonus = 2m; // 9 -> 11（原版是 +3 -> 12）
    private const int BaseCards = 1;
    private const int UpgradedCards = 2;

    [HarmonyPatch(typeof(DaggerThrow), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.DynamicVars.Damage.UpgradeValueBy(UpgradeDamageBonus);
            return false; // 跳过原版的 +3
        }
    }

    /// <summary>
    /// 抽/弃张数要随升级变化，而原版把 1 写死在 async 状态机里，改不动，所以整体替换 OnPlay。
    /// 特效沿用原版，避免这张卡打起来和其他猎人攻击牌观感不一致。
    /// </summary>
    [HarmonyPatch(typeof(DaggerThrow), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            __result = NewOnPlay(__instance, choiceContext, cardPlay);
            return false;
        }

        private static async Task NewOnPlay(CardModel card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);

            var daggerColor = new Color("#b1ccca");
            await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue).FromCard(card).Targeting(cardPlay.Target)
                .WithAttackerFx(() => NDaggerSprayFlurryVfx.Create(card.Owner.Creature, daggerColor, goingRight: true))
                .WithHitVfxNode((Creature t) => NDaggerSprayImpactVfx.Create(t, daggerColor, goingRight: true))
                .Execute(choiceContext);

            int count = card.IsUpgraded ? UpgradedCards : BaseCards;

            await CardPileCmd.Draw(choiceContext, count, card.Owner);

            // 抽完再弃，且按实际抽到的手牌选——手牌不足时 selector 自己会少给。
            var chosen = await CardSelectCmd.FromHandForDiscard(
                choiceContext, card.Owner,
                new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, count), null, card);

            if (chosen.Any()) await CardCmd.Discard(choiceContext, chosen);
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

            // 抽弃张数随升级变化，所以用 {IfUpgraded:show:升级态|常态}（升级态在前）。
            // 不加 '#' 前缀以跳过 SimpleLoc，故变量要写全名 {Damage}。
            string key = ModelDb.Card<DaggerThrow>().Id.Entry + ".description";
            translations[key] = LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "造成 {Damage} 点伤害。抽 {IfUpgraded:show:2|1} 张牌，然后弃 {IfUpgraded:show:2|1} 张牌。",
                _ =>
                    "Deal {Damage} damage. Draw {IfUpgraded:show:2|1} card(s), then discard {IfUpgraded:show:2|1} card(s).",
            };
        }
    }
}
