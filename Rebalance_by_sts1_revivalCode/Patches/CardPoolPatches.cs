using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches;

/// <summary>
/// op: remove —— 把基础游戏卡移出卡池。
///
/// CardPoolModel.AllCards 的构造顺序是
///     _allCards = GenerateAllCards();                              // 基础游戏卡
///     _allCards = ModHelper.ConcatModelsFromMods(this, _allCards);  // mod 卡追加在后
/// 然后缓存。所以 postfix GenerateAllCards 每个池只跑一次，且在 mod 卡追加**之前**——
/// 既不会重复执行，也不会误删别的 mod 加的卡。
/// </summary>
public static class CardPoolPatches
{
    [HarmonyPatch(typeof(IroncladCardPool), "GenerateAllCards")]
    private static class Ironclad
    {
        // cards.yaml 中战士池 op: remove 的条目。
        // 先古卡也在角色池里（Break 的 Rarity 就是 CardRarity.Ancient），所以一并在这里删。
        private static readonly HashSet<Type> Removed =
        [
            typeof(Cinder),   // 余烬
            typeof(Stomp),    // 踩踏
            typeof(Bully),    // 欺凌
            typeof(EvilEye),  // 邪眼
            typeof(Break),    // 破击（先古）
        ];

        [HarmonyPostfix]
        private static void Postfix(ref CardModel[] __result) => Drop(__result: ref __result, removed: Removed, pool: "Ironclad");
    }

    [HarmonyPatch(typeof(SilentCardPool), "GenerateAllCards")]
    private static class Silent
    {
        private static readonly HashSet<Type> Removed =
        [
            typeof(MasterPlanner), // 谋划专家
            typeof(DodgeAndRoll),  // 闪躲翻滚
        ];

        [HarmonyPostfix]
        private static void Postfix(ref CardModel[] __result) => Drop(__result: ref __result, removed: Removed, pool: "Silent");
    }

    [HarmonyPatch(typeof(RegentCardPool), "GenerateAllCards")]
    private static class Regent
    {
        private static readonly HashSet<Type> Removed =
        [
            typeof(BundleOfJoy),   // 新生之喜
            typeof(KnockoutBlow),  // 决胜一击
        ];

        [HarmonyPostfix]
        private static void Postfix(ref CardModel[] __result) => Drop(__result: ref __result, removed: Removed, pool: "Regent");
    }

    [HarmonyPatch(typeof(NecrobinderCardPool), "GenerateAllCards")]
    private static class Necrobinder
    {
        private static readonly HashSet<Type> Removed =
        [
            typeof(Deathbringer), // 死亡使者
            typeof(Afterlife),    // 来生
        ];

        [HarmonyPostfix]
        private static void Postfix(ref CardModel[] __result) => Drop(__result: ref __result, removed: Removed, pool: "Necrobinder");
    }

    [HarmonyPatch(typeof(DefectCardPool), "GenerateAllCards")]
    private static class Defect
    {
        private static readonly HashSet<Type> Removed =
        [
            // 强撑：move 到战士池（cards.yaml op: move），机器人这侧对应移除。
            // 入战士池在 MainFile.Initialize，跨池带来的卡图/边框问题见 FightThroughPatch。
            typeof(FightThrough),

            typeof(Hailstorm),       // 寒冰风暴
            typeof(Null),            // 空值
            typeof(ConsumingShadow), // 吞噬暗影
            typeof(Quadcast),        // 四重释放（先古）
        ];

        [HarmonyPostfix]
        private static void Postfix(ref CardModel[] __result) => Drop(__result: ref __result, removed: Removed, pool: "Defect");
    }

    private static void Drop(ref CardModel[] __result, HashSet<Type> removed, string pool)
    {
        int before = __result.Length;
        __result = __result.Where(c => !removed.Contains(c.GetType())).ToArray();

        int dropped = before - __result.Length;
        if (dropped != removed.Count)
        {
            // A rename or a card moving pools between game versions would silently no-op otherwise.
            MainFile.Logger.Info($"{pool} pool: expected to remove {removed.Count} cards but removed {dropped} — check class names");
        }
        MainFile.Logger.Info($"{pool} pool: {before} -> {__result.Length} cards");
    }
}
