using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Defect;

/// <summary>
/// 核心电泳 — 机器人先古
/// 1费能力：获得 3 层人工制品；升级后获得【固有】。
///
/// 设计文档把它归在「禁忌魔典」相关——先古能力卡才能被那件遗物加进牌组，
/// 所以这张必须是 Power 且是 Ancient。
/// </summary>
[Pool(typeof(DefectCardPool))]
public sealed class CoreSurge() : Rebalance_by_sts1_revivalCard(1, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    private const string ArtifactVar = "ArtifactPower";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<ArtifactPower>(3m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<ArtifactPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "PowerUp", Owner.Character.PowerUpAnimDelay);
        await PowerCmd.Apply<ArtifactPower>(
            choiceContext, Owner.Creature, DynamicVars[ArtifactVar].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级不加数值，改为获得词条——AddKeyword 在自己的子类里可以直接调（protected）。
        AddKeyword(CardKeyword.Innate);
    }
}
