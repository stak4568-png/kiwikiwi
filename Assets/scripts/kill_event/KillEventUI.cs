using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 처치 이벤트 UI
/// 간단한 팝업으로 일러스트와 EP 상승 표시
/// </summary>
public class KillEventUI : MonoBehaviour
{
    [Header("팝업 UI")]
    public GameObject popupPanel;
    public Image illustrationImage;
    public TMP_Text eventText;
    public TMP_Text epGainText;
    public CanvasGroup canvasGroup;

    [Header("상세 이벤트 UI (선택)")]
    public GameObject detailedPanel;
    public Image detailedIllustration;
    public TMP_Text detailedDialogue;
    public TMP_Text speakerName;
    public Button nextButton;

    [Header("설정")]
    public float defaultFadeDuration = 0.2f;

    private bool _isWaitingForInput = false;

    void Start()
    {
        // 초기 상태 숨기기
        if (popupPanel != null) popupPanel.SetActive(false);
        if (detailedPanel != null) detailedPanel.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(() => _isWaitingForInput = false);
    }

    /// <summary>
    /// 처치 이벤트 표시
    /// </summary>
    public IEnumerator ShowKillEvent(KillEventData eventData, CardDisplay killer, CardDisplay victim)
    {
        if (eventData == null) yield break;

        // 상세 이벤트인 경우
        if (eventData.showFullEvent && eventData.detailedPages != null && eventData.detailedPages.Length > 0)
        {
            yield return ShowDetailedEvent(eventData);
        }
        else
        {
            // 간단한 팝업
            yield return ShowSimplePopup(eventData);
        }
    }

    /// <summary>
    /// 간단한 팝업 표시
    /// </summary>
    IEnumerator ShowSimplePopup(KillEventData eventData)
    {
        if (popupPanel == null) yield break;

        // UI 설정
        popupPanel.SetActive(true);

        if (illustrationImage != null && eventData.eventIllustration != null)
        {
            illustrationImage.sprite = eventData.eventIllustration;
            illustrationImage.gameObject.SetActive(true);
        }

        if (eventText != null)
        {
            eventText.text = eventData.eventText ?? "";
            eventText.gameObject.SetActive(!string.IsNullOrEmpty(eventData.eventText));
        }

        if (epGainText != null)
        {
            epGainText.text = $"EP +{eventData.lustGain}";
        }

        // 페이드 인
        yield return FadeIn(eventData.fadeDuration);

        // 표시 시간 대기 (클릭으로 스킵 가능)
        float elapsed = 0f;
        while (elapsed < eventData.displayDuration)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 페이드 아웃
        yield return FadeOut(eventData.fadeDuration);

        popupPanel.SetActive(false);
    }

    /// <summary>
    /// 상세 이벤트 표시 (VN 스타일)
    /// </summary>
    IEnumerator ShowDetailedEvent(KillEventData eventData)
    {
        if (detailedPanel == null) yield break;

        detailedPanel.SetActive(true);

        foreach (var page in eventData.detailedPages)
        {
            // 일러스트
            if (detailedIllustration != null && page.illustration != null)
            {
                detailedIllustration.sprite = page.illustration;
            }

            // 대사
            if (detailedDialogue != null)
            {
                detailedDialogue.text = page.dialogue;
            }

            // 화자
            if (speakerName != null)
            {
                speakerName.text = page.speakerName ?? "";
                speakerName.gameObject.SetActive(!string.IsNullOrEmpty(page.speakerName));
            }

            // 입력 대기
            yield return WaitForInput();
        }

        detailedPanel.SetActive(false);
    }

    IEnumerator WaitForInput()
    {
        _isWaitingForInput = true;

        while (_isWaitingForInput)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                _isWaitingForInput = false;
            }
            yield return null;
        }
    }

    IEnumerator FadeIn(float duration)
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut(float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }
}

