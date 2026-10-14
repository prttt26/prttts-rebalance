using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Ironclad;

/// <summary>
/// 灼热攻击（原塔1战士）— 战士罕见
/// 2费攻击，**可以多次升级**：伤害 = 12 + 0.5n² + 3.5n（n = 已升级次数）。
///   n=0 → 12，n=1 → 16，n=2 → 21，n=3 → 27，n=4 → 34 …
///
/// 塔2 原生支持多次升级：CardModel 上有 `public virtual int MaxUpgradeLevel => 1`，
/// 且 CurrentUpgradeLevel 的 setter 会在超过它时抛异常，所以想多升就必须 override。
/// </summary>
[Pool(typeof(IroncladCardPool))]
public sealed class SearingBlow() : Rebalance_by_sts1_revivalCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    /// <summary>塔1原版即为无上限；这里给个够大的数，实际打不到。</summary>
    public override int MaxUpgradeLevel => 99;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(12m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // UpgradeInternal 先 CurrentUpgradeLevel++ 再调本方法，所以这里的值就是新的 n。
        // 每次加 (n+3)，累加即 12 + Σ(k+3) = 12 + 0.5n² + 3.5n，正好是设计给的闭式。
        DynamicVars.Damage.UpgradeValueBy(CurrentUpgradeLevel + 3);
    }
}
