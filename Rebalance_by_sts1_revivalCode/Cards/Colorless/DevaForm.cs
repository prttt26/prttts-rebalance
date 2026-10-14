using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 天人形态 / Deva Form（原塔1观者）— 无色稀有
/// 3费能力，虚无（升级后去除虚无）：
/// 回合结束时获得「每回合获得 1 费」—— 每回合能量依次为 3 → 4 → 5 → 6 …（递增，不是恒定 +1）
///
/// 递增逻辑全在 DevaFormPower 里，见那里的说明。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class DevaForm() : Rebalance_by_sts1_revivalCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    /// <summary>每个回合结束叠 1 层，也就是每回合再多 1 费。</summary>
    private const int EnergyGainPerTurn = 1;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DevaFormPower>(),
        HoverTipFactory.FromPower<PyrePower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);

        // 引擎：此后每个回合结束叠一层 PyrePower。打出当回合的回合结束就会叠第一层，
        // 所以这里不预先给 PyrePower——预给会让序列变成快一拍的 3 → 5 → 6。
        await PowerCmd.Apply<DevaFormPower>(choiceContext, Owner.Creature, EnergyGainPerTurn, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
