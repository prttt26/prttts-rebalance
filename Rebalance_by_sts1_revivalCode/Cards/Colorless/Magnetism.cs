using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 磁力（原塔1无色稀有）— 无色罕见
/// 2(1)费能力：每回合向手牌中加入一张随机无色卡。
///
/// Same effect as the Regent card 光谱偏移 (SpectrumShift), which stays in the Regent pool —
/// so this reuses the base game's SpectrumShiftPower rather than cloning it./// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class Magnetism() : Rebalance_by_sts1_revivalCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<SpectrumShiftPower>(choiceContext, Owner.Creature, DynamicVars.Cards.BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 -> 1
    }
}
