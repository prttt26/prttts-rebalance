using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using BaseLib.Patches.Localization;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Ironclad;

/// <summary>
/// 被遗忘的仪式 (ForgottenRitual) — op: modify
///   原效果：1费技能，若本回合消耗过卡牌则获得 3(4) 费，消耗
///   新效果：获得 5(8) 格挡。此卡被消耗时获得 3(4) 费。若本回合消耗过卡牌，打出时自身消耗。
///
/// 三段效果分别落在三处：
///   格挡      → CanonicalVars + OnPlay
///   消耗给费  → AfterCardExhausted（卡牌自身也会收到战斗钩子）
///   条件消耗  → GetResultPileTypeForCardPlay
///
/// 注意原版的【消耗】词条要**去掉**：新设计里消耗是有条件的，留着词条会变成无条件消耗。
///
/// 这也让这张卡自洽了：满足条件时打出 → 拿格挡 → 自身消耗 → 再拿 3(4) 费。
/// </summary>
public static class ForgottenRitualPatch
{
    private const decimal BaseBlock = 5m;
    private const decimal UpgradeBlockBonus = 3m;   // 5 -> 8
    private const int BaseEnergy = 3;
    private const decimal UpgradeEnergyBonus = 1m;  // 3 -> 4

    /// <summary>本回合是否消耗过卡牌（照搬原版那个 private 属性的判定）。</summary>
    private static bool WasCardExhaustedThisTurn(CardModel card) =>
        CombatManager.Instance.History.Entries
            .OfType<CardExhaustedEntry>()
            .Any(e => e.HappenedThisTurn(card.CombatState) && e.Card.Owner == card.Owner);

    // ---- 数值：格挡 + 消耗时要给的能量 ------------------------------------------------
    [HarmonyPatch(typeof(ForgottenRitual), "CanonicalVars", MethodType.Getter)]
    private static class Vars
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<DynamicVar> __result)
            => __result = [new BlockVar(BaseBlock, ValueProp.Move), new EnergyVar(BaseEnergy)];
    }

    [HarmonyPatch(typeof(ForgottenRitual), "OnUpgrade")]
    private static class Upgrade
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance)
        {
            __instance.DynamicVars.Block.UpgradeValueBy(UpgradeBlockBonus);
            __instance.DynamicVars.Energy.UpgradeValueBy(UpgradeEnergyBonus);
            return false; // 原版只升 Energy，且假定没有 Block var
        }
    }

    // ---- 行为：改为给格挡 --------------------------------------------------------------
    /// <summary>
    /// OnPlay 是 async Task（编译成状态机，postfix 改不了），但 CardModel.OnPlayWrapper 里
    /// 只有一处 `await OnPlay(...)`，所以 prefix 自造 Task + return false 就整体换掉了。
    /// </summary>
    [HarmonyPatch(typeof(ForgottenRitual), "OnPlay")]
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
            await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);
            await CreatureCmd.GainBlock(card.Owner.Creature, card.DynamicVars.Block, cardPlay);
        }
    }

    /// <summary>
    /// GainsBlock 驱动 UI 的格挡预览，只声明在 CardModel 上（ForgottenRitual 没 override），
    /// 所以 patch 基类再判类型。
    /// </summary>
    [HarmonyPatch(typeof(CardModel), "GainsBlock", MethodType.Getter)]
    private static class Block
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref bool __result)
        {
            if (__instance is ForgottenRitual) __result = true;
        }
    }

    // ---- 【消耗】词条：从无条件改为有条件 ---------------------------------------------
    // ForgottenRitual 自己声明了 CanonicalKeywords，所以这里可以直接指向它。
    [HarmonyPatch(typeof(ForgottenRitual), "CanonicalKeywords", MethodType.Getter)]
    private static class Keywords
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<CardKeyword> __result) => __result = [];
    }

    /// <summary>
    /// 条件自身消耗。必须走这里而不是在 OnPlay 里设 ExhaustOnNextPlay——
    /// OnPlayWrapper 是先算好去向的堆再调 OnPlay，OnPlay 里改已经晚了一步。
    /// GetResultPileTypeForCardPlay 声明在 CardModel 上，故 patch 基类再判类型。
    /// </summary>
    [HarmonyPatch(typeof(CardModel), "GetResultPileTypeForCardPlay")]
    private static class ResultPile
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref PileType __result)
        {
            if (__instance is ForgottenRitual && WasCardExhaustedThisTurn(__instance))
                __result = PileType.Exhaust;
        }
    }

    // ---- 被消耗时获得能量 --------------------------------------------------------------
    /// <summary>
    /// AfterCardExhausted 声明在 AbstractModel 上，ForgottenRitual 没有 override，
    /// 所以 patch AbstractModel。CardModel.ShouldReceiveCombatHooks 是
    /// `Pile?.IsCombatPile ?? false`，卡牌在战斗牌堆里就会收到钩子，因此它能听见自己被消耗。
    ///
    /// 钩子返回 Task，postfix 用 ref __result 把原 Task 串在自己的前面，保持时序。
    /// </summary>
    [HarmonyPatch(typeof(AbstractModel), "AfterCardExhausted")]
    private static class ExhaustReward
    {
        [HarmonyPostfix]
        private static void Postfix(AbstractModel __instance, CardModel card, ref Task __result)
        {
            if (__instance is ForgottenRitual ritual && ReferenceEquals(card, __instance))
                __result = GainEnergy(ritual, __result);
        }

        private static async Task GainEnergy(CardModel ritual, Task original)
        {
            await original;
            await PlayerCmd.GainEnergy(ritual.DynamicVars.Energy.IntValue, ritual.Owner);
        }
    }

    // ---- 卡面文本 ---------------------------------------------------------------------
    // 改数据源而不是 DescriptionOverrides：后者在 GetFormattedText 之后才触发，
    // 拿到的是成品字符串，写占位符不会被替换。
    [HarmonyPatch(typeof(ModelDb), "Init")]
    private static class Loc
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            var table = LocManager.Instance.GetTable("cards");
            if (AccessTools.Field(typeof(LocTable), "_translations").GetValue(table)
                is not Dictionary<string, string> translations) return;

            string key = ModelDb.Card<ForgottenRitual>().Id.Entry + ".description";
            MainFile.Logger.Info($"loc override [{LocManager.Instance.Language}] {key}");
            translations[key] = SimpleLoc.TrySimplify(LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "#获得 {B} 点*格挡*。此卡被*消耗*时，获得 [E?]。若本回合消耗过卡牌，此卡在打出时消耗。",
                _ =>
                    "#Gain {B} *Block*. When this card is *Exhausted*, gain [E?]. If a card was Exhausted this turn, this card Exhausts when played.",
            });
        }
    }
}
