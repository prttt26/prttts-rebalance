using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 操控现实（原塔1观者）— 无色稀有
/// 1(0)费能力：战斗中所有产生的卡都升级。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class MasterReality() : Rebalance_by_sts1_revivalCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<MasterRealityPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<MasterRealityPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
