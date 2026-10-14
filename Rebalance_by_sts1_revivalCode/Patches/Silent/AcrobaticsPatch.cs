using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Silent;

/// <summary>
/// 杂技 (Acrobatics) — op: move，猎人 罕见 → 普通
///
/// 效果和数值都不动（1费技能，抽 3(4) 张牌，然后弃 1 张牌），只降稀有度——塔1 里它本来就是普通卡。
/// 「弃子争先」进了罕见、「闪躲翻滚」又已从普通移除，下放这张把两个稀有度的卡数补平。
/// 同池内换稀有度不涉及卡图和配色，所以只要改 Rarity 一项。
/// </summary>
public static class AcrobaticsPatch
{
    /// <summary>Rarity 只声明在 CardModel 上，patch 基类再判类型。</summary>
    [HarmonyPatch(typeof(CardModel), "Rarity", MethodType.Getter)]
    private static class Rarity
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref CardRarity __result)
        {
            if (__instance is Acrobatics) __result = CardRarity.Common;
        }
    }
}
