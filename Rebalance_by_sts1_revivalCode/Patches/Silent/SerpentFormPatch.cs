using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Silent;

/// <summary>
/// 群蛇形态 (SerpentForm) — op: modify，猎人稀有
///   原效果：3费能力，每打出一张牌对随机敌人造成 4(6) 点伤害
///   新效果：3费能力，每打出一张牌对随机敌人造成 4 点伤害（固定）；**升级改为获得【奇巧】**
///
/// 升级从「数值 +2」换成「多一个词条」：奇巧让这张能力牌被弃掉时自动打出，
/// 和猎人的弃牌流联动，比单纯多 2 点伤害更有构筑意义。
///
/// 卡面不用另外改：伤害值 {SerpentFormPower} 恒为 4，而【奇巧】是 CardKeyword，
/// 游戏会自动往描述里追加词条文本。
/// </summary>
public static class SerpentFormPatch
{
    [HarmonyPatch(typeof(SerpentForm), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            // AddKeyword 是 CardModel 上的非公开方法，只能反射调用。
            AccessTools.Method(typeof(CardModel), "AddKeyword")
                .Invoke(__instance, [CardKeyword.Sly]);
            return false; // 跳过原版的伤害 +2
        }
    }
}
