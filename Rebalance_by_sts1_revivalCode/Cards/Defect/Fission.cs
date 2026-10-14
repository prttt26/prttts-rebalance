using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Defect;

/// <summary>
/// 裂变（原塔1机器人稀有）— 机器人稀有
/// 0费技能，消耗：失去所有充能球；每失去一个，获得 1 点能量并抽 1 张牌。
/// 升级后改为**激发**（而非失去）所有充能球，收益不变。
///
/// 升级的价值在于把「白扔」变成「先吃一轮激发效果再拿收益」，
/// 所以数值一点没变，差别全在处理方式上。
/// </summary>
[Pool(typeof(DefectCardPool))]
public sealed class Fission() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>升级后会激发全部充能球，未升级则只是移除，不触发激发效果。</summary>
    public override OrbEvokeType OrbEvokeType => IsUpgraded ? OrbEvokeType.All : OrbEvokeType.None;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var queue = Owner.PlayerCombatState.OrbQueue;
        int count = queue.Orbs.Count;
        if (count <= 0) return;

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        if (IsUpgraded)
        {
            // 激发会逐个出队，所以按开始时的数量循环，而不是边跑边读 Count。
            for (int i = 0; i < count; i++)
                await OrbCmd.EvokeNext(choiceContext, Owner, dequeue: true);
        }
        else
        {
            RemoveAllOrbs();
        }

        await PlayerCmd.GainEnergy(count, Owner);
        await CardPileCmd.Draw(choiceContext, count, Owner);
    }

    /// <summary>
    /// 「失去」所有充能球：出队 + 播移除动画 + 清理模型，但不触发激发效果。
    ///
    /// ⚠️ 别用 OrbQueue.Clear()。它是**战斗收尾**用的（原版只在生物死亡路径里调），会做两件坏事：
    ///   1. 不播任何动画，球的图像会留在屏幕上（数据没了、图还在，看着像 bug）
    ///   2. 顺手把 Capacity 也清零，等于**永久没收本场战斗的所有充能球栏位**
    /// 正确做法是照 OrbCmd.Evoke 的收尾步骤逐个来，只是跳过 orb.Evoke()。
    /// </summary>
    private void RemoveAllOrbs()
    {
        var queue = Owner.PlayerCombatState.OrbQueue;
        var orbManager = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature)?.OrbManager;

        foreach (var orb in queue.Orbs.ToList()) // 快照：下面会边遍历边出队
        {
            queue.Remove(orb);
            orbManager?.EvokeOrbAnim(orb);
            orb.RemoveInternal();
        }
    }
}
