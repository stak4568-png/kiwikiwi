using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

public enum GameUIState { None, CardZoom, SeduceEvent, ClimaxEvent }

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager instance;

    [Header("Current State")]
    public GameUIState currentState = GameUIState.None;

    [Header("1. Common References")]
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

    [Header("3. Climax/Narrative Group")]
    public GameObject climaxButtonGroup;
    public Button nextButton;

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

    // --- [기능 1] 유혹 이벤트 ---
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

        blockButtonText.text = $"마나로 저항\n(보유: {GameManager.instance.currentMana})";

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

    // --- [기능 2] 클라이맥스 ---
    public void ShowClimaxEvent(ClimaxEventData data)
    {
        CloseAllUI();
        currentState = GameUIState.ClimaxEvent;
        currentClimaxData = data;
        dialogueIndex = 0;

        uiRoot.SetActive(true);
        climaxButtonGroup.SetActive(true);
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
            FinishEvent();
        }
    }

    // --- [기능 3] 카드 확대 보기 ---
    public void ShowCardZoom(CardDisplay card)
    {
        // 수정: 이미 줌 상태라면 중복 실행 방지하되, UI 갱신은 허용
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
        CardData data = card.cardData;
        titleText.text = data.cardName;

        // ★ 실시간 이미지/텍스트 갱신부 ★
        mainIllustration.sprite = card.isArtRevealed ? (data.originalArt ?? data.censoredArt) : data.censoredArt;
        descriptionText.text = card.isInfoRevealed ? data.description : data.censoredDescription;

        if (data is MonsterCardData monster)
            statsText.text = $"{monster.attack} / {monster.health}";
        else
            statsText.text = "";

        if (gazeButtonGroup != null)
        {
            DropZone dz = card.transform.parent?.GetComponent<DropZone>();
            bool isEnemy = (dz != null && dz.zoneType == ZoneType.EnemyField);
            gazeButtonGroup.SetActive(isEnemy);
        }
    }

    // --- 시선 시스템 클릭 ---
    public void OnClickAppreciate()
    {
        if (currentZoomedCard == null) return;
        if (GameManager.instance.TryUseFocus())
        {
            currentZoomedCard.isArtRevealed = true;
            SyncZoomUI();
        }
    }

    public void OnClickAnalyze()
    {
        if (currentZoomedCard == null) return;
        if (GameManager.instance.TryUseFocus())
        {
            currentZoomedCard.isInfoRevealed = true;
            SyncZoomUI();
        }
    }

    void SyncZoomUI()
    {
        if (currentZoomedCard != null)
        {
            currentZoomedCard.UpdateCardUI(); // 필드 카드 갱신

            // 줌 패널의 이미지와 텍스트 즉시 다시 로드
            CardData data = currentZoomedCard.cardData;
            mainIllustration.sprite = currentZoomedCard.isArtRevealed ? (data.originalArt ?? data.censoredArt) : data.censoredArt;
            descriptionText.text = currentZoomedCard.isInfoRevealed ? data.description : data.censoredDescription;

            Debug.Log("확대창 UI 실시간 동기화 완료");
        }
    }

    void FinishEvent()
    {
        CloseAllUI();
        onEventComplete?.Invoke();
        onEventComplete = null;
    }
}