// CardDisplay.cs
// 카드 비주얼 및 상호작용 - 효과 시스템 통합 버전

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardDisplay : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    [Header("Data Sources")]
    public CardData cardData;
    public ElementIconData iconData;

    [Header("Live Stats (전투 실시간 데이터)")]
    public int currentHealth;
    public int currentAttack;

    [Header("Runtime Keywords (실시간 키워드)")]
    public List<Keyword> activeKeywords = new List<Keyword>();

    [Header("Combat State")]
    public bool canAttack = false;          // 이번 턴 공격 가능 여부
    public int attacksThisTurn = 0;         // 이번 턴 공격 횟수
    public bool hasDivineShield = false;    // 천상의 보호막 활성화
    public bool isStealthed = false;        // 은신 상태
    public bool hasReborn = false;          // 환생 사용 가능

    [Header("State")]
    public bool isArtRevealed = false;
    public bool isInfoRevealed = false;

    [Header("Visual Groups")]
    public GameObject handVisual;
    public GameObject fieldVisual;

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

    [Header("Keyword Icons (선택)")]
    public GameObject tauntIcon;
    public GameObject divineShieldIcon;
    public GameObject stealthIcon;

    void Start()
    {
        InitializeFromData();
        UpdateSourceZone();
        UpdateCardUI();
    }

    /// <summary>
    /// 카드 데이터로부터 초기화
    /// </summary>
    public void InitializeFromData()
    {
        if (cardData is MonsterCardData monster)
        {
            currentHealth = monster.health;
            currentAttack = monster.attack;

            // 기본 키워드 복사
            activeKeywords.Clear();
            activeKeywords.AddRange(monster.keywords);

            // 특수 키워드 초기화
            hasDivineShield = HasKeyword(Keyword.Divine);
            isStealthed = HasKeyword(Keyword.Stealth);
            hasReborn = HasKeyword(Keyword.Reborn);
            canAttack = HasKeyword(Keyword.Charge); // 돌진 있으면 바로 공격 가능
        }
    }

    /// <summary>
    /// 구역에 따라 비주얼 전환
    /// </summary>
    public void UpdateSourceZone()
    {
        if (transform.parent == null) return;

        DropZone dz = transform.parent.GetComponent<DropZone>();
        if (dz != null)
        {
            bool isHand = (dz.zoneType == ZoneType.Hand);
            if (handVisual != null) handVisual.SetActive(isHand);
            if (fieldVisual != null) fieldVisual.SetActive(!isHand);

            if (isHand)
            {
                isArtRevealed = true;
                isInfoRevealed = true;
            }
        }
    }

    /// <summary>
    /// UI 갱신
    /// </summary>
    public void UpdateCardUI()
    {
        if (cardData == null) return;

        Sprite targetArt = isArtRevealed 
            ? (cardData.originalArt ?? cardData.censoredArt) 
            : (cardData.censoredArt ?? cardData.originalArt);

        // 효과 설명 포함한 텍스트
        string targetDesc = GetFullDescription();

        // Hand Visual
        if (handVisual != null && handVisual.activeSelf)
        {
            if (nameText != null) nameText.text = cardData.cardName;
            if (manaText != null) manaText.text = cardData.manaCost.ToString();
            if (cardImage != null) cardImage.sprite = targetArt;
            if (descriptionText != null) descriptionText.text = targetDesc;
            if (elementImage != null && iconData != null) 
                elementImage.sprite = iconData.GetIcon(cardData.element);

            if (cardData is MonsterCardData)
            {
                if (attackText != null) attackText.text = currentAttack.ToString();
                if (healthText != null) healthText.text = currentHealth.ToString();
            }
        }

        // Field Visual
        if (fieldVisual != null && fieldVisual.activeSelf)
        {
            if (fieldArt != null) fieldArt.sprite = targetArt;
            if (fieldATK != null) fieldATK.text = currentAttack.ToString();
            if (fieldHP != null) fieldHP.text = currentHealth.ToString();
            if (fieldElement != null && iconData != null) 
                fieldElement.sprite = iconData.GetIcon(cardData.element);
        }

        // 키워드 아이콘 업데이트
        UpdateKeywordIcons();
    }

    /// <summary>
    /// 전체 설명 텍스트 생성 (효과 포함)
    /// </summary>
    string GetFullDescription()
    {
        if (!isInfoRevealed)
            return cardData.censoredDescription;

        string baseDesc = cardData.description;

        if (cardData is MonsterCardData monster)
        {
            string effectsDesc = monster.GetEffectsDescription();
            if (!string.IsNullOrEmpty(effectsDesc))
            {
                return effectsDesc + (string.IsNullOrEmpty(baseDesc) ? "" : "\n" + baseDesc);
            }
        }

        return baseDesc;
    }

    /// <summary>
    /// 키워드 아이콘 표시 업데이트
    /// </summary>
    void UpdateKeywordIcons()
    {
        if (tauntIcon != null) tauntIcon.SetActive(HasKeyword(Keyword.Taunt));
        if (divineShieldIcon != null) divineShieldIcon.SetActive(hasDivineShield);
        if (stealthIcon != null) stealthIcon.SetActive(isStealthed);
    }

    // === 키워드 관리 ===

    public bool HasKeyword(Keyword keyword)
    {
        return activeKeywords.Contains(keyword);
    }

    public void AddKeyword(Keyword keyword)
    {
        if (!activeKeywords.Contains(keyword))
        {
            activeKeywords.Add(keyword);

            // 특수 키워드 처리
            if (keyword == Keyword.Divine) hasDivineShield = true;
            if (keyword == Keyword.Stealth) isStealthed = true;
            if (keyword == Keyword.Reborn) hasReborn = true;

            UpdateKeywordIcons();
        }
    }

    public void RemoveKeyword(Keyword keyword)
    {
        activeKeywords.Remove(keyword);
        UpdateKeywordIcons();
    }

    // === 전투 관련 ===

    /// <summary>
    /// 소환 시 호출
    /// </summary>
    public void OnSummoned()
    {
        // 돌진이 없으면 소환 턴에 공격 불가
        canAttack = HasKeyword(Keyword.Charge);
        attacksThisTurn = 0;

        // 소환 효과 발동
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnSummon);
        }

        Debug.Log($"{cardData.cardName} 소환됨! 공격 가능: {canAttack}");
    }

    /// <summary>
    /// 턴 시작 시 호출
    /// </summary>
    public void OnTurnStart()
    {
        canAttack = true;
        attacksThisTurn = 0;

        // 턴 시작 효과 발동
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnTurnStart);
        }
    }

    /// <summary>
    /// 공격 가능 횟수 반환
    /// </summary>
    public int GetMaxAttacksPerTurn()
    {
        return HasKeyword(Keyword.Windfury) ? 2 : 1;
    }

    /// <summary>
    /// 공격 가능 여부 확인
    /// </summary>
    public bool CanAttackNow()
    {
        if (!canAttack) return false;
        if (attacksThisTurn >= GetMaxAttacksPerTurn()) return false;
        return true;
    }

    /// <summary>
    /// 공격 실행 시 호출
    /// </summary>
    public void OnAttack(CardDisplay target)
    {
        attacksThisTurn++;

        // 은신 해제
        if (isStealthed)
        {
            isStealthed = false;
            RemoveKeyword(Keyword.Stealth);
            Debug.Log($"{cardData.cardName}의 은신이 해제됨!");
        }

        // 공격 효과 발동
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnAttack, target);
        }
    }

    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(int amount)
    {
        // 천상의 보호막 체크
        if (hasDivineShield && amount > 0)
        {
            hasDivineShield = false;
            Debug.Log($"{cardData.cardName}의 천상의 보호막이 피해를 흡수!");
            UpdateKeywordIcons();
            return;
        }

        currentHealth -= amount;
        Debug.Log($"{cardData.cardName}이(가) {amount}의 피해를 입음. 남은 체력: {currentHealth}");

        // 피해 효과 발동
        if (EffectManager.instance != null)
        {
            var context = new EffectContext(this, EffectTiming.OnDamaged);
            context.damageAmount = amount;
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnDamaged);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            UpdateCardUI();
        }
    }

    /// <summary>
    /// 카드 파괴
    /// </summary>
    void Die()
    {
        Debug.Log($"{cardData.cardName}이(가) 파괴되었습니다.");

        // 죽음 효과 발동
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnDeath);
        }

        // 환생 체크
        if (hasReborn)
        {
            hasReborn = false;
            currentHealth = 1;
            Debug.Log($"{cardData.cardName}이(가) 환생했습니다!");
            UpdateCardUI();
            return;
        }

        // TODO: 파괴 애니메이션, 묘지로 이동 등
        Destroy(gameObject);
    }

    // === 클릭 이벤트 ===

    public void OnPointerClick(PointerEventData eventData)
    {
        // 릴리스 모드일 때
        if (ReleaseManager.instance != null && ReleaseManager.instance.IsInReleaseMode())
        {
            if (ReleaseManager.instance.TryReleaseCard(this))
            {
                return; // 릴리스 성공
            }
            // 릴리스 불가한 카드면 계속 진행
        }

        // 타겟 선택 모드일 때
        if (EffectManager.instance != null && EffectManager.instance.IsWaitingForTarget())
        {
            EffectManager.instance.OnTargetSelected(this);
            return;
        }

        // 일반 클릭 - 확대 보기
        if (CardZoomManager.instance != null)
        {
            CardZoomManager.instance.ShowCardZoom(this);
        }
    }

    /// <summary>
    /// 마우스 오버 시 릴리스 미리보기 (선택적 구현)
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ReleaseManager.instance != null && ReleaseManager.instance.CanBeReleased(this))
        {
            int previewMana = ReleaseManager.instance.PreviewManaRecovery(this);
            Debug.Log($"릴리스 시 마나 +{previewMana}");
            // TODO: 툴팁 표시
        }
    }
}
