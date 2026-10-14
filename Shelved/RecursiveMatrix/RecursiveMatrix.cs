// ── 备用选项，未启用（2026-10-12）────────────────────────────────────
// 「递归矩阵」是机器人先古卡的备用方案，启用中的是「自举循环」。
// 本目录只做存档，**不参与编译**（csproj 里 <Compile Remove="Shelved/**" />），也不会被打进 .pck。
//
// 启用步骤：
//   1. 把 RecursiveMatrix.cs 移回 Rebalance_by_sts1_revivalCode/Cards/Defect/，删掉这段说明
//   2. 把 cards.eng.json / cards.zhs.json 里的条目并回 Rebalance_by_sts1_revival/localization/<lang>/cards.json
//   3. 若要让古老牙齿给它：改 AncientRelicPatch 里 Dualcast 的映射（现在指向 BootstrapLoop）
//   4. 设计文档、cards.yaml（status: shelved）里的「备用选项」标记一并改回
// ────────────────────────────────────────────────────────────────────

using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Defect;

/// <summary>
/// 递归矩阵 — 机器人先古
/// 1(0)费技能：激发所有充能球，然后按顺序生成所有被激发的充能球。
///
/// 与「自举循环」同为四重释放的替代方案；二选一里启用的是自举循环，本卡是备用选项。
/// </summary>
[Pool(typeof(DefectCardPool))]
public sealed class RecursiveMatrix() : Rebalance_by_sts1_revivalCard(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    /// <summary>这张会激发全部充能球，所以是 All 而不是 Front。</summary>
    public override OrbEvokeType OrbEvokeType => OrbEvokeType.All;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var queue = Owner.PlayerCombatState.OrbQueue;
        if (queue.Orbs.Count <= 0) return;

        // 先把顺序快照下来：激发会逐个出队，边激发边读 Orbs 会漏。
        var order = queue.Orbs.Select(orb => orb.Id).ToList();

        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 每次都激发队首，激发 Count 次即等于「激发所有」。
        for (int i = 0; i < order.Count; i++)
            await OrbCmd.EvokeNext(choiceContext, Owner, dequeue: true);

        // 「按顺序」生成——用快照的顺序重新入队，保持原本的左右次序。
        foreach (var id in order)
            await OrbCmd.Channel(choiceContext, ModelDb.GetById<OrbModel>(id).ToMutable(), Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
