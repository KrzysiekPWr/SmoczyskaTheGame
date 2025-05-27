using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class DragonCardType
{
    public int value; // Actual value (-2 to 8)
    public Sprite frontSprite;
    public string displayName; // Custom name for each value
}

public class Deck : MonoBehaviour
{
    [Header("Card Assets")]
    public GameObject cardPrefab;
    public Sprite[] dragonFrontSprites;
    public Sprite[] crowFrontSprites;
    public Sprite cardBackSprite;
    [Header("Dragon Configurations")]
    public List<DragonCardType> dragonCards;

    private List<CardData> _cards = new List<CardData>();
    private int _currentCardIndex = 0;

    private void Start()
    {
        InitializeDeck();
        Shuffle();
    }

    public void InitializeDeck()
    {
        _cards.Clear();
        _currentCardIndex = 0;
        
        // Assign sprites to dragon cards if not already done
        if (dragonCards.Count == 0)
        {
            InitializeDragonCardTypes();
        }
        
        // Initialize all dragon cards
        InitializeDragons();
        
        // Initialize all crow cards (3 types)
        InitializeCrows();
        
        Debug.Log($"Deck initialized with {_cards.Count} cards");
    }

    private void InitializeDragonCardTypes()
    {
        dragonCards = new List<DragonCardType>
        {
            new DragonCardType { value = -2, displayName = "Ancient Dragon" },
            new DragonCardType { value = 0, displayName = "Dragonling" },
            new DragonCardType { value = 1, displayName = "Fire Drake" },
            new DragonCardType { value = 2, displayName = "Frost Wyrm" },
            new DragonCardType { value = 3, displayName = "Volcanic Dragon" },
            new DragonCardType { value = 4, displayName = "Thunder Drake" },
            new DragonCardType { value = 5, displayName = "Shadow Dragon" },
            new DragonCardType { value = 6, displayName = "Golden Wyvern" },
            new DragonCardType { value = 7, displayName = "Emerald Serpent" },
            new DragonCardType { value = 8, displayName = "Celestial Dragon" }
        };

        // Assign sprites to dragon cards
        for (int i = 0; i < dragonCards.Count && i < dragonFrontSprites.Length; i++)
        {
            dragonCards[i].frontSprite = dragonFrontSprites[i];
        }
    }

    private void InitializeDragons()
    {
        // First validate we have enough sprites
        if (dragonFrontSprites.Length < dragonCards.Count)
        {
            Debug.LogError($"Need {dragonCards.Count} dragon sprites, got {dragonFrontSprites.Length}");
            return;
        }

        for (int i = 0; i < dragonCards.Count; i++)
        {
            var dragonType = dragonCards[i];
            
            // Create 4 copies of each dragon card
            for (int j = 0; j < 4; j++)
            {
                _cards.Add(new CardData
                {
                    type = CardType.Dragon,
                    value = dragonType.value,
                    cardName = dragonType.displayName,
                    frontSprite = dragonFrontSprites[i],
                    backSprite = cardBackSprite
                });
            }
        }
    }

    private void InitializeCrows()
    {
        // Crow types with their special abilities
        string[] crowNames = { "Scout Crow", "Thief Crow", "Mirror Crow" };
        int[] crowValues = { 9, 10, 11 }; // Special values for crow cards
        
        if (crowFrontSprites.Length < crowNames.Length)
        {
            Debug.LogError($"Need {crowNames.Length} crow sprites, got {crowFrontSprites.Length}");
            return;
        }

        for (int type = 0; type < crowNames.Length; type++)
        {
            // Create 4 copies of each crow card
            for (int i = 0; i < 4; i++)
            {
                _cards.Add(new CardData
                {
                    type = CardType.Crow,
                    value = crowValues[type],
                    cardName = crowNames[type],
                    frontSprite = crowFrontSprites[type],
                    backSprite = cardBackSprite
                });
            }
        }
    }

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
        _currentCardIndex = 0;
        Debug.Log("Deck shuffled");
    }

    public (Card cardComponent, CardData cardData) DrawCard(Transform parent)
    {
        if (_currentCardIndex >= _cards.Count)
        {
            Debug.LogWarning("Deck is empty!");
            return (null, null);
        }

        CardData data = _cards[_currentCardIndex];
        _currentCardIndex++;
        
        GameObject cardObject = Instantiate(cardPrefab, parent);
        Card card = cardObject.GetComponent<Card>();
        if (card != null)
        {
            // Return both the component and the data
            return (card, data);
        }
        
        Debug.LogError("Card prefab does not have a Card component!");
        return (null, null);
    }

    public int RemainingCards => _cards.Count - _currentCardIndex;
}