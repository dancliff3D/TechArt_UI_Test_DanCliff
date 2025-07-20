using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BackgroundClickCatcher : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private BottomBarController barController; // Reference to the bottom bar controller

    public void OnPointerClick(PointerEventData eventData)
    {
        // When background is clicked, close the bottom bar if assigned
        if (barController != null)
        {
            barController?.CloseExternal();
        }
        else
        {
            Debug.LogWarning("BackgroundClickCatcher: No BottomBarController assigned.");
        }
    }

    private void Awake()
    {
        // Auto-assign BottomBarController if not set in inspector
        if (barController == null)
        {
            barController = FindObjectOfType<BottomBarController>();

#if UNITY_EDITOR
            // Warn in editor if no BottomBarController exists
            if (barController == null)
            {
                Debug.LogWarning("BackgroundClickCatcher: No BottomBarController found in scene.");
            }
#endif
        }
    }
}