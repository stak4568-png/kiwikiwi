using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 튜토리얼 UI
/// </summary>
public class TutorialUI : MonoBehaviour
{
    [Header("메인 패널")]
    public GameObject tutorialPanel;
    public CanvasGroup canvasGroup;
    public Image backgroundOverlay;

    [Header("메시지 박스")]
    public RectTransform messageBox;
    public TMP_Text titleText;
    public TMP_Text messageText;
    public Image speakerImage;
    public TMP_Text speakerNameText;

    [Header("진행 표시")]
    public GameObject progressIndicator;
    public TMP_Text progressText;
    public Slider progressBar;
    public TMP_Text clickToContinueText;

    [Header("버튼")]
    public Button skipButton;
    public Button nextButton;

    [Header("화살표")]
    public GameObject arrowIndicator;
    public RectTransform arrowTransform;

    [Header("애니메이션")]
    public float fadeInDuration = 0.3f;
    public float textTypeSpeed = 0.02f;

    private Coroutine _typewriterCoroutine;
    private bool _isTyping = false;
    private string _fullMessage;

    void Start()
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipClicked);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
    }

    /// <summary>
    /// 튜토리얼 단계 표시
    /// </summary>
    public void ShowStep(TutorialStep step)
    {
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        // 배경 오버레이
        if (backgroundOverlay != null)
            backgroundOverlay.gameObject.SetActive(step.blockInput);

        // 제목
        if (titleText != null)
        {
            titleText.text = step.title ?? "";
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(step.title));
        }

        // 화자
        if (speakerImage != null)
        {
            if (step.speakerPortrait != null)
            {
                speakerImage.sprite = step.speakerPortrait;
                speakerImage.gameObject.SetActive(true);
            }
            else
            {
                speakerImage.gameObject.SetActive(false);
            }
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = step.speakerName ?? "";
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(step.speakerName));
        }

        // 메시지 (타이핑 효과)
        _fullMessage = step.message;
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);
        _typewriterCoroutine = StartCoroutine(TypeMessage(step.message));

        // 위치 조정
        PositionMessageBox(step.popupPosition, step.customOffset);

        // 화살표
        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(step.showArrow);
        }

        // 진행 표시
        UpdateProgress();

        // "클릭하여 계속" 표시
        if (clickToContinueText != null)
        {
            clickToContinueText.gameObject.SetActive(step.progressCondition == TutorialProgressCondition.ClickAnywhere);
        }
    }

    void PositionMessageBox(TutorialPopupPosition position, Vector2 offset)
    {
        if (messageBox == null) return;

        Vector2 anchorPos = position switch
        {
            TutorialPopupPosition.Top => new Vector2(0.5f, 0.85f),
            TutorialPopupPosition.Bottom => new Vector2(0.5f, 0.15f),
            TutorialPopupPosition.Left => new Vector2(0.2f, 0.5f),
            TutorialPopupPosition.Right => new Vector2(0.8f, 0.5f),
            TutorialPopupPosition.TopLeft => new Vector2(0.2f, 0.85f),
            TutorialPopupPosition.TopRight => new Vector2(0.8f, 0.85f),
            TutorialPopupPosition.BottomLeft => new Vector2(0.2f, 0.15f),
            TutorialPopupPosition.BottomRight => new Vector2(0.8f, 0.15f),
            _ => new Vector2(0.5f, 0.5f)
        };

        messageBox.anchorMin = anchorPos;
        messageBox.anchorMax = anchorPos;
        messageBox.anchoredPosition = offset;
    }

    IEnumerator TypeMessage(string message)
    {
        _isTyping = true;

        if (messageText != null)
        {
            messageText.text = "";

            foreach (char c in message)
            {
                if (!_isTyping) break;

                messageText.text += c;
                yield return new WaitForSeconds(textTypeSpeed);
            }

            messageText.text = message;
        }

        _isTyping = false;
    }

    void UpdateProgress()
    {
        if (TutorialManager.instance == null || TutorialManager.instance.tutorialData == null) return;

        int current = TutorialManager.instance.currentStepIndex + 1;
        int total = TutorialManager.instance.tutorialData.steps.Length;

        if (progressText != null)
            progressText.text = $"{current}/{total}";

        if (progressBar != null)
        {
            progressBar.maxValue = total;
            progressBar.value = current;
        }
    }

    void OnSkipClicked()
    {
        if (TutorialManager.instance != null)
            TutorialManager.instance.SkipTutorial();
    }

    void OnNextClicked()
    {
        // 타이핑 중이면 즉시 완료
        if (_isTyping)
        {
            _isTyping = false;
            if (messageText != null)
                messageText.text = _fullMessage;
        }
    }

    public void Hide()
    {
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
    }
}

