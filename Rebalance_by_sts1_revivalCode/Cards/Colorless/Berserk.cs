using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// 狂暴（原塔1战士「暴怒」）— 无色罕见
/// 0费能力：获得 2(1) 层易伤，此后每回合开始时获得 1 费。
/// 易伤是代价，所以升级是「少受一层」而不是常见的「多给一点」。
/// </summary>
[Pool(typeof(ColorlessCardPool))]
public sealed class Berserk() : Rebalance_by_sts1_revivalCard(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const string VulnerableVar = "VulnerablePower";
    private const int EnergyPerTurn = 1;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VulnerablePower>(2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<PyrePower>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, DynamicVars[VulnerableVar].BaseValue, Owner.Creature, this);

        // 直接用基础游戏薪火之源的能力，不自己造一个同义 power：
        // PyrePower 就是「每回合永久多获得 Amount 点能量」（ModifyMaxEnergy + Counter 叠加），
        // 于是狂暴 / 天人形态 / 真正的薪火之源会合并进同一个计数器显示。
        await PowerCmd.Apply<PyrePower>(choiceContext, Owner.Creature, EnergyPerTurn, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[VulnerableVar].UpgradeValueBy(-1m); // 2 -> 1 Vulnerable; the drawback shrinks
    }
}
