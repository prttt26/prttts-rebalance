using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 双重存在（原塔1猎人）— 无色稀有
/// X费技能，消耗：下回合获得 X(X+1) 费，抽 X(X+1) 张牌。
///
/// X 费的写法是 HasEnergyCostX + ResolveEnergyXValue()（见基础卡 Whirlwind）。
/// 「下回合」的两半都有现成的 power：EnergyNextTurnPower / DrawCardsNextTurnPower，
/// 两者都在触发后自我移除，所以不需要额外清理。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class Doppelganger() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // The +1 reads off IsUpgraded (i.e. CurrentUpgradeLevel) rather than a field set in
        // OnUpgrade: card models are cloned via MutableClone, and only core state and DynamicVars
        // are guaranteed to survive that. A custom field could silently come back 0 on the copy
        // that actually gets played.
        //
        // There is deliberately no OnUpgrade override — X has no DynamicVar to bump, and the
        // card face swaps X -> X+1 through the loc entry's {IfUpgraded:show:...} instead.
        int amount = ResolveEnergyXValue() + (IsUpgraded ? 1 : 0);
        if (amount <= 0) return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
        await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }
}
