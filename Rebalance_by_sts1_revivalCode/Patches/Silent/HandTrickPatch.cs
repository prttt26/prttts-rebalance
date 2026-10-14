using HarmonyLib;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Silent;

/// <summary>
/// 手上技法 (HandTrick) — op: modify，猎人罕见
///   原效果：1费，获得 7(10) 格挡，选择手中一张技能牌获得**临时**奇巧（仅本回合）
///   新效果：1费，获得 7 格挡（不再升级），选择手中一张技能牌获得奇巧；**升级后奇巧变永久**
///
/// 升级从「+3 格挡」换成「临时 → 永久」：永久奇巧能让那张技能牌在整场战斗里反复吃弃牌收益，
/// 比一次性的 3 点格挡更契合猎人的弃牌流。
///
/// 临时/永久两条路走不同 API：
///   临时 → CardCmd.ApplySingleTurnSly(card)（游戏自带，只管本回合）
///   永久 → CardModel.AddKeyword(CardKeyword.Sly)（非公开，反射调用）
/// </summary>
public static class HandTrickPatch
{
    private static readonly System.Reflection.MethodInfo AddKeyword =
        AccessTools.Method(typeof(CardModel), "AddKeyword");

    /// <summary>CardModel.SelectionScreenPrompt 是 protected，只能反射取。原版这张卡就用它。</summary>
    private static LocString Prompt(CardModel card) =>
        (LocString)AccessTools.Property(typeof(CardModel), "SelectionScreenPrompt").GetValue(card)!;

    /// <summary>格挡固定 7，升级的收益改为「奇巧变永久」，所以这里什么都不做。</summary>
    [HarmonyPatch(typeof(HandTrick), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix() => false;
    }

    [HarmonyPatch(typeof(HandTrick), "OnPlay")]
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
            await CreatureCmd.GainBlock(card.Owner.Creature, card.DynamicVars.Block, cardPlay);

            // 升级版给永久奇巧，所以候选里不能再用「本回合已是奇巧」来过滤——
            // 一张临时奇巧的牌，把它变成永久仍然有意义。改为按是否已永久持有词条过滤。
            var target = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: card.Owner,
                prefs: new CardSelectorPrefs(Prompt(card), 1),
                filter: c => c.Type == CardType.Skill &&
                             (card.IsUpgraded ? !c.Keywords.Contains(CardKeyword.Sly) : !c.IsSlyThisTurn),
                source: card)).FirstOrDefault();

            if (target == null) return;

            if (card.IsUpgraded) AddKeyword.Invoke(target, [CardKeyword.Sly]);
            else CardCmd.ApplySingleTurnSly(target);
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

            string key = ModelDb.Card<HandTrick>().Id.Entry + ".description";
            translations[key] = SimpleLoc.TrySimplify(LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "#获得 {B} 点*格挡*。选择手牌中一张技能牌，使其获得-本回合-+永久+*奇巧*。",
                _ =>
                    "#Gain {B} *Block*. Choose a Skill in your hand; it gains *Sly* -this turn-+permanently+.",
            });
        }
    }
}
