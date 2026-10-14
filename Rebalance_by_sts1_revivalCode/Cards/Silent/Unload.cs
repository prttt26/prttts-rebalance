using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Silent;

/// <summary>
/// 乾坤一掷 / Unload — 猎人稀有
/// 1费攻击：造成 10(11) 点伤害，丢弃所有手牌，然后抽等量（升级后等量+1）的牌。
///
/// 丢弃+抽牌用基础卡「计算过的赌局」CalculatedGamble 的同一个命令 CardCmd.DiscardAndDraw，
/// 它内部把两步串好，不用自己拆。
/// </summary>
[Pool(typeof(SilentCardPool))]
public sealed class Unload() : Rebalance_by_sts1_revivalCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10m, ValueProp.Move)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 先打后弃：抽牌数按「实际丢弃的张数」算，所以要在攻击结算后再取手牌。
        var hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        int cardsToDraw = hand.Count + (IsUpgraded ? 1 : 0);

        await CardCmd.DiscardAndDraw(choiceContext, hand, cardsToDraw);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m); // 10 -> 11；多抽的那 1 张走 IsUpgraded
    }
}
