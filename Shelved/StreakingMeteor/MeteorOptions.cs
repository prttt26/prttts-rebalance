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

using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Regent;

/// <summary>
/// 「划空流星」的选项衍生卡。
///
/// 设计要求「在 N 个效果中随机若干个，选择触发 1 个」，但游戏里的选择界面全部是围绕**选卡**
/// 构建的，没有「选效果」这种通用组件（储君的 DecisionsDecisions 看着像，实际也是选卡）。
/// 所以把每个选项做成一张 0 费衍生卡，用 CardSelectCmd.FromChooseACardScreen 让玩家选一张，
/// 再 CardCmd.AutoPlay 打出去。
///
/// 这些卡登记在 TokenCardPool（衍生卡专属池，游戏自己的 灵魂/燃料/君王之剑 也在里面）。
/// ⚠️ 不能省掉 [Pool]：BaseLib 的 CustomCardModel 构造时会自动登记，而登记**强制要求**
/// PoolAttribute，否则 ModelDb 初始化直接抛异常、游戏起不来。
///
/// 「预见」（MeteorForesight）是本 mod 新加的机制——塔2 原本没有等价物
/// （Prophesize 只是抽6张）。实现见该类的说明。
/// </summary>
internal static class MeteorOptions
{
    /// <summary>划空流星的随机池。加新选项只需在这里补一个类型。</summary>
    public static readonly Type[] All =
    [
        typeof(MeteorEnergy),
        typeof(MeteorStars),
        typeof(MeteorDraw),
        typeof(MeteorCycle),
        typeof(MeteorExhaust),
        typeof(MeteorForge),
        typeof(MeteorPlating),
        typeof(MeteorBlur),
        typeof(MeteorForesight),
    ];
}

/// <summary>选项：获得 1 点能量。</summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorEnergy() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [EnergyHoverTip];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);
}

/// <summary>选项：获得 2 辉星。</summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorStars() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new StarsVar(2)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PlayerCmd.GainStars(DynamicVars["Stars"].BaseValue, Owner);
}

/// <summary>选项：抽 1 张牌。</summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorDraw() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
}

/// <summary>选项：抽 2 弃 2。</summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorCycle() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    private const int Count = 2;
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, Count, Owner);

        // 抽完再弃，按抽后的手牌选——手牌不足时 selector 会自己少给。
        var chosen = await CardSelectCmd.FromHandForDiscard(
            choiceContext, Owner, new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, Count), null, this);
        if (chosen.Any()) await CardCmd.Discard(choiceContext, chosen);
    }
}

/// <summary>选项：选择 0-1 张手牌消耗。</summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorExhaust() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = (await CardSelectCmd.FromHand(
            context: choiceContext, player: Owner,
            prefs: new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, 1),
            filter: null, source: this)).FirstOrDefault();

        if (target != null) await CardCmd.Exhaust(choiceContext, target);
    }
}

/// <summary>选项：铸造 5。</summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorForge() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new ForgeVar(5)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => HoverTipFactory.FromForge();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await ForgeCmd.Forge(DynamicVars.Forge.IntValue, Owner, this);
}

/// <summary>选项：获得 2 层覆甲。</summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorPlating() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<PlatingPower>(2m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<PlatingPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PowerCmd.Apply<PlatingPower>(
            choiceContext, Owner.Creature, DynamicVars["PlatingPower"].BaseValue, Owner.Creature, this);
}

/// <summary>
/// 选项：本回合保留护甲。
/// 用基础游戏的残影 BlurPower：它的 ShouldClearBlock 对自己返回 false（护甲不清），
/// 并在每个回合开始 Decrement，所以 1 层正好等于「保留一个回合」。
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorBlur() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<BlurPower>(1m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<BlurPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        => await PowerCmd.Apply<BlurPower>(
            choiceContext, Owner.Creature, DynamicVars["BlurPower"].BaseValue, Owner.Creature, this);
}

/// <summary>
/// 选项：预见 3。查看抽牌堆顶的 3 张牌，可以弃掉其中任意张（含 0 张），其余保持原序留在顶部。
///
/// 塔2 没有「预见」机制，这是本 mod 新增的。实现要点：
///   - 抽牌堆的**顶**是 `Cards.First()`（CardPileCmd.Draw 就是从这端取的）
///   - 用 FromSimpleGrid 从任意一组卡里多选；CardSelectorPrefs 的 (min,max) 构造
///     允许选 0..N 张——单参数构造是 min==max，会强制必选，与「任意张」不符
///   - 没被选中的牌不用动：Discard 只把选中的移走，其余自然保持原顺序
/// </summary>
[Pool(typeof(TokenCardPool))]
public sealed class MeteorForesight() : Rebalance_by_sts1_revivalCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    private const int LookAt = 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(LookAt)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var top = PileType.Draw.GetPile(Owner).Cards.Take(DynamicVars.Cards.IntValue).ToList();
        if (top.Count == 0) return;

        var prefs = new CardSelectorPrefs(ForesightPrompt, 0, top.Count);
        var toDiscard = await CardSelectCmd.FromSimpleGrid(choiceContext, top, Owner, prefs);

        if (toDiscard.Any()) await CardCmd.Discard(choiceContext, toDiscard);
    }

    /// <summary>提示语走本卡自己的 loc 条目（在 cards.json 里补 .selectionScreenPrompt）。</summary>
    private LocString ForesightPrompt =>
        (LocString)HarmonyLib.AccessTools.Property(typeof(CardModel), "SelectionScreenPrompt").GetValue(this)!;
}
