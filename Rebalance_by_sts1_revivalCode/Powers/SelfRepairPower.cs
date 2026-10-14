using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

/// <summary>
/// 自我修复的持续效果：战斗结束时回复 Amount 点生命。
///
/// 用 AfterCombatEnd 而不是 AfterCombatVictory：**逃跑也要给回血**，只有战败（死亡）不给。
/// AfterCombatVictory 只在打赢时触发，会把逃跑一起漏掉。
/// </summary>
public sealed class SelfRepairPower : Rebalance_by_sts1_revivalPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner.IsDead) return;

        Flash();
        await CreatureCmd.Heal(Owner, Amount);
    }
}
