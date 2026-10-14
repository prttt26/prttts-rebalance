using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Silent;

/// <summary>
/// 袖里乾坤 (UpMySleeve) — op: modify，猎人罕见
///   原效果：2费，将 3(4) 张小刀加入手牌。本场战斗中此牌耗能减少 1
///   新效果：3(2)费，将 3 张小刀加入手牌，**获得 1 点能量**。本场战斗中此牌耗能减少 1
///
/// 涨 1 费同时返 1 费，所以净耗能序列从原版的 2 → 1 → 0 → 0 …
/// 变成 2 → 1 → 0 → **-1** → -1 …：前三次打出与原版完全相同，
/// 第四次起（耗能已降到 0）每次打出净赚 1 费。加强只落在长战斗里。
///
/// 升级由「小刀 +1」改为「费用 -1」：整条曲线提前一次打出，净耗能 1 → 0 → -1 → -1 …
/// 封顶仍是每次净赚 1 费（返费量不变，耗能下限是 0）。
/// </summary>
public static class UpMySleevePatch
{
    private const int NewCost = 3;
    private const int EnergyGain = 1;

    /// <summary>改基础费用。为什么 patch CardEnergyCost 的构造函数而不是 SetCustomBaseCost，见 RainbowPatch。</summary>
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
            if (card is not UpMySleeve) return;

            CanonicalField?.SetValue(__instance, NewCost);
            BaseField?.SetValue(__instance, NewCost);
        }
    }

    /// <summary>
    /// 原版 OnPlay 整段保留（发小刀、本场战斗降费），只在它**跑完之后**追加给费。
    /// OnPlay 是 async，postfix 触发时原方法才跑到第一个 await，所以要把返回的 Task 包一层再等。
    /// </summary>
    [HarmonyPatch(typeof(UpMySleeve), "OnPlay")]
    private static class Play
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref Task __result) =>
            __result = ThenGainEnergy(__result, __instance);

        private static async Task ThenGainEnergy(Task original, CardModel card)
        {
            await original;
            await PlayerCmd.GainEnergy(EnergyGain, card.Owner);
        }
    }

    [HarmonyPatch(typeof(UpMySleeve), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.EnergyCost.UpgradeBy(-1); // 3 -> 2；原版是小刀 +1
            return false;
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

            string key = ModelDb.Card<UpMySleeve>().Id.Entry + ".description";
            translations[key] = SimpleLoc.TrySimplify(LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "#将 {C} 张*小刀*加入你的手牌。获得 [E]。本场战斗中，此牌的耗能减少 1。",
                _ =>
                    "#Add {C} *Shivs* into your hand. Gain [E]. Reduce this card's cost by 1 this combat.",
            });
        }
    }
}
