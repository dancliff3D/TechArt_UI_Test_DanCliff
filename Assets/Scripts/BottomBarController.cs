using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.Rendering.Universal;
#endif

public class BottomBarController : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Button Parents")] 
    [SerializeField] private Transform buttonParent;           // Parent transform holding main buttons
    [SerializeField] private Transform trackerButtonParent;    // Parent for invisible tracker buttons (for highlight)

    [Header("Button Data")]
    [SerializeField] private List<BottomBarButton> buttons = new();          // List of visual buttons
    [SerializeField] private List<GameObject> lockedButtons = new();         // Buttons that are locked
    [SerializeField] private List<BottomBarButton> trackerButtons = new();   // Tracker buttons for highlight positioning
    
    [Header("Default Button Selection")]
    [SerializeField] private int defaultSelectedIndex = -1;    // Which button is selected by default (-1 = none)
    
    [Header("UI Settings")]
    [SerializeField] float selectedButtonWidthIncrease = 20f;  // Extra width added to selected button
    [SerializeField] private float widthTweenDuration = 0.25f; // Time for width animations
    [SerializeField] private Color selectedColor = Color.blue; // Icon color when selected
    [SerializeField] private Color unselectedColor = Color.gray; // Icon color when unselected
    [SerializeField] private RectTransform highlightTab;       // The highlight tab (visible)
    [SerializeField] private RectTransform highlightTabTracker; // The tracker highlight tab
    
    [Header("Event System")]
    public UIEventManager uiEventManager;                      // Assigned in inspector
    public string eventMethodName;                             // Dropdown method for button events
    
    #endregion
    
    #region Private Fields
    private float normalButtonWidth;     // Standard button width
    private float selectedButtonWidth;   // Button width when selected
    private int currentIndex = -1;       // Currently selected button index
    private bool IsValidIndex(int index) => index >= 0 && index < buttons.Count;
    #endregion

    [System.Serializable]
    public class BottomBarButton
    {
        public RectTransform panel;      // Full button container
        public LayoutElement layoutElement; // Controls flexible width
        public Button button;            // Clickable button
        public Image icon;               // Icon image to recolor
        public Text label;               // Label text
        public string eventMethodName;   // Method to call on click
    }
    
    private void Start()
    {
        InitializeButtons();
    }
    
    private void InitializeButtons()
    {
        // Fill button lists and calculate widths
        PopulateButtonList(buttonParent, buttons);
        PopulateButtonList(trackerButtonParent, trackerButtons, false);
        UpdateButtonWidths();
        
        if (buttons.Count == 0)
        {
            Debug.LogWarning("BottomBarController: No buttons set up. Use Auto-Fill.");
            return;
        }
        
        SetupButtonCallbacks();
        ApplyInitialSelection();
    }

    private void ApplyInitialSelection()
    {
        // Deselect everything if default is invalid
        if (!IsValidIndex(defaultSelectedIndex))
        {
            CloseExternal();
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            bool isSelected = i == defaultSelectedIndex;
            SetButtonVisuals(trackerButtons, i, isSelected, true);
            SetButtonVisuals(buttons, i, isSelected, false);
        }
        
        currentIndex = defaultSelectedIndex;
    }

    private void UpdateButtonWidths()
    {
        // Calculate base width based on count
        if (buttons == null || buttons.Count == 0)
        {
            normalButtonWidth = 0f;
            selectedButtonWidth = 0f;
            return;
        }

        int count = buttons.Count;
        normalButtonWidth = 100f / count;
        selectedButtonWidth = normalButtonWidth + selectedButtonWidthIncrease;
    }
    
    private void SetupButtonCallbacks()
    {
        if (buttons == null || buttons.Count == 0)
        {
            Debug.LogWarning("No buttons available to set up.");
            return;
        }
        
        // Assign click listeners
        for (int i = 0; i < buttons.Count; i++)
        {
            int buttonIndex = i; 
            buttons[i].button.onClick.RemoveAllListeners();
            buttons[i].button.onClick.AddListener(() => OnButtonClicked(buttonIndex));
        }
    }
    
    private void OnButtonClicked(int buttonIndex)
    {
        if (lockedButtons.Contains(buttons[buttonIndex].panel.gameObject))
        {
            Debug.Log($"Button {buttonIndex} is locked");
            return;
        }
        
        if (!IsValidIndex(buttonIndex) || currentIndex == buttonIndex)
            return;

        // Deselect previous
        if (currentIndex != -1)
        {
            SetButtonVisuals(trackerButtons, currentIndex, false, true);
            SetButtonVisuals(buttons, currentIndex, false, false);
        }

        bool wasSelectionEmpty = currentIndex == -1;
        
        // Select new button
        SetButtonVisuals(trackerButtons, buttonIndex, true, true);
        SetButtonVisuals(buttons, buttonIndex, true, false);
        currentIndex = buttonIndex;

        if (wasSelectionEmpty)
        {
            // Snap highlight before animating
            RectTransform target = trackerButtons[buttonIndex].panel;
            highlightTab.position = new Vector3(target.position.x, highlightTab.position.y, highlightTab.position.z);
            AnimateHighlightAppear();
        }

        InvokeButtonMethod(buttonIndex);
    }
    
    public void CloseExternal()
    {
        // Deselect and hide highlight
        if (currentIndex != -1)
        {
            SetButtonVisuals(trackerButtons, currentIndex, false, true);
            SetButtonVisuals(buttons, currentIndex, false, false);
            currentIndex = -1;
            Debug.Log("Button deselected: Background tapped");
        }
        AnimateHighlightDisappear();
    }

    private void AnimateHighlightAppear()
    {
        if (highlightTab == null) return;
        highlightTab.localScale = Vector3.zero;
        highlightTab.gameObject.SetActive(true);
        highlightTab.DOScale(Vector3.one, widthTweenDuration).SetEase(Ease.OutBack);
    }

    private void AnimateHighlightDisappear()
    {
        if (highlightTab == null) return;
        highlightTab.DOScale(Vector3.zero, widthTweenDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() => highlightTab.gameObject.SetActive(false));
    }
    
    private void SetButtonVisuals(List<BottomBarButton> buttonsToUpdate, int buttonIndex, bool isSelected, bool isTracker)
    {
        var button = buttonsToUpdate[buttonIndex];

        // Update icon and label colors
        if (button.icon != null)
            button.icon.color = isSelected ? selectedColor : unselectedColor;
        if (button.label != null)
            button.label.color = isSelected ? Color.white : Color.gray;
        
        float targetWidth = isSelected ? selectedButtonWidth : normalButtonWidth;
        
        if (!isTracker)
        {
            // Tween flexible width
            DOTween.To(
                () => button.layoutElement.flexibleWidth,
                value =>
                {
                    button.layoutElement.flexibleWidth = value;
                    LayoutRebuilder.MarkLayoutForRebuild(button.panel.parent as RectTransform);
                },
                targetWidth,
                widthTweenDuration
            );
        }
        else
        {
            button.layoutElement.flexibleWidth = targetWidth;
            LayoutRebuilder.MarkLayoutForRebuild(button.panel.parent as RectTransform);
        }
        
        // Move highlight if needed
        if (isSelected & isTracker)
            MoveHighlightTab(button.panel, true);
    }
    
    private void MoveHighlightTab(RectTransform targetPanel, bool useTween)
    {
        if (highlightTab == null || targetPanel == null) return;
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
            Canvas.ForceUpdateCanvases(); // Ensure layout is correct
#endif
        
        LayoutRebuilder.ForceRebuildLayoutImmediate(targetPanel.parent as RectTransform);
        
        highlightTab.sizeDelta = new Vector2(targetPanel.sizeDelta.x, highlightTab.sizeDelta.y);
        
        if (useTween)
        {
            highlightTab.DOMoveX(targetPanel.position.x, widthTweenDuration);
        }
        else
        {
            highlightTab.position = new Vector3(targetPanel.position.x, highlightTab.position.y, highlightTab.position.z);
        }
        
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.SceneView.RepaintAll();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
#endif
    }
    
    private void InvokeButtonMethod(int index)
    {
        if (!IsValidIndex(index)) return;
        
        string methodName = buttons[index].eventMethodName;
        if (!string.IsNullOrEmpty(methodName) && uiEventManager != null)
        {
            MethodInfo method = uiEventManager.GetType().GetMethod(methodName);
            if (method != null)
                method.Invoke(uiEventManager, null);
            else
                Debug.LogWarning($"Method '{methodName}' not found on UIEventManager.");
        }
    }

#if UNITY_EDITOR
    public void AutoFillAndSetWidths()
    {
        if (buttonParent == null)
        {
            Debug.LogWarning("Button parent not assigned.");
            return;
        }
        
        PopulateButtonList(buttonParent, buttons);
        PopulateButtonList(trackerButtonParent, trackerButtons, false);
        
        if (buttons.Count == 0)
        {
            Debug.LogWarning("No valid buttons found in buttonParent.");
            return;
        }
        
        // Apply visuals
        for (int i = 0; i < buttons.Count; i++)
        {
            bool isSelected = i == defaultSelectedIndex;
            SetButtonVisuals(trackerButtons, i, isSelected, true);
            SetButtonVisuals(buttons, i, isSelected, false);
        }

        SetupButtonCallbacks();
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
    
    public void ApplyDefaultSelectionInEditor()
    {
        if (Application.isPlaying) return;
        if (buttons == null || buttons.Count == 0) return;

        UpdateButtonWidths();
        for (int i = 0; i < buttons.Count; i++)
        {
            bool isSelected = i == defaultSelectedIndex;
            float targetWidth = isSelected ? selectedButtonWidth : normalButtonWidth;
            var iconColour = isSelected ? selectedColor : unselectedColor;
            var labelColour = isSelected ? Color.white : Color.gray;
            
            if (buttons[i].icon != null)
                buttons[i].icon.color = iconColour;
            if (buttons[i].label != null)
                buttons[i].label.color = labelColour;
            if (buttons[i].layoutElement != null)
                buttons[i].layoutElement.flexibleWidth = targetWidth;
            
            if (i < trackerButtons.Count && trackerButtons[i].layoutElement != null)
                trackerButtons[i].layoutElement.flexibleWidth = targetWidth;
            
            if (buttons[i].panel != null)
                LayoutRebuilder.MarkLayoutForRebuild(buttons[i].panel.parent as RectTransform);
        }
        
        if (defaultSelectedIndex >= 0 && defaultSelectedIndex < trackerButtons.Count)
        {
            highlightTab.localScale = Vector3.one;
            MoveHighlightTab(trackerButtons[defaultSelectedIndex].panel, false);
        }
        else
        {
            highlightTab.localScale = Vector3.zero;
        }

        currentIndex = defaultSelectedIndex;
        EditorUtility.SetDirty(this);
        EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
    
    private void PopulateButtonList(Transform parent, List<BottomBarButton> listToFill, bool syncEventMethods = true)
    {
        Dictionary<GameObject, string> previousEventMethods = null;
        if (syncEventMethods)
        {
            previousEventMethods = new Dictionary<GameObject, string>();
            foreach (var btn in listToFill)
            {
                if (btn.panel != null)
                    previousEventMethods[btn.panel.gameObject] = btn.eventMethodName;
            }
        }
        
        listToFill.Clear();

        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            var layout = child.GetComponent<LayoutElement>();
            var button = child.GetComponent<Button>();
            var image = child.GetComponent<Image>();
            var rectTransform = child.GetComponent<RectTransform>();
            var label = child.GetComponentInChildren<Text>();

            if (layout != null && button != null && image != null && rectTransform != null)
            {
                BottomBarButton newButton = new BottomBarButton
                {
                    panel = rectTransform,
                    layoutElement = layout,
                    button = button,
                    icon = image,
                    label = label
                };
                
                if (syncEventMethods && i < buttons.Count)
                    newButton.eventMethodName = buttons[i].eventMethodName;
                
                if (syncEventMethods && previousEventMethods.TryGetValue(child.gameObject, out var savedMethod))
                    newButton.eventMethodName = savedMethod;

                listToFill.Add(newButton);
            }
        }
    }
#endif
}
