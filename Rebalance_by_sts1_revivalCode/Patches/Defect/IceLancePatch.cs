using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Defect;

/// <summary>
/// 冰之长枪 (IceLance) — op: modify，机器人稀有
///   原效果：3费攻击，造成 19(24) 点伤害，然后获得 3 个寒冰充能球（升级只加伤害）
///   新效果：3(2)费攻击，获得 3 个寒冰充能球，造成「本场战斗生成的寒冰充能球数 × 2」点伤害
///
/// 从固定数值改成随本场战斗累计的寒冰球数成长——寒冰流铺得越久，这张越重。
/// 升级由「+5 伤害」改为「降 1 费」。
/// </summary>
public static class IceLancePatch
{
    private const int OrbsGranted = 2;      // 每个寒冰球提供的伤害
    private const int FrostOrbsChanneled = 3; // 本卡自身生成的寒冰球数

    /// <summary>
    /// 本场战斗中该玩家生成过的寒冰充能球总数（累计量，不是当前场上的数量）。
    /// 数据源是战斗历史：OrbCmd.Channel 每次都会记一条 OrbChanneledEntry，
    /// 所以不用自己维护跨回合状态。
    /// </summary>
    private static int FrostChanneledThisCombat(CardModel card) =>
        CombatManager.Instance.History.Entries
            .OfType<OrbChanneledEntry>()
            .Count(e => e.Orb is FrostOrb && e.Actor == card.Owner.Creature);

    /// <summary>
    /// 卡面用的倍率。**要把本卡即将生成的 3 个球算进去**：
    /// 设计是「先获得球、再按总数结算」，如果显示时不算这 3 个，
    /// 打出前卡面显示 2N、实际却打出 2(N+3)，玩家会觉得数字是错的。
    ///
    /// 必须是静态方法：CalculatedVar.WithMultiplier 里有
    /// `if (multiplierCalc.Target is AbstractModel) throw`。
    /// </summary>
    private static decimal FrostMultiplier(CardModel card, Creature? target) =>
        FrostChanneledThisCombat(card) + FrostOrbsChanneled;

    [HarmonyPatch(typeof(IceLance), "CanonicalVars", MethodType.Getter)]
    private static class Vars
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<DynamicVar> __result) =>
            // 结构同基础卡 MindBlast：伤害 = 基础值 + 每单位值 × 倍率。
            __result =
            [
                new CalculationBaseVar(0m),
                new ExtraDamageVar(OrbsGranted),
                new CalculatedDamageVar(ValueProp.Move).WithMultiplier(FrostMultiplier),
            ];
    }

    [HarmonyPatch(typeof(IceLance), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.EnergyCost.UpgradeBy(-1); // 3 -> 2；原版是伤害 +5
            return false;
        }
    }

    [HarmonyPatch(typeof(IceLance), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
        {
            __result = NewOnPlay(__instance, choiceContext, cardPlay);
            return false;
        }

        private static async Task NewOnPlay(CardModel card, PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);

            // 先生成，再结算——顺序就是设计里写的。
            for (int i = 0; i < FrostOrbsChanneled; i++)
                await OrbCmd.Channel<FrostOrb>(choiceContext, card.Owner);

            // 这里不能把 CalculatedDamageVar 交给 DamageCmd：此刻 3 个球已经入队，
            // 而那个倍率函数还会再加一次 FrostOrbsChanneled，会算成 2(N+6)。
            // 该 var 只负责卡面显示，实际伤害在这里显式算。
            decimal damage = FrostChanneledThisCombat(card) * OrbsGranted;

            await DamageCmd.Attack(damage).FromCard(card).Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
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

            string key = ModelDb.Card<IceLance>().Id.Entry + ".description";
            translations[key] = SimpleLoc.TrySimplify(LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "#获得 3 个*寒冰*充能球，然后造成 {CD} 点伤害（本场战斗每生成 1 个寒冰充能球提供 2 点）。",
                _ =>
                    "#Channel 3 *Frost* orbs, then deal {CD} damage (2 per Frost orb Channeled this combat).",
            });
        }
    }
}
