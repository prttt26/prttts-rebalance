using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Necrobinder;

/// <summary>
/// 点燃灵魂 — 亡灵契约师罕见
/// 1费技能：获得 6(7) 点格挡，将手牌中的所有「灵魂」变成「燃料」（升级后变成燃料+）。
///
/// 启用的是**燃料版**。备用选项是变成「奇迹」，但奇迹在塔2里不存在、需要另造一张衍生卡；
/// 燃料 (Fuel) 是现成的，成本低得多。
///
/// 设计目标是把灵魂从「过牌」转成「加费」：
///   灵魂 = 0费抽2(3)消耗；燃料 = 0费获得1费、抽1(2)、消耗。
/// </summary>
[Pool(typeof(NecrobinderCardPool))]
public sealed class IgniteSoul() : Rebalance_by_sts1_revivalCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(6m, ValueProp.Move)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<Soul>(),
        HoverTipFactory.FromCard<Fuel>(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 快照：Transform 会改动手牌，边遍历边改会漏。
        foreach (var soul in PileType.Hand.GetPile(Owner).Cards.OfType<Soul>().ToList())
        {
            var fuel = CombatState.CreateCard<Fuel>(Owner);
            if (IsUpgraded) CardCmd.Upgrade(fuel); // 升级版给的是「燃料+」
            await CardCmd.Transform(soul, fuel);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m); // 6 -> 7
    }
}
