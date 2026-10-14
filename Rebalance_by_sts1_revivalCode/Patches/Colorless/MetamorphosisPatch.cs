using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Colorless;

/// <summary>
/// 羽化 (Metamorphosis) — 提升为无色稀有卡
///
/// 塔2 里这张卡已经存在，是**事件卡**，效果和数值与设计要求完全一致
/// （2费技能、消耗、CardsVar(3)、升级+2、向抽牌堆塞攻击牌并 SetToFreeThisCombat）。
/// 所以不新写一张，而是把现成这张改成无色稀有 + 塞进无色池。
///
/// **事件那边不用动**：事件给的还是 ModelDb.Card&lt;Metamorphosis&gt;() 这同一张卡，
/// 只是它现在的稀有度是稀有、并且也能从无色卡奖励里拿到。
/// 这样全局只有一张羽化，不会出现「事件版 + 无色版」两张并存。
///
/// 入池在 MainFile.Initialize 里用 ModHelper.AddModelToPool 完成——
/// BaseLib 的 [Pool] 特性只能标在自己的卡类上，改不了基础游戏的卡。
/// </summary>
public static class MetamorphosisPatch
{
    /// <summary>
    /// 注意 patch 的是 CardModel 而不是 Metamorphosis：Rarity 只**声明**在 CardModel 上
    /// （`public virtual CardRarity Rarity { get; }`），Metamorphosis 并没有 override 它。
    /// 指向派生类会抛 "Undefined target method"，并且**整个 PatchAll 会中断**，
    /// 后面的 patch 类和 Initialize 里的后续代码全部静默失效。
    /// </summary>
    [HarmonyPatch(typeof(CardModel), "Rarity", MethodType.Getter)]
    private static class Rarity
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref CardRarity __result)
        {
            if (__instance is Metamorphosis) __result = CardRarity.Rare;
        }
    }

    /// <summary>
    /// 卡现在同时属于事件池和无色池，而 CardModel.Pool 的默认实现是
    /// 「扫 ModelDb.AllCardPools 取第一个包含此卡的池」并缓存结果——顺序不受我们控制。
    /// 必须钉死，否则卡图会时好时坏：PortraitPath 是
    /// `atlases/card_atlas.sprites/{Pool.Title}/metamorphosis.tres`，
    /// 基础卡图在 "event" 目录下，一旦解析成 colorless 就找不到图。
    ///
    /// 钉在 EventCardPool 没有视觉代价：它和 ColorlessCardPool 同样是
    /// card_frame_colorless + colorless 能量图标 + IsColorless，只有 Title 不同。
    /// </summary>
    [HarmonyPatch(typeof(CardModel), "Pool", MethodType.Getter)]
    private static class Pool
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref CardPoolModel __result)
        {
            if (__instance is Metamorphosis) __result = ModelDb.CardPool<EventCardPool>();
        }
    }
}
