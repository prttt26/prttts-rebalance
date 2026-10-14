using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Necrobinder;

/// <summary>
/// 忧郁 (Melancholy) — op: modify，亡灵契约师罕见
///   原效果：3费，获得 13(17) 格挡。每当有任何生物死亡时，这张牌的耗能减少 1
///   新效果：3费，获得 13(17) 格挡，**获得 1 点能量**。每当有任何生物死亡时，这张牌的耗能减少 1
///
/// 亡灵契约师缺可复用的加费，这张改成「有条件的加费卡」：返费恒定为 1，
/// 净耗能随死亡数递减 —— 0 次死亡净 2 费、1 次净 1、2 次净 0、3 次起每次打出**净赚 1 费**。
/// 奥斯提的死亡也算，所以它和「增生」那类送死奥斯提的打法是一套。
/// </summary>
public static class MelancholyPatch
{
    /// <summary>
    /// 原版 OnPlay（给格挡）保留，跑完之后追加给费。async 方法的 postfix 要包 Task，见 UpMySleevePatch。
    /// 返费量直接读卡上原有的 Energy 变量（原版用它表示「每次死亡降几费」，值为 1），
    /// 卡面两处能量图标因此始终一致。
    /// </summary>
    [HarmonyPatch(typeof(Melancholy), "OnPlay")]
    private static class Play
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref Task __result) =>
            __result = ThenGainEnergy(__result, __instance);

        private static async Task ThenGainEnergy(Task original, CardModel card)
        {
            await original;
            await PlayerCmd.GainEnergy(card.DynamicVars.Energy.IntValue, card.Owner);
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

            // 沿用原版条目的写法（{Block:diff()} / {Energy:energyIcons()}），只在中间插一句。
            string key = ModelDb.Card<Melancholy>().Id.Entry + ".description";
            translations[key] = LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "获得{Block:diff()}点[gold]格挡[/gold]。\n获得{Energy:energyIcons()}。\n每当有任何生物死亡时，这张牌的耗能减少{Energy:energyIcons()}。",
                _ =>
                    "Gain {Block:diff()} [gold]Block[/gold].\nGain {Energy:energyIcons()}.\nReduce this card's cost by {Energy:energyIcons()} whenever ANYONE dies.",
            };
        }
    }
}
