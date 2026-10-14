using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Colorless;

/// <summary>
/// 许愿 (Wish) — op: modify，无色先古卡（来自先古遗物「原初之爪」）
///   原：0费，从抽牌堆选择一张卡加入手牌，消耗；升级获得【保留】
///   新：同上，但升级改为获得【固有】
///
/// 效果本身不动，只换升级给的词条，所以整张卡只要替换 OnUpgrade 一个成员——
/// 不涉及 CanonicalVars，也就不用连带处理数值和描述。
/// </summary>
public static class WishPatch
{
    [HarmonyPatch(typeof(Wish), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            AccessTools.Method(typeof(CardModel), "AddKeyword")
                .Invoke(__instance, [CardKeyword.Innate]);
            return false; // skip vanilla's AddKeyword(CardKeyword.Retain)
        }
    }
}
