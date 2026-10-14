using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Defect;

/// <summary>
/// 耗尽（原塔1机器人罕见）— 机器人稀有
/// 2费技能：失去一个充能球栏位，获得 2(3) 集中。
/// </summary>
[Pool(typeof(DefectCardPool))]
public sealed class Consume() : Rebalance_by_sts1_revivalCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const string FocusVar = "FocusPower";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<FocusPower>(2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<FocusPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // RemoveSlots 是同步的（内部就是 OrbQueue.RemoveCapacity）。
        OrbCmd.RemoveSlots(Owner, 1);

        await PowerCmd.Apply<FocusPower>(
            choiceContext, Owner.Creature, DynamicVars[FocusVar].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[FocusVar].UpgradeValueBy(1m); // 2 -> 3
    }
}
