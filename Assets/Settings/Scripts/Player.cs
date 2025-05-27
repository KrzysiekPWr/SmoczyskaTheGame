using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public int playerIndex;
    public RectTransform handContainer;
    public List<Card> cardsInHand = new List<Card>();
    
    private GameManager gameManager;
    
    private void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("No GameManager found in scene!");
        }
    }

    public void AddCardToHand(Card card)
    {
        if (card != null)
        {
            cardsInHand.Add(card);
            card.transform.SetParent(handContainer, false);
            RepositionCards();
        }
    }

    public void RemoveCardFromHand(Card card)
    {
        if (cardsInHand.Contains(card))
        {
            cardsInHand.Remove(card);
            RepositionCards();
        }
    }

    private void RepositionCards()
    {
        // Diagnostic logs can be removed or commented out now
        // Debug.Log($"Player {playerIndex} RepositionCards: Starting. Hand count: {cardsInHand.Count}");
        // for(int k=0; k < cardsInHand.Count; ++k) {
        //     if (cardsInHand[k] != null && cardsInHand[k].Data != null) {
        //         Debug.Log($"Player {playerIndex} RepositionCards: Initial state - Card at list index {k} is {cardsInHand[k].Data.cardName} (InstanceID: {cardsInHand[k].GetInstanceID()})");
        //     } else {
        //          Debug.Log($"Player {playerIndex} RepositionCards: Initial state - Card at list index {k} is NULL CARD DATA OR CARD");
        //     }
        // }

        for (int i = 0; i < cardsInHand.Count; i++)
        {
            Card card = cardsInHand[i];
            if (card != null)
            {
                // The GridLayoutGroup will handle visual positioning.
                // We just need to ensure the card's sibling order in the hierarchy
                // matches its order in the cardsInHand list.
                card.transform.SetSiblingIndex(i);

                // Old manual positioning logic (now removed/commented):
                // int row = i / 3;
                // int col = i % 3;
                // Vector3 position;
                // if (playerIndex == 0 || playerIndex == 2)
                // {
                //     position = new Vector3(
                //         col * 120f - 120f, 
                //         -row * 150f, 
                //         0
                //     );
                // }
                // else
                // {
                //     position = new Vector3(
                //         0,
                //         -col * 150f + 150f, 
                //         0
                //     );
                // }
                // if (card.Data != null) {
                //     Debug.Log($"Player {playerIndex} RepositionCards: Processing card '{card.Data.cardName}' (InstanceID: {card.GetInstanceID()}) from list index {i}. Calculated row: {row}, col: {col}. Target localPosition: {position}");
                // } else {
                //     Debug.Log($"Player {playerIndex} RepositionCards: Processing card with NULL DATA (InstanceID: {card.GetInstanceID()}) from list index {i}. Calculated row: {row}, col: {col}. Target localPosition: {position}");
                // }
                // card.transform.localPosition = position;
                // if (card.Data != null) {
                //     Debug.Log($"Player {playerIndex} RepositionCards: Card '{card.Data.cardName}' (InstanceID: {card.GetInstanceID()}) at list index {i} actual localPosition after set: {card.transform.localPosition}");
                // } else {
                //     Debug.Log($"Player {playerIndex} RepositionCards: Card with NULL DATA (InstanceID: {card.GetInstanceID()}) at list index {i} actual localPosition after set: {card.transform.localPosition}");
                // }
            }
        }
    }

    public void RevealAllCards()
    {
        foreach (Card card in cardsInHand)
        {
            if (card != null && !card.IsFaceUp)
            {
                card.SetFaceUp();
            }
        }
    }

    public void UpdateHandLayout()
    {
        RepositionCards();
    }

    public int CalculateScore()
    {
        int score = 0;
        List<int> leftColumnValues = new List<int>();
        List<int> rightColumnValues = new List<int>();
        
        // Separate cards into left column (0, 1, 2) and right column (3, 4, 5)
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            if (i < 3) // Left column
            {
                leftColumnValues.Add(cardsInHand[i].Data.value);
            }
            else // Right column
            {
                rightColumnValues.Add(cardsInHand[i].Data.value);
            }
        }
        
        // Check for pairs in columns (two identical cards in the same column)
        bool leftColumnHasPair = HasPair(leftColumnValues);
        bool rightColumnHasPair = HasPair(rightColumnValues);
        
        // Calculate score based on the game rules
        if (!leftColumnHasPair)
        {
            foreach (int value in leftColumnValues)
            {
                score += value;
            }
        }
        
        if (!rightColumnHasPair)
        {
            foreach (int value in rightColumnValues)
            {
                score += value;
            }
        }
        
        return score;
    }
    
    private bool HasPair(List<int> values)
    {
        for (int i = 0; i < values.Count; i++)
        {
            for (int j = i + 1; j < values.Count; j++)
            {
                if (values[i] == values[j])
                {
                    return true;
                }
            }
        }
        return false;
    }
} 