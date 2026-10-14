using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Regent;

/// <summary>
/// 追打 — 储君罕见
/// 1费攻击：造成 5(7) 点伤害，铸造 5(7)。本回合若打出过君王之剑，获得 1 点能量并抽 1 张牌。
///
/// 名字取「追着剑打」的意思——鼓励先出君王之剑再接这张，形成一个小连招。
/// </summary>
[Pool(typeof(RegentCardPool))]
public sealed class FollowUp() : Rebalance_by_sts1_revivalCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const int BonusEnergy = 1;
    private const int BonusDraw = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new ForgeVar(5),
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await ForgeCmd.Forge(DynamicVars.Forge.IntValue, Owner, this);

        if (!PlayedSovereignBladeThisTurn()) return;

        await PlayerCmd.GainEnergy(BonusEnergy, Owner);
        await CardPileCmd.Draw(choiceContext, BonusDraw, Owner);
    }

    /// <summary>
    /// 本回合是否打出过君王之剑。用战斗历史的 CardPlayFinishedEntry + HappenedThisTurn，
    /// 不需要自己维护状态（铸造那边没有历史条目才要自己记，打出是有的）。
    /// </summary>
    private bool PlayedSovereignBladeThisTurn() =>
        CombatManager.Instance.History.Entries
            .OfType<CardPlayFinishedEntry>()
            .Any(e => e.HappenedThisTurn(CombatState)
                      && e.CardPlay.Card is SovereignBlade
                      && e.CardPlay.Card.Owner == Owner);

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); // 5 -> 7
        DynamicVars.Forge.UpgradeValueBy(2m);  // 5 -> 7
    }
}
