using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Necrobinder;

/// <summary>
/// 榨取 (Squeeze) — op: move + modify，亡灵契约师 稀有 → 普通
///   原效果：3费，奥斯提造成 25(30) 点伤害，牌组每有一张**其他**奥斯提攻击牌额外 5(6) 点
///   新效果：2费，奥斯提造成 12(14) 点伤害，牌组每有一张奥斯提攻击牌（**含本卡**）额外 3(4) 点
///
/// 下放成普通卡，所以基数和每张加成都大幅下调；同时把计数口径从「其他」改成「包含自身」，
/// 这样单卡下限是 12+3=15 而不是 12。
/// </summary>
public static class SqueezePatch
{
    private const int NewCost = 2;

    /// <summary>
    /// 牌组里奥斯提攻击牌的张数，**包含本卡自身**。
    /// 原版是 `c.Tags.Contains(OstyAttack) && c != card`——设计要求含自身，所以去掉后半个条件。
    /// 必须是静态方法：CalculatedVar.WithMultiplier 会拒绝 target 为 AbstractModel 的委托。
    /// </summary>
    private static decimal OstyAttackCount(CardModel card, Creature? target) =>
        card.Owner.PlayerCombatState.AllCards.Count(c => c.Tags.Contains(CardTag.OstyAttack));

    [HarmonyPatch(typeof(Squeeze), "CanonicalVars", MethodType.Getter)]
    private static class Vars
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<DynamicVar> __result) =>
            __result =
            [
                new CalculationBaseVar(12m),
                new ExtraDamageVar(3m).FromOsty(),
                new CalculatedDamageVar(ValueProp.Move).FromOsty().WithMultiplier(OstyAttackCount),
            ];
    }

    [HarmonyPatch(typeof(Squeeze), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.DynamicVars.CalculationBase.UpgradeValueBy(2m); // 12 -> 14
            __instance.DynamicVars.ExtraDamage.UpgradeValueBy(1m);     // 3 -> 4
            return false; // 原版是 +5 / +1
        }
    }

    /// <summary>稀有 → 普通。Rarity 只声明在 CardModel 上，patch 基类再判类型。</summary>
    [HarmonyPatch(typeof(CardModel), "Rarity", MethodType.Getter)]
    private static class Rarity
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref CardRarity __result)
        {
            if (__instance is Squeeze) __result = CardRarity.Common;
        }
    }

    /// <summary>
    /// 3费 → 2费。同彩虹：不能用 EnergyCost.SetCustomBaseCost（内部 AssertMutable，
    /// 对规范实例会抛异常并让游戏起不来），改为 postfix CardEnergyCost 构造函数写字段。
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
            if (card is not Squeeze) return;
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

            string key = ModelDb.Card<Squeeze>().Id.Entry + ".description";
            translations[key] = SimpleLoc.TrySimplify(LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "#奥斯提造成 {CD} 点伤害。你的牌组中每有一张奥斯提攻击牌（含此卡），额外造成 {ExtraDamage} 点。",
                _ =>
                    "#Osty deals {CD} damage. Deals {ExtraDamage} more for each Osty Attack in your deck, including this one.",
            });
        }
    }
}
