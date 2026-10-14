using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Silent;

/// <summary>
/// 暗影步 (ShadowStep) — op: modify，猎人稀有
///   原效果：1(0)费，丢弃所有手牌，下回合攻击伤害翻倍
///   新效果：1费（**不再降费**），丢弃所有手牌，下回合攻击伤害翻倍；
///           **升级后可以选择保留一张手牌**，只丢弃其余的
///
/// 升级从「省 1 费」换成「留 1 张牌」：原版升级后 0 费清手牌太顺滑，
/// 改成保留一张能让它和留牌/奇巧的取舍产生互动，而不是无脑丢。
/// </summary>
public static class ShadowStepPatch
{
    /// <summary>CardModel.SelectionScreenPrompt 是 protected，只能反射取。</summary>
    private static LocString Prompt(CardModel card) =>
        (LocString)AccessTools.Property(typeof(CardModel), "SelectionScreenPrompt").GetValue(card)!;

    /// <summary>费用固定 1，升级的收益改为「保留一张手牌」，所以这里什么都不做。</summary>
    [HarmonyPatch(typeof(ShadowStep), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(ShadowStep), "OnPlay")]
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
            var hand = PileType.Hand.GetPile(card.Owner).Cards.ToList();

            if (card.IsUpgraded && hand.Count > 0)
            {
                // 选中的那张是**保留**的，不是丢弃的——所以提示语用卡自己的
                // SelectionScreenPrompt，别用 DiscardSelectionPrompt，否则玩家会理解反。
                var keep = (await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: card.Owner,
                    prefs: new CardSelectorPrefs(Prompt(card), 1),
                    filter: null,
                    source: card)).FirstOrDefault();

                if (keep != null) hand.Remove(keep);
            }

            if (hand.Count > 0) await CardCmd.Discard(choiceContext, hand);

            await PowerCmd.Apply<ShadowStepPower>(choiceContext, card.Owner.Creature, 1m, card.Owner.Creature, card);
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

            string entry = ModelDb.Card<ShadowStep>().Id.Entry;
            bool zh = LocManager.Instance.Language is "zhs" or "zht" or "zh";

            // 原版这张卡不需要选牌，所以 loc 里没有 selectionScreenPrompt 条目——
            // 升级后要弹选择框，得自己补一条，否则界面上会显示成缺失的 key。
            translations[entry + ".selectionScreenPrompt"] = zh ? "选择要保留的牌" : "Choose a card to keep";

            string key = entry + ".description";
            translations[key] = LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "{IfUpgraded:show:选择一张手牌保留，丢弃其余手牌|丢弃所有手牌}。下回合你的攻击造成双倍伤害。",
                _ =>
                    "{IfUpgraded:show:Choose a card to keep and discard the rest of your hand|Discard your hand}. Next turn, your Attacks deal double damage.",
            };
        }
    }
}
