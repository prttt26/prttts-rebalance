using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Ironclad;

/// <summary>
/// 飞剑回旋镖 (SwordBoomerang) — op: move + modify，战士普通 → 战士罕见
///   原效果：1费，随机对敌人造成 3 点伤害 3(4) 次
///   新效果：1费，随机对敌人造成 3 点伤害「2 + 敌人数量」次；升级后为「2 + 敌人数量 × 2」
///           （只翻倍敌人数量那一项，**不是**整体翻倍）
///
/// 同池内换稀有度不涉及卡图/配色（Pool 不变），所以只要改 Rarity + 行为。
/// </summary>
public static class SwordBoomerangPatch
{
    private const int BaseHits = 2;

    /// <summary>Rarity 只声明在 CardModel 上，必须 patch 基类再判类型。</summary>
    [HarmonyPatch(typeof(CardModel), "Rarity", MethodType.Getter)]
    private static class Rarity
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref CardRarity __result)
        {
            if (__instance is SwordBoomerang) __result = CardRarity.Uncommon;
        }
    }

    /// <summary>
    /// 命中次数改为随场上敌人数量浮动，只能在打出时算，所以整体替换 OnPlay。
    /// 原版用的是固定的 RepeatVar，升级加 1；现在那个 var 不再参与计算（见下方 Upgrade）。
    /// </summary>
    [HarmonyPatch(typeof(SwordBoomerang), "OnPlay")]
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
            // GetOpponentsOf 返回对面**所有**生物，不过滤存活，已死的不该计入。
            int aliveEnemies = card.CombatState.GetOpponentsOf(card.Owner.Creature).Count(c => !c.IsDead);

            // 升级只翻倍**敌人数量那一项**，不是整体翻倍：
            //   未升级 2 + n，升级后 2 + 2n。写成 (2 + n) * 2 会凭空多出 2 次。
            int hits = BaseHits + aliveEnemies * (card.IsUpgraded ? 2 : 1);

            await DamageCmd.Attack(card.DynamicVars.Damage.BaseValue).WithHitCount(hits).FromCard(card)
                .TargetingRandomOpponents(card.CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    /// <summary>
    /// 原版 OnUpgrade 是 `DynamicVars.Repeat.UpgradeValueBy(1)`。命中次数现在由上面动态算，
    /// 让 Repeat 继续变化只会让卡面数字和实际行为对不上，所以整个跳过。
    /// 升级的收益体现在 NewOnPlay 里的 `hits *= 2`。
    /// </summary>
    [HarmonyPatch(typeof(SwordBoomerang), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix() => false;
    }

    /// <summary>
    /// 卡面必须重写：次数不再是固定数字，原文的 {Repeat} 会显示成一个和实际行为无关的值。
    ///
    /// 这里写的是最终 SmartFormat、**不加 '#' 前缀**（加了会走 SimpleLoc）：
    /// 因为升级态文本里含字面 '+'，而 SimpleLoc 的 -常态-+升级态+ 简写需要把它转义成 /+，
    /// 直接写终态更不容易出错。注意不走 SimpleLoc 就没有 {D} 简写，要写全 {Damage}。
    /// </summary>
    [HarmonyPatch(typeof(ModelDb), "Init")]
    private static class Loc
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            var table = LocManager.Instance.GetTable("cards");
            if (AccessTools.Field(typeof(LocTable), "_translations").GetValue(table)
                is not Dictionary<string, string> translations) return;

            string key = ModelDb.Card<SwordBoomerang>().Id.Entry + ".description";
            translations[key] = LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "随机对敌人造成 {Damage} 点伤害，次数为 {IfUpgraded:show:2 + 敌人数量 × 2|2 + 敌人数量}。",
                _ =>
                    "Deal {Damage} damage to random enemies {IfUpgraded:show:2 + enemy count × 2|2 + enemy count} times.",
            };
        }
    }
}
