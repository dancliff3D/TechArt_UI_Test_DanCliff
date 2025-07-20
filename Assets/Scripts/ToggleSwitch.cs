using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [Header("Slider setup")] 
    [SerializeField, Range(0, 1f)]
    protected float sliderValue;               // Current slider progress (0 = off, 1 = on)
    public bool CurrentValue { get; private set; }  // Current toggle state (true = on)
    
    private bool _previousValue;               // Used to detect state changes
    private Slider _slider;                    // Reference to the underlying UI Slider

    [Header("Animation")] 
    [SerializeField, Range(0, 1f)] private float animationDuration = 0.5f; // Time for toggle animation
    [SerializeField] private AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1); // Easing curve for slider movement

    private Coroutine _animateSliderCoroutine; // Tracks running animation coroutine

    [Header("Events")] 
    [SerializeField] private UnityEvent onToggleOn;   // Event fired when toggled on
    [SerializeField] private UnityEvent onToggleOff;  // Event fired when toggled off

    [SerializeField] private RectTransform sliderArea;        // Area that slider handle moves across
    [SerializeField] private RectTransform handleRect;        // Handle object that slides
    [SerializeField] private RectTransform ToggleSwitchRect;  // Entire toggle container
    [SerializeField] private RectTransform backgroundImageRect; // Background image rect
    
    [Header("Elements to Recolor")]
    [SerializeField] private Image backgroundImage;   // Background image to recolor
    [SerializeField] private Image handleImage;       // Handle image to recolor
    private Vector2 backgroundSize;                   // Cached background size
    
    [Space]
    [SerializeField] private bool recolorBackground;  // Whether to change background color
    [SerializeField] private bool recolorHandle;      // Whether to change handle color
    
    [Header("Colors")]
    [SerializeField] private Color backgroundColorOff = Color.white;
    [SerializeField] private Color backgroundColorOn = Color.white;
    [Space]
    [SerializeField] private Color handleColorOff = Color.white;
    [SerializeField] private Color handleColorOn = Color.white;
    
    protected virtual void OnValidate()
    {
        // Run in editor when values change
        SetupSliderComponent();
        _slider.value = sliderValue;
        ChangeColors();
        AdjustHandleAndSlider();
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh ToggleSwitch")]
    public void EditorRefresh()
    {
        // Manual refresh in editor
        SetupSliderComponent();
        _slider.value = sliderValue;
        ChangeColors();
        AdjustHandleAndSlider();
        UnityEditor.EditorUtility.SetDirty(this);
    }

    [ContextMenu("Refresh All ToggleSwitches")]
    public void EditorRefreshAll()
    {
        // Refresh every ToggleSwitch in scene
        ToggleSwitch[] toggles = FindObjectsOfType<ToggleSwitch>();
        foreach (var toggle in toggles)
        {
            toggle.SetupSliderComponent();
            toggle._slider.value = toggle.sliderValue;
            toggle.ChangeColors();
            toggle.AdjustHandleAndSlider();
            UnityEditor.EditorUtility.SetDirty(toggle);
        }
        Debug.Log($"Refreshed {toggles.Length} ToggleSwitches");
    }
#endif
    
    protected virtual void Awake()
    {
        SetupSliderComponent();
        ChangeColors();
    }

    private void ChangeColors()
    {
        // Update background color based on sliderValue
        if (recolorBackground && backgroundImage)
            backgroundImage.color = Color.Lerp(backgroundColorOff, backgroundColorOn, sliderValue); 
        
        // Update handle color based on sliderValue
        if (recolorHandle && handleImage)
            handleImage.color = Color.Lerp(handleColorOff, handleColorOn, sliderValue); 
    }

    private void SetupSliderComponent()
    {
        // Grab or validate Slider component
        _slider = GetComponent<Slider>();

        if (_slider == null)
        {
            Debug.Log("No slider found!", this);
            return;
        }

        // Make slider non-interactable, we handle clicks ourselves
        _slider.interactable = false;
        var sliderColors = _slider.colors;
        sliderColors.disabledColor = Color.white;
        _slider.colors = sliderColors;
        _slider.transition = Selectable.Transition.None;
    }

    // Calculates how large the image is displayed, respecting aspect ratio
    Vector2 GetImageDisplaySize(Image image)
    {
        if (image.sprite == null) 
            return image.rectTransform.rect.size;

        float imageAspect = image.sprite.rect.width / image.sprite.rect.height;
        float containerWidth = image.rectTransform.rect.width;
        float containerHeight = image.rectTransform.rect.height;
        float containerAspect = containerWidth / containerHeight;

        if (containerAspect > imageAspect)
        {
            // Container is wider, height matches container
            float width = containerHeight * imageAspect;
            return new Vector2(width, containerHeight);
        }
        else
        {
            // Container is taller, width matches container
            float height = containerWidth / imageAspect;
            return new Vector2(containerWidth, height);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        // Update handle/slider if size changes
        if (!gameObject.activeInHierarchy) return;
        AdjustHandleAndSlider();
    }

    private void AdjustHandleAndSlider()
    {
        backgroundSize = GetImageDisplaySize(backgroundImage);
        
        // Update handle size
        float handleSize = Mathf.Min(backgroundSize.x, backgroundSize.y);
        handleRect.sizeDelta = new Vector2(backgroundSize.y, handleRect.sizeDelta.y);
        
        // Adjust slider area width
        float handleOffset = handleRect.rect.height + 10;
        sliderArea.sizeDelta = new Vector2(backgroundSize.x - backgroundSize.y, sliderArea.sizeDelta.y);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Toggle the state when clicked
        SetStateAndStartAnimation(!CurrentValue);
    }
    
    private void SetStateAndStartAnimation(bool state)
    {
        // Re-adjust size before animating
        AdjustHandleAndSlider();
        
        _previousValue = CurrentValue;
        CurrentValue = state;

        // Fire toggle events if state changed
        if (_previousValue != CurrentValue)
        {
            if (CurrentValue)
                onToggleOn?.Invoke();
            else
                onToggleOff?.Invoke();
        }

        // Stop any ongoing animation
        if (_animateSliderCoroutine != null)
            StopCoroutine(_animateSliderCoroutine);

        // Start sliding animation
        _animateSliderCoroutine = StartCoroutine(AnimateSlider());
    }

    private IEnumerator AnimateSlider()
    {
        float startValue = _slider.value;
        float endValue = CurrentValue ? 1 : 0;
        float time = 0;

        // Animate over time using easing curve
        if (animationDuration > 0)
        {
            while (time < animationDuration)
            {
                time += Time.deltaTime;

                float lerpFactor = slideEase.Evaluate(time / animationDuration);
                _slider.value = sliderValue = Mathf.Lerp(startValue, endValue, lerpFactor);

                ChangeColors();
                    
                yield return null;
            }
        }

        // Ensure final state is reached
        _slider.value = endValue;
    }
}
