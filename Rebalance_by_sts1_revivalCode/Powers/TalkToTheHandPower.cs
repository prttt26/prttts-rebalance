using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

/// <summary>
/// 以手拒之留在敌人身上的标记：**攻击者**每次用攻击牌打这个敌人时，攻击者获得 Amount 点格挡。
///
/// ⚠️ 格挡给 dealer（攻击方），不是给 Owner（挂着标记的敌人）。设计文档那句
/// 「这个敌人每次被攻击时获得2格挡」主语有歧义，塔1原版是
/// "Whenever you attack this enemy, gain 2 Block" —— 收益是玩家的。
/// 给成敌人就成了纯负面卡。
///
/// 挂在敌人身上但对敌人不利，所以 Type 是 Debuff。
/// 只认攻击牌造成的伤害——中毒、灼烧一类的非攻击伤害不该触发；
/// 多段攻击会逐段触发，这与塔1一致。
/// </summary>
public sealed class TalkToTheHandPower : Rebalance_by_sts1_revivalPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || cardSource?.Type != CardType.Attack) return;

        Flash();
        await CreatureCmd.GainBlock(dealer, Amount, ValueProp.Move, null);
    }
}
