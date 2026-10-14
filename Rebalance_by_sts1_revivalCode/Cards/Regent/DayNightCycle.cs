using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Regent;

/// <summary>
/// 昼夜轮替 — 储君稀有
/// X费X辉星，消耗（升级后去除消耗）：将你的能量与辉星互换。
///
/// 打出时 X 费和 X 辉星会把两种资源都吃光，然后按互换的结果还回来：
///     新能量 = 被消耗的辉星数
///     新辉星 = 被消耗的能量数（含 X 化合物加成）
///
/// 例：2费7星，带遗物 X化合物(+2) → 7费4星。
///
/// ⚠️ 两侧的取值口径**故意不对称**：
///   能量侧用 LastStarsSpent（**原始**辉星数），
///   辉星侧用 ResolveEnergyXValue()（经 Hook.ModifyXValue，含 X 化合物）。
/// 如果能量侧也用 ResolveStarXValue()，上面的例子会变成 9费4星——
/// X 化合物会被算两次。设计确认过是只加在「能量→辉星」这一侧。
/// </summary>
[Pool(typeof(RegentCardPool))]
public sealed class DayNightCycle() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;
    public override bool HasStarCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 此刻两种资源都已被 X 消耗清零，下面是「换个方向还回去」。
        int energyBack = LastStarsSpent;            // 原始辉星数，不吃 X 化合物
        int starsBack = ResolveEnergyXValue();      // 能量数 + X 化合物

        if (energyBack > 0) await PlayerCmd.GainEnergy(energyBack, Owner);
        if (starsBack > 0) await PlayerCmd.GainStars(starsBack, Owner);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
