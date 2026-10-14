using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Necrobinder;

/// <summary>
/// 收集灵魂 — 亡灵契约师稀有
/// 1费技能，保留：将弃牌堆和抽牌堆中的「灵魂」尽可能加入手牌。
/// 升级后额外向手牌加入一张新的灵魂。
///
/// 灵魂是亡灵契约师的核心过牌件（0费抽2(3)、消耗），大量卡会往两个牌堆里塞灵魂。
/// 这张的作用是把散落的灵魂一次性收拢，配合「点燃灵魂」可以直接转成一波加费。
/// </summary>
[Pool(typeof(NecrobinderCardPool))]
public sealed class CollectSouls() : Rebalance_by_sts1_revivalCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromCard<Soul>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 按次序：先弃牌堆、后抽牌堆（设计里写明「按次序执行」）。
        var souls = PileType.Discard.GetPile(Owner).Cards.OfType<Soul>()
            .Concat(PileType.Draw.GetPile(Owner).Cards.OfType<Soul>())
            .ToList();

        foreach (var soul in souls)
        {
            // 逐张加而不是一次性 Add：手牌满了要停下来，剩下的留在原堆里。
            // 不去查手牌上限常量（游戏里没暴露），改用「加完没变多就说明满了」这个行为判据——
            // 无论上限是多少、将来怎么改，这个判断都成立。
            int before = PileType.Hand.GetPile(Owner).Cards.Count;
            await CardPileCmd.Add(soul, PileType.Hand);
            if (PileType.Hand.GetPile(Owner).Cards.Count == before) break;
        }

        if (!IsUpgraded) return;

        // 升级额外给一张全新的灵魂（同样可能因手牌满而落空）。
        var fresh = CombatState.CreateCard<Soul>(Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.AddGeneratedCardToCombat(fresh, PileType.Hand, Owner));
    }
}
