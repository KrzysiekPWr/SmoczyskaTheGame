using UnityEngine;
using UnityEngine.UI;

// This script helps you set up a Card prefab with all required components
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class CardPrefab : MonoBehaviour
{
    [Header("Card Components")]
    public Image cardImage;
    public Button cardButton;
    public CanvasGroup canvasGroup;
    
    // Set in the prefab
    public float width = 50f;
    public float height = 68f;
    
    private void Reset()
    {
        // This method is called when the script is added to a GameObject in the Editor
        SetupCardComponents();
    }
    
    private void Awake()
    {
        // Make sure components are initialized
        SetupCardComponents();
    }
    
    private void SetupCardComponents()
    {
        // Get or add required components
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (cardImage == null) cardImage = GetComponent<Image>();
        if (cardButton == null) cardButton = GetComponent<Button>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        
        // Set default size
        rectTransform.sizeDelta = new Vector2(width, height);
        
        // Setup Card component references
        Card cardComponent = GetComponent<Card>();
        if (cardComponent == null)
        {
            cardComponent = gameObject.AddComponent<Card>();
        }
        
        // Configure references for card component using reflection
        // Unity's SerializeField doesn't allow public setting, so we use reflection as a workaround
        System.Type cardType = cardComponent.GetType();
        System.Reflection.FieldInfo imageField = cardType.GetField("cardImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo buttonField = cardType.GetField("cardButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        System.Reflection.FieldInfo canvasGroupField = cardType.GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (imageField != null) imageField.SetValue(cardComponent, cardImage);
        if (buttonField != null) buttonField.SetValue(cardComponent, cardButton);
        if (canvasGroupField != null) canvasGroupField.SetValue(cardComponent, canvasGroup);
    }
    
#if UNITY_EDITOR
    // This helps create a Card prefab from the Unity Editor menu
    [UnityEditor.MenuItem("GameObject/UI/Card Prefab")]
    public static void CreateCardPrefab()
    {
        GameObject cardObject = new GameObject("CardPrefab", typeof(CardPrefab));
        UnityEditor.Selection.activeGameObject = cardObject;
        
        // Parent to canvas if one exists in scene
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            cardObject.transform.SetParent(canvas.transform, false);
        }
    }
#endif
} 