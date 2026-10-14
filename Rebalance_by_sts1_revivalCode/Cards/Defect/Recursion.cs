using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Defect;

/// <summary>
/// 递归 — 机器人罕见
/// 1(0)费技能：激发最右侧充能球，然后生成一个刚才被激发的充能球。
///
/// 「最右侧」= OrbCmd.EvokeNext（取 Orbs.First()），详见 BootstrapLoop 的说明。
/// </summary>
[Pool(typeof(DefectCardPool))]
public sealed class Recursion() : Rebalance_by_sts1_revivalCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override OrbEvokeType OrbEvokeType => OrbEvokeType.Front;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var queue = Owner.PlayerCombatState.OrbQueue;
        if (queue.Orbs.Count <= 0) return;

        var evoked = queue.Orbs.First();
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await OrbCmd.EvokeNext(choiceContext, Owner, dequeue: true);
        await OrbCmd.Channel(choiceContext, ModelDb.GetById<OrbModel>(evoked.Id).ToMutable(), Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
