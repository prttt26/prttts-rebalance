using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;

namespace Rebalance_by_sts1_revival.Rebalance_by_sts1_revivalCode.Cards.Colorless;

/// <summary>
/// Shared body of 结茧 / 羽化: put N randomly generated cards of one type into the draw pile,
/// free for the rest of the combat.
///
/// Lifted from the base game's Metamorphosis so the generation obeys the same rules — cards come
/// from the owner's own character pool filtered by unlock/multiplayer state, and draw from the
/// dedicated CombatCardGeneration RNG stream so runs stay reproducible and multiplayer stays in sync.
/// </summary>
internal static class FreeCardGenerator
{
    public static async Task AddFreeCardsToDrawPile(CardModel source, CardType type, int count)
    {
        var player = source.Owner;

        var candidates = player.Character.CardPool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(c => c.Type == type);

        var generated = CardFactory.GetForCombat(player, candidates, count, player.RunState.Rng.CombatCardGeneration);

        foreach (var card in generated)
        {
            card.SetToFreeThisCombat();
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, player, CardPilePosition.Random));
        }
    }
}
