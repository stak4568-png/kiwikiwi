using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

// GameUI ���� ����
public enum GameUIState { None, CardZoom, SeduceEvent, ClimaxEvent, ClimaxResolution }

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("Current State")]
    public GameUIState currentState = GameUIState.None;

    [Header("1. Common UI References")]
    public GameObject uiRoot;            // ��ü UI ��Ʈ �г�
    public Image overlayBackground;      // ������ ��� �̹���
    public Image mainIllustration;       // �߾� ū �Ϸ���Ʈ
    public TMP_Text titleText;           // �̸�/����
    public TMP_Text descriptionText;     // ���� �� ���â

    [Header("2. Seduce Event Group")]
    public GameObject seduceButtonGroup;
    public Button blockButton;
    public Button endureButton;
    public TMP_Text blockButtonText;

    [Header("3. Climax & Choice Group")]
    public GameObject climaxButtonGroup; // Next ��ư ���� �׷�
    public Button nextButton;
    public GameObject choiceGroup;       // ����/���� ��ư �θ� �׷�
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
    [Tooltip("응시 시 초당 증가하는 Lust")]
    public int gazeLustPerSecond = 5;
    [Tooltip("경고 시작 시간 (gazeSeductionTime의 비율)")]
    public float warningThreshold = 0.5f;
    public Image gazeWarningOverlay;
    public TMP_Text gazeTimerText;

    [Header("Background Settings")]
    public float zoomDimAlpha = 0.4f;    // ���� �� ��� ������
    public float eventDimAlpha = 0.9f;   // �̺�Ʈ�� �� ��� ������

    private Action onEventComplete;
    private int dialogueIndex = 0;
    private ClimaxEventData currentClimaxData;
    private CardDisplay currentZoomedCard;

    private string[] resolutionDialogues;
    private int resolutionIndex = 0;

    // 응시 시스템 상태
    private float _gazeTimer = 0f;
    private bool _isGazingUncensored = false;
    private Coroutine _gazeCoroutine;

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

        // 응시 시스템 리셋
        StopGazeTimer();

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

    // ��� Ŭ�� (�� ��忡���� �۵�)
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

    // --- [��� 1] ��Ȥ �̺�Ʈ (����/�ϼ��� ���� ���) ---
    // �� ���� �ذ�: ���ڸ� 4�� �޴� �����ε� ���� ��
    public void ShowSeduceEvent(string name, Sprite art, int lustAtk, int manaDef, Action callback)
    {
        CloseAllUI();
        currentState = GameUIState.SeduceEvent;
        onEventComplete = callback;

        uiRoot.SetActive(true);
        seduceButtonGroup.SetActive(true);
        SetBackgroundAlpha(eventDimAlpha);

        titleText.text = name;
        mainIllustration.sprite = art;
        descriptionText.text = $"{name}�� ��Ȥ! \n(����: {lustAtk} Lust / ��� ���: {manaDef} Mana)";

        // [��� ��ư ����]
        int currentMana = GameManager.instance.playerCurrentMana;
        bool canAfford = currentMana >= manaDef;

        blockButtonText.text = $"������ ���\n({manaDef} �Ҹ� / ����: {currentMana})";
        blockButton.interactable = canAfford; // ������ �����ϸ� ��ư ��Ȱ��ȭ

        blockButton.onClick.RemoveAllListeners();
        blockButton.onClick.AddListener(() => {
            // ������ ��� ������ �Ҹ��ϰ� �������� 0
            GameManager.instance.TrySpendMana(manaDef);
            HeroPortrait.playerHero.TakeLustDamage(0, true);
            FinishEvent();
        });

        // [�γ�(�׳� �±�) ��ư ����]
        endureButton.onClick.RemoveAllListeners();
        endureButton.onClick.AddListener(() => {
            // ������ �Ƴ��� ��Ȥ �������� �״�� ����
            HeroPortrait.playerHero.TakeLustDamage(lustAtk, true);
            FinishEvent();
        });
    }

    // --- [��� 2] Ŭ���̸ƽ� �̺�Ʈ ---
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

    // --- [��� 3] ī�� Ȯ�� ���� (�ü� �ý���) ---
    public void ShowCardZoom(CardDisplay card)
    {
        // �̹� �ٸ� �̺�Ʈ ���̸� �� �Ұ� (��, �� ���¿��� �������ô� ���)
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
            gazeButtonGroup.SetActive(!card.isMine); // 적 카드일 때만 응시 버튼 활성화

        // ★ 응시 유혹 시스템: 적 카드의 검열 해제 일러스트를 볼 때 타이머 시작
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

            // ★ 검열 해제 시 응시 타이머 시작
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
            currentZoomedCard.UpdateVisual(); // �ʵ� ī�� ����
            ShowCardZoom(currentZoomedCard);  // Ȯ��â ����
        }
    }

    void FinishEvent()
    {
        CloseAllUI();
        onEventComplete?.Invoke();
        onEventComplete = null;
    }

    // === 응시 유혹 시스템 ===

    void StartGazeTimer()
    {
        _gazeTimer = 0f;
        _isGazingUncensored = true;
        if (gazeWarningOverlay != null)
        {
            gazeWarningOverlay.gameObject.SetActive(true);
            gazeWarningOverlay.color = new Color(1f, 0f, 0.5f, 0f); // 투명하게 시작
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
        if (gazeWarningOverlay != null)
        {
            gazeWarningOverlay.gameObject.SetActive(false);
        }
        if (gazeTimerText != null)
        {
            gazeTimerText.gameObject.SetActive(false);
        }
    }

    void UpdateGazeWarningUI()
    {
        float progress = _gazeTimer / gazeSeductionTime;

        // 경고 오버레이: 시간이 지날수록 붉게 변함
        if (gazeWarningOverlay != null && progress >= warningThreshold)
        {
            float warningProgress = (progress - warningThreshold) / (1f - warningThreshold);
            gazeWarningOverlay.color = new Color(1f, 0f, 0.5f, warningProgress * 0.5f);
        }

        // 타이머 텍스트 표시
        if (gazeTimerText != null)
        {
            float remaining = gazeSeductionTime - _gazeTimer;
            if (remaining > 0)
            {
                gazeTimerText.text = $"♥ {remaining:F1}s";
                // 위험할수록 빨갛게
                gazeTimerText.color = Color.Lerp(Color.white, Color.red, progress);
            }
        }
    }

    void TriggerGazeSeduction()
    {
        if (currentZoomedCard == null || currentZoomedCard.data == null) return;

        // 유혹 데미지 계산 (카드의 lust_attack 기반)
        int lustDamage = Mathf.Max(1, currentZoomedCard.data.lust_attack / 2);

        Debug.Log($"<color=magenta>★ 응시 유혹! {currentZoomedCard.data.title}에게 홀려 {lustDamage} Lust 증가!</color>");

        // 플레이어에게 유혹 데미지
        if (HeroPortrait.playerHero != null)
        {
            HeroPortrait.playerHero.TakeLustDamage(lustDamage, true);
        }

        // 타이머 리셋 (계속 보고 있으면 반복 발동)
        _gazeTimer = 0f;

        // 100% 도달 시 클라이맥스 체크
        if (HeroPortrait.playerHero != null && HeroPortrait.playerHero.currentLust >= 100)
        {
            CloseAllUI();
        }
    }
}