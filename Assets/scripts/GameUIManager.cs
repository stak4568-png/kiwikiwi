using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

// GameUI 상태 정의
public enum GameUIState { None, CardZoom, SeduceEvent, ClimaxEvent, ClimaxResolution }

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("Current State")]
    public GameUIState currentState = GameUIState.None;

    [Header("1. Common UI References")]
    public GameObject uiRoot;            // 전체 UI 루트 패널
    public Image overlayBackground;      // 암전용 배경 이미지
    public Image mainIllustration;       // 중앙 큰 일러스트
    public TMP_Text titleText;           // 이름/제목
    public TMP_Text descriptionText;     // 설명 및 대사창

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

    [Header("Background Settings")]
    public float zoomDimAlpha = 0.4f;    // 줌일 때 배경 투명도
    public float eventDimAlpha = 0.9f;   // 이벤트일 때 배경 투명도

    private Action onEventComplete;
    private int dialogueIndex = 0;
    private ClimaxEventData currentClimaxData;
    private CardDisplay currentZoomedCard;

    private string[] resolutionDialogues;
    private int resolutionIndex = 0;

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

        currentState = GameUIState.None;
        currentZoomedCard = null;
    }

    // 배경 클릭 (줌 모드에서만 작동)
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

    // --- [기능 1] 유혹 이벤트 (영웅/하수인 공통 사용) ---
    // ★ 에러 해결: 인자를 4개 받는 오버로드 버전 ★
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
        descriptionText.text = $"{name}의 유혹! \n(피해: {lustAtk} Lust / 방어 비용: {manaDef} Mana)";

        // [방어 버튼 설정]
        int currentMana = GameManager.instance.playerCurrentMana;
        bool canAfford = currentMana >= manaDef;

        blockButtonText.text = $"마나로 방어\n({manaDef} 소모 / 보유: {currentMana})";
        blockButton.interactable = canAfford; // 마나가 부족하면 버튼 비활성화

        blockButton.onClick.RemoveAllListeners();
        blockButton.onClick.AddListener(() => {
            // 정해진 방어 마나만 소모하고 데미지는 0
            GameManager.instance.TrySpendMana(manaDef);
            HeroPortrait.playerHero.TakeLustDamage(0, true);
            FinishEvent();
        });

        // [인내(그냥 맞기) 버튼 설정]
        endureButton.onClick.RemoveAllListeners();
        endureButton.onClick.AddListener(() => {
            // 마나는 아끼고 유혹 데미지를 그대로 받음
            HeroPortrait.playerHero.TakeLustDamage(lustAtk, true);
            FinishEvent();
        });
    }

    // --- [기능 2] 클라이맥스 이벤트 ---
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

    // --- [기능 3] 카드 확대 보기 (시선 시스템) ---
    public void ShowCardZoom(CardDisplay card)
    {
        // 이미 다른 이벤트 중이면 줌 불가 (단, 줌 상태에서 리프레시는 허용)
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
            gazeButtonGroup.SetActive(!card.isMine); // 적 카드일 때만 시선 버튼 활성화
    }

    public void OnClickAppreciate()
    {
        if (currentZoomedCard && GameManager.instance.TryUseFocus())
        {
            currentZoomedCard.isArtRevealed = true;
            SyncZoomUI();
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
            currentZoomedCard.UpdateVisual(); // 필드 카드 갱신
            ShowCardZoom(currentZoomedCard);  // 확대창 갱신
        }
    }

    void FinishEvent()
    {
        CloseAllUI();
        onEventComplete?.Invoke();
        onEventComplete = null;
    }
}