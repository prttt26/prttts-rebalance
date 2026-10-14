using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Defect;

/// <summary>
/// 自举循环 — 机器人先古
/// 1(0)费技能：触发最右侧充能球的被动，激发它 2 次，然后生成一个刚才被激发的充能球。
///
/// 用来替代被移除的「四重释放」(Quadcast)。备用选项「递归矩阵」未启用，存档在 Shelved/RecursiveMatrix/。
///
/// ⚠️ 「最右侧」对应的是 OrbCmd.EvokeNext（内部取 `Orbs.First()`），不是 EvokeLast。
/// 依据：四重释放的中文描述就是「激发最右侧充能球4次」，而 Quadcast 的实现用的正是 EvokeNext。
/// 别被 First/Last 的字面意思带偏。
/// </summary>
[Pool(typeof(DefectCardPool))]
public sealed class BootstrapLoop() : Rebalance_by_sts1_revivalCard(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    /// <summary>让 UI 知道这张卡会激发队首的球（预览高亮用），与 Quadcast 一致。</summary>
    public override OrbEvokeType OrbEvokeType => OrbEvokeType.Front;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var queue = Owner.PlayerCombatState.OrbQueue;
        if (queue.Orbs.Count <= 0) return;

        // 先记下这个球，激发出队后还要按它的类型生成一个新的。
        var evoked = queue.Orbs.First();

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await OrbCmd.Passive(choiceContext, evoked, null);

        // 激发两次：第一次 dequeue:false 保留在队里，最后一次才真正出队——照搬 Quadcast 的写法。
        await OrbCmd.EvokeNext(choiceContext, Owner, dequeue: false);
        await OrbCmd.EvokeNext(choiceContext, Owner, dequeue: true);

        // 生成一个同类型的新球。不能复用 evoked 本身——它已经出队了；
        // 照 Channel<T> 的做法（ModelDb.Orb<T>().ToMutable()）从规范实例克隆，
        // 只是这里类型要在运行时按 Id 取。
        var fresh = ModelDb.GetById<OrbModel>(evoked.Id).ToMutable();
        await OrbCmd.Channel(choiceContext, fresh, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
