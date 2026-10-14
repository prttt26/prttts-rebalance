using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 疯狂（原塔1无色罕见）— 无色罕见
/// 1(0)费技能，消耗：使手牌中一张随机卡本场战斗免费。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class Madness() : Rebalance_by_sts1_revivalCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        // 排除掉「变免费也没有意义」的卡；自身也排除（结算时它还在手牌里，而它马上就要消耗）
        var eligible = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this && IsWorthMakingFree(c))
            .ToList();

        // 分两档：优先选此刻真的还要花费的卡。
        // 只有当没有这种卡时，才退而求其次选「临时0费」的——把临时0费转成永久0费仍有价值，
        // 但显然不如直接省下一张全价卡，所以后置。这与塔1的取舍一致。
        var payingNow = eligible.Where(c => c.CostsEnergyOrStars(includeGlobalModifiers: true)).ToList();
        var candidates = payingNow.Count > 0 ? payingNow : eligible;

        // 一张合适的都没有时，这张牌打出来无事发生（仍然消耗）。
        if (candidates.Count == 0) return;

        candidates[Owner.RunState.Rng.CombatCardGeneration.NextInt(candidates.Count)].SetToFreeThisCombat();
    }

    /// <summary>
    /// 让这张卡变免费是否有意义。三类排除：
    ///   - 不可打出：免费了也打不出来
    ///   - X 费：X 费卡吃的是当前全部能量，「变0费」对它无效
    ///   - 本来就是 0 费 0 辉星：没有任何收益可拿
    /// 注意「当前恰好是 0 费」不在排除之列——那可能只是临时效果，转成永久仍有价值，
    /// 由调用处降级处理。
    /// </summary>
    private static bool IsWorthMakingFree(CardModel card)
    {
        if (card.Keywords.Contains(CardKeyword.Unplayable)) return false;
        if (card.EnergyCost.CostsX || card.HasStarCostX) return false;

        // Canonical / BaseStarCost 是**永久**基础值，不含临时修正；
        // CanonicalStarCost 默认 -1 表示无辉星消耗，所以用 > 0 判断。
        return card.EnergyCost.Canonical > 0 || card.BaseStarCost > 0;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 1 -> 0
    }
}
