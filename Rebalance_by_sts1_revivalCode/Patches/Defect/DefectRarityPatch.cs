using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Defect;

/// <summary>
/// 机器人「稀有卡下放」— op: move，稀有 → 罕见
///   碎片整理 Defragment：1费能力，获得 1(2) 集中
///   冷却剂   Coolant：   1费能力，回合结束时每有一种球获得 2(3) 格挡
///
/// 效果数值都不动，只降稀有度。同池内换稀有度不涉及卡图和配色（Pool 不变），
/// 所以只要改 Rarity 一项——比强撑那种跨角色池的移动简单得多。
/// </summary>
public static class DefectRarityPatch
{
    /// <summary>Rarity 只声明在 CardModel 上，必须 patch 基类再判类型。</summary>
    [HarmonyPatch(typeof(CardModel), "Rarity", MethodType.Getter)]
    private static class Rarity
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref CardRarity __result)
        {
            if (__instance is Defragment or Coolant) __result = CardRarity.Uncommon;
        }
    }
}
