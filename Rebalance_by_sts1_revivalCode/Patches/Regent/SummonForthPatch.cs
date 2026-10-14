using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Patches.Regent;

/// <summary>
/// 征召上前 (SummonForth) — op: modify，储君罕见
///   原效果：1费，铸造 8(11)。不论何处，将君王之剑放入你的手牌
///   新效果：1费，**打出时手牌中每有一张君王之剑，获得 1 点能量**。铸造 7(10)。不论何处，将君王之剑放入你的手牌
///
/// 铸造量小幅下调，换来一个和「手里囤剑」联动的加费——剑越多这张越像仪式性的爆发点。
///
/// ⚠️ 加费**只读打出那一刻的手牌**：先数、先给费，然后才铸造和收剑。
/// 所以本卡铸造时新生成的那把、以及本卡自己收进手牌的剑，都**不**计入。
/// 要拿到费，剑必须在打出之前就已经在手上（君王之剑自带保留，囤得住）。
/// 卡面三句的顺序与结算顺序一致。
/// </summary>
public static class SummonForthPatch
{
    private const int EnergyPerBlade = 1;

    [HarmonyPatch(typeof(SummonForth), "CanonicalVars", MethodType.Getter)]
    private static class Vars
    {
        [HarmonyPostfix]
        private static void Postfix(ref IEnumerable<DynamicVar> __result) =>
            __result = [new ForgeVar(7), new EnergyVar(EnergyPerBlade)];
    }

    [HarmonyPatch(typeof(SummonForth), "OnPlay")]
    private static class Play
    {
        [HarmonyPrefix]
        private static bool Prefix(CardModel __instance, PlayerChoiceContext choiceContext, ref Task __result)
        {
            __result = NewOnPlay(__instance, choiceContext);
            return false;
        }

        private static async Task NewOnPlay(CardModel card, PlayerChoiceContext choiceContext)
        {
            await CreatureCmd.TriggerAnim(card.Owner.Creature, "Cast", card.Owner.Character.CastAnimDelay);

            // 先按打出这一刻的手牌数给费——必须在铸造和收剑之前，那两步都会往手牌里加剑。
            int blades = PileType.Hand.GetPile(card.Owner).Cards.OfType<SovereignBlade>().Count();
            if (blades > 0) await PlayerCmd.GainEnergy(blades * card.DynamicVars.Energy.IntValue, card.Owner);

            // 铸造和收剑的内容与原版相同，只是顺序跟着卡面走（原版先收剑再铸造，两种顺序结果一样）。
            await ForgeCmd.Forge(card.DynamicVars.Forge.IntValue, card.Owner, card);

            var elsewhere = card.Owner.PlayerCombatState.AllCards.OfType<SovereignBlade>()
                .Where(c => c.Pile == null || c.Pile.Type != PileType.Hand);
            await CardPileCmd.Add(elsewhere, PileType.Hand);
        }
    }

    [HarmonyPatch(typeof(ModelDb), "Init")]
    private static class Loc
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            var table = LocManager.Instance.GetTable("cards");
            if (AccessTools.Field(typeof(LocTable), "_translations").GetValue(table)
                is not Dictionary<string, string> translations) return;

            // 沿用原版条目的写法（[gold]、{Forge:diff()}），三句顺序与结算顺序一致。
            string key = ModelDb.Card<SummonForth>().Id.Entry + ".description";
            translations[key] = LocManager.Instance.Language switch
            {
                "zhs" or "zht" or "zh" =>
                    "打出时，你的[gold]手牌[/gold]中每有一张[gold]君王之剑[/gold]，获得{Energy:energyIcons()}。\n[gold]铸造[/gold]{Forge:diff()}。\n不论何处，将[gold]君王之剑[/gold]放入你的[gold]手牌[/gold]。",
                _ =>
                    "When played, gain {Energy:energyIcons()} for each [gold]Sovereign Blade[/gold] in your [gold]Hand[/gold].\n[gold]Forge[/gold] {Forge:diff()}.\nPut [gold]Sovereign Blade[/gold] into your [gold]Hand[/gold] from anywhere.",
            };
        }
    }
}
