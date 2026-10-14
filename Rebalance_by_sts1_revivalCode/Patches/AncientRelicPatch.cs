using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Defect;
using Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Ironclad;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches;

/// <summary>
/// 两件先古遗物的适配。它们都和「每个角色池有哪些先古卡」强耦合，我们动了先古卡就必须跟着改。
///
/// 原版每个角色池正好 2 张先古卡：
///   战士 Break / Corruption，猎人 Suppress / WraithForm，机器人 Quadcast / BiasedCognition
/// 古老牙齿吃掉其中一张，尘封魔典从剩下的里挑。
/// </summary>
public static class AncientRelicPatch
{
    /// <summary>
    /// 古老牙齿 (ArchaicTooth)：把角色的招牌初始卡转化成一张特定先古卡。
    ///
    /// 映射表是**硬编码**的，其中两个目标已被本 mod 移出卡池：
    ///   Bash → Break（破击已移除）        ⇒ 改为 Reaper（死亡收割）
    ///   Dualcast → Quadcast（四重释放已移除）⇒ 改为 BootstrapLoop（自举循环）
    /// 不改的话遗物会把删掉的卡重新塞回牌组，等于移除白做了。
    ///
    /// 猎人/储君那几条（Neutralize→Suppress、Unleash→Protector、FallingStar→MeteorShower）
    /// 目标都没动过，保持原样。
    ///
    /// 机器人的替代卡启用的是自举循环；备用选项「递归矩阵」未进卡池，存档在 Shelved/RecursiveMatrix/。
    /// </summary>
    [HarmonyPatch(typeof(ArchaicTooth), "TranscendenceUpgrades", MethodType.Getter)]
    private static class Transcendence
    {
        [HarmonyPostfix]
        private static void Postfix(ref System.Collections.Generic.Dictionary<ModelId, CardModel> __result)
        {
            __result[ModelDb.Card<Bash>().Id] = ModelDb.Card<Reaper>();
            __result[ModelDb.Card<Dualcast>().Id] = ModelDb.Card<BootstrapLoop>();
        }
    }

    /// <summary>
    /// 尘封魔典 (DustyTome)：加一张先古卡进牌组。
    ///
    /// 它的候选是 `角色卡池的先古卡 - 古老牙齿的目标`，读的就是我们改过的卡池，
    /// 所以新先古卡会自动进候选、被删的自动出局——这部分不用管。
    ///
    /// 要改的是**类型**：设计文档写「必须是能力卡」，但原版代码里**没有任何类型过滤**。
    /// 之所以原版总是给能力卡，纯粹是因为每池只剩一张候选而它恰好是能力卡
    /// （Corruption / WraithForm / BiasedCognition 都是 Power）。
    /// 我们给战士加了死亡收割（攻击）、给机器人加了自举循环（技能），
    /// 这个巧合就不成立了，所以这里把「能力卡」显式补成硬条件。
    ///
    /// 补过之后各池仍有候选，不会出现空集：战士 Corruption；猎人 WraithForm。
    ///
    /// 机器人是例外：它现在有**两张**先古能力卡（BiasedCognition 偏差认知 + CoreSurge 核心电泳），
    /// 随机二选一会让这件遗物在机器人身上变得不确定，所以按设计**硬编码给核心电泳**。
    /// 其他角色仍走「能力卡 + 非古老牙齿目标」的随机筛选。
    /// </summary>
    [HarmonyPatch(typeof(DustyTome), "SetupForPlayer")]
    private static class TomeCandidates
    {
        private static readonly System.Reflection.PropertyInfo AncientCardProp =
            AccessTools.Property(typeof(DustyTome), "AncientCard");

        [HarmonyPrefix]
        private static bool Prefix(RelicModel __instance, Player player)
        {
            // 用卡池判角色，而不是 `player.Character is Defect`：
            // 本 mod 的 Cards.Defect 命名空间会和角色类 Characters.Defect 撞名。
            if (player.Character.CardPool is DefectCardPool)
            {
                AncientCardProp.SetValue(__instance, ModelDb.Card<CoreSurge>().Id);
                return false;
            }

            var candidates = player.Character.CardPool
                .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity == CardRarity.Ancient
                            && c.Type == CardType.Power
                            && !ArchaicTooth.TranscendenceCards.Contains(c))
                .ToList();

            if (candidates.Count == 0) return true; // 兜底：真没有就走原版逻辑，别让遗物拿不到卡

            AncientCardProp.SetValue(__instance, player.PlayerRng.Rewards.NextItem(candidates).Id);
            return false;
        }
    }
}
