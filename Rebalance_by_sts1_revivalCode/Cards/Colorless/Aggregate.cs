using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 汇集（原塔1机器人「汇集」）— 无色罕见
/// 1费技能：抽牌堆每有 4(3) 张卡，获得 1 费。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class Aggregate() : Rebalance_by_sts1_revivalCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string CardsPerEnergy = "Cards";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(CardsPerEnergy, 4m)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int per = DynamicVars[CardsPerEnergy].IntValue;
        if (per <= 0) return; // guard: a 0 divisor would be infinite energy, not a design intent

        int energy = PileType.Draw.GetPile(Owner).Cards.Count / per;
        if (energy > 0) await PlayerCmd.GainEnergy(energy, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[CardsPerEnergy].UpgradeValueBy(-1m); // 4 -> 3 cards per energy
    }
}
