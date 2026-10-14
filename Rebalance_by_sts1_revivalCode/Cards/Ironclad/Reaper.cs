using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Ironclad;

/// <summary>
/// 死亡收割（原塔1战士，数值增强）— 战士先古
/// 2费攻击，消耗、恢复性：对所有敌人造成 8(11) 点伤害，并按**未被格挡**的总伤害回复生命。
/// 塔1原版是 4(5) 伤，为先古卡地位而强化。
/// </summary>
[Pool(typeof(IroncladCardPool))]
public sealed class Reaper() : Rebalance_by_sts1_revivalCard(2, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    /// <summary>
    /// 「恢复性」——防止靠拖长战斗反复刷回血。
    /// 严格说这里是冗余的：CardFactory.FilterForCombat 本来就排除 Ancient 稀有度，
    /// 战斗内生成永远不会产出先古卡。保留是为了让设计意图显式可见，且万一以后调稀有度也不会漏。
    /// </summary>
    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(8m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var attack = await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // Execute 返回 AttackCommand 本身，Results 里是每个目标的 DamageResult。
        // 按未被格挡的部分求和——打在格挡上的伤害不该转化成回血。
        int healed = attack.Results.SelectMany(results => results).Sum(result => result.UnblockedDamage);
        if (healed > 0) await CreatureCmd.Heal(Owner.Creature, healed);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 8 -> 11
    }
}
