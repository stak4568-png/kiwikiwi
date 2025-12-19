using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardDisplay : MonoBehaviour, IPointerClickHandler
{
    [Header("Data Sources")]
    public CardData cardData;         // 카드 기본 데이터
    public ElementIconData iconData;  // 속성 아이콘 데이터

    [Header("Live Stats (전투 실시간 데이터)")]
    public int currentHealth;         // 현재 체력
    public int currentAttack;         // 현재 공격력

    [Header("State")]
    public bool isArtRevealed = false;   // 일러스트 해금 여부
    public bool isInfoRevealed = false;  // 효과 텍스트 해금 여부

    [Header("Visual Groups")]
    public GameObject handVisual;   // 손패용 UI 그룹
    public GameObject fieldVisual;  // 필드용 UI 그룹

    [Header("Hand Visual References")]
    public Image cardImage;
    public Image elementImage;
    public TMP_Text nameText;
    public TMP_Text manaText;
    public TMP_Text descriptionText;
    public TMP_Text attackText;
    public TMP_Text healthText;

    [Header("Field Visual References")]
    public Image fieldArt;
    public Image fieldElement;
    public TMP_Text fieldATK;
    public TMP_Text fieldHP;

    void Start()
    {
        // 1. 초기 실시간 스탯 설정 (데이터로부터 복사)
        if (cardData is MonsterCardData monster)
        {
            currentHealth = monster.health;
            currentAttack = monster.attack;
        }

        // 2. 초기 구역 확인 및 UI 갱신
        UpdateSourceZone();
        UpdateCardUI();
    }

    // 구역(손패/필드)에 따라 어떤 비주얼을 켤지 결정
    public void UpdateSourceZone()
    {
        if (transform.parent == null) return;

        DropZone dz = transform.parent.GetComponent<DropZone>();
        if (dz != null)
        {
            bool isHand = (dz.zoneType == ZoneType.Hand);
            if (handVisual != null) handVisual.SetActive(isHand);
            if (fieldVisual != null) fieldVisual.SetActive(!isHand);

            // 손패에 있으면 내 카드이므로 자동 해금
            if (isHand)
            {
                isArtRevealed = true;
                isInfoRevealed = true;
            }
        }
    }

    // UI 요소들에 데이터를 적용
    public void UpdateCardUI()
    {
        if (cardData == null) return;

        // --- 공통 데이터 준비 ---
        Sprite targetArt = isArtRevealed ? (cardData.originalArt ?? cardData.censoredArt) : (cardData.censoredArt ?? cardData.originalArt);
        string targetDesc = isInfoRevealed ? cardData.description : cardData.censoredDescription;

        // --- A. 손패 비주얼 업데이트 ---
        if (handVisual != null && handVisual.activeSelf)
        {
            if (nameText != null) nameText.text = cardData.cardName;
            if (manaText != null) manaText.text = cardData.manaCost.ToString();
            if (cardImage != null) cardImage.sprite = targetArt;
            if (descriptionText != null) descriptionText.text = targetDesc;
            if (elementImage != null && iconData != null) elementImage.sprite = iconData.GetIcon(cardData.element);

            if (cardData is MonsterCardData)
            {
                if (attackText != null) attackText.text = currentAttack.ToString();
                if (healthText != null) healthText.text = currentHealth.ToString();
            }
        }

        // --- B. 필드 비주얼 업데이트 ---
        if (fieldVisual != null && fieldVisual.activeSelf)
        {
            if (fieldArt != null) fieldArt.sprite = targetArt;
            if (fieldATK != null) fieldATK.text = currentAttack.ToString();
            if (fieldHP != null) fieldHP.text = currentHealth.ToString();
            if (fieldElement != null && iconData != null) fieldElement.sprite = iconData.GetIcon(cardData.element);
        }
    }

    // 전투 시 데미지를 받는 함수
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        Debug.Log($"{cardData.cardName}이(가) {amount}의 피해를 입음. 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            UpdateCardUI(); // 체력 텍스트 즉시 갱신
        }
    }

    // 카드 파괴 처리
    void Die()
    {
        Debug.Log($"{cardData.cardName}이(가) 파괴되었습니다.");
        // 여기서 파괴 이펙트나 사운드 재생 가능
        Destroy(gameObject);
    }

    // 클릭 시 확대 창 표시
    public void OnPointerClick(PointerEventData eventData)
    {
        if (CardZoomManager.instance != null)
        {
            CardZoomManager.instance.ShowCardZoom(this);
        }
    }
}