using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Powers;

/// <summary>
/// 增生挂在**玩家**身上的状态：本回合奥斯提先于你的格挡承受攻击伤害。
///
/// 正常顺序是 格挡 → 奥斯提 → 你的生命；有本状态时变成 奥斯提 → 格挡 → 你的生命。
/// 奥斯提吃不完的部分才轮到格挡，所以打穿奥斯提之后格挡照常生效。
///
/// 伤害管线（CreatureCmd.Damage）里没有「格挡前改目标」的钩子，做法是分两步：
///   1. BeforeDamageReceived —— 它紧挨在扣格挡之前被调用，且带着原始目标。
///      在这里判定这一击是否适用，适用就记下「奥斯提要先吃掉多少」。
///   2. SpurPatch 里 prefix Creature.DamageBlockInternal，把记下的量从「要用格挡去挡的伤害」里扣掉。
///      没被格挡的部分随后按原版流程走：DieForYouPower 把它转给奥斯提，
///      奥斯提的溢出伤害（OverkillDamage）再回到玩家生命上。
/// 即只动「格挡挡多少」这一个数，转移和溢出全用原版逻辑。
///
/// 适用条件与 DieForYouPower 保持一致（只认 powered attack、奥斯提活着），
/// 否则会出现「格挡让开了、奥斯提却不接」而白白掉血。
///
/// ⚠️ 不叠加持续时间：StackType 是 Single，一回合打多少张增生都只有这一个状态，
/// 在你的下个回合开始时整体移除（要撑过敌方回合，所以不是在回合结束时移除）。
/// </summary>
public sealed class SpurPower : Rebalance_by_sts1_revivalPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>BeforeDamageReceived 记下的那一击的伤害量；null 表示这一击不适用。</summary>
    private decimal? _pendingHitAmount;
    private decimal _pendingAbsorb;

    public override Task BeforeDamageReceived(PlayerChoiceContext choiceContext, Creature target, decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // 直接打奥斯提的攻击不在这里处理（target 是奥斯提），那种情况保持原版：先扣你的格挡。
        if (target != Owner) return Task.CompletedTask;

        _pendingHitAmount = null;

        var osty = Owner.Player?.Osty;
        if (osty == null || !osty.IsAlive || !osty.HasPower<DieForYouPower>()) return Task.CompletedTask;
        if (!props.IsPoweredAttack() || props.HasFlag(ValueProp.Unblockable)) return Task.CompletedTask;

        _pendingHitAmount = amount;
        _pendingAbsorb = Math.Min(amount, osty.CurrentHp);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 由 SpurPatch 在扣格挡前调用，返回奥斯提要先承受的伤害量。按伤害量对一下，确认就是
    /// BeforeDamageReceived 记下的那一击——两者之间别的模型的钩子理论上可以再打出一次伤害。
    /// </summary>
    internal decimal ConsumePendingAbsorb(decimal amount)
    {
        if (_pendingHitAmount != amount) return 0m;

        _pendingHitAmount = null;
        Flash();
        return _pendingAbsorb;
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner)) await PowerCmd.Remove(this);
    }
}
