using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 自我修复（原塔1机器人，数值增强）— 无色稀有
/// 1费能力，恢复性：战斗结束时回复 9(12) 点生命（逃跑也给，战败不给）。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class SelfRepair() : Rebalance_by_sts1_revivalCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    /// <summary>
    /// 「恢复性」——战斗中随机生成卡牌的效果不会产生此卡，防止刻意拖长战斗刷回复。
    ///
    /// 它不是 CardKeyword（枚举里没有对应项），而是 CardModel 上的这个虚属性：
    /// CardFactory.FilterForCombat 会 `cards.Where(c => c.CanBeGeneratedInCombat && ...)`，
    /// 所以结茧/羽化/磁力/发现 一类的战斗内生成都会跳过它。
    /// 卡牌奖励、商店、转化不走这条过滤，正常可获得。
    /// </summary>
    public override bool CanBeGeneratedInCombat => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<SelfRepairPower>(9m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<SelfRepairPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<SelfRepairPower>(choiceContext, Owner.Creature, DynamicVars["SelfRepairPower"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["SelfRepairPower"].UpgradeValueBy(3m); // 9 -> 12
    }
}
