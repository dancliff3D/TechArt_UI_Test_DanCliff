using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class PopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupParent;  // Parent GameObject for the popup
    [SerializeField] private RectTransform popupPanel; // The main panel that slides in/out
    [SerializeField] private CanvasGroup canvasGroup;  // Used for enabling/disabling interactivity
    [SerializeField] private Image dimBackground;      // The darkened background overlay
    [SerializeField] private float slideDuration = 0.25f; // Time for slide animations
    
    private Vector2 hiddenPos;     // Position where the panel is fully hidden (off-screen)
    private Vector2 visiblePos;    // Position where the panel is fully visible (centered)
    private Tween currentTween;    // Stores any active panel tween

    void OnEnable()
    {
        // Force a layout update so we have the correct panel height
        Canvas.ForceUpdateCanvases();

        // Set visible position to center (0,0 relative to parent)
        visiblePos = Vector2.zero; 
        // Hidden position is below screen, offset by panel height
        hiddenPos = visiblePos + new Vector2(0, -popupPanel.rect.height);

        // Start in hidden state
        popupPanel.anchoredPosition = hiddenPos;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        
        // Start dim background fully transparent so it can fade in
        dimBackground.color = new Color(dimBackground.color.r, dimBackground.color.g, dimBackground.color.b, 0f);
    }

    public void Show()
    {
        // Cancel any existing tween
        currentTween?.Kill();

        // Reset position and make canvas interactable
        popupPanel.anchoredPosition = hiddenPos;
        canvasGroup.alpha = 1f; 
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Slide panel up into view
        currentTween = popupPanel.DOAnchorPos(visiblePos, slideDuration)
            .SetEase(Ease.OutCubic);

        // Fade in background to 60% opacity
        dimBackground.DOFade(0.6f, slideDuration);
    }
    
    public void Hide()
    {
        // Cancel any active tween
        currentTween?.Kill();

        // Create a sequence: slide panel down and fade background
        DOTween.Sequence()
            .Join(popupPanel.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InCubic))
            .Join(dimBackground.DOFade(0f, slideDuration))
            .OnComplete(() =>
            {
                // Disable interactivity after animation finishes
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            });
    }
}
