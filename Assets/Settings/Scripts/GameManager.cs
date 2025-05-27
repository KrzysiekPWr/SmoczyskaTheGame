using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro; // Added for TextMeshPro support

public enum GameState { Setup, PlayerTurn, GameEnd }

public class GameManager : MonoBehaviour
{
    [Header("Deck Settings")]
    public Deck deck;
    public int cardsPerPlayer = 6;

    [Header("Player Areas")]
    public RectTransform[] playerHands = new RectTransform[4];
    public Vector2 cardSpacing = new Vector2(120f, 150f);
    public bool dealFaceDown = true;

    [Header("Game UI")]
    public Button deckButton;
    public Transform[] discardPiles;
    public Text currentPlayerText;
    public Button endTurnButton;
    public TextMeshProUGUI gameOverText; // Added for Game Over message
    public GameObject gameOverOverlayPanel; // Added for screen dimming
    public Button replayButton; // Added for replay functionality
    
    [Header("UI Score Displays")]
    public TextMeshProUGUI[] playerScoreTexts;
    
    [Header("Card Size Settings")]
    public Vector2 handCardSize = new Vector2(100f, 150f);
    public Vector2 discardCardSize = new Vector2(50f, 68f);
    public float cardDealDelay = 0.2f;
    
    // Array of Player script references
    private Player[] players;
    private List<Card>[] playerCards;
    private int currentPlayerTurn = 0;
    private GameState gameState = GameState.Setup;
    private Card[] discardPileTopCards;
    private Card revealedDeckCard = null;
    private Card cardAwaitingDiscardDecision = null;

    private void Start()
    {
        // Ensure we have the deck and player areas
        if (deck == null)
        {
            deck = FindObjectOfType<Deck>();
            if (deck == null)
            {
                Debug.LogError("No Deck found in the scene!");
                return;
            }
        }
        
        // Initialize player objects
        InitializePlayers();
        
        // Set up UI buttons
        if (deckButton != null)
        {
            deckButton.onClick.AddListener(OnDeckButtonClicked);
        }
        
        if (endTurnButton != null)
        {
            endTurnButton.onClick.AddListener(NextTurn);
            endTurnButton.gameObject.SetActive(false);  // Hide until game starts
        }

        if (replayButton != null)
        {
            replayButton.onClick.AddListener(RestartGame);
            replayButton.gameObject.SetActive(false);  // Hide until game ends
        }
        
        // Initially hide game over text and overlay
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
        if (gameOverOverlayPanel != null)
        {
            gameOverOverlayPanel.SetActive(false);
        }
        
        // Initialize discard pile tracking
        if (discardPiles != null && discardPiles.Length > 0)
        {
            discardPileTopCards = new Card[discardPiles.Length];
        }

        InitializeGame();
        UpdateCardInteractability(); // Initial call
    }
    
    private void InitializePlayers()
    {
        players = new Player[playerHands.Length];
        
        for (int i = 0; i < playerHands.Length; i++)
        {
            // Check if player component exists, otherwise add it
            Player existingPlayer = playerHands[i].GetComponent<Player>();
            if (existingPlayer == null)
            {
                // Create new player component
                Player player = playerHands[i].gameObject.AddComponent<Player>();
                player.playerIndex = i;
                player.handContainer = playerHands[i];
                players[i] = player;
            }
            else
            {
                // Use existing player component
                existingPlayer.playerIndex = i;
                existingPlayer.handContainer = playerHands[i];
                players[i] = existingPlayer;
            }
        }
    }

    private void InitializeGame()
    {
        // Initialize player card lists
        playerCards = new List<Card>[playerHands.Length];
        for (int i = 0; i < playerHands.Length; i++)
        {
            playerCards[i] = new List<Card>();
            
            // Clear player's hand
            players[i].cardsInHand.Clear();
        }

        // Setup the deck
        deck.InitializeDeck();
        deck.Shuffle();
        
        // Hide Game Over text and overlay on restart
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
        if (gameOverOverlayPanel != null)
        {
            gameOverOverlayPanel.SetActive(false);
        }
        
        // Clear any existing cards in player areas
        foreach (Transform hand in playerHands)
        {
            foreach (Transform child in hand)
            {
                if (child.GetComponent<Card>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
        
        // Clear discard piles
        if (discardPiles != null)
        {
            foreach (Transform discardPile in discardPiles)
            {
                foreach (Transform child in discardPile)
                {
                    // Only destroy children that are cards
                    if (child.GetComponent<Card>() != null)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
            
            // Reset discard pile tracking
            for (int i = 0; i < discardPileTopCards.Length; i++)
            {
                discardPileTopCards[i] = null;
            }
        }
        
        // Deal initial cards to players
        StartCoroutine(DealInitialCards());
    }

    private IEnumerator DealInitialCards()
    {
        gameState = GameState.Setup;
        
        // Deal cards with animation delay
        for (int cardIndex = 0; cardIndex < cardsPerPlayer; cardIndex++)
        {
            for (int playerIndex = 0; playerIndex < players.Length; playerIndex++)
            {
                yield return new WaitForSeconds(cardDealDelay);
                DealCardToPlayer(playerIndex, cardIndex);
            }
        }
        
        // Initialize discard piles if needed
        if (discardPiles != null && discardPiles.Length >= 2)
        {
            for (int i = 0; i < 2; i++) // Initialize 2 discard piles
            {
                yield return new WaitForSeconds(cardDealDelay);
                var (discardCard, discardCardData) = deck.DrawCard(discardPiles[i]);
                
                if (discardCard != null && discardCardData != null)
                {
                    // Initialize the discard card (belongs to no player initially, use index -1)
                    discardCard.Initialize(cardData: discardCardData, gameManager: this, playerIndex: -1);
                    
                    discardCard.SetFaceUp();
                    SetCardSize(discardCard, discardCardSize);
                    discardPileTopCards[i] = discardCard;
                    // ConfigureForDiscardPile makes it non-interactable by default. UpdateCardInteractability will manage.
                    discardCard.ConfigureForDiscardPile(); 
                }
            }
        }
        
        // Set current player to Player 1 (index 0)
        currentPlayerTurn = 0;
        UpdateCurrentPlayerUI();
        
        // Start the game
        gameState = GameState.PlayerTurn;
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(true);
        }
        
        // Calculate and display initial scores
        CalculateAndDisplayPlayerScores();

        Debug.Log($"Game initialized. Player {currentPlayerTurn + 1}'s turn.");
        UpdateCardInteractability(); // Call after game state is PlayerTurn
    }

    private void DealCardToPlayer(int playerIndex, int cardPosition)
    {
        if (playerIndex < 0 || playerIndex >= players.Length)
        {
            Debug.LogError($"Invalid player index: {playerIndex}");
            return;
        }

        Player player = players[playerIndex];
        
        // Get card position in grid (2 rows of 3)
        int row = cardPosition / 3;
        int col = cardPosition % 3;
        
        // Different layouts based on player position
        Vector3 position;
        
        // Bottom player (0), Top player (2)
        if (playerIndex == 0 || playerIndex == 2)
        {
            position = new Vector3(
                col * cardSpacing.x - cardSpacing.x, 
                -row * cardSpacing.y, 
                0
            );
        }
        // Left player (1), Right player (3)
        else
        {
            position = new Vector3(
                0,
                -col * cardSpacing.y + cardSpacing.y, 
                0
            );
        }

        // Draw and position card
        var (card, cardData) = deck.DrawCard(player.handContainer);
        
        if (card != null && cardData != null)
        {
            card.MoveTo(player.handContainer, position);
            
            // Set card size
            SetCardSize(card, handCardSize);
            
            // Set face down initially
            if (dealFaceDown)
            {
                card.SetFaceDown();
            }
            else
            {
                card.SetFaceUp();
            }
            
            // Add to player's card list
            player.cardsInHand.Add(card);
            playerCards[playerIndex].Add(card);
            
            // Initialize the card with GameManager and player index
            card.Initialize(cardData: cardData, gameManager: this, playerIndex: playerIndex);
        }
    }
    
    // Set card size based on location
    private void SetCardSize(Card card, Vector2 size)
    {
        if (card == null) return;
        
        // Set the card size directly
        card.fixedCardSize = size;
        
        // Force the card to update its display
        card.SetupCardDisplay();
        
        // Also directly set the RectTransform size for immediate visual update
        RectTransform rectTransform = card.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = size;
        }
    }
    
    private void OnDeckButtonClicked()
    {
        if (gameState != GameState.PlayerTurn) 
            return;

        // Condition: Can only draw if no card is revealed and no card awaits discard
        if (revealedDeckCard != null || cardAwaitingDiscardDecision != null)
        {
            Debug.Log("Cannot draw from deck: An action is pending with a revealed or discard-pending card.");
            return;
        }
            
        Transform deckPanelTransform = (deckButton != null) ? deckButton.transform : transform; // Should be a dedicated reveal panel
        var (drawnCard, drawnCardData) = deck.DrawCard(deckPanelTransform);
        
        if (drawnCard != null && drawnCardData != null)
        {
            drawnCard.Initialize(cardData: drawnCardData, gameManager: this, playerIndex: -1); 
            drawnCard.SetFaceUp();
            drawnCard.transform.localPosition = Vector3.zero; 
            SetCardSize(drawnCard, handCardSize);

            this.revealedDeckCard = drawnCard;
            Debug.Log($"Player {currentPlayerTurn + 1} drew: {drawnCardData.cardName} onto DeckPanel. It is now {this.revealedDeckCard.Data.cardName}.");
            
            UpdateCardInteractability(); // Update interactability after drawing
        }
    }
    
    public void NextTurn()
    {
        if (gameState != GameState.PlayerTurn)
            return;
            
        currentPlayerTurn = (currentPlayerTurn + 1) % players.Length;
        UpdateCurrentPlayerUI();
        Debug.Log($"Player {currentPlayerTurn + 1}'s turn.");

        // Display scores at the end of the previous player's turn (which is the start of the new turn for calculation purposes)
        CalculateAndDisplayPlayerScores();

        // Check for game end condition (e.g., all cards flipped)
        CheckGameEndCondition(); 
        UpdateCardInteractability();
    }
    
    private void UpdateCurrentPlayerUI()
    {
        if (currentPlayerText != null)
        {
            currentPlayerText.text = $"Player {currentPlayerTurn + 1} Turn";
        }
    }
    
    private void CheckGameEndCondition()
    {
        // Check if any player has all cards face up
        foreach (Player player in players)
        {
            bool allCardsRevealed = true;
            foreach (Card card in player.cardsInHand)
            {
                if (!card.IsFaceUp)
                {
                    allCardsRevealed = false;
                    break;
                }
            }
            
            if (allCardsRevealed && player.cardsInHand.Count >= cardsPerPlayer)
            {
                EndGame();
                return;
            }
        }
    }
    
    private void EndGame()
    {
        gameState = GameState.GameEnd;
        Debug.Log("Game Over!");

        // Reveal all players' cards before calculating scores
        if (players != null)
        {
            foreach (Player player in players)
            {
                if (player != null)
                {
                    player.RevealAllCards();
                }
            }
        }

        int winnerIndex = -1;
        int minScore = int.MaxValue;

        if (players != null)
        {
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null || players[i].cardsInHand == null) continue;

                int currentPlayerScore = 0;
                foreach (Card card in players[i].cardsInHand)
                {
                    if (card != null && card.Data != null && card.IsFaceUp)
                    {
                        currentPlayerScore += card.Data.value;
                    }
                }

                if (currentPlayerScore < minScore)
                {
                    minScore = currentPlayerScore;
                    winnerIndex = i;
                }
                // Basic tie-breaking: first player to achieve the lowest score wins.
                // More complex tie-breaking could be added here if needed.
            }
        }

        // Display Game Over message and overlay
        if (gameOverOverlayPanel != null)
        {
            gameOverOverlayPanel.SetActive(true);
        }
        if (gameOverText != null)
        {
            if (winnerIndex != -1)
            {
                gameOverText.text = $"Game Over!\nPlayer {winnerIndex + 1} won";
            }
            else
            {
                gameOverText.text = "Game Over!"; // Fallback if no winner determined
            }
            gameOverText.gameObject.SetActive(true);
        }
        
        // Show replay button
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(true);
        }
        
        // Display final scores for all players
        CalculateAndDisplayPlayerScores(); 

        // Disable end turn button and further interactions as game is over
        if (endTurnButton != null)
        {
            endTurnButton.gameObject.SetActive(false);
        }
    }
    
    // Reset game with a button click
    public void RestartGame()
    {
        // Hide replay button
        if (replayButton != null)
        {
            replayButton.gameObject.SetActive(false);
        }
        
        InitializeGame();
    }

    public bool IsRevealedDeckCard(Card card)
    {
        return revealedDeckCard != null && card == revealedDeckCard;
    }

    public void HandleClickedRevealedDeckCard(Card clickedDeckCard)
    {
        if (gameState != GameState.PlayerTurn || cardAwaitingDiscardDecision != null || revealedDeckCard == null || clickedDeckCard != revealedDeckCard)
        {
            Debug.LogWarning("HandleClickedRevealedDeckCard called inappropriately.");
            return;
        }

        Debug.Log($"Player {currentPlayerTurn + 1} clicked the revealed deck card: {clickedDeckCard.Data.cardName}. Preparing to discard.");
        this.cardAwaitingDiscardDecision = this.revealedDeckCard;
        this.revealedDeckCard = null;
        Debug.Log($"Card {cardAwaitingDiscardDecision.Data.cardName} is now awaiting discard. Please select a discard pile.");
        UpdateCardInteractability(); // Update after revealed card is moved to awaiting discard
    }

    // New method to handle card clicks and manage turns
    public void HandleCardClick(Card clickedCard) // Called when a hand card is clicked (not for a valid swap, and not the revealedDeckCard)
    {
        if (gameState != GameState.PlayerTurn)
        {
            Debug.Log("Cannot interact with cards outside of PlayerTurn state.");
            return;
        }

        if (cardAwaitingDiscardDecision != null)
        {
            // This state means player has already swapped and needs to discard the card from deck panel,
            // OR they clicked the revealedDeckCard and now need to discard it.
            // A click on another card at this point is invalid.
            Debug.Log("A card is awaiting discard. Please select a discard pile before interacting with other cards.");
            return;
        }

        // At this point, Card.OnClick determined it's not a valid swap and not the revealedDeckCard itself.
        // So, clickedCard is likely a player's hand card.
        if (clickedCard.PlayerIndex == currentPlayerTurn) // It's current player's hand card
        {
            if (revealedDeckCard == null)
            {
                // Player clicked a hand card, but no card is revealed from the deck panel.
                // This is the direct flip in hand we want to prevent.
                Debug.Log($"Player {currentPlayerTurn + 1}, action denied: Cannot interact with hand card {clickedCard.Data.cardName}. Must first reveal a card from the deck panel to initiate a swap.");
            }
            else
            {
                // Player clicked a hand card, a card IS revealed from deck panel,
                // but this specific hand card is not a valid target for a swap (CanSwapWithRevealedCard was false).
                Debug.Log($"Player {currentPlayerTurn + 1}, action denied: Hand card {clickedCard.Data.cardName} is not a valid swap target for the revealed deck card {revealedDeckCard.Data.cardName}. Try a different hand card or discard the revealed card.");
            }
        }
        else if (clickedCard.PlayerIndex != -1) // It's another player's card
        {
            Debug.Log($"Cannot interact with Player {clickedCard.PlayerIndex + 1}'s card. It is Player {currentPlayerTurn + 1}'s turn.");
        }
        else // Card has PlayerIndex -1 but is NOT the revealedDeckCard (that case is handled by HandleClickedRevealedDeckCard)
        {
            // This could be a card on a discard pile if they were made clickable, or some other unowned card.
            Debug.Log($"Invalid card click: {clickedCard.Data?.cardName}. This card is not part of your hand or the revealed deck card.");
        }
        // No flip action, no NextTurn() here. Player needs to make a valid move.
    }

    public bool CanSwapWithRevealedCard(Card handCard)
    {
        if (revealedDeckCard == null || gameState != GameState.PlayerTurn || cardAwaitingDiscardDecision != null)
        {
            return false;
        }
        return handCard.PlayerIndex == currentPlayerTurn;
    }

    public void PerformSwap(Card playerHandCardToDeck)
    {
        if (!CanSwapWithRevealedCard(playerHandCardToDeck))
        {
            Debug.LogWarning("Swap conditions not met or invalid card for swap.");
            return;
        }

        Player currentPlayer = players[currentPlayerTurn];
        Card cardFromDeckToHand = this.revealedDeckCard; // This is the card from DeckPanel/DiscardPanel

        Transform deckPanelTransform = cardFromDeckToHand.transform.parent; 
        Transform playerHandContainer = currentPlayer.handContainer;

        // 1. Update Player's cardsInHand list
        int originalIndex = currentPlayer.cardsInHand.IndexOf(playerHandCardToDeck);

        if (originalIndex != -1)
        {
            currentPlayer.cardsInHand.RemoveAt(originalIndex);
            currentPlayer.cardsInHand.Insert(originalIndex, cardFromDeckToHand);
        }
        else
        {
            Debug.LogError($"Critical Error: Card {playerHandCardToDeck.Data.cardName} (Player: {playerHandCardToDeck.PlayerIndex}) was clicked for swap but not found in current player {currentPlayer.playerIndex}'s hand list. Aborting swap.");
            return; 
        }
        
        // 2. Configure and move card from Hand to Deck Panel (playerHandCardToDeck)
        // This card becomes the one awaiting discard, effectively replacing the revealedDeckCard's spot conceptually.
        playerHandCardToDeck.transform.SetParent(deckPanelTransform, false); 
        playerHandCardToDeck.transform.SetAsLastSibling(); 
        playerHandCardToDeck.SetPlayerIndex(-1); 
        SetCardSize(playerHandCardToDeck, handCardSize); 
        playerHandCardToDeck.SetFaceUp(); 
        playerHandCardToDeck.transform.localPosition = Vector3.zero;

        // 3. Configure and move card from Deck Panel to Hand (cardFromDeckToHand)
        cardFromDeckToHand.transform.SetParent(playerHandContainer, false);
        cardFromDeckToHand.SetPlayerIndex(currentPlayerTurn); 
        SetCardSize(cardFromDeckToHand, handCardSize); 
        
        currentPlayer.UpdateHandLayout();

        // 5. The card that moved from hand to the deck panel is now awaiting discard.
        // The original revealedDeckCard is now in the player's hand.
        this.cardAwaitingDiscardDecision = playerHandCardToDeck; 
        this.revealedDeckCard = null; // No card is "revealed on deck panel" anymore.

        Debug.Log($"Player {currentPlayerTurn + 1} swapped. Card from hand {playerHandCardToDeck.Data.cardName} moved to deck panel area. Card {cardFromDeckToHand.Data.cardName} moved to hand. Card {this.cardAwaitingDiscardDecision.Data.cardName} is awaiting discard.");
        
        UpdateCardInteractability(); // Update after swap
    }

    public void SelectDiscardPile(int pileIndex)
    {
        if (cardAwaitingDiscardDecision == null)
        {
            Debug.LogWarning("SelectDiscardPile called, but no card is awaiting discard decision.");
            return;
        }
        if (pileIndex < 0 || discardPiles == null || pileIndex >= discardPiles.Length)
        {
            Debug.LogError($"Invalid pileIndex {pileIndex} for SelectDiscardPile.");
            cardAwaitingDiscardDecision = null; 
            NextTurn(); 
            return;
        }

        Card cardToDiscard = this.cardAwaitingDiscardDecision;
        this.cardAwaitingDiscardDecision = null;

        Debug.Log($"Player {currentPlayerTurn + 1} is discarding {cardToDiscard.Data.cardName} to discard pile {pileIndex + 1}.");

        cardToDiscard.transform.SetParent(discardPiles[pileIndex], false);
        SetCardSize(cardToDiscard, discardCardSize); 
        cardToDiscard.transform.localPosition = Vector3.zero; 
        cardToDiscard.transform.SetAsLastSibling(); 
        cardToDiscard.ConfigureForDiscardPile(); // Makes it non-interactable and sets alpha

        // Play card place sound
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayCardPlace();
        }

        if (discardPileTopCards != null && pileIndex < discardPileTopCards.Length) {
            discardPileTopCards[pileIndex] = cardToDiscard; 
        }

        NextTurn(); // NextTurn will call UpdateCardInteractability
    }

    public bool IsTopDiscardCard(Card card, out int pileIndex)
    {
        pileIndex = -1;
        if (discardPileTopCards == null || card == null) return false;

        for (int i = 0; i < discardPileTopCards.Length; i++)
        {
            if (discardPileTopCards[i] == card)
            {
                pileIndex = i;
                return true;
            }
        }
        return false;
    }

    public void HandleDiscardPileCardSelected(Card selectedCard, int pileIndex)
    {
        if (gameState != GameState.PlayerTurn || revealedDeckCard != null || cardAwaitingDiscardDecision != null)
        {
            Debug.LogWarning("Cannot select card from discard pile: Action already in progress or not player's turn.");
            return;
        }

        if (pileIndex < 0 || pileIndex >= discardPileTopCards.Length || discardPileTopCards[pileIndex] != selectedCard)
        {
            Debug.LogError($"Invalid discard pile selection or card mismatch. Pile: {pileIndex}");
            return;
        }

        Debug.Log($"Player {currentPlayerTurn + 1} selected {selectedCard.Data.cardName} from discard pile {pileIndex + 1}.");

        // Take the card from the discard pile
        // We will reparent it, then update discardPileTopCards based on what's left.
        Transform originalParentPileTransform = discardPiles[pileIndex]; // The transform of the pile card was taken from

        // This card becomes the "revealedDeckCard"
        this.revealedDeckCard = selectedCard;
        
        // Move it to the same location as a card drawn from the deck panel
        Transform revealParent = (deckButton != null) ? deckButton.transform : this.transform; 
        selectedCard.transform.SetParent(revealParent, false); 
        
        // Reset scale and ensure proper RectTransform setup for centering
        selectedCard.transform.localScale = Vector3.one;
        RectTransform cardRect = selectedCard.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            // Assuming card's pivot is already centered (0.5, 0.5) in its prefab settings.
            // If not, uncomment and set: cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
        }
        else // Fallback for non-RectTransform (unlikely for UI card)
        {
            selectedCard.transform.localPosition = Vector3.zero; 
        }

        selectedCard.SetPlayerIndex(-1); 
        SetCardSize(selectedCard, handCardSize); // Ensure card is resized to hand card size
        selectedCard.SetFaceUp(); // Ensure card is face up

        // Now, update the original discard pile from which the card was taken
        // Check if the originalParentPileTransform (which is discardPiles[pileIndex]) has any children left.
        if (originalParentPileTransform.childCount > 0)
        {
            Transform newTopCardTransform = originalParentPileTransform.GetChild(originalParentPileTransform.childCount - 1);
            Card newTopCardComponent = newTopCardTransform.GetComponent<Card>();
            if (newTopCardComponent != null)
            {
                discardPileTopCards[pileIndex] = newTopCardComponent;
                Debug.Log($"New top card on discard pile {pileIndex + 1} is {newTopCardComponent.Data.cardName}");
            }
            else
            {
                discardPileTopCards[pileIndex] = null; // Safety: if the top child isn't a card
                Debug.Log($"Top child object on discard pile {pileIndex + 1} is not a Card component after taking one!");
            }
        }
        else
        {
            discardPileTopCards[pileIndex] = null; // Pile is now empty
            Debug.Log($"Discard pile {pileIndex + 1} is now empty after taking {selectedCard.Data.cardName}.");
        }

        Debug.Log($"Card {revealedDeckCard.Data.cardName} is now the revealed card.");
        UpdateCardInteractability();
    }

    private void CalculateAndDisplayPlayerScores()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
            {
                Debug.LogWarning($"Player {i + 1} is null. Skipping score calculation.");
                if (playerScoreTexts != null && i < playerScoreTexts.Length && playerScoreTexts[i] != null)
                {
                    playerScoreTexts[i].text = $"P{i+1} Score: N/A";
                }
                continue;
            }

            Player currentPlayer = players[i];
            int currentPlayerIndex = i; // Player index (0-3)
            int playerScore = 0;

            if (currentPlayer.cardsInHand == null)
            {
                Debug.LogWarning($"Player {currentPlayerIndex + 1} cardsInHand is null. Skipping score calculation for this player.");
                if (playerScoreTexts != null && currentPlayerIndex < playerScoreTexts.Length && playerScoreTexts[currentPlayerIndex] != null)
                {
                    playerScoreTexts[currentPlayerIndex].text = $"P{currentPlayerIndex+1} Score: N/A";
                }
                continue;
            }

            List<Card> playerCardsInHand = currentPlayer.cardsInHand;
            bool[] isZeroPointCard = new bool[playerCardsInHand.Count]; // Defaults to false

            if (playerCardsInHand.Count == cardsPerPlayer && cardsPerPlayer == 6)
            {
                // Players 1 and 3 (indices 0 and 2) have a layout where cardsInHand[col] and cardsInHand[col+3] are visually vertical.
                // Players 2 and 4 (indices 1 and 3) might have a layout where cardsInHand is effectively column-major filled into the visual 2x3 grid.
                bool isAlternateLayout = (currentPlayerIndex == 1 || currentPlayerIndex == 3);

                if (isAlternateLayout)
                {
                    // Assumed mapping for P2/P4 (indices 1, 3): Visual 2Rx3C grid, filled column by column from playerCardsInHand
                    // Visual Col 0: playerCardsInHand[0] (top), playerCardsInHand[1] (bottom)
                    // Visual Col 1: playerCardsInHand[2] (top), playerCardsInHand[3] (bottom)
                    // Visual Col 2: playerCardsInHand[4] (top), playerCardsInHand[5] (bottom)
                    for (int visualCol = 0; visualCol < 3; visualCol++)
                    {
                        int topCardIndex = visualCol * 2;
                        int bottomCardIndex = visualCol * 2 + 1;

                        Card topCard = playerCardsInHand[topCardIndex];
                        Card bottomCard = playerCardsInHand[bottomCardIndex];

                        if (topCard != null && topCard.Data != null && topCard.IsFaceUp &&
                            bottomCard != null && bottomCard.Data != null && bottomCard.IsFaceUp)
                        {
                            if (topCard.Data.cardName == bottomCard.Data.cardName)
                            {
                                isZeroPointCard[topCardIndex] = true;
                                isZeroPointCard[bottomCardIndex] = true;
                            }
                        }
                    }
                }
                else
                {
                    // Original logic for P1/P3 (indices 0, 2): cardsInHand is row-major for the visual 2Rx3C grid
                    // Visual Col 0: playerCardsInHand[0] (top), playerCardsInHand[3] (bottom)
                    // Visual Col 1: playerCardsInHand[1] (top), playerCardsInHand[4] (bottom)
                    // Visual Col 2: playerCardsInHand[2] (top), playerCardsInHand[5] (bottom)
                    for (int col = 0; col < 3; col++) // Iterate through visual columns 0, 1, 2
                    {
                        Card topCard = playerCardsInHand[col];       // Top card in the visual column
                        Card bottomCard = playerCardsInHand[col + 3]; // Bottom card in the visual column

                        if (topCard != null && topCard.Data != null && topCard.IsFaceUp &&
                            bottomCard != null && bottomCard.Data != null && bottomCard.IsFaceUp)
                        {
                            if (topCard.Data.cardName == bottomCard.Data.cardName)
                            {
                                isZeroPointCard[col] = true;
                                isZeroPointCard[col + 3] = true;
                            }
                        }
                    }
                }
            }
            else if (cardsPerPlayer == 6 && playerCardsInHand.Count != cardsPerPlayer)
            {
                Debug.LogWarning($"Player {currentPlayerIndex + 1} hand count ({playerCardsInHand.Count}) does not match expected cardsPerPlayer ({cardsPerPlayer}). Vertical pair rule skipped; standard scoring will apply.");
            }

            for (int cardIdx = 0; cardIdx < playerCardsInHand.Count; cardIdx++)
            {
                Card card = playerCardsInHand[cardIdx];
                if (card != null && card.Data != null && card.IsFaceUp)
                {
                    if (!isZeroPointCard[cardIdx])
                    {
                        playerScore += card.Data.value;
                    }
                }
            }
            
            if (playerScoreTexts != null && currentPlayerIndex < playerScoreTexts.Length && playerScoreTexts[currentPlayerIndex] != null)
            {
                playerScoreTexts[currentPlayerIndex].text = $"P{currentPlayerIndex+1} Score: {playerScore}";
            }
            else
            {
                Debug.Log($"Player {currentPlayerIndex + 1} Score (UI Text not set): {playerScore}");
            }
        }
    }

    private void UpdateCardInteractability()
    {
        if (players == null || players.Length == 0) return; // Not initialized yet

        bool canPerformInitialAction = gameState == GameState.PlayerTurn && revealedDeckCard == null && cardAwaitingDiscardDecision == null;

        // Deck button
        if (deckButton != null)
        {
            deckButton.interactable = canPerformInitialAction;
        }

        // Discard pile top cards
        if (discardPileTopCards != null)
        {
            for (int i = 0; i < discardPileTopCards.Length; i++)
            {
                if (discardPileTopCards[i] != null)
                {
                    // A card on discard pile is interactable if it's an initial action phase
                    discardPileTopCards[i].SetInteractable(canPerformInitialAction);
                }
            }
        }
        
        // Revealed deck card (from deck or discard pile)
        if (revealedDeckCard != null)
        {
            revealedDeckCard.SetInteractable(true); // Always interactable if it exists, for discarding or initiating swap
        }

        // Player hand cards
        for (int i = 0; i < players.Length; i++)
        {
            bool isCurrentPlayer = i == currentPlayerTurn;
            foreach (Card cardInHand in players[i].cardsInHand)
            {
                if (cardInHand != null)
                {
                    // Hand card is interactable if it's current player's turn AND a card is revealed (for swap)
                    bool canSwapThisCard = isCurrentPlayer && revealedDeckCard != null && CanSwapWithRevealedCard(cardInHand);
                    cardInHand.SetInteractable(canSwapThisCard);
                }
            }
        }
        
        // If a card is awaiting discard, only discard piles (buttons, not cards) should be active.
        // This part is handled by the discard pile UI elements themselves, not card interactability.
        // For example, actual discard pile GameObjects might have Button components.
        // The Card objects on discard piles are managed above.
    }
}