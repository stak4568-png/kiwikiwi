using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

// GameUI 상태 정의
public enum GameUIState { None, CardZoom, SeduceEvent, ClimaxEvent, ClimaxResolution, GazeTemptation }

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("Current State")]
    public GameUIState currentState = GameUIState.None;

    [Header("1. Common UI References")]
    public GameObject uiRoot;            // 전체 UI 루트 패널
    public Image overlayBackground;      // 반투명 배경 이미지
    public Image mainIllustration;       // 중앙 큰 일러스트
    public TMP_Text titleText;           // 이름/제목
    public TMP_Text descriptionText;     // 대사 및 설명창

    [Header("2. Seduce Event Group")]
    public GameObject seduceButtonGroup;
    public Button blockButton;
    public Button endureButton;
    public TMP_Text blockButtonText;

    [Header("3. Climax & Choice Group")]
    public GameObject climaxButtonGroup; // Next 버튼 포함 그룹
    public Button nextButton;
    public GameObject choiceGroup;       // 수락/거절 버튼 부모 그룹
    public Button acceptButton;
    public Button rejectButton;
    public TMP_Text acceptText;
    public TMP_Text rejectText;

    [Header("4. Card Zoom Group")]
    public GameObject zoomStatsGroup;
    public TMP_Text statsText;
    public GameObject gazeButtonGroup;

    [Header("5. Gaze Seduction System")]
    [Tooltip("검열 해제 일러스트 응시 시 유혹 발동까지 걸리는 시간(초)")]
    public float gazeSeductionTime = 3.0f;
    [Tooltip("경고 시작 시간 (gazeSeductionTime의 비율)")]
    public float warningThreshold = 0.5f;
    public Image gazeWarningOverlay;
    public TMP_Text gazeTimerText;

    [Header("6. Gaze Temptation Event UI")]
    public GameObject temptationButtonGroup;
    public Button temptAcceptButton;
    public Button temptRejectButton;
    public TMP_Text temptAcceptText;
    public TMP_Text temptRejectText;
    public TMP_Text rejectCountText;

    [Header("Background Settings")]
    public float zoomDimAlpha = 0.4f;    // 카드 확대 시 배경 투명도
    public float eventDimAlpha = 0.9f;   // 이벤트 시 배경 투명도

    private Action onEventComplete;
    private int dialogueIndex = 0;
    private ClimaxEventData currentClimaxData;
    private CardDisplay currentZoomedCard;

    private string[] resolutionDialogues;
    private int resolutionIndex = 0;

    // 응시 시스템 상태
    private float _gazeTimer = 0f;
    private bool _isGazingUncensored = false;

    // 응시 유혹 이벤트 상태
    private TemptationData _currentTemptation;
    private int _rejectCount = 0;
    private Sprite _originalCardArt;
    private int _forcedDialogueIndex = 0;

    // 성능 최적화: 이전 프레임 타이머 값 캐싱 (불필요한 텍스트 업데이트 방지)
    private float _lastDisplayedTime = -1f;
    private const float TEXT_UPDATE_INTERVAL = 0.1f;  // 0.1초마다 텍스트 업데이트

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
        CloseAllUI();
    }

    public void CloseAllUI()
    {
        if (uiRoot != null) uiRoot.SetActive(false);
        if (seduceButtonGroup != null) seduceButtonGroup.SetActive(false);
        if (climaxButtonGroup != null) climaxButtonGroup.SetActive(false);
        if (choiceGroup != null) choiceGroup.SetActive(false);
        if (zoomStatsGroup != null) zoomStatsGroup.SetActive(false);
        if (gazeButtonGroup != null) gazeButtonGroup.SetActive(false);
        if (temptationButtonGroup != null) temptationButtonGroup.SetActive(false);

        // 응시 시스템 리셋
        StopGazeTimer();
        _currentTemptation = null;
        _rejectCount = 0;
        _forcedDialogueIndex = 0;
       
        currentState = GameUIState.None;
        currentZoomedCard = null;
    }

    void Update()
    {
        // 응시 유혹 시스템 업데이트
        if (_isGazingUncensored && currentState == GameUIState.CardZoom)
        {
            _gazeTimer += Time.deltaTime;
            UpdateGazeWarningUI();

            // 유혹 발동 체크
            if (_gazeTimer >= gazeSeductionTime)
            {
                TriggerGazeSeduction();
            }
        }
    }

    // 배경 클릭 시 (줌 모드에서만 작동)
    public void OnBackgroundClick()
    {
        if (currentState == GameUIState.CardZoom)
        {
            CloseAllUI();
        }
    }

    void SetBackgroundAlpha(float alpha)
    {
        if (overlayBackground != null)
        {
            Color c = overlayBackground.color;
            c.a = alpha;
            overlayBackground.color = c;
        }
    }

    // --- [단계 1] 일반 유혹 이벤트 ---
    public void ShowSeduceEvent(string name, Sprite art, int lustAtk, int manaDef, Action callback)
    {
        CloseAllUI();
        currentState = GameUIState.SeduceEvent;
        onEventComplete = callback;

        if (uiRoot != null) uiRoot.SetActive(true);
        if (seduceButtonGroup != null) seduceButtonGroup.SetActive(true);
        SetBackgroundAlpha(eventDimAlpha);

        if (titleText != null) titleText.text = name;
        if (mainIllustration != null) mainIllustration.sprite = art;
        if (descriptionText != null)
            descriptionText.text = $"{name}의 유혹! \n(수치: {lustAtk} Lust / 마나 소모: {manaDef} Mana)";

        int currentMana = GameManager.instance.playerCurrentMana;
        bool canAfford = currentMana >= manaDef;

        if (blockButtonText != null)
            blockButtonText.text = $"마나로 방어\n({manaDef} 소모 / 보유: {currentMana})";

        if (blockButton != null)
        {
            blockButton.interactable = canAfford;
            blockButton.onClick.RemoveAllListeners();
            blockButton.onClick.AddListener(() => {
                GameManager.instance.TrySpendMana(manaDef);
                HeroPortrait.playerHero.TakeLustDamage(0, true);
                FinishEvent();
            });
        }

        if (endureButton != null)
        {
            endureButton.onClick.RemoveAllListeners();
            endureButton.onClick.AddListener(() => {
                HeroPortrait.playerHero.TakeLustDamage(lustAtk, true);
                FinishEvent();
            });
        }
    }

    // --- [단계 2] 클라이맥스 이벤트 ---
    public void ShowClimaxEvent(ClimaxEventData data)
    {
        CloseAllUI();
        currentState = GameUIState.ClimaxEvent;
        currentClimaxData = data;
        dialogueIndex = 0;

        if (uiRoot != null) uiRoot.SetActive(true);
        if (climaxButtonGroup != null) climaxButtonGroup.SetActive(true);
        if (nextButton != null) nextButton.gameObject.SetActive(true);
        if (choiceGroup != null) choiceGroup.SetActive(false);
        SetBackgroundAlpha(eventDimAlpha);

        if (titleText != null) titleText.text = data.eventTitle;
        if (mainIllustration != null) mainIllustration.sprite = data.climaxIllustration;

        ShowNextDialogue();
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ShowNextDialogue);
        }
    }

    void ShowNextDialogue()
    {
        if (currentClimaxData == null) return;

        if (dialogueIndex < currentClimaxData.dialogueLines.Length)
        {
            if (descriptionText != null)
                descriptionText.text = currentClimaxData.dialogueLines[dialogueIndex];
            dialogueIndex++;
        }
        else
        {
            ShowFinalChoices();
        }
    }

    void ShowFinalChoices()
    {
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (choiceGroup != null) choiceGroup.SetActive(true);

        if (acceptText != null) acceptText.text = currentClimaxData.acceptButtonText;
        if (rejectText != null) rejectText.text = currentClimaxData.rejectButtonText;

        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(() => StartResolution(true));
        }

        if (rejectButton != null)
        {
            rejectButton.onClick.RemoveAllListeners();
            rejectButton.onClick.AddListener(() => StartResolution(false));
        }
    }

    void StartResolution(bool isAccept)
    {
        if (choiceGroup != null) choiceGroup.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(true);
        currentState = GameUIState.ClimaxResolution;
        resolutionIndex = 0;

        if (isAccept)
        {
            if (mainIllustration != null) mainIllustration.sprite = currentClimaxData.acceptArt ?? currentClimaxData.climaxIllustration;
            resolutionDialogues = currentClimaxData.acceptDialogues;
            HeroPortrait.playerHero.ReduceLust(currentClimaxData.acceptLustReduction);
            GameManager.instance.SetManaLock(currentClimaxData.acceptManaLockTurns);
        }
        else
        {
            if (mainIllustration != null) mainIllustration.sprite = currentClimaxData.rejectArt ?? currentClimaxData.climaxIllustration;
            resolutionDialogues = currentClimaxData.rejectDialogues;
            HeroPortrait.playerHero.ReduceLust(currentClimaxData.rejectLustReduction);
            GameManager.instance.SetManaLock(currentClimaxData.rejectManaLockTurns);
        }

        ShowNextResolutionDialogue();
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ShowNextResolutionDialogue);
        }
    }

    void ShowNextResolutionDialogue()
    {
        if (resolutionDialogues == null) return;

        if (resolutionIndex < resolutionDialogues.Length)
        {
            if (descriptionText != null)
                descriptionText.text = resolutionDialogues[resolutionIndex];
            resolutionIndex++;
        }
        else
        {
            FinishEvent();
        }
    }

    // --- [단계 3] 카드 확대 화면 (응시 시스템) ---
    public void ShowCardZoom(CardDisplay card)
    {
        if (currentState != GameUIState.None && currentState != GameUIState.CardZoom) return;

        bool isRefreshing = (currentState == GameUIState.CardZoom);
        if (!isRefreshing)
        {
            CloseAllUI();
            currentState = GameUIState.CardZoom;
            if (uiRoot != null) uiRoot.SetActive(true);
            if (zoomStatsGroup != null) zoomStatsGroup.SetActive(true);
            SetBackgroundAlpha(zoomDimAlpha);
        }

        currentZoomedCard = card;
        if (titleText != null) titleText.text = card.data.title;
        if (mainIllustration != null)
            mainIllustration.sprite = card.isArtRevealed ? card.data.art_full : (card.data.art_censored ?? card.data.art_full);
        if (descriptionText != null)
            descriptionText.text = card.isInfoRevealed ? card.data.text : (card.data.text_censored ?? "해금되지 않은 정보입니다.");

        if (statsText != null)
            statsText.text = $"{card.currentAttack} / {card.currentHp}";

        if (gazeButtonGroup != null)
            gazeButtonGroup.SetActive(!card.isMine);

        if (!card.isMine && card.isArtRevealed && card.data.lust_attack > 0)
        {
            StartGazeTimer();
        }
        else
        {
            StopGazeTimer();
        }
    }

    public void OnClickAppreciate()
    {
        if (currentZoomedCard && GameManager.instance.TryUseFocus())
        {
            currentZoomedCard.isArtRevealed = true;
            SyncZoomUI();

            if (!currentZoomedCard.isMine && currentZoomedCard.data.lust_attack > 0)
            {
                StartGazeTimer();
            }
        }
    }

    public void OnClickAnalyze()
    {
        if (currentZoomedCard && GameManager.instance.TryUseFocus())
        {
            currentZoomedCard.isInfoRevealed = true;
            SyncZoomUI();
        }
    }

    void SyncZoomUI()
    {
        if (currentZoomedCard)
        {
            currentZoomedCard.UpdateVisual();
            ShowCardZoom(currentZoomedCard);
        }
    }

    void FinishEvent()
    {
        CloseAllUI();
        onEventComplete?.Invoke();
        onEventComplete = null;
    }

    // === 응시 유혹 시스템 로직 ===

    void StartGazeTimer()
    {
        _gazeTimer = 0f;
        _isGazingUncensored = true;
        if (gazeWarningOverlay != null)
        {
            gazeWarningOverlay.gameObject.SetActive(true);
            gazeWarningOverlay.color = new Color(1f, 0f, 0.5f, 0f);
        }
        if (gazeTimerText != null)
        {
            gazeTimerText.gameObject.SetActive(true);
        }
    }

    void StopGazeTimer()
    {
        _gazeTimer = 0f;
        _isGazingUncensored = false;
        _lastDisplayedTime = -1f;  // 타이머 캐시 초기화
        if (gazeWarningOverlay != null) gazeWarningOverlay.gameObject.SetActive(false);
        if (gazeTimerText != null) gazeTimerText.gameObject.SetActive(false);
    }

    void UpdateGazeWarningUI()
    {
        float progress = _gazeTimer / gazeSeductionTime;

        if (gazeWarningOverlay != null && progress >= warningThreshold)
        {
            float warningProgress = (progress - warningThreshold) / (1f - warningThreshold);
            gazeWarningOverlay.color = new Color(1f, 0f, 0.5f, warningProgress * 0.5f);
        }

        // 성능 최적화: 0.1초마다만 텍스트 업데이트 (매 프레임 문자열 생성 방지)
        float remaining = gazeSeductionTime - _gazeTimer;
        float displayTime = Mathf.Floor(remaining * 10f) / 10f;  // 0.1초 단위로 반올림

        if (gazeTimerText != null && remaining > 0 && displayTime != _lastDisplayedTime)
        {
            _lastDisplayedTime = displayTime;
            gazeTimerText.text = $"♥ {displayTime:F1}s";
            gazeTimerText.color = Color.Lerp(Color.white, Color.red, progress);
        }
    }

    void TriggerGazeSeduction()
    {
        if (currentZoomedCard == null || currentZoomedCard.data == null) return;

        if (currentZoomedCard.data.temptation_data != null)
        {
            ShowGazeTemptationEvent();
            return;
        }

        int lustDamage = Mathf.Max(1, currentZoomedCard.data.lust_attack / 2);
        if (HeroPortrait.playerHero != null)
        {
            HeroPortrait.playerHero.TakeLustDamage(lustDamage, true);
        }

        _gazeTimer = 0f;
        if (HeroPortrait.playerHero != null && HeroPortrait.playerHero.currentLust >= 100)
        {
            CloseAllUI();
        }
    }

    // === 응시 유혹 이벤트 시스템 ===

    void ShowGazeTemptationEvent()
    {
        if (currentZoomedCard == null) return;

        _currentTemptation = currentZoomedCard.data.temptation_data;
        if (_currentTemptation == null) return;

        currentState = GameUIState.GazeTemptation;
        StopGazeTimer();

        _originalCardArt = currentZoomedCard.data.art_full;

        if (zoomStatsGroup != null) zoomStatsGroup.SetActive(false);
        if (gazeButtonGroup != null) gazeButtonGroup.SetActive(false);
        if (temptationButtonGroup != null) temptationButtonGroup.SetActive(true);

        if (mainIllustration != null)
            mainIllustration.sprite = _currentTemptation.temptationArt ?? _originalCardArt;

        if (descriptionText != null)
            descriptionText.text = _currentTemptation.temptationDialogue;

        UpdateRejectCountUI();
        SetupTemptationButtons();
        SetBackgroundAlpha(eventDimAlpha);
    }

    void SetupTemptationButtons()
    {
        if (_currentTemptation == null) return;

        if (temptAcceptText != null) temptAcceptText.text = _currentTemptation.acceptButtonText;
        if (temptAcceptButton != null)
        {
            temptAcceptButton.onClick.RemoveAllListeners();
            temptAcceptButton.onClick.AddListener(OnTemptationAccept);
        }

        if (temptRejectText != null) temptRejectText.text = _currentTemptation.rejectButtonText;
        if (temptRejectButton != null)
        {
            temptRejectButton.onClick.RemoveAllListeners();
            temptRejectButton.onClick.AddListener(OnTemptationReject);
        }
    }

    void UpdateRejectCountUI()
    {
        if (rejectCountText != null && _currentTemptation != null)
        {
            int remaining = _currentTemptation.maxRejectCount - _rejectCount;
            rejectCountText.text = remaining > 0 ? $"남은 저항: {remaining}회" : "더 이상 저항할 수 없다...";
            rejectCountText.color = remaining <= 1 ? Color.red : Color.white;
        }
    }

    void OnTemptationAccept()
    {
        if (_currentTemptation == null || currentZoomedCard == null) return;

        if (mainIllustration != null && _currentTemptation.acceptArt != null)
            mainIllustration.sprite = _currentTemptation.acceptArt;

        if (descriptionText != null)
            descriptionText.text = _currentTemptation.acceptDialogue;

        if (HeroPortrait.playerHero != null)
            HeroPortrait.playerHero.TakeLustDamage(_currentTemptation.acceptLustGain, true);

        if (temptRejectButton != null) temptRejectButton.gameObject.SetActive(false);
        if (temptAcceptText != null) temptAcceptText.text = _currentTemptation.confirmButtonText;
        if (temptAcceptButton != null)
        {
            temptAcceptButton.onClick.RemoveAllListeners();
            temptAcceptButton.onClick.AddListener(OnConfirmAndClose);
        }
    }

    void OnConfirmAndClose()
    {
        if (temptRejectButton != null) temptRejectButton.gameObject.SetActive(true);

        if (HeroPortrait.playerHero != null && HeroPortrait.playerHero.currentLust >= 100)
        {
            CloseAllUI();
            return;
        }

        _rejectCount = 0;
        currentState = GameUIState.CardZoom;
        if (temptationButtonGroup != null) temptationButtonGroup.SetActive(false);

        if (currentZoomedCard != null) ShowCardZoom(currentZoomedCard);
        else CloseAllUI();
    }

    void OnTemptationReject()
    {
        if (_currentTemptation == null) return;

        _rejectCount++;
        if (_rejectCount >= _currentTemptation.maxRejectCount)
        {
            TriggerForcedTemptation();
            return;
        }

        if (descriptionText != null)
        {
            int idx = Mathf.Min(_rejectCount - 1, _currentTemptation.rejectDialogues.Length - 1);
            if (_currentTemptation.rejectDialogues.Length > 0)
                descriptionText.text = _currentTemptation.rejectDialogues[idx];
        }

        if (HeroPortrait.playerHero != null && _currentTemptation.rejectLustReduction > 0)
            HeroPortrait.playerHero.ReduceLust(_currentTemptation.rejectLustReduction);

        UpdateRejectCountUI();
        StartCoroutine(ReturnToGazeAfterDelay(1.0f));
    }

    void TriggerForcedTemptation()
    {
        if (_currentTemptation == null) return;

        _forcedDialogueIndex = 0;

        if (mainIllustration != null && _currentTemptation.forcedArt != null)
            mainIllustration.sprite = _currentTemptation.forcedArt;

        if (descriptionText != null)
            descriptionText.text = _currentTemptation.forcedDialogue;

        if (temptRejectButton != null) temptRejectButton.gameObject.SetActive(false);
        if (temptAcceptText != null) temptAcceptText.text = _currentTemptation.forcedButtonText;

        if (temptAcceptButton != null)
        {
            temptAcceptButton.onClick.RemoveAllListeners();
            temptAcceptButton.onClick.AddListener(OnForcedTemptationAccept);
        }

        UpdateRejectCountUI();
    }

    void OnForcedTemptationAccept()
    {
        if (_currentTemptation == null) return;

        if (_forcedDialogueIndex == 0)
        {
            if (HeroPortrait.playerHero != null)
                HeroPortrait.playerHero.TakeLustDamage(_currentTemptation.forcedLustGain, true);
        }

        if (_currentTemptation.forcedAfterDialogues != null &&
            _forcedDialogueIndex < _currentTemptation.forcedAfterDialogues.Length)
        {
            if (descriptionText != null)
                descriptionText.text = _currentTemptation.forcedAfterDialogues[_forcedDialogueIndex];

            if (_currentTemptation.forcedAfterArts != null &&
                _forcedDialogueIndex < _currentTemptation.forcedAfterArts.Length &&
                _currentTemptation.forcedAfterArts[_forcedDialogueIndex] != null)
            {
                if (mainIllustration != null)
                    mainIllustration.sprite = _currentTemptation.forcedAfterArts[_forcedDialogueIndex];
            }

            if (temptAcceptText != null) temptAcceptText.text = _currentTemptation.confirmButtonText;
            _forcedDialogueIndex++;
        }
        else
        {
            
            if (temptRejectButton != null) temptRejectButton.gameObject.SetActive(true);

            if (HeroPortrait.playerHero != null && HeroPortrait.playerHero.currentLust >= 100)
            {
                CloseAllUI();
                return;
            }

            _rejectCount = 0;
            currentState = GameUIState.CardZoom;
            if (temptationButtonGroup != null) temptationButtonGroup.SetActive(false);

            if (currentZoomedCard != null) ShowCardZoom(currentZoomedCard);
            else CloseAllUI();
        }
    }

    IEnumerator ReturnToGazeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentState != GameUIState.GazeTemptation) yield break;

        currentState = GameUIState.CardZoom;
        if (temptationButtonGroup != null) temptationButtonGroup.SetActive(false);
        if (zoomStatsGroup != null) zoomStatsGroup.SetActive(true);
        if (gazeButtonGroup != null) gazeButtonGroup.SetActive(!currentZoomedCard.isMine);

        if (mainIllustration != null && currentZoomedCard != null)
        {
            mainIllustration.sprite = currentZoomedCard.isArtRevealed
                ? currentZoomedCard.data.art_full
                : (currentZoomedCard.data.art_censored ?? currentZoomedCard.data.art_full);
        }

        if (currentZoomedCard != null && descriptionText != null)
        {
            descriptionText.text = currentZoomedCard.isInfoRevealed
                ? currentZoomedCard.data.text
                : (currentZoomedCard.data.text_censored ?? "해금되지 않은 정보입니다.");
        }

        SetBackgroundAlpha(zoomDimAlpha);
        StartGazeTimer();
    }
}