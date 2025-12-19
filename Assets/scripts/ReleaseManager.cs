// ReleaseManager.cs
// 릴리스 시스템 - 필드의 아군 유닛을 희생하여 마나 회복

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReleaseManager : MonoBehaviour
{
    public static ReleaseManager instance;

    [Header("릴리스 설정")]
    [Tooltip("희생 시 회복하는 마나 = 카드 코스트 * 배율")]
    public float manaRecoveryRate = 0.5f;   // 기본: 코스트의 50%
    [Tooltip("최소 회복 마나")]
    public int minManaRecovery = 1;

    [Header("UI 연결")]
    public Button releaseButton;            // 릴리스 모드 활성화 버튼
    public GameObject releasePanel;         // 릴리스 모드 안내 패널 (선택)
    public TMP_Text releaseInfoText;        // "희생할 카드를 선택하세요" 텍스트

    [Header("시각 효과")]
    public Color highlightColor = new Color(1f, 0.3f, 0.3f, 0.8f);  // 선택 가능한 카드 하이라이트
    public GameObject releaseEffectPrefab;  // 희생 이펙트 (선택)

    // 상태
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
        // 버튼 연결
        if (releaseButton != null)
        {
            releaseButton.onClick.AddListener(OnReleaseButtonClicked);
        }

        // 초기 상태
        SetReleaseModeUI(false);
    }

    void Update()
    {
        // ESC로 릴리스 모드 취소
        if (isReleaseMode && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelReleaseMode();
        }

        // 우클릭으로도 취소
        if (isReleaseMode && Input.GetMouseButtonDown(1))
        {
            CancelReleaseMode();
        }
    }

    /// <summary>
    /// 릴리스 버튼 클릭 시
    /// </summary>
    public void OnReleaseButtonClicked()
    {
        if (isReleaseMode)
        {
            CancelReleaseMode();
        }
        else
        {
            EnterReleaseMode();
        }
    }

    /// <summary>
    /// 릴리스 모드 진입
    /// </summary>
    public void EnterReleaseMode()
    {
        // 적 턴이면 불가
        if (GameManager.instance != null && GameManager.instance.isEnemyTurn)
        {
            Debug.Log("적 턴에는 릴리스할 수 없습니다.");
            return;
        }

        // 마나가 이미 가득 찼으면 불필요
        if (GameManager.instance != null && 
            GameManager.instance.currentMana >= GameManager.instance.maxMana)
        {
            Debug.Log("마나가 이미 가득 찼습니다.");
            return;
        }

        // 플레이어 필드에서 희생 가능한 카드 찾기
        selectableCards.Clear();
        if (GameManager.instance != null && GameManager.instance.playerField != null)
        {
            CardDisplay[] fieldCards = GameManager.instance.playerField.GetComponentsInChildren<CardDisplay>();
            foreach (var card in fieldCards)
            {
                // 모든 아군 카드는 희생 가능 (조건 추가 가능)
                if (card.cardData != null)
                {
                    selectableCards.Add(card);
                }
            }
        }

        if (selectableCards.Count == 0)
        {
            Debug.Log("희생할 수 있는 카드가 없습니다.");
            return;
        }

        isReleaseMode = true;
        SetReleaseModeUI(true);
        HighlightSelectableCards(true);

        Debug.Log($"릴리스 모드 진입 - {selectableCards.Count}장의 카드 희생 가능");
    }

    /// <summary>
    /// 릴리스 모드 취소
    /// </summary>
    public void CancelReleaseMode()
    {
        isReleaseMode = false;
        SetReleaseModeUI(false);
        HighlightSelectableCards(false);
        selectableCards.Clear();
        originalColors.Clear();

        Debug.Log("릴리스 모드 취소");
    }

    /// <summary>
    /// 카드 클릭 시 호출 (CardDisplay에서 호출)
    /// </summary>
    public bool TryReleaseCard(CardDisplay card)
    {
        if (!isReleaseMode) return false;
        if (!selectableCards.Contains(card)) return false;

        // 릴리스 실행
        ExecuteRelease(card);
        return true;
    }

    /// <summary>
    /// 릴리스 실행
    /// </summary>
    void ExecuteRelease(CardDisplay card)
    {
        // 회복할 마나 계산
        int manaToRecover = CalculateManaRecovery(card);

        Debug.Log($"★ 릴리스! {card.cardData.cardName}을(를) 희생하여 마나 {manaToRecover} 회복!");

        // 릴리스 전용 효과 발동 (OnRelease)
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(card, EffectTiming.OnRelease);
        }

        // 죽음 효과도 발동 (OnDeath)
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(card, EffectTiming.OnDeath);
        }

        // 이펙트 재생
        if (releaseEffectPrefab != null)
        {
            GameObject effect = Instantiate(releaseEffectPrefab, card.transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // 카드 파괴
        Destroy(card.gameObject);

        // 마나 회복
        if (GameManager.instance != null)
        {
            GameManager.instance.GainMana(manaToRecover);
        }

        // 릴리스 모드 종료
        CancelReleaseMode();
    }

    /// <summary>
    /// 회복할 마나 계산
    /// </summary>
    int CalculateManaRecovery(CardDisplay card)
    {
        if (card.cardData == null) return minManaRecovery;

        int baseCost = card.cardData.manaCost;
        int recovery = Mathf.RoundToInt(baseCost * manaRecoveryRate);
        return Mathf.Max(recovery, minManaRecovery);
    }

    /// <summary>
    /// 릴리스 모드 UI 업데이트
    /// </summary>
    void SetReleaseModeUI(bool active)
    {
        if (releasePanel != null)
            releasePanel.SetActive(active);

        if (releaseInfoText != null)
            releaseInfoText.text = active ? "희생할 카드를 선택하세요 (ESC: 취소)" : "";

        // 버튼 텍스트 변경
        if (releaseButton != null)
        {
            TMP_Text buttonText = releaseButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = active ? "취소" : "릴리스";
            }
        }
    }

    /// <summary>
    /// 선택 가능한 카드 하이라이트
    /// </summary>
    void HighlightSelectableCards(bool highlight)
    {
        foreach (var card in selectableCards)
        {
            if (card == null) continue;

            Image cardImage = card.GetComponent<Image>();
            if (cardImage == null)
            {
                // 자식에서 배경 이미지 찾기
                cardImage = card.GetComponentInChildren<Image>();
            }

            if (cardImage != null)
            {
                if (highlight)
                {
                    // 원래 색상 저장
                    if (!originalColors.ContainsKey(card))
                    {
                        originalColors[card] = cardImage.color;
                    }
                    cardImage.color = highlightColor;
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

    /// <summary>
    /// 릴리스 모드인지 확인
    /// </summary>
    public bool IsInReleaseMode()
    {
        return isReleaseMode;
    }

    /// <summary>
    /// 특정 카드가 희생 가능한지 확인
    /// </summary>
    public bool CanBeReleased(CardDisplay card)
    {
        return isReleaseMode && selectableCards.Contains(card);
    }

    /// <summary>
    /// 예상 마나 회복량 미리보기
    /// </summary>
    public int PreviewManaRecovery(CardDisplay card)
    {
        return CalculateManaRecovery(card);
    }
}
