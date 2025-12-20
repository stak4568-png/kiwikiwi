using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 패배씬 UI
/// </summary>
public class DefeatSceneUI : MonoBehaviour
{
    [Header("메인 패널")]
    public GameObject defeatPanel;
    public CanvasGroup canvasGroup;

    [Header("일러스트")]
    public Image mainIllustration;
    public Image characterPortrait;

    [Header("대화")]
    public GameObject dialogueBox;
    public TMP_Text speakerText;
    public TMP_Text dialogueText;

    [Header("효과")]
    public Image screenTintOverlay;
    public Image fadeOverlay;

    [Header("결과")]
    public GameObject resultPanel;
    public TMP_Text resultText;

    [Header("오디오")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("설정")]
    public float textSpeed = 0.03f;
    public float fadeSpeed = 0.5f;

    private bool _isWaitingForInput = false;
    private bool _skipRequested = false;

    void Start()
    {
        if (defeatPanel != null)
            defeatPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    /// <summary>
    /// 패배씬 재생
    /// </summary>
    public IEnumerator PlayScene(DefeatSceneData scene)
    {
        if (scene == null) yield break;

        _skipRequested = false;

        // UI 표시
        if (defeatPanel != null)
            defeatPanel.SetActive(true);

        // 페이드 인
        yield return FadeIn();

        // 각 페이지 재생
        foreach (var page in scene.pages)
        {
            yield return PlayPage(page);
        }

        // 결과 표시
        yield return ShowResult(scene);

        // 페이드 아웃
        yield return FadeOut();

        if (defeatPanel != null)
            defeatPanel.SetActive(false);
    }

    IEnumerator PlayPage(DefeatPage page)
    {
        // 효과 시작
        yield return ApplyEffect(page.effect);

        // 화면 틴트
        if (screenTintOverlay != null)
            screenTintOverlay.color = page.screenTint;

        // 일러스트
        if (mainIllustration != null && page.illustration != null)
        {
            mainIllustration.sprite = page.illustration;
            mainIllustration.gameObject.SetActive(true);
        }

        // 캐릭터 초상화
        if (characterPortrait != null)
        {
            if (page.characterPortrait != null)
            {
                characterPortrait.sprite = page.characterPortrait;
                characterPortrait.gameObject.SetActive(true);
            }
            else
            {
                characterPortrait.gameObject.SetActive(false);
            }
        }

        // 화자
        if (speakerText != null)
        {
            speakerText.text = page.speakerName ?? "";
            speakerText.gameObject.SetActive(!string.IsNullOrEmpty(page.speakerName));
        }

        // 사운드 효과
        if (sfxSource != null && page.soundEffect != null)
        {
            sfxSource.PlayOneShot(page.soundEffect);
        }

        // 음성
        if (voiceSource != null && page.voiceLine != null)
        {
            voiceSource.clip = page.voiceLine;
            voiceSource.Play();
        }

        // 대사 타이핑
        yield return TypeText(page.dialogue);

        // 입력 대기
        yield return WaitForInput();
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

    IEnumerator ApplyEffect(DefeatPageEffect effect)
    {
        switch (effect)
        {
            case DefeatPageEffect.FadeIn:
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = Color.black;
                    float elapsed = 0f;
                    while (elapsed < fadeSpeed)
                    {
                        elapsed += Time.deltaTime;
                        fadeOverlay.color = Color.Lerp(Color.black, Color.clear, elapsed / fadeSpeed);
                        yield return null;
                    }
                    fadeOverlay.color = Color.clear;
                }
                break;

            case DefeatPageEffect.FadeOut:
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = Color.clear;
                    float elapsed = 0f;
                    while (elapsed < fadeSpeed)
                    {
                        elapsed += Time.deltaTime;
                        fadeOverlay.color = Color.Lerp(Color.clear, Color.black, elapsed / fadeSpeed);
                        yield return null;
                    }
                    fadeOverlay.color = Color.black;
                }
                break;

            case DefeatPageEffect.WhiteFade:
                if (fadeOverlay != null)
                {
                    fadeOverlay.color = Color.white;
                    float elapsed = 0f;
                    while (elapsed < fadeSpeed)
                    {
                        elapsed += Time.deltaTime;
                        fadeOverlay.color = Color.Lerp(Color.white, Color.clear, elapsed / fadeSpeed);
                        yield return null;
                    }
                    fadeOverlay.color = Color.clear;
                }
                break;

            case DefeatPageEffect.HeartBeat:
                if (screenTintOverlay != null)
                {
                    Color pink = new Color(1f, 0.5f, 0.5f, 0.3f);
                    for (int i = 0; i < 2; i++)
                    {
                        screenTintOverlay.color = pink;
                        yield return new WaitForSeconds(0.15f);
                        screenTintOverlay.color = Color.clear;
                        yield return new WaitForSeconds(0.1f);
                    }
                }
                break;

            case DefeatPageEffect.Shake:
                if (defeatPanel != null)
                {
                    Vector3 originalPos = defeatPanel.transform.position;
                    float elapsed = 0f;
                    while (elapsed < 0.3f)
                    {
                        elapsed += Time.deltaTime;
                        float x = Random.Range(-8f, 8f);
                        float y = Random.Range(-8f, 8f);
                        defeatPanel.transform.position = originalPos + new Vector3(x, y, 0);
                        yield return null;
                    }
                    defeatPanel.transform.position = originalPos;
                }
                break;

            case DefeatPageEffect.SlowZoom:
                if (mainIllustration != null)
                {
                    Vector3 startScale = Vector3.one;
                    Vector3 endScale = Vector3.one * 1.1f;
                    float elapsed = 0f;
                    while (elapsed < 2f)
                    {
                        elapsed += Time.deltaTime;
                        mainIllustration.transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / 2f);
                        yield return null;
                    }
                }
                break;
        }
    }

    IEnumerator ShowResult(DefeatSceneData scene)
    {
        if (resultPanel == null) yield break;

        resultPanel.SetActive(true);

        string result = "<color=red>패배...</color>\n\n";

        if (scene.acquiredFetish != FetishType.None)
        {
            result += $"<color=magenta>페티시 획득: {scene.acquiredFetish}</color>\n";
        }

        if (resultText != null)
            resultText.text = result;

        yield return WaitForInput();

        resultPanel.SetActive(false);
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
    }

    public void RequestSkip()
    {
        _skipRequested = true;
        _isWaitingForInput = false;
    }
}

