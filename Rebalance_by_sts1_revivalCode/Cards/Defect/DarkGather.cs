using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Defect;

/// <summary>
/// 黑暗汇集 — 机器人稀有
/// X费技能，消耗（升级后去除消耗）：生成 1 个黑暗充能球，然后触发所有黑暗充能球的被动 X 次。
///
/// 注意生成在前、触发在后——新生成的这个也会吃到这 X 次触发，这是这张卡的核心。
/// </summary>
[Pool(typeof(DefectCardPool))]
public sealed class DarkGather() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.Channeling),
        HoverTipFactory.FromOrb<DarkOrb>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await OrbCmd.Channel<DarkOrb>(choiceContext, Owner);

        int times = ResolveEnergyXValue();
        for (int i = 0; i < times; i++)
        {
            // 每轮都重新取一次：期间可能有球被激发或新增，用快照会触发到已经不在队里的球。
            foreach (var orb in Owner.PlayerCombatState.OrbQueue.Orbs.OfType<DarkOrb>().ToList())
                await OrbCmd.Passive(choiceContext, orb, null);
        }
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
