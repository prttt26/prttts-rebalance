using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 以手拒之 / Talk to the Hand（原塔1观者）— 无色罕见
/// 1费攻击，消耗：造成 5(7) 点伤害；此后每当你攻击该敌人时，**你**获得 2(3) 点格挡。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class TalkToTheHand() : Rebalance_by_sts1_revivalCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string BlockPerHit = "TalkToTheHandPower";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new PowerVar<TalkToTheHandPower>(2m),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<TalkToTheHandPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);

        // Apply after the hit so this attack itself doesn't grant block.
        await PowerCmd.Apply<TalkToTheHandPower>(choiceContext, cardPlay.Target, DynamicVars[BlockPerHit].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);      // 5 -> 7
        DynamicVars[BlockPerHit].UpgradeValueBy(1m); // 2 -> 3
    }
}
