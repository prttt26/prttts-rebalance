using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Silent;

/// <summary>
/// 触不可及 (Untouchable) — op: modify，猎人普通
///   原效果：2费，奇巧，获得 6(9) 点格挡（只有本回合）
///   新效果：2费，奇巧，本回合和下回合各获得 5(7) 点格挡
///
/// 【奇巧】原版就有，不用动。改的是两点：数值下调，以及把格挡延续到下回合。
/// 这张卡等于接手了被移除的「闪躲翻滚」(DodgeAndRoll) 的定位——后者正是
/// 「本回合和下回合各获得格挡」，实现也照它抄：GainBlock 的返回值就是实际获得的格挡量，
/// 拿它去 apply BlockNextTurnPower，这样力量/敏捷等修正会一并带到下回合。
/// </summary>
public static class UntouchablePatch
{
    private const decimal BaseBlock = 5m;
    private const decimal UpgradeBlockBonus = 2m; // 5 -> 7

    [HarmonyPatch(typeof(Untouchable), "CanonicalVars", MethodType.Getter)]
    private static class Vars
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<DynamicVar> __result)
            => __result = [new BlockVar(BaseBlock, ValueProp.Move)];
    }

    [HarmonyPatch(typeof(Untouchable), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
            return false; // 原版是 +3
        }
    }

    [HarmonyPatch(typeof(Untouchable), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance, CardPlay cardPlay, ref Task __result)
        {
            __result = NewOnPlay(__instance, cardPlay);
            return false;
        }

        private static async Task NewOnPlay(CardModel card, CardPlay cardPlay)
        {
            decimal gained = await CreatureCmd.GainBlock(card.Owner.Creature, card.DynamicVars.Block, cardPlay);
            await PowerCmd.Apply<BlockNextTurnPower>(
                new ThrowingPlayerChoiceContext(), card.Owner.Creature, gained, card.Owner.Creature, card);
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

            string key = ModelDb.Card<Untouchable>().Id.Entry + ".description";
            translations[key] = SimpleLoc.TrySimplify(LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" => "#本回合和下回合各获得 {B} 点*格挡*。",
                _ => "#Gain {B} *Block* this turn and next turn.",
            });
        }
    }
}
