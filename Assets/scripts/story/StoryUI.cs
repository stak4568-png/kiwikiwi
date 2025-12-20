using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스토리 UI
/// VN 스타일 대화창, 캐릭터 표시, 선택지
/// </summary>
public class StoryUI : MonoBehaviour
{
    [Header("메인 패널")]
    public GameObject storyPanel;
    public CanvasGroup canvasGroup;

    [Header("배경")]
    public Image backgroundImage;
    public Image fadeOverlay;

    [Header("캐릭터 표시")]
    public Image leftCharacter;
    public Image centerCharacter;
    public Image rightCharacter;

    [Header("대화창")]
    public GameObject dialogueBox;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    public Image nameBackground;

    [Header("선택지")]
    public GameObject choicePanel;
    public Transform choiceContainer;
    public GameObject choiceButtonPrefab;

    [Header("컨트롤")]
    public Button skipButton;
    public Button autoButton;
    public Button logButton;
    public TMP_Text clickToContinue;

    [Header("설정")]
    public float textSpeed = 0.03f;
    public float fadeSpeed = 0.5f;
    public Color activeCharacterColor = Color.white;
    public Color inactiveCharacterColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("오디오")]
    public AudioSource bgmSource;
    public AudioSource voiceSource;

    // 상태
    private bool _isTyping = false;
    private string _fullText = "";
    private Coroutine _typewriterCoroutine;
    private Action<int> _choiceCallback;
    private List<GameObject> _activeChoiceButtons = new List<GameObject>();

    void Start()
    {
        if (storyPanel != null)
            storyPanel.SetActive(false);

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkipClicked);

        if (choicePanel != null)
            choicePanel.SetActive(false);
    }

    /// <summary>
    /// UI 표시
    /// </summary>
    public void Show()
    {
        if (storyPanel != null)
            storyPanel.SetActive(true);

        if (canvasGroup != null)
            StartCoroutine(FadeIn());

        ClearCharacters();
    }

    /// <summary>
    /// UI 숨기기
    /// </summary>
    public void Hide()
    {
        StopBGM();

        if (canvasGroup != null)
            StartCoroutine(FadeOut());
        else if (storyPanel != null)
            storyPanel.SetActive(false);
    }

    /// <summary>
    /// 배경 설정
    /// </summary>
    public void SetBackground(Sprite background)
    {
        if (backgroundImage != null && background != null)
        {
            backgroundImage.sprite = background;
            backgroundImage.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 대화 표시
    /// </summary>
    public void ShowDialogue(StoryDialogue dialogue)
    {
        // 화자 이름
        if (speakerNameText != null)
        {
            speakerNameText.text = dialogue.speakerName ?? "";
            speakerNameText.gameObject.SetActive(!string.IsNullOrEmpty(dialogue.speakerName));
        }

        if (nameBackground != null)
            nameBackground.gameObject.SetActive(!string.IsNullOrEmpty(dialogue.speakerName));

        // 캐릭터 표시
        ShowCharacter(dialogue.speakerPortrait, dialogue.portraitPosition);

        // 효과 적용
        StartCoroutine(ApplyEffect(dialogue.effect, dialogue.effectDuration));

        // 대사 타이핑
        _fullText = dialogue.text;
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);
        _typewriterCoroutine = StartCoroutine(TypeText(dialogue.text));

        // 음성 재생
        if (dialogue.voiceLine != null && voiceSource != null)
        {
            voiceSource.clip = dialogue.voiceLine;
            voiceSource.Play();
        }

        // 클릭하여 계속 표시
        if (clickToContinue != null)
            clickToContinue.gameObject.SetActive(true);
    }

    void ShowCharacter(Sprite portrait, CharacterPosition position)
    {
        // 모든 캐릭터 비활성화 색상
        DimAllCharacters();

        Image targetImage = position switch
        {
            CharacterPosition.Left => leftCharacter,
            CharacterPosition.Center => centerCharacter,
            CharacterPosition.Right => rightCharacter,
            _ => centerCharacter
        };

        if (targetImage != null && portrait != null)
        {
            targetImage.sprite = portrait;
            targetImage.color = activeCharacterColor;
            targetImage.gameObject.SetActive(true);
        }
    }

    void DimAllCharacters()
    {
        if (leftCharacter != null && leftCharacter.gameObject.activeSelf)
            leftCharacter.color = inactiveCharacterColor;
        if (centerCharacter != null && centerCharacter.gameObject.activeSelf)
            centerCharacter.color = inactiveCharacterColor;
        if (rightCharacter != null && rightCharacter.gameObject.activeSelf)
            rightCharacter.color = inactiveCharacterColor;
    }

    void ClearCharacters()
    {
        if (leftCharacter != null) leftCharacter.gameObject.SetActive(false);
        if (centerCharacter != null) centerCharacter.gameObject.SetActive(false);
        if (rightCharacter != null) rightCharacter.gameObject.SetActive(false);
    }

    IEnumerator TypeText(string text)
    {
        _isTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = "";

            foreach (char c in text)
            {
                if (!_isTyping) break;

                dialogueText.text += c;
                yield return new WaitForSeconds(textSpeed);
            }

            dialogueText.text = text;
        }

        _isTyping = false;
    }

    IEnumerator ApplyEffect(DialogueEffect effect, float duration)
    {
        switch (effect)
        {
            case DialogueEffect.FadeIn:
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = Color.black;
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        fadeOverlay.color = Color.Lerp(Color.black, Color.clear, elapsed / duration);
                        yield return null;
                    }
                    fadeOverlay.color = Color.clear;
                }
                break;

            case DialogueEffect.FadeOut:
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = Color.clear;
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        fadeOverlay.color = Color.Lerp(Color.clear, Color.black, elapsed / duration);
                        yield return null;
                    }
                    fadeOverlay.color = Color.black;
                }
                break;

            case DialogueEffect.Shake:
                if (dialogueBox != null)
                {
                    Vector3 originalPos = dialogueBox.transform.position;
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        float x = UnityEngine.Random.Range(-5f, 5f);
                        float y = UnityEngine.Random.Range(-5f, 5f);
                        dialogueBox.transform.position = originalPos + new Vector3(x, y, 0);
                        yield return null;
                    }
                    dialogueBox.transform.position = originalPos;
                }
                break;

            case DialogueEffect.Flash:
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = Color.white;
                    yield return new WaitForSeconds(0.1f);
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        fadeOverlay.color = Color.Lerp(Color.white, Color.clear, elapsed / duration);
                        yield return null;
                    }
                    fadeOverlay.color = Color.clear;
                }
                break;
        }
    }

    /// <summary>
    /// 선택지 표시
    /// </summary>
    public void ShowChoices(List<StoryChoice> choices, Action<int> callback)
    {
        _choiceCallback = callback;

        // 기존 버튼 제거
        foreach (var btn in _activeChoiceButtons)
        {
            if (btn != null) Destroy(btn);
        }
        _activeChoiceButtons.Clear();

        // 선택지 패널 표시
        if (choicePanel != null)
            choicePanel.SetActive(true);

        // 클릭하여 계속 숨기기
        if (clickToContinue != null)
            clickToContinue.gameObject.SetActive(false);

        // 선택지 버튼 생성
        for (int i = 0; i < choices.Count; i++)
        {
            int index = i; // 클로저용
            StoryChoice choice = choices[i];

            GameObject btnGo = Instantiate(choiceButtonPrefab, choiceContainer);
            _activeChoiceButtons.Add(btnGo);

            TMP_Text btnText = btnGo.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = choice.choiceText;

            Button btn = btnGo.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => OnChoiceSelected(index));

            // 조건 미충족 시 비활성화 표시
            if (choice.conditionType != ChoiceConditionType.None)
            {
                bool met = true;
                if (StoryManager.instance != null)
                {
                    var result = typeof(StoryManager).GetMethod("CheckCondition",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(StoryManager.instance, new object[] { choice }) as bool?;
                    met = result ?? true;
                }

                if (!met)
                {
                    btn.interactable = false;
                    if (btnText != null)
                        btnText.color = Color.gray;
                }
            }
        }
    }

    void OnChoiceSelected(int index)
    {
        if (choicePanel != null)
            choicePanel.SetActive(false);

        _choiceCallback?.Invoke(index);
    }

    void OnSkipClicked()
    {
        if (_isTyping)
        {
            // 타이핑 스킵
            _isTyping = false;
            if (dialogueText != null)
                dialogueText.text = _fullText;
        }
        else
        {
            // 전체 스킵
            StoryManager.instance?.SkipStory();
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }

    IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeSpeed)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = elapsed / fadeSpeed;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeSpeed)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / fadeSpeed);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        if (storyPanel != null)
            storyPanel.SetActive(false);
    }
}

