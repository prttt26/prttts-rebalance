using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

/// <summary>
/// 操控现实的持续效果：本场战斗中产生的卡都升级。
///
/// AfterCardGeneratedForCombat 覆盖的正是「战斗中产生」这个范畴——
/// 结茧/羽化塞进抽牌堆的、磁力每回合给的随机无色，都会走这里。
/// </summary>
public sealed class MasterRealityPower : Rebalance_by_sts1_revivalPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        // Only our own generated cards, and never double-upgrade one that already is.
        if (creator == Owner.Player && !card.IsUpgraded)
        {
            Flash();
            CardCmd.Upgrade(card);
        }
        return Task.CompletedTask;
    }
}
