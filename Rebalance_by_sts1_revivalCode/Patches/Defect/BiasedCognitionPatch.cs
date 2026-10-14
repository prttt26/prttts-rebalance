using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Defect;

/// <summary>
/// 偏差认知 (BiasedCognition) — op: modify，机器人先古（cards.yaml 的 defect_bias 方案1）
///   原效果：获得 4(5) 集中，**每**回合开始时失去 1 集中
///   新效果：获得 4(5) 集中，**若上一回合未曾失去集中**，回合开始时才失去 1 集中
///
/// 意图是和「临时集中」(TemporaryFocusPower) 联动：临时集中到期时本身就会扣一次集中，
/// 这时本卡不再叠加第二次扣除，从而让两者能配合而不是互相踩。
///
/// 卡数值本身不动（FocusPower 4(5)、BiasedCognitionPower 1），只改扣除的触发条件，
/// 所以只需要换掉 BiasedCognitionPower 的回合开始逻辑。
/// </summary>
public static class BiasedCognitionPatch
{
    /// <summary>
    /// 上一回合失去过集中的实例。回合开始时消费掉，等价于「只记一回合」。
    ///
    /// 用 ConditionalWeakTable 而不是 HashSet：键是弱引用，power 实例随战斗结束被回收后
    /// 条目自动消失，不需要额外挂个「战斗结束」钩子来清理（也就不用去赌那个方法叫什么名字）。
    /// </summary>
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<PowerModel, object> LostFocusLastTurn = new();

    /// <summary>本卡自己扣集中时置位，避免把自己的扣除也算成「失去过集中」而变成隔回合触发。</summary>
    private static bool _applyingOwnDrain;

    private static readonly System.Reflection.MethodInfo FlashMethod =
        AccessTools.Method(typeof(PowerModel), "Flash");

    [HarmonyPatch(typeof(BiasedCognitionPower), "AfterSideTurnStart")]
    private static class TurnStart
    {
        [HarmonyPrefix]
        private static bool Prefix(PowerModel __instance, IReadOnlyList<Creature> participants, ref Task __result)
        {
            __result = Run(__instance, participants);
            return false; // 原版是无条件扣
        }

        private static async Task Run(PowerModel power, IReadOnlyList<Creature> participants)
        {
            if (!participants.Contains(power.Owner)) return;

            // 上一回合失去过集中 → 这回合免扣。Remove 同时起到「读取并清零」的作用。
            if (LostFocusLastTurn.Remove(power)) return;


            FlashMethod?.Invoke(power, null);

            _applyingOwnDrain = true;
            try
            {
                await PowerCmd.Apply<FocusPower>(
                    new ThrowingPlayerChoiceContext(), power.Owner, -power.Amount, power.Owner, null);
            }
            finally
            {
                _applyingOwnDrain = false;
            }
        }
    }

    /// <summary>
    /// 记录「失去集中」。AfterPowerAmountChanged 声明在 AbstractModel 上，
    /// 而 PowerModel 在战斗中会收到钩子，所以每个 BiasedCognitionPower 实例都能听见集中变化。
    /// </summary>
    [HarmonyPatch(typeof(AbstractModel), "AfterPowerAmountChanged")]
    private static class TrackFocusLoss
    {
        [HarmonyPostfix]
        private static void Postfix(AbstractModel __instance, PowerModel power, decimal amount)
        {
            if (_applyingOwnDrain) return; // 自己造成的不算
            if (amount >= 0) return;       // 只关心「失去」

            if (__instance is BiasedCognitionPower self && power is FocusPower && power.Owner == self.Owner)
            {
                LostFocusLastTurn.Remove(self);      // 幂等：同一回合多次失去只记一次
                LostFocusLastTurn.Add(self, Marker);
            }
        }
    }

    private static readonly object Marker = new();

    /// <summary>
    /// 卡面和能力说明都要改：原文是「每回合开始时失去 1 集中」，无条件；
    /// 新逻辑多了「上一回合未失去过集中」这个前提，不写出来玩家没法预判什么时候会扣。
    ///
    /// 走覆盖 loc 表条目的路子，**不要**用 BaseLib 的 DescriptionOverrides.CustomizeDescription：
    /// 那个是在 LocString.GetFormattedText() **之后**注入的，{FocusPower} 这类占位符
    /// 已经过了替换时机，会原样显示成一串花括号。
    /// </summary>
    [HarmonyPatch(typeof(ModelDb), "Init")]
    private static class Loc
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            bool zh = LocManager.Instance.Language is "zhs" or "zht" or "zh";

            // 卡面：变量名取自 CanonicalVars，是 PowerVar<T> 的 typeof(T).Name，
            // 所以是 {FocusPower} / {BiasedCognitionPower}，不是 {Focus}。
            if (Translations("cards") is { } cards)
            {
                cards[ModelDb.Card<BiasedCognition>().Id.Entry + ".description"] = zh
                    ? "获得 {FocusPower} 层[gold]集中[/gold]。回合开始时，若你上一回合未失去过[gold]集中[/gold]，失去 {BiasedCognitionPower} 层[gold]集中[/gold]。"
                    : "Gain {FocusPower} [gold]Focus[/gold]. At the start of your turn, if you did not lose [gold]Focus[/gold] last turn, lose {BiasedCognitionPower} [gold]Focus[/gold].";
            }

            // 能力图标的悬浮说明。能力条目用 {Amount}，不是卡上的变量名。
            if (Translations("powers") is { } powers)
            {
                string entry = ModelDb.Power<BiasedCognitionPower>().Id.Entry;
                powers[entry + ".description"] = zh
                    ? "回合开始时，若你上一回合未失去过[gold]集中[/gold]，失去[gold]集中[/gold]。"
                    : "At the start of your turn, if you did not lose [gold]Focus[/gold] last turn, lose [gold]Focus[/gold].";
                powers[entry + ".smartDescription"] = zh
                    ? "回合开始时，若你上一回合未失去过[gold]集中[/gold]，失去 {Amount} 层[gold]集中[/gold]。"
                    : "At the start of your turn, if you did not lose [gold]Focus[/gold] last turn, lose {Amount} [gold]Focus[/gold].";
            }
        }

        private static Dictionary<string, string>? Translations(string table) =>
            AccessTools.Field(typeof(LocTable), "_translations")
                       .GetValue(LocManager.Instance.GetTable(table)) as Dictionary<string, string>;
    }
}
