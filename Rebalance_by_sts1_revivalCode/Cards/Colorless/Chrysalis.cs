using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 结茧（原塔1无色稀有）— 无色稀有
/// 2费技能，消耗：向抽牌堆加入 3(5) 张技能卡，它们本场战斗免费。
///
/// 塔2 里没有这张卡（只有攻击卡版本的羽化 Metamorphosis 作为事件卡），所以这是技能卡对应版。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class Chrysalis() : Rebalance_by_sts1_revivalCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await FreeCardGenerator.AddFreeCardsToDrawPile(this, CardType.Skill, DynamicVars.Cards.IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(2m); // 3 -> 5
    }
}
