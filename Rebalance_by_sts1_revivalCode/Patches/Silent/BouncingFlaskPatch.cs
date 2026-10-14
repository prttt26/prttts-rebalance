using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Silent;

/// <summary>
/// 弹跳药瓶 (BouncingFlask) — op: modify，猎人罕见
///   原效果：2费，对随机敌人施加 3 层毒 3(4) 次
///   新效果：2费，对随机敌人施加 3 层毒「2 + 敌人数量」(「3 + 敌人数量」) 次
///
/// 原版 OnPlay 里有一大段投掷/飞溅/毒雾特效，重写会全丢。
/// 好在循环条件是 `for (i = 0; i < DynamicVars.Repeat.IntValue; i++)`，
/// 而 DynamicVar.BaseValue 有 setter——所以只要在 **prefix 里改掉次数再放行原方法**，
/// 特效和结算逻辑全部原样保留。这比整体替换 OnPlay 省事也安全得多。
/// </summary>
public static class BouncingFlaskPatch
{
    private const int BaseHits = 2;      // 升级后 3
    private const int UpgradedHits = 3;

    [HarmonyPatch(typeof(BouncingFlask), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static void Prefix(CardModel __instance)
        {
            // HittableEnemies 就是原版循环里取随机目标用的那个集合，用它保证「敌人数量」
            // 和实际可命中的目标一致（已死/不可命中的不算）。
            int enemies = __instance.CombatState.HittableEnemies.Count;
            int hits = (__instance.IsUpgraded ? UpgradedHits : BaseHits) + enemies;

            __instance.DynamicVars.Repeat.BaseValue = hits;
            // 不 return false：原方法照常跑，只是次数换了。
        }
    }

    /// <summary>原版升级是 Repeat +1；现在次数由上面动态算，留着只会让卡面和行为对不上。</summary>
    [HarmonyPatch(typeof(BouncingFlask), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix() => false;
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

            string key = ModelDb.Card<BouncingFlask>().Id.Entry + ".description";
            translations[key] = LocManager.Instance.Language switch
            {
                // ⚠️ 变量名是 {PoisonPower} 不是 {Poison}：PowerVar<T> 用 typeof(T).Name 命名，
                // DynamicVars.Poison 只是 C# 侧的便捷访问器（内部就是 _vars["PoisonPower"]），
                // 而 DynamicVars.AddTo(LocString) 是按 Name 注入的。写错会解析不出数字。
                "zhs" or "zht" or "zh" =>
                    "对随机敌人施加 {PoisonPower} 层[gold]毒[/gold]，共 {IfUpgraded:show:3|2} + 敌人数量 次。",
                _ =>
                    "Apply {PoisonPower} [gold]Poison[/gold] to random enemies {IfUpgraded:show:3|2} + enemy count times.",
            };
        }
    }
}
