using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CardDisplay : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Data Source")]
    public CardData data;
    public bool isMine = false;

    [Header("Runtime Stats")]
    public int currentAttack;
    public int currentHp;
    public int currentLust;

    [Header("Combat State")]
    public bool canAttack = false;

    [Header("Gaze System State")]
    public bool isArtRevealed = false;
    public bool isInfoRevealed = false;

    [Header("Visual Groups (가변 비주얼)")]
    public GameObject handVisual;
    public GameObject boardVisual;

    [Header("Hand UI References")]
    public Image handArt;
    public TMP_Text handTitle;
    public TMP_Text handMana;
    public TMP_Text handAttack;
    public TMP_Text handHp;
    public TMP_Text handLust;
    public GameObject handLustIcon;

    [Header("Board UI References")]
    public Image boardArt;
    public TMP_Text boardAttack;
    public TMP_Text boardHp;
    public GameObject boardTauntOverlay;
    public GameObject boardStealthOverlay; // ★ 은신 오버레이 추가

    private Vector3 originalScale;

    void Awake() { originalScale = transform.localScale; }

    public void Init(CardData cardData, bool ownedByPlayer)
    {
        if (cardData == null) return;
        this.data = cardData;
        this.isMine = ownedByPlayer;

        // 실시간 스탯 초기화
        currentAttack = data.attack;
        currentHp = data.hp;
        currentLust = data.lust_attack;

        if (isMine)
        {
            isArtRevealed = true;
            isInfoRevealed = true;
            // ★ [추가] 돌진 키워드가 있다면 소환 즉시 공격 가능, 아니면 대기
            canAttack = data.HasKeyword(Keyword.Charge);
        }

        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (data == null) return;

        // 1. 구역 확인
        DropZone currentZone = GetComponentInParent<DropZone>();
        bool isOnBoard = (currentZone != null && (currentZone.zoneType == ZoneType.PlayerField || currentZone.zoneType == ZoneType.EnemyField));

        // 2. 비주얼 전환
        if (handVisual != null) handVisual.SetActive(!isOnBoard);
        if (boardVisual != null) boardVisual.SetActive(isOnBoard);

        // 3. 일러스트 결정
        Sprite targetArt = isArtRevealed ? data.art_full : (data.art_censored ?? data.art_full);

        // 4. 손패 UI 갱신
        if (!isOnBoard)
        {
            if (handArt != null) handArt.sprite = targetArt;
            if (handTitle != null) handTitle.text = data.title;
            if (handMana != null) handMana.text = data.mana.ToString();
            if (handAttack != null) handAttack.text = currentAttack.ToString();
            if (handHp != null) handHp.text = currentHp.ToString();
            if (handLust != null)
            {
                handLust.text = currentLust > 0 ? currentLust.ToString() : "";
                if (handLustIcon != null) handLustIcon.SetActive(currentLust > 0);
            }
        }
        // 5. 필드 UI 갱신
        else
        {
            if (boardArt != null)
            {
                boardArt.sprite = data.art_board ?? targetArt;
                // ★ [추가] 공격 가능 여부 시각화 (녹색 테두리 등)
                boardArt.color = (isMine && canAttack) ? Color.green : Color.white;
            }
            if (boardAttack != null) boardAttack.text = currentAttack.ToString();
            if (boardHp != null) boardHp.text = currentHp.ToString();

            // ★ [추가] 키워드 오버레이 처리 (도발/은신)
            if (boardTauntOverlay != null) boardTauntOverlay.SetActive(data.HasKeyword(Keyword.Taunt));
            if (boardStealthOverlay != null) boardStealthOverlay.SetActive(data.HasKeyword(Keyword.Stealth));
        }
    }

    // --- 전투 및 턴 로직 ---
    public void OnTurnStart()
    {
        if (isMine) canAttack = true;

        // ★ [추가] 턴 시작 시 발동하는 효과 트리거
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnTurnStart);

        UpdateVisual();
    }

    public bool CanAttackNow() => canAttack && currentAttack > 0 && data.IsCharacter();

    public void OnAttack(CardDisplay target)
    {
        canAttack = false;

        // ★ [추가] 공격 시 발동하는 효과 트리거
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnAttack, target);

        UpdateVisual();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        currentHp -= amount;
        Debug.Log($"{data.title}이(가) {amount} 피해를 입음.");

        // ★ [추가] 피해를 입었을 때 발동하는 효과 트리거
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnDamaged);

        if (currentHp <= 0)
        {
            Die();
        }
        else UpdateVisual();
    }

    private void Die()
    {
        // ★ [추가] 죽음 시 발동하는 효과 트리거
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnDeath);

        Destroy(gameObject);
    }

    // --- 마우스 상호작용 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // 타겟 선택 모드 우선 처리
        if (EffectManager.instance != null && EffectManager.instance.IsWaitingForTarget())
        {
            EffectManager.instance.OnTargetSelected(this);
            return;
        }

        // 일반 클릭: 카드 확대
        if (GameUIManager.instance != null)
            GameUIManager.instance.ShowCardZoom(this);
    }

    public void OnPointerEnter(PointerEventData eventData) { transform.localScale = originalScale * 1.05f; }
    public void OnPointerExit(PointerEventData eventData) { transform.localScale = originalScale; }
}