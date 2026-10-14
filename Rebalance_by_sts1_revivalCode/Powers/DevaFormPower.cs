using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

/// <summary>
/// 天人形态的「引擎」：**回合结束时**给自己再叠一层 PyrePower（每回合永久 +1 费）。
/// 即设计原文的「回合结束时获得"每回合获得1费"」。
///
/// 这里是**两个分离的能力**：本 power 只负责「回合结束时再加一层」，实际的加费由基础游戏的
/// PyrePower（薪火之源的能力）承担。好处：
///   - 状态栏一眼分得清「引擎」和「已经攒下的加费」
///   - 与狂暴、以及真正的薪火之源共用同一个 PyrePower 计数器，自动合并显示
///   - 引擎万一被移除，已攒下的加费仍在
/// 所以本类**不**自己实现 ModifyMaxEnergy。
///
/// 叠层放在回合结束，卡牌打出时**不**预先给层数，序列为
///   打出当回合 3（不变）→ 回合结束叠 1 层 → 下回合 3+1=4 → 再下回合 3+2=5 …
/// ⚠️ 打出时的预给层和回合结束叠层只能留一个：两个都有会变成 3→5→6，快一拍。
/// </summary>
public sealed class DevaFormPower : Rebalance_by_sts1_revivalPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PyrePower>()];

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (!participants.Contains(Owner)) return;

        Flash();
        await PowerCmd.Apply<PyrePower>(choiceContext, Owner, Amount, Owner, null);
    }
}
