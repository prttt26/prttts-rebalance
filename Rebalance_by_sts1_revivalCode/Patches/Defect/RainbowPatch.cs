using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Defect;

/// <summary>
/// 彩虹 (Rainbow) — op: modify，机器人稀有
///   原效果：2费，消耗（升级去除），获得 闪电 / 寒冰 / 黑暗 三种充能球
///   新效果：3费，消耗（升级去除），获得 闪电 / 寒冰 / 玻璃 / 黑暗 / 等离子 **五种**充能球
///
/// 游戏里一共就 5 种可用充能球，与中文名一一对应：
///   LightningOrb 闪电 / FrostOrb 寒冰 / GlassOrb 玻璃 / DarkOrb 黑暗 / PlasmaOrb 等离子
/// （设计文档初版把玻璃写成了「旋转」，已按游戏内正式名统一为玻璃。）
/// 涨 1 费换多 2 种球——彩虹从「三色起手」变成「一张凑齐全部球型」。
/// </summary>
public static class RainbowPatch
{
    private const int NewCost = 3;

    /// <summary>
    /// 改基础费用。
    ///
    /// ⚠️ 别用公开的 `EnergyCost.SetCustomBaseCost()`——它第一行是 `_card.AssertMutable()`，
    /// 而 ModelDb 建的**规范实例（canonical）是不可变的**，在卡牌构造函数里调它会抛
    /// CanonicalModelException 并直接让游戏启动失败（不是加载不了 mod，是整个起不来）。
    ///
    /// 改为 postfix CardEnergyCost 自己的构造函数：它不碰卡牌的可变性断言。
    /// 真正决定费用的是 `_base`（GetWithModifiers 以它为起点），Canonical 只在升降级重置时用，
    /// 两个都写才不会在升级后被打回 2 费。
    /// </summary>
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
            if (card is not Rainbow) return;

            CanonicalField?.SetValue(__instance, NewCost);
            BaseField?.SetValue(__instance, NewCost);
        }
    }

    [HarmonyPatch(typeof(Rainbow), "ExtraHoverTips", MethodType.Getter)]
    private static class Tips
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<IHoverTip> __result) =>
            __result =
            [
                HoverTipFactory.Static(StaticHoverTip.Channeling),
                HoverTipFactory.FromOrb<LightningOrb>(),
                HoverTipFactory.FromOrb<FrostOrb>(),
                HoverTipFactory.FromOrb<GlassOrb>(),
                HoverTipFactory.FromOrb<DarkOrb>(),
                HoverTipFactory.FromOrb<PlasmaOrb>(),
            ];
    }

    [HarmonyPatch(typeof(Rainbow), "OnPlay")]
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
            await OrbCmd.Channel<LightningOrb>(choiceContext, card.Owner);
            await OrbCmd.Channel<FrostOrb>(choiceContext, card.Owner);
            await OrbCmd.Channel<GlassOrb>(choiceContext, card.Owner);
            await OrbCmd.Channel<DarkOrb>(choiceContext, card.Owner);
            await OrbCmd.Channel<PlasmaOrb>(choiceContext, card.Owner);
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

            string key = ModelDb.Card<Rainbow>().Id.Entry + ".description";
            translations[key] = SimpleLoc.TrySimplify(LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" => "#通道 1 个*闪电*、1 个*寒冰*、1 个*玻璃*、1 个*黑暗*、1 个*等离子*充能球。",
                _ => "#Channel 1 *Lightning*, 1 *Frost*, 1 *Glass*, 1 *Dark* and 1 *Plasma* orb.",
            });
        }
    }
}
