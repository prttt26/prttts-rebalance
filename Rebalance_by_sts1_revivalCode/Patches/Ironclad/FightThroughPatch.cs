using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Ironclad;

/// <summary>
/// 强撑 (FightThrough) — op: move，机器人罕见 → 战士罕见
///
/// 效果和数值都不用改（1费技能、BlockVar(13) 升级+4、2张伤口进弃牌堆，与设计完全一致），
/// 只是换个池：机器人侧的移除在 CardPoolPatches.Defect，入战士池在 MainFile.Initialize。
///
/// 唯一的麻烦是卡图：`PortraitPath` 用 `Pool.Title` 拼图集路径，而这张卡的图躺在
/// `card_atlas.sprites/defect/` 下，跟着 Pool 走就会找不到。
///
/// ⚠️ 别用「把 Pool 钉回 DefectCardPool」来绕过——那样卡图是对了，但 `Pool` 是**身份**属性，
/// 到处都在读：卡牌总览的筛选就是 `c.Pool is IroncladCardPool`，钉回去这张卡会一直显示在
/// 机器人分类下；`CardFactory.GetDefaultTransformationOptions` 也用它决定转化候选池。
/// 正确做法是让 Pool 正常解析成 Ironclad（移出机器人池后它只在战士池里），
/// 只单独覆盖卡图路径这一项。
///
/// VisualCardPool 也不用管：它默认就等于 Pool，所以边框和能量图标自动变成战士红。
/// </summary>
public static class FightThroughPatch
{
    /// <summary>卡图所在的图集目录，跟着原角色走。</summary>
    private const string ArtPool = "defect";

    private static string ArtPath(CardModel card, string subDir = "")
        => ImageHelper.GetImagePath($"atlases/card_atlas.sprites/{ArtPool}/{subDir}{card.Id.Entry.ToLowerInvariant()}.tres");

    [HarmonyPatch(typeof(CardModel), "PortraitPath", MethodType.Getter)]
    private static class Portrait
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref string __result)
        {
            if (__instance is FightThrough) __result = ArtPath(__instance);
        }
    }

    [HarmonyPatch(typeof(CardModel), "BetaPortraitPath", MethodType.Getter)]
    private static class BetaPortrait
    {
        [HarmonyPostfix]
        private static void Postfix(CardModel __instance, ref string __result)
        {
            if (__instance is FightThrough) __result = ArtPath(__instance, "beta/");
        }
    }
}
