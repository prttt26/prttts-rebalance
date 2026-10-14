using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Necrobinder;

/// <summary>
/// 挽歌 (Dirge) — op: modify，亡灵契约师罕见
///   效果不变：X费，消耗。召唤 3，X 次；将 X 张灵魂加入抽牌堆
///   原升级：召唤 3 → 4，且加入的灵魂变为灵魂+
///   新升级：**去除消耗**（召唤量和灵魂都不再随升级变化）
///
/// 原版升级有两半：召唤 +1 写在 OnUpgrade 里，「灵魂变灵魂+」则是 OnPlay / 悬浮提示 / 卡面文本
/// 各自去读 IsUpgraded。所以只换 OnUpgrade 不够，这三处都要一起改，否则升级后还会偷偷给灵魂+。
/// </summary>
public static class DirgePatch
{
    [HarmonyPatch(typeof(Dirge), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.RemoveKeyword(CardKeyword.Exhaust);
            return false; // 跳过原版的召唤 +1
        }
    }

    [HarmonyPatch(typeof(Dirge), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance, PlayerChoiceContext choiceContext, ref Task __result)
        {
            __result = NewOnPlay(__instance, choiceContext);
            return false;
        }

        /// <summary>与原版逐行相同，只去掉了「已升级则把灵魂升级」那一段。</summary>
        private static async Task NewOnPlay(CardModel card, PlayerChoiceContext choiceContext)
        {
            await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);

            int xValue = card.ResolveEnergyXValue();
            for (int i = 0; i < xValue; i++)
                await OstyCmd.Summon(choiceContext, card.Owner, card.DynamicVars.Summon.BaseValue, card);

            var souls = Soul.Create(card.Owner, xValue, card.CombatState).ToList();
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardsToCombat(souls, PileType.Draw, card.Owner, CardPilePosition.Random));
        }
    }

    /// <summary>原版第二项是 FromCard&lt;Soul&gt;(IsUpgraded)，升级后会预览灵魂+。</summary>
    [HarmonyPatch(typeof(Dirge), "ExtraHoverTips", MethodType.Getter)]
    private static class Tips
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result) =>
            __result =
            [
                HoverTipFactory.Static(StaticHoverTip.SummonDynamic, __instance.DynamicVars.Summon),
                HoverTipFactory.FromCard<Soul>(),
            ];
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

            // 原版条目里的 {IfUpgraded:show:灵魂+|灵魂} 换成固定的「灵魂」，其余照抄。
            string key = ModelDb.Card<Dirge>().Id.Entry + ".description";
            translations[key] = LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "[gold]召唤[/gold]{Summon:diff()}X次。\n将X张[gold]灵魂[/gold]添加到你的[gold]抽牌堆[/gold]中。",
                _ =>
                    "[gold]Summon[/gold] {Summon:diff()} X times.\nAdd X [gold]Souls[/gold] into your [gold]Draw Pile[/gold].",
            };
        }
    }
}
