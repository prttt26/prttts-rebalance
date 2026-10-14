using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Silent;

/// <summary>
/// 弃子争先 — 猎人罕见
/// 0费技能：丢弃 2 张手牌，下回合获得 2(3) 点能量。
///
/// 「下回合给费」用现成的一次性 buff EnergyNextTurnPower（给完自我移除）——
/// 这类效果不要用 ModifyMaxEnergy，那是永续加费用的。
/// </summary>
[Pool(typeof(SilentCardPool))]
public sealed class PawnSacrifice() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const int CardsToDiscard = 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(2)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var chosen = await CardSelectCmd.FromHandForDiscard(
            choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, CardsToDiscard), null, this);

        if (chosen.Any()) await CardCmd.Discard(choiceContext, chosen);

        // 手牌不足 2 张时照样给费：丢弃是代价，付不满不该连收益一起没收。
        await PowerCmd.Apply<EnergyNextTurnPower>(
            choiceContext, Owner.Creature, DynamicVars.Energy.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m); // 2 -> 3
    }
}
