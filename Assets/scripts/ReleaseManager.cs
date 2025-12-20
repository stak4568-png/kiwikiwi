using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReleaseManager : MonoBehaviour
{
    public static ReleaseManager instance;

    [Header("릴리스 설정")]
    [Tooltip("희생 시 회복하는 마나 = 카드 코스트 * 배율")]
    public float manaRecoveryRate = 0.5f;
    [Tooltip("최소 회복 마나")]
    public int minManaRecovery = 1;

    [Header("UI 연결")]
    public Button releaseButton;
    public GameObject releasePanel;
    public TMP_Text releaseInfoText;

    [Header("시각 효과")]
    public Color highlightColor = new Color(1f, 0.3f, 0.3f, 0.8f);
    public GameObject releaseEffectPrefab;

    private bool isReleaseMode = false;
    private List<CardDisplay> selectableCards = new List<CardDisplay>();
    private Dictionary<CardDisplay, Color> originalColors = new Dictionary<CardDisplay, Color>();

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (releaseButton != null) releaseButton.onClick.AddListener(OnReleaseButtonClicked);
        SetReleaseModeUI(false);
    }

    void Update()
    {
        if (isReleaseMode && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            CancelReleaseMode();
        }
    }

    public void OnReleaseButtonClicked()
    {
        if (isReleaseMode) CancelReleaseMode();
        else EnterReleaseMode();
    }

    public void EnterReleaseMode()
    {
        if (GameManager.instance != null && !GameManager.instance.CanPlayerAct()) return;

        if (GameManager.instance != null &&
            GameManager.instance.currentMana >= GameManager.instance.maxMana)
        {
            Debug.Log("마나가 이미 가득 찼습니다.");
            return;
        }

        selectableCards.Clear();
        if (GameManager.instance != null && GameManager.instance.playerField != null)
        {
            CardDisplay[] fieldCards = GameManager.instance.playerField.GetComponentsInChildren<CardDisplay>();
            foreach (var card in fieldCards)
            {
                // ★ 수정 포인트: cardData -> data ★
                if (card.data != null)
                {
                    selectableCards.Add(card);
                }
            }
        }

        if (selectableCards.Count == 0) return;

        isReleaseMode = true;
        SetReleaseModeUI(true);
        HighlightSelectableCards(true);
    }

    public void CancelReleaseMode()
    {
        isReleaseMode = false;
        SetReleaseModeUI(false);
        HighlightSelectableCards(false);
        selectableCards.Clear();
        originalColors.Clear();
    }

    public bool TryReleaseCard(CardDisplay card)
    {
        if (!isReleaseMode || !selectableCards.Contains(card)) return false;
        ExecuteRelease(card);
        return true;
    }

    void ExecuteRelease(CardDisplay card)
    {
        int manaToRecover = CalculateManaRecovery(card);

        // ★ 수정 포인트: cardData.cardName -> data.title ★
        Debug.Log($"★ 릴리스! {card.data.title}을(를) 희생하여 마나 {manaToRecover} 회복!");

        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(card, EffectTiming.OnRelease);
            EffectManager.instance.TriggerEffects(card, EffectTiming.OnDeath);
        }

        if (releaseEffectPrefab != null)
        {
            GameObject effect = Instantiate(releaseEffectPrefab, card.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        Destroy(card.gameObject);

        if (GameManager.instance != null)
        {
            GameManager.instance.GainMana(manaToRecover);
        }

        CancelReleaseMode();
    }

    int CalculateManaRecovery(CardDisplay card)
    {
        if (card.data == null) return minManaRecovery;

        // ★ 수정 포인트: manaCost -> mana ★
        int baseCost = card.data.mana;
        int recovery = Mathf.RoundToInt(baseCost * manaRecoveryRate);
        return Mathf.Max(recovery, minManaRecovery);
    }

    void SetReleaseModeUI(bool active)
    {
        if (releasePanel != null) releasePanel.SetActive(active);
        if (releaseInfoText != null) releaseInfoText.text = active ? "희생할 카드를 선택하세요 (ESC: 취소)" : "";
        if (releaseButton != null)
        {
            TMP_Text bText = releaseButton.GetComponentInChildren<TMP_Text>();
            if (bText != null) bText.text = active ? "취소" : "릴리스";
        }
    }

    void HighlightSelectableCards(bool highlight)
    {
        foreach (var card in selectableCards)
        {
            if (card == null) continue;

            // ★ 수정 포인트: artImage 대신 필드용 이미지인 boardArt를 사용합니다.
            Image cardImage = card.boardArt;

            // 만약 필드가 아니라 손패에서 릴리스하는 기능도 있다면 아래처럼 체크할 수 있습니다.
            if (cardImage == null) cardImage = card.handArt;

            if (cardImage != null)
            {
                if (highlight)
                {
                    // 원래 색상 저장
                    if (!originalColors.ContainsKey(card))
                    {
                        originalColors[card] = cardImage.color;
                    }
                    cardImage.color = highlightColor; // 강조색(예: 붉은색) 적용
                }
                else
                {
                    // 원래 색상 복원
                    if (originalColors.ContainsKey(card))
                    {
                        cardImage.color = originalColors[card];
                    }
                }
            }
        }
    }

    public bool IsInReleaseMode() => isReleaseMode;
    public bool CanBeReleased(CardDisplay card) => isReleaseMode && selectableCards.Contains(card);
    public int PreviewManaRecovery(CardDisplay card) => CalculateManaRecovery(card);
}