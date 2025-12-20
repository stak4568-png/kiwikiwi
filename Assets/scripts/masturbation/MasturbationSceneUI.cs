using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 자위씬 UI 관리
/// </summary>
public class MasturbationSceneUI : MonoBehaviour
{
    [Header("메인 UI")]
    public GameObject scenePanel;
    public Image backgroundOverlay;
    public Image illustrationImage;
    public TMP_Text dialogueText;
    public TMP_Text speakerNameText;
    public Button nextButton;
    public Button skipButton;

    [Header("타겟 선택 UI")]
    public GameObject targetSelectionPanel;
    public TMP_Text selectionGuideText;

    [Header("결과 UI")]
    public GameObject resultPanel;
    public TMP_Text resultText;

    [Header("설정")]
    public float fadeInDuration = 0.3f;
    public float textSpeed = 0.03f;
    public Color overlayColor = new Color(0, 0, 0, 0.8f);

    // 상태
    private bool _isWaitingForInput = false;
    private bool _skipRequested = false;
    private int _currentPageIndex = 0;
    private MasturbationPage[] _currentPages;
    private MasturbationSceneData _currentSceneData;

    void Start()
    {
        // 초기 상태 숨기기
        if (scenePanel != null) scenePanel.SetActive(false);
        if (targetSelectionPanel != null) targetSelectionPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        // 버튼 이벤트 연결
        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);
    }

    /// <summary>
    /// 타겟 선택 UI 표시
    /// </summary>
    public void ShowTargetSelectionUI()
    {
        if (targetSelectionPanel != null)
        {
            targetSelectionPanel.SetActive(true);
            if (selectionGuideText != null)
                selectionGuideText.text = "자위할 대상을 선택하세요... (ESC: 취소)";
        }
    }

    /// <summary>
    /// 타겟 선택 UI 숨기기
    /// </summary>
    public void HideTargetSelectionUI()
    {
        if (targetSelectionPanel != null)
            targetSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// 자위씬 재생
    /// </summary>
    public IEnumerator PlayScene(MasturbationSceneData sceneData, MasturbationPage[] pages, bool isArtRevealed)
    {
        _currentSceneData = sceneData;
        _currentPages = pages;
        _currentPageIndex = 0;
        _skipRequested = false;

        // UI 표시
        if (scenePanel != null) scenePanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);

        // 페이드 인
        yield return FadeIn();

        // 검열 상태면 안내 텍스트 먼저 표시
        if (!isArtRevealed && !string.IsNullOrEmpty(sceneData.censoredDescription))
        {
            ShowDialogue("", sceneData.censoredDescription);
            yield return WaitForInput();
        }

        // 페이지 순회
        while (_currentPageIndex < _currentPages.Length)
        {
            MasturbationPage page = _currentPages[_currentPageIndex];
            ShowPage(page);
            yield return WaitForInput();
            _currentPageIndex++;
        }

        // 결과 표시
        yield return ShowResult();

        // 페이드 아웃 및 정리
        yield return FadeOut();

        if (scenePanel != null) scenePanel.SetActive(false);
    }

    void ShowPage(MasturbationPage page)
    {
        // 일러스트
        if (illustrationImage != null && page.illustration != null)
        {
            illustrationImage.sprite = page.illustration;
            illustrationImage.gameObject.SetActive(true);
        }

        // 대사
        ShowDialogue(page.speakerName, page.dialogue);

        // 음성 재생 (있다면)
        if (page.voiceLine != null)
        {
            // AudioSource.PlayClipAtPoint(page.voiceLine, Vector3.zero);
        }
    }

    void ShowDialogue(string speaker, string dialogue)
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = speaker;
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(speaker));
        }

        if (dialogueText != null)
        {
            if (_skipRequested)
            {
                dialogueText.text = dialogue;
            }
            else
            {
                StartCoroutine(TypeText(dialogue));
            }
        }
    }

    IEnumerator TypeText(string text)
    {
        if (dialogueText == null) yield break;

        dialogueText.text = "";
        foreach (char c in text)
        {
            if (_skipRequested)
            {
                dialogueText.text = text;
                yield break;
            }
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }
    }

    IEnumerator WaitForInput()
    {
        _isWaitingForInput = true;

        while (_isWaitingForInput && !_skipRequested)
        {
            // 클릭이나 스페이스바로 진행
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                _isWaitingForInput = false;
            }
            yield return null;
        }

        _isWaitingForInput = false;
    }

    IEnumerator ShowResult()
    {
        if (_currentSceneData == null) yield break;

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);

            string resultMessage = $"EP +{_currentSceneData.lustGain}";
            if (_currentSceneData.acquiredFetish != FetishType.None)
            {
                resultMessage += $"\n페티시 획득: {_currentSceneData.acquiredFetish}";
            }
            if (_currentSceneData.triggersDefeat)
            {
                resultMessage += "\n\n<color=red>패배...</color>";
            }

            if (resultText != null)
                resultText.text = resultMessage;

            yield return WaitForInput();
            resultPanel.SetActive(false);
        }
    }

    IEnumerator FadeIn()
    {
        if (backgroundOverlay != null)
        {
            Color startColor = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0);
            Color endColor = overlayColor;

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                backgroundOverlay.color = Color.Lerp(startColor, endColor, elapsed / fadeInDuration);
                yield return null;
            }
            backgroundOverlay.color = endColor;
        }
    }

    IEnumerator FadeOut()
    {
        if (backgroundOverlay != null)
        {
            Color startColor = overlayColor;
            Color endColor = new Color(overlayColor.r, overlayColor.g, overlayColor.b, 0);

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                backgroundOverlay.color = Color.Lerp(startColor, endColor, elapsed / fadeInDuration);
                yield return null;
            }
            backgroundOverlay.color = endColor;
        }
    }

    void OnNextClicked()
    {
        _isWaitingForInput = false;
    }

    void OnSkipClicked()
    {
        _skipRequested = true;
        _isWaitingForInput = false;
    }
}

