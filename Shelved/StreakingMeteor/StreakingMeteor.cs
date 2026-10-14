// ── 暂时放弃（2026-10-12）────────────────────────────────────────────
// 「划空流星」已从 mod 中撤下，本目录只做存档，**不参与编译**
// （csproj 里 <Compile Remove="Shelved/**" />），也不会被打进 .pck。
//
// 恢复步骤：
//   1. 把 StreakingMeteor.cs / MeteorOptions.cs 移回 Rebalance_by_sts1_revivalCode/Cards/Regent/，删掉这段说明
//   2. 把 cards.eng.json / cards.zhs.json 里的条目并回 Rebalance_by_sts1_revival/localization/<lang>/cards.json
//   3. 储君稀有池会多 1 张，需要另删/另移一张稀有卡维持卡数
//   4. 设计文档、cards.yaml（status: shelved）、测试方案 里的「暂时放弃」标记一并改回
// ────────────────────────────────────────────────────────────────────

using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Regent;

/// <summary>
/// 划空流星 — 储君稀有
/// 1费技能，虚无（升级后改为保留）：**被抽到时立即触发效果，且本卡留在手牌中**。
/// 从选项池中随机 2(3) 个，由你选择其中 1 个触发。
///
/// 「抽到即触发但不离手」是这张卡的核心：抽到白嫖一次，牌还在手上，
/// 想要第二次就再付 1 费。未升级的【虚无】给这个循环划了个回合末的止损线，
/// 升级换成【保留】之后才真正变成可以反复运作的引擎。
///
/// 选项做成了衍生卡（见 MeteorOptions）——游戏的选择界面都是围绕选卡构建的，
/// 没有「选效果」的通用组件。用 FromChooseACardScreen 从任意一组卡里选一张，
/// 不需要先把它们塞进手牌。
/// </summary>
[Pool(typeof(RegentCardPool))]
public sealed class StreakingMeteor() : Rebalance_by_sts1_revivalCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const int OptionsBase = 2;
    private const int OptionsUpgraded = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    /// <summary>
    /// 「抽到时触发打出效果」。本卡是自己的模组卡，可以直接 override 钩子，不用 Harmony。
    /// CardModel.ShouldReceiveCombatHooks 是 `Pile?.IsCombatPile ?? false`，
    /// 抽到手牌时已在战斗牌堆里，所以能听见自己被抽。
    ///
    /// ⚠️ 这里直接调效果，**不能用 CardCmd.AutoPlay** —— AutoPlay 会真的把卡打出去，
    /// 结算完就进弃牌堆／消耗堆。设计要求这张**留在手牌里**：
    /// 抽到白嫖一次，卡还在手上，之后可以再花 1 费手动触发一次。
    /// 未升级靠【虚无】在回合末烧掉，升级后靠【保留】跨回合留存。
    /// 代价是绕过了打出管线（BeforeCardPlayed/AfterCardPlayed 等钩子不会触发），
    /// 但这本来就不是一次「打出」，语义上正确。
    /// </summary>
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (!ReferenceEquals(card, this)) return;
        await TriggerEffect(choiceContext);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await TriggerEffect(choiceContext);

    /// <summary>抽到时和手动打出时共用同一套逻辑。</summary>
    private async Task TriggerEffect(PlayerChoiceContext choiceContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        int count = IsUpgraded ? OptionsUpgraded : OptionsBase;
        var rng = Owner.RunState.Rng.CombatCardGeneration;

        // 从选项池里不重复地随机取 count 个，实例化成本场战斗的卡。
        var pool = MeteorOptions.All.ToList();
        var offered = new List<CardModel>();
        for (int i = 0; i < count && pool.Count > 0; i++)
        {
            var type = pool[rng.NextInt(pool.Count)];
            pool.Remove(type);

            var canonical = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
            offered.Add(CombatState.CreateCard(canonical, Owner));
        }

        if (offered.Count == 0) return;

        // canSkip:false —— 设计是「选择触发 1 个」，不是可选。
        var chosen = await CardSelectCmd.FromChooseACardScreen(choiceContext, offered, Owner, canSkip: false);
        if (chosen != null) await CardCmd.AutoPlay(choiceContext, chosen, null);
    }

    protected override void OnUpgrade()
    {
        // 升级把【虚无】换成【保留】：不再是「留在手里就烧掉」，而是能攒到想要的时机。
        RemoveKeyword(CardKeyword.Ethereal);
        AddKeyword(CardKeyword.Retain);
    }
}
