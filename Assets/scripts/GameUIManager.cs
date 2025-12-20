using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

// GameUI 상태 정의
public enum GameUIState { None, CardZoom, SeduceEvent, ClimaxEvent }

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("Current State")]
    public GameUIState currentState = GameUIState.None;

    [Header("1. Common UI References")]
    public GameObject uiRoot;
    public Image overlayBackground;
    public Image mainIllustration;
    public TMP_Text titleText;
    public TMP_Text descriptionText;

    [Header("2. Seduce Event Group")]
    public GameObject seduceButtonGroup;
    public Button blockButton;
    public Button endureButton;
    public TMP_Text blockButtonText;

    [Header("3. Climax & Choice Group")]
    public GameObject climaxButtonGroup;
    public Button nextButton;
    public GameObject choiceGroup;
    public Button acceptButton;
    public Button rejectButton;
    public TMP_Text acceptText;
    public TMP_Text rejectText;

    [Header("4. Card Zoom Group")]
    public GameObject zoomStatsGroup;
    public TMP_Text statsText;
    public GameObject gazeButtonGroup;

    [Header("Background Settings")]
    public float zoomDimAlpha = 0.4f;
    public float eventDimAlpha = 0.9f;

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

    // --- [유혹 이벤트] ---
    public void ShowSeduceEvent(string name, Sprite art, int lustAtk, Action callback)
    {
        CloseAllUI();
        currentState = GameUIState.SeduceEvent;
        onEventComplete = callback;
        uiRoot.SetActive(true);
        seduceButtonGroup.SetActive(true);
        SetBackgroundAlpha(eventDimAlpha);
        titleText.text = name;
        mainIllustration.sprite = art;
        descriptionText.text = $"{name}의 유혹 공격!\n({lustAtk} Lust)";
        blockButtonText.text = $"마나로 저항\n(보유 마나: {GameManager.instance.currentMana})";

        blockButton.onClick.RemoveAllListeners();
        blockButton.onClick.AddListener(() => {
            int mana = GameManager.instance.currentMana;
            HeroPortrait.playerHero.TakeLustDamage(Mathf.Max(0, lustAtk - mana), true);
            GameManager.instance.TrySpendMana(mana);
            FinishEvent();
        });

        endureButton.onClick.RemoveAllListeners();
        endureButton.onClick.AddListener(() => {
            HeroPortrait.playerHero.TakeLustDamage(lustAtk, true);
            FinishEvent();
        });
    }

    // --- [클라이맥스 이벤트] ---
    public void ShowClimaxEvent(ClimaxEventData data)
    {
        CloseAllUI();
        currentState = GameUIState.ClimaxEvent;
        currentClimaxData = data;
        dialogueIndex = 0;

        uiRoot.SetActive(true);
        climaxButtonGroup.SetActive(true);
        nextButton.gameObject.SetActive(true);
        choiceGroup.SetActive(false);
        SetBackgroundAlpha(eventDimAlpha);
        titleText.text = data.eventTitle;
        mainIllustration.sprite = data.climaxIllustration;

        ShowNextDialogue();
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(ShowNextDialogue);
    }

    void ShowNextDialogue()
    {
        if (dialogueIndex < currentClimaxData.dialogueLines.Length)
        {
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
        nextButton.gameObject.SetActive(false);
        choiceGroup.SetActive(true);
        acceptText.text = currentClimaxData.acceptButtonText;
        rejectText.text = currentClimaxData.rejectButtonText;

        acceptButton.onClick.RemoveAllListeners();
        acceptButton.onClick.AddListener(() => StartResolution(true));

        rejectButton.onClick.RemoveAllListeners();
        rejectButton.onClick.AddListener(() => StartResolution(false));
    }

    void StartResolution(bool isAccept)
    {
        choiceGroup.SetActive(false);
        nextButton.gameObject.SetActive(true);
        resolutionIndex = 0;

        if (isAccept)
        {
            mainIllustration.sprite = currentClimaxData.acceptArt ?? currentClimaxData.climaxIllustration;
            resolutionDialogues = currentClimaxData.acceptDialogues;
            HeroPortrait.playerHero.ReduceLust(currentClimaxData.acceptLustReduction);
            GameManager.instance.SetManaLock(currentClimaxData.acceptManaLockTurns);
        }
        else
        {
            mainIllustration.sprite = currentClimaxData.rejectArt ?? currentClimaxData.climaxIllustration;
            resolutionDialogues = currentClimaxData.rejectDialogues;
            HeroPortrait.playerHero.ReduceLust(currentClimaxData.rejectLustReduction);
            GameManager.instance.SetManaLock(currentClimaxData.rejectManaLockTurns);
        }

        ShowNextResolutionDialogue();
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(ShowNextResolutionDialogue);
    }

    void ShowNextResolutionDialogue()
    {
        if (resolutionIndex < resolutionDialogues.Length)
        {
            descriptionText.text = resolutionDialogues[resolutionIndex];
            resolutionIndex++;
        }
        else
        {
            FinishEvent();
        }
    }

    // --- [카드 줌] ---
    public void ShowCardZoom(CardDisplay card)
    {
        if (currentState != GameUIState.None && currentState != GameUIState.CardZoom) return;

        bool isRefreshing = (currentState == GameUIState.CardZoom);
        if (!isRefreshing)
        {
            CloseAllUI();
            currentState = GameUIState.CardZoom;
            uiRoot.SetActive(true);
            zoomStatsGroup.SetActive(true);
            SetBackgroundAlpha(zoomDimAlpha);
        }

        currentZoomedCard = card;
        titleText.text = card.cardData.cardName;
        mainIllustration.sprite = card.isArtRevealed ? card.cardData.originalArt : card.cardData.censoredArt;
        descriptionText.text = card.isInfoRevealed ? card.cardData.description : card.cardData.censoredDescription;

        if (card.cardData is MonsterCardData monster)
            statsText.text = $"{monster.attack} / {monster.health}";
        else
            statsText.text = "";

        if (gazeButtonGroup != null)
            gazeButtonGroup.SetActive(card.transform.parent?.GetComponent<DropZone>()?.zoneType == ZoneType.EnemyField);
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
            currentZoomedCard.UpdateCardUI();
            ShowCardZoom(currentZoomedCard);
        }
    }

    void FinishEvent()
    {
        CloseAllUI();
        onEventComplete?.Invoke();
        onEventComplete = null;
    }
} // 클래스 닫기