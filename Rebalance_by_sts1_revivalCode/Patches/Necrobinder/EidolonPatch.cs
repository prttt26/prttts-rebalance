using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Necrobinder;

/// <summary>
/// 幻景 (Eidolon) — op: modify，亡灵契约师稀有
///   原效果：2(1)费，消耗**所有**手牌；若消耗了 9 张，获得 1 层无实体
///   新效果：2(1)费，**选择 0-1(0-2) 张手牌保留**，消耗其余手牌；若消耗了 9 张，获得 1 层无实体
///
/// 从「全清」变成「可以留关键牌」，代价是留得越多越难够到 9 张的无实体阈值——
/// 保留张数本身成了一个取舍，而不是纯粹的增益。
///
/// 费用不用改：原版升级就是 EnergyCost.UpgradeBy(-1)，与设计一致，OnUpgrade 保持原样。
/// </summary>
public static class EidolonPatch
{
    private const int IntangibleThreshold = 9;
    private const int KeepBase = 1;
    private const int KeepUpgraded = 2;

    /// <summary>CardModel.SelectionScreenPrompt 是 protected，只能反射取。</summary>
    private static LocString Prompt(CardModel card) =>
        (LocString)AccessTools.Property(typeof(CardModel), "SelectionScreenPrompt").GetValue(card)!;

    [HarmonyPatch(typeof(Eidolon), "OnPlay")]
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

            var hand = card.Owner.PlayerCombatState.Hand.Cards.ToList();
            int keepCount = card.IsUpgraded ? KeepUpgraded : KeepBase;

            if (hand.Count > 0 && keepCount > 0)
            {
                // 选中的是**保留**的牌，不是消耗的，所以用卡自己的提示语而不是消耗提示语。
                var kept = await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: card.Owner,
                    // (min,max) 而不是单参数：单参数是 min==max，会强制必选，与「至多」不符
                    prefs: new CardSelectorPrefs(Prompt(card), 0, keepCount),
                    filter: null,
                    source: card);

                foreach (var k in kept) hand.Remove(k);
            }

            int exhausted = 0;
            foreach (var c in hand)
            {
                await CardCmd.Exhaust(choiceContext, c);
                exhausted++;
            }

            // 阈值判定用**实际消耗数**，所以留牌会让无实体更难拿到——这正是这次改动的取舍点。
            if (exhausted >= IntangibleThreshold)
                await PowerCmd.Apply<IntangiblePower>(choiceContext, card.Owner.Creature, 1m, card.Owner.Creature, card);
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

            string entry = ModelDb.Card<Eidolon>().Id.Entry;
            bool zh = LocManager.Instance.Language is "zhs" or "zht" or "zh";

            // 原版不需要选牌，loc 里没有 selectionScreenPrompt，自己补一条。
            translations[entry + ".selectionScreenPrompt"] = zh ? "选择要保留的牌" : "Choose cards to keep";

            translations[entry + ".description"] = zh
                ? "选择至多 {IfUpgraded:show:2|1} 张手牌保留，消耗其余手牌。若以此消耗了 9 张牌，获得 1 层[gold]无实体[/gold]。"
                : "Choose up to {IfUpgraded:show:2|1} cards to keep and Exhaust the rest of your hand. If 9 cards were Exhausted this way, gain 1 [gold]Intangible[/gold].";
        }
    }
}
