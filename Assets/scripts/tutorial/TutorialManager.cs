using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 튜토리얼 관리자
/// 첫 전투에서 게임 메커니즘을 단계별로 설명
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    [Header("상태")]
    public bool isTutorialActive = false;
    public int currentStepIndex = 0;
    public bool hasCompletedTutorial = false;

    [Header("튜토리얼 데이터")]
    public TutorialData tutorialData;

    [Header("UI 참조")]
    public TutorialUI tutorialUI;

    [Header("하이라이트")]
    public GameObject highlightPrefab;
    private List<GameObject> _activeHighlights = new List<GameObject>();

    // 단계별 조건 체크용
    private Dictionary<string, bool> _completedConditions = new Dictionary<string, bool>();

    // 이벤트
    public event Action OnTutorialStarted;
    public event Action OnTutorialCompleted;
    public event Action<TutorialStep> OnStepChanged;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 튜토리얼 시작
    /// </summary>
    public void StartTutorial()
    {
        if (hasCompletedTutorial) return;
        if (tutorialData == null || tutorialData.steps == null || tutorialData.steps.Length == 0)
        {
            Debug.LogWarning("튜토리얼 데이터가 없습니다.");
            return;
        }

        isTutorialActive = true;
        currentStepIndex = 0;
        _completedConditions.Clear();

        OnTutorialStarted?.Invoke();

        // 첫 단계 시작
        StartCoroutine(ExecuteCurrentStep());
    }

    /// <summary>
    /// 현재 단계 실행
    /// </summary>
    IEnumerator ExecuteCurrentStep()
    {
        if (!isTutorialActive) yield break;
        if (currentStepIndex >= tutorialData.steps.Length)
        {
            CompleteTutorial();
            yield break;
        }

        TutorialStep step = tutorialData.steps[currentStepIndex];
        OnStepChanged?.Invoke(step);

        // 하이라이트 표시
        ShowHighlights(step.highlightTargets);

        // UI 표시
        if (tutorialUI != null)
        {
            tutorialUI.ShowStep(step);
        }

        // 진행 조건 대기
        yield return WaitForStepCompletion(step);

        // 하이라이트 제거
        ClearHighlights();

        // 다음 단계로
        currentStepIndex++;
        yield return new WaitForSeconds(step.delayAfter);

        StartCoroutine(ExecuteCurrentStep());
    }

    /// <summary>
    /// 단계 완료 조건 대기
    /// </summary>
    IEnumerator WaitForStepCompletion(TutorialStep step)
    {
        switch (step.progressCondition)
        {
            case TutorialProgressCondition.ClickAnywhere:
                yield return WaitForClick();
                break;

            case TutorialProgressCondition.ClickTarget:
                yield return WaitForTargetClick(step.targetObjectName);
                break;

            case TutorialProgressCondition.PlayCard:
                yield return WaitForCondition("PlayCard");
                break;

            case TutorialProgressCondition.AttackEnemy:
                yield return WaitForCondition("AttackEnemy");
                break;

            case TutorialProgressCondition.EndTurn:
                yield return WaitForCondition("EndTurn");
                break;

            case TutorialProgressCondition.UseMana:
                yield return WaitForCondition("UseMana");
                break;

            case TutorialProgressCondition.Delay:
                yield return new WaitForSeconds(step.autoProgressDelay);
                break;

            case TutorialProgressCondition.None:
            default:
                yield return new WaitForSeconds(0.5f);
                break;
        }
    }

    IEnumerator WaitForClick()
    {
        while (!Input.GetMouseButtonDown(0) && !Input.GetKeyDown(KeyCode.Space))
        {
            yield return null;
        }
    }

    IEnumerator WaitForTargetClick(string targetName)
    {
        // 특정 오브젝트가 클릭될 때까지 대기
        // (실제 구현은 EventSystem과 연동 필요)
        while (!_completedConditions.ContainsKey(targetName) || !_completedConditions[targetName])
        {
            if (Input.GetMouseButtonDown(0))
            {
                // 간단히 처리: 클릭하면 다음으로
                break;
            }
            yield return null;
        }
    }

    IEnumerator WaitForCondition(string conditionKey)
    {
        while (!_completedConditions.ContainsKey(conditionKey) || !_completedConditions[conditionKey])
        {
            yield return null;
        }
        _completedConditions[conditionKey] = false; // 리셋
    }

    /// <summary>
    /// 외부에서 조건 완료 알림
    /// </summary>
    public void NotifyConditionMet(string conditionKey)
    {
        _completedConditions[conditionKey] = true;
    }

    /// <summary>
    /// 하이라이트 표시
    /// </summary>
    void ShowHighlights(string[] targets)
    {
        if (highlightPrefab == null || targets == null) return;

        foreach (string targetName in targets)
        {
            GameObject target = GameObject.Find(targetName);
            if (target != null)
            {
                GameObject highlight = Instantiate(highlightPrefab, target.transform);
                highlight.transform.localPosition = Vector3.zero;
                _activeHighlights.Add(highlight);
            }
        }
    }

    void ClearHighlights()
    {
        foreach (var hl in _activeHighlights)
        {
            if (hl != null) Destroy(hl);
        }
        _activeHighlights.Clear();
    }

    /// <summary>
    /// 튜토리얼 완료
    /// </summary>
    void CompleteTutorial()
    {
        isTutorialActive = false;
        hasCompletedTutorial = true;

        ClearHighlights();

        if (tutorialUI != null)
            tutorialUI.Hide();

        OnTutorialCompleted?.Invoke();

        Debug.Log("<color=cyan>튜토리얼 완료!</color>");

        // 저장 (PlayerPrefs 또는 SaveManager 사용)
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 튜토리얼 스킵
    /// </summary>
    public void SkipTutorial()
    {
        isTutorialActive = false;
        hasCompletedTutorial = true;

        ClearHighlights();

        if (tutorialUI != null)
            tutorialUI.Hide();

        OnTutorialCompleted?.Invoke();
    }

    /// <summary>
    /// 튜토리얼 완료 여부 로드
    /// </summary>
    public void LoadTutorialState()
    {
        hasCompletedTutorial = PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
    }
}

