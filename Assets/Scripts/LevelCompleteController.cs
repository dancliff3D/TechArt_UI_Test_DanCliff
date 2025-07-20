using UnityEngine;
using DG.Tweening;

public class LevelCompleteController : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private CanvasGroup levelCompleteCanvas; // Main screen canvas
    [SerializeField] private float fadeDuration = 0.3f;        // Fade in/out time

    [Header("Title")]
    [SerializeField] private RectTransform levelCompleteTitle; // "Level Completed" title
    [SerializeField] private float titlePopDuration = 0.6f;    // Title pop animation speed

    [Header("Star Score Container")]
    [SerializeField] private RectTransform starScoreContainer; // Stars display
    [SerializeField] private float starPopDuration = 0.5f;      // Star pop animation speed
    [SerializeField] private ParticleSystem starBurstVFX;       // VFX when stars appear
    [SerializeField] private float starVFXDelay = 0f;           // Delay before VFX starts

    [Header("Rewards")]
    [SerializeField] private RectTransform rewardsContainer;    // Container for all reward items
    [SerializeField] private float rewardPopDuration = 0.4f;    // Pop animation for each reward
    [SerializeField] private float rewardDelay = 0.2f;          // Time between each reward popping in
    [SerializeField] private float rewardFadeDuration = 0.2f;   // Fade duration for rewards

    [Header("Bottom Buttons")]
    [SerializeField] private RectTransform bottomButtonsContainer; // Bottom button group
    [SerializeField] private float bottomPopDuration = 0.4f;        // Pop speed for bottom buttons

    [Header("Delays")]
    [SerializeField] private float delayBetweenLayers = 0.2f;       // Delay between title > stars > rewards
    [SerializeField] private float bottomButtonExtraDelay = 0.15f;  // Extra delay before showing buttons

    private Tween currentTween;     // For fading out
    private Sequence showSequence;  // Controls the "show" animation chain

    // Cached default scales for resetting animations
    private Vector3 titleDefaultScale;
    private Vector3 starDefaultScale;
    private Vector3[] rewardDefaultScales;
    private Vector3 bottomButtonsDefaultScale;

    private void Awake()
    {
        // Store default scales
        if (levelCompleteTitle != null)
            titleDefaultScale = levelCompleteTitle.localScale;

        if (starScoreContainer != null)
            starDefaultScale = starScoreContainer.localScale;

        if (rewardsContainer != null)
        {
            rewardDefaultScales = new Vector3[rewardsContainer.childCount];
            for (int i = 0; i < rewardsContainer.childCount; i++)
                rewardDefaultScales[i] = rewardsContainer.GetChild(i).localScale;
        }

        if (bottomButtonsContainer != null)
            bottomButtonsDefaultScale = bottomButtonsContainer.localScale;

        // Hide the screen on start
        SetCanvasVisible(false, true);
        ResetElements();
    }

    public void ShowLevelComplete()
    {
        // Stop any previous tweens
        currentTween?.Kill();
        showSequence?.Kill();

        showSequence = DOTween.Sequence();

        // Fade canvas in
        levelCompleteCanvas.alpha = 0;
        levelCompleteCanvas.interactable = true;
        levelCompleteCanvas.blocksRaycasts = true;
        showSequence.Append(levelCompleteCanvas.DOFade(1f, fadeDuration));

        // Title pop
        if (levelCompleteTitle != null)
        {
            levelCompleteTitle.localScale = Vector3.zero;
            showSequence.Append(levelCompleteTitle.DOScale(titleDefaultScale, titlePopDuration)
                .SetEase(Ease.OutBack));
        }

        // Stars pop + VFX
        if (starScoreContainer != null)
        {
            starScoreContainer.localScale = Vector3.zero;
            showSequence.AppendInterval(delayBetweenLayers);
            showSequence.AppendCallback(() =>
            {
                if (starBurstVFX != null)
                {
                    starBurstVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    DOVirtual.DelayedCall(starVFXDelay, () => starBurstVFX.Play());
                }
            });
            showSequence.Join(
                starScoreContainer.DOScale(starDefaultScale, starPopDuration)
                    .SetEase(Ease.OutBack)
            );
        }

        // Rewards pop one by one
        if (rewardsContainer != null)
        {
            for (int i = 0; i < rewardsContainer.childCount; i++)
            {
                int index = i;
                RectTransform reward = rewardsContainer.GetChild(index) as RectTransform;
                reward.localScale = Vector3.zero;

                CanvasGroup cg = reward.GetComponent<CanvasGroup>();
                if (cg == null) cg = reward.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0;

                showSequence.AppendInterval(rewardDelay);
                showSequence.AppendCallback(() =>
                {
                    cg.DOFade(1f, rewardFadeDuration);
                    reward.DOScale(rewardDefaultScales[index], rewardPopDuration)
                        .SetEase(Ease.OutBack);
                });
            }
        }

        // Bottom buttons
        if (bottomButtonsContainer != null)
        {
            bottomButtonsContainer.localScale = Vector3.zero;
            showSequence.AppendInterval(delayBetweenLayers + bottomButtonExtraDelay);
            showSequence.Append(bottomButtonsContainer.DOScale(bottomButtonsDefaultScale, bottomPopDuration)
                .SetEase(Ease.OutBack));
        }
    }

    public void HideLevelComplete()
    {
        // Stop animations
        currentTween?.Kill();
        showSequence?.Kill();

        // Fade out then reset
        currentTween = levelCompleteCanvas.DOFade(0f, fadeDuration)
            .OnComplete(() =>
            {
                levelCompleteCanvas.interactable = false;
                levelCompleteCanvas.blocksRaycasts = false;
                ResetElements();
            });

        // Stop any running VFX
        if (starBurstVFX != null)
            starBurstVFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // Quick helper to set canvas state
    private void SetCanvasVisible(bool visible, bool instant = false)
    {
        levelCompleteCanvas.alpha = visible ? 1 : 0;
        levelCompleteCanvas.interactable = visible;
        levelCompleteCanvas.blocksRaycasts = visible;
    }

    // Reset all elements to their hidden states
    private void ResetElements()
    {
        if (levelCompleteTitle != null)
            levelCompleteTitle.localScale = Vector3.zero;

        if (starScoreContainer != null)
            starScoreContainer.localScale = Vector3.zero;

        if (rewardsContainer != null)
        {
            for (int i = 0; i < rewardsContainer.childCount; i++)
            {
                Transform child = rewardsContainer.GetChild(i);
                child.localScale = Vector3.zero;

                CanvasGroup cg = child.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 0;
            }
        }

        if (bottomButtonsContainer != null)
            bottomButtonsContainer.localScale = Vector3.zero;
    }
}
