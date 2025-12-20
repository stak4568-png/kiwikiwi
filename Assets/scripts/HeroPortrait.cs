// HeroPortrait.cs
// 영웅 초상화 컴포넌트 - 플레이어/적 영웅 UI 및 상호작용

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class HeroPortrait : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public static HeroPortrait playerHero;
    public static HeroPortrait enemyHero;

    [Header("영웅 타입")]
    public bool isPlayerHero = true;

    [Header("영웅 데이터")]
    public HeroData heroData;

    [Header("현재 상태")]
    public int currentHealth;
    public int currentArmor = 0;
    public int currentLust = 0;  // 플레이어 전용

    [Header("무기 상태")]
    public WeaponState equippedWeapon;
    public bool canAttackWithWeapon = false;

    [Header("영웅 능력 상태")]
    public int heroPowerUsesThisTurn = 0;

    [Header("UI 참조 - 초상화")]
    public Image portraitImage;
    public Image portraitFrame;
    public Image attackGlow;  // 공격 가능 시 테두리 효과

    [Header("UI 참조 - 스탯")]
    public TMP_Text healthText;
    public TMP_Text armorText;
    public GameObject armorIcon;
    public TMP_Text lustText;       // 플레이어 전용
    public Slider lustSlider;       // 플레이어 전용

    [Header("UI 참조 - 무기")]
    public GameObject weaponSlot;
    public Image weaponImage;
    public TMP_Text weaponAttackText;
    public TMP_Text weaponDurabilityText;

    [Header("UI 참조 - 영웅 능력")]
    public Button heroPowerButton;
    public Image heroPowerIcon;
    public TMP_Text heroPowerCostText;
    public GameObject heroPowerUsedOverlay;

    [Header("시각 효과")]
    public Color normalFrameColor = Color.white;
    public Color damagedFrameColor = Color.red;
    public Color attackableFrameColor = Color.green;

    void Awake()
    {
        // 싱글톤 패턴으로 영웅 참조 설정
        if (isPlayerHero)
            playerHero = this;
        else
            enemyHero = this;
    }

    void Start()
    {
        InitializeHero();
        UpdateUI();
    }

    /// <summary>
    /// 영웅 초기화
    /// </summary>
    public void InitializeHero()
    {
        if (heroData == null) return;

        currentHealth = heroData.maxHealth;
        currentArmor = heroData.startingArmor;
        currentLust = 0;
        heroPowerUsesThisTurn = 0;

        // 시작 무기 장착
        if (heroData.startingWeapon != null)
        {
            EquipWeapon(heroData.startingWeapon);
        }

        // 초상화 설정
        if (portraitImage != null && heroData.portrait != null)
        {
            portraitImage.sprite = heroData.portrait;
        }

        // 영웅 능력 설정
        if (heroData.heroPower != null)
        {
            SetupHeroPower();
        }
    }

    /// <summary>
    /// 영웅 능력 UI 설정
    /// </summary>
    void SetupHeroPower()
    {
        if (heroData.heroPower == null) return;

        if (heroPowerIcon != null)
            heroPowerIcon.sprite = heroData.heroPower.icon;

        if (heroPowerCostText != null)
            heroPowerCostText.text = heroData.heroPower.manaCost.ToString();

        if (heroPowerButton != null)
            heroPowerButton.onClick.AddListener(OnHeroPowerClicked);
    }

    // ===== 턴 관리 =====

    /// <summary>
    /// 턴 시작 시 호출
    /// </summary>
    public void OnTurnStart()
    {
        heroPowerUsesThisTurn = 0;

        if (equippedWeapon != null)
        {
            equippedWeapon.OnTurnStart();
            canAttackWithWeapon = true;
        }

        UpdateUI();
    }

    /// <summary>
    /// 턴 종료 시 호출
    /// </summary>
    public void OnTurnEnd()
    {
        canAttackWithWeapon = false;
        UpdateUI();
    }

    // ===== 무기 시스템 =====

    /// <summary>
    /// 무기 장착
    /// </summary>
    public void EquipWeapon(WeaponData weapon)
    {
        // 기존 무기 파괴 효과 발동
        if (equippedWeapon != null && equippedWeapon.weaponData.onBreakEffects != null)
        {
            // TODO: 기존 무기 파괴 효과 실행
            Debug.Log($"기존 무기 {equippedWeapon.weaponData.weaponName} 파괴");
        }

        // 새 무기 장착
        equippedWeapon = new WeaponState(weapon);
        canAttackWithWeapon = !GameManager.instance.isEnemyTurn;

        // 장착 효과 발동
        if (weapon.onEquipEffects != null)
        {
            foreach (var effect in weapon.onEquipEffects)
            {
                // TODO: 장착 효과 실행
            }
        }

        Debug.Log($"{heroData.heroName}이(가) {weapon.weaponName}을(를) 장착!");
        UpdateUI();
    }

    /// <summary>
    /// 무기 파괴
    /// </summary>
    public void DestroyWeapon()
    {
        if (equippedWeapon == null) return;

        // 파괴 효과 발동
        if (equippedWeapon.weaponData.onBreakEffects != null)
        {
            foreach (var effect in equippedWeapon.weaponData.onBreakEffects)
            {
                // TODO: 파괴 효과 실행
            }
        }

        Debug.Log($"{equippedWeapon.weaponData.weaponName}이(가) 파괴됨!");
        equippedWeapon = null;
        canAttackWithWeapon = false;
        UpdateUI();
    }

    /// <summary>
    /// 무기로 공격 가능 여부
    /// </summary>
    public bool CanAttackWithWeapon()
    {
        if (!isPlayerHero) return false;
        if (GameManager.instance.isEnemyTurn) return false;
        if (equippedWeapon == null) return false;

        return equippedWeapon.CanAttack() && canAttackWithWeapon;
    }

    /// <summary>
    /// 영웅이 무기로 대상 공격
    /// </summary>
    public void AttackWithWeapon(HeroPortrait targetHero)
    {
        if (!CanAttackWithWeapon()) return;

        // 도발 체크
        if (HasTauntOnEnemyField())
        {
            Debug.Log("도발 하수인을 먼저 처치해야 합니다!");
            return;
        }

        int damage = equippedWeapon.currentAttack;

        // 무기 특수 효과
        if (equippedWeapon.weaponData.weaponEffect == WeaponEffect.BonusDamageToHeroes)
        {
            damage += equippedWeapon.weaponData.effectValue;
        }

        Debug.Log($"{heroData.heroName}이(가) {targetHero.heroData.heroName}을(를) 무기로 공격! (데미지: {damage})");

        // 데미지 적용
        targetHero.TakeDamage(damage);

        // 생명력 흡수
        if (equippedWeapon.weaponData.hasLifesteal)
        {
            Heal(damage);
        }

        // 무기 내구도 감소
        equippedWeapon.UseWeapon();

        // 무기 파괴 체크
        if (equippedWeapon.IsBroken())
        {
            DestroyWeapon();
        }

        UpdateUI();
    }

    /// <summary>
    /// 영웅이 무기로 하수인 공격
    /// </summary>
    public void AttackWithWeapon(CardDisplay targetMinion)
    {
        if (!CanAttackWithWeapon()) return;

        // 도발 체크
        if (HasTauntOnEnemyField() && !targetMinion.HasKeyword(Keyword.Taunt))
        {
            Debug.Log("도발 하수인을 먼저 처치해야 합니다!");
            return;
        }

        int damage = equippedWeapon.currentAttack;

        // 무기 특수 효과
        if (equippedWeapon.weaponData.weaponEffect == WeaponEffect.BonusDamageToMinions)
        {
            damage += equippedWeapon.weaponData.effectValue;
        }

        Debug.Log($"{heroData.heroName}이(가) {targetMinion.cardData.cardName}을(를) 무기로 공격!");

        // 하수인에게 데미지
        targetMinion.TakeDamage(damage);

        // 반격 데미지 (피해 면역 아니면)
        if (equippedWeapon.weaponData.weaponEffect != WeaponEffect.DamageImmune)
        {
            TakeDamage(targetMinion.currentAttack);
        }

        // 생명력 흡수
        if (equippedWeapon.weaponData.hasLifesteal)
        {
            Heal(damage);
        }

        // 독성 - 하수인 즉사
        if (equippedWeapon.weaponData.hasPoisonous && targetMinion.currentHealth > 0)
        {
            targetMinion.TakeDamage(999);
        }

        // 무기 내구도 감소
        equippedWeapon.UseWeapon();

        if (equippedWeapon.IsBroken())
        {
            DestroyWeapon();
        }

        UpdateUI();
    }

    // ===== 영웅 능력 =====

    /// <summary>
    /// 영웅 능력 버튼 클릭
    /// </summary>
    void OnHeroPowerClicked()
    {
        if (!CanUseHeroPower())
        {
            Debug.Log("영웅 능력 사용 불가");
            return;
        }

        HeroPowerData power = heroData.heroPower;

        // 타겟 지정이 필요한 경우
        if (power.requiresTarget)
        {
            // TODO: 타겟 선택 모드 진입
            Debug.Log("타겟을 선택하세요");
            return;
        }

        // 타겟 불필요 - 즉시 발동
        ExecuteHeroPower(null);
    }

    /// <summary>
    /// 영웅 능력 사용 가능 여부
    /// </summary>
    public bool CanUseHeroPower()
    {
        if (!isPlayerHero) return false;
        if (GameManager.instance.isEnemyTurn) return false;
        if (heroData.heroPower == null) return false;

        HeroPowerData power = heroData.heroPower;

        // 사용 횟수 체크
        if (heroPowerUsesThisTurn >= power.usesPerTurn) return false;

        // 마나 체크
        if (GameManager.instance.currentMana < power.manaCost) return false;

        // 집중력 체크
        if (power.focusCost > 0 && GameManager.instance.currentFocus < power.focusCost) return false;

        return true;
    }

    /// <summary>
    /// 영웅 능력 실행
    /// </summary>
    public void ExecuteHeroPower(object target)
    {
        HeroPowerData power = heroData.heroPower;

        // 비용 지불
        GameManager.instance.TrySpendMana(power.manaCost);
        if (power.focusCost > 0)
        {
            for (int i = 0; i < power.focusCost; i++)
                GameManager.instance.TryUseFocus();
        }

        heroPowerUsesThisTurn++;

        // 능력 실행
        switch (power.powerType)
        {
            case HeroPowerType.DealDamage:
                if (target is HeroPortrait targetHero)
                    targetHero.TakeDamage(power.effectValue);
                else if (target is CardDisplay targetCard)
                    targetCard.TakeDamage(power.effectValue);
                break;

            case HeroPowerType.DealDamageAll:
                foreach (var card in GameManager.instance.GetCardsInZone(ZoneType.EnemyField))
                    card.TakeDamage(power.effectValue);
                break;

            case HeroPowerType.GainArmor:
                GainArmor(power.effectValue);
                break;

            case HeroPowerType.HealSelf:
                Heal(power.effectValue);
                break;

            case HeroPowerType.HealTarget:
                if (target is HeroPortrait healHero)
                    healHero.Heal(power.effectValue);
                else if (target is CardDisplay healCard)
                    healCard.currentHealth += power.effectValue;
                break;

            case HeroPowerType.DrawCard:
                if (DeckManager.instance != null)
                    for (int i = 0; i < power.effectValue; i++)
                        DeckManager.instance.DrawCard();
                break;

            case HeroPowerType.GainMana:
                GameManager.instance.GainMana(power.effectValue);
                break;

            case HeroPowerType.GainFocus:
                GameManager.instance.GainFocus(power.effectValue);
                break;

            case HeroPowerType.ReduceLust:
                ReduceLust(power.effectValue);
                break;

            case HeroPowerType.SummonMinion:
                // TODO: 하수인 소환 구현
                break;

            case HeroPowerType.Custom:
                // 추가 효과 실행
                if (power.additionalEffects != null)
                {
                    foreach (var effect in power.additionalEffects)
                    {
                        // TODO: 효과 실행
                    }
                }
                break;
        }

        Debug.Log($"{heroData.heroName}이(가) {power.powerName} 사용!");
        UpdateUI();
    }

    // ===== 데미지 및 회복 =====

    /// <summary>
    /// 데미지 받기
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        // 방어력 먼저 소모
        if (currentArmor > 0)
        {
            int armorDamage = Mathf.Min(currentArmor, amount);
            currentArmor -= armorDamage;
            amount -= armorDamage;
            Debug.Log($"방어력 {armorDamage} 소모 (남은 방어력: {currentArmor})");
        }

        // 남은 데미지 체력에 적용
        if (amount > 0)
        {
            currentHealth -= amount;
            Debug.Log($"{heroData.heroName}이(가) {amount} 피해! (남은 체력: {currentHealth})");

            // 피해 입은 초상화
            if (heroData.portraitDamaged != null && portraitImage != null)
            {
                // TODO: 피해 연출
            }
        }

        // 사망 체크
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnHeroDeath();
        }

        UpdateUI();
    }

    /// <summary>
    /// 회복
    /// </summary>
    public void Heal(int amount)
    {
        int maxHealth = heroData != null ? heroData.maxHealth : 30;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{heroData.heroName} 회복: +{amount} (현재: {currentHealth})");
        UpdateUI();
    }

    /// <summary>
    /// 방어력 획득
    /// </summary>
    public void GainArmor(int amount)
    {
        currentArmor += amount;
        Debug.Log($"{heroData.heroName} 방어력 획득: +{amount} (현재: {currentArmor})");
        UpdateUI();
    }

    // ===== 유혹 시스템 (플레이어 전용) =====

    /// <summary>
    /// 유혹 데미지 받기
    /// </summary>
    public void TakeLustDamage(int amount)
    {
        if (!isPlayerHero) return;

        currentLust += amount;
        Debug.Log($"흥분도 증가: +{amount} (현재: {currentLust}%)");

        // 유혹 상태 초상화
        if (currentLust >= 50 && heroData.portraitSeduced != null && portraitImage != null)
        {
            portraitImage.sprite = heroData.portraitSeduced;
        }

        // 클라이맥스 체크
        if (currentLust >= 100)
        {
            currentLust = 100;
            OnClimax();
        }

        // GameManager와 동기화
        GameManager.instance.playerLust = currentLust;
        UpdateUI();
    }

    /// <summary>
    /// 흥분도 감소
    /// </summary>
    public void ReduceLust(int amount)
    {
        if (!isPlayerHero) return;

        currentLust = Mathf.Max(0, currentLust - amount);
        Debug.Log($"흥분도 감소: -{amount} (현재: {currentLust}%)");

        // 초상화 복구
        if (currentLust < 50 && heroData.portrait != null && portraitImage != null)
        {
            portraitImage.sprite = heroData.portrait;
        }

        GameManager.instance.playerLust = currentLust;
        UpdateUI();
    }

    // ===== 적 영웅 유혹 공격 =====

    /// <summary>
    /// 적 영웅이 유혹 공격 실행
    /// </summary>
    public void ExecuteSeduceAttack()
    {
        if (isPlayerHero) return;
        if (heroData == null || !heroData.canSeduceAttack) return;

        int seduceDamage = heroData.seducePower;
        Debug.Log($"{heroData.heroName}의 유혹 공격! (유혹력: {seduceDamage})");

        if (playerHero != null)
        {
            playerHero.TakeLustDamage(seduceDamage);
        }
    }

    // ===== 이벤트 처리 =====

    void OnHeroDeath()
    {
        Debug.Log($"{heroData.heroName} 사망!");

        if (isPlayerHero)
        {
            // 플레이어 패배
            Debug.Log("플레이어 패배...");
        }
        else
        {
            // 플레이어 승리
            Debug.Log("플레이어 승리!");
        }
    }

    void OnClimax()
    {
        Debug.Log("<color=magenta>★★★ CLIMAX! ★★★</color>");
        // TODO: 클라이맥스 이벤트 연출
    }

    // ===== 클릭/드롭 이벤트 =====

    public void OnPointerClick(PointerEventData eventData)
    {
        // 타겟 선택 모드일 때
        if (EffectManager.instance != null && EffectManager.instance.IsWaitingForTarget())
        {
            EffectManager.instance.OnHeroTargetSelected(this);
            return;
        }

        // 플레이어 영웅이 무기를 들고 있을 때 - 공격 모드
        if (isPlayerHero && CanAttackWithWeapon())
        {
            // TODO: 공격 타겟 선택 모드
            Debug.Log("공격 대상을 선택하세요");
        }
    }

    /// <summary>
    /// 카드가 영웅 위에 드롭됐을 때 (공격 대상으로 지정)
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        // 플레이어 영웅에겐 드롭 불가 (자해 방지)
        if (isPlayerHero) return;

        Draggable dragged = eventData.pointerDrag?.GetComponent<Draggable>();
        if (dragged == null) return;

        CardDisplay attackerCard = dragged.GetComponent<CardDisplay>();
        if (attackerCard == null) return;

        // 플레이어 필드에서 온 카드만 공격 가능
        if (dragged.sourceZone != ZoneType.PlayerField) return;

        // 도발 체크
        if (HasTauntOnEnemyField())
        {
            Debug.Log("도발 하수인을 먼저 처치해야 합니다!");
            return;
        }

        // 공격 가능 체크
        if (!attackerCard.CanAttackNow())
        {
            Debug.Log("이 하수인은 지금 공격할 수 없습니다!");
            return;
        }

        // 영웅 공격 실행
        ExecuteHeroAttack(attackerCard);
    }

    /// <summary>
    /// 하수인이 영웅을 공격
    /// </summary>
    void ExecuteHeroAttack(CardDisplay attacker)
    {
        Debug.Log($"{attacker.cardData.cardName}이(가) {heroData.heroName}을(를) 공격!");

        // 공격 처리
        attacker.OnAttack(null);
        TakeDamage(attacker.currentAttack);

        attacker.UpdateCardUI();
        UpdateUI();
    }

    // ===== 유틸리티 =====

    /// <summary>
    /// 적 필드에 도발 하수인이 있는지 확인
    /// </summary>
    bool HasTauntOnEnemyField()
    {
        if (GameManager.instance.enemyField == null) return false;

        DropZone enemyZone = GameManager.instance.enemyField.GetComponent<DropZone>();
        return enemyZone != null && enemyZone.HasTaunt();
    }

    // ===== UI 업데이트 =====

    public void UpdateUI()
    {
        // 체력
        if (healthText != null)
            healthText.text = currentHealth.ToString();

        // 방어력
        if (armorText != null)
            armorText.text = currentArmor.ToString();
        if (armorIcon != null)
            armorIcon.SetActive(currentArmor > 0);

        // 흥분도 (플레이어 전용)
        if (isPlayerHero)
        {
            if (lustText != null)
                lustText.text = $"{currentLust}%";
            if (lustSlider != null)
                lustSlider.value = currentLust / 100f;
        }

        // 무기
        if (weaponSlot != null)
        {
            bool hasWeapon = equippedWeapon != null;
            weaponSlot.SetActive(hasWeapon);

            if (hasWeapon)
            {
                if (weaponImage != null)
                    weaponImage.sprite = equippedWeapon.weaponData.weaponIcon;
                if (weaponAttackText != null)
                    weaponAttackText.text = equippedWeapon.currentAttack.ToString();
                if (weaponDurabilityText != null)
                    weaponDurabilityText.text = equippedWeapon.currentDurability.ToString();
            }
        }

        // 영웅 능력
        if (heroData != null && heroData.heroPower != null)
        {
            bool canUse = CanUseHeroPower();
            if (heroPowerButton != null)
                heroPowerButton.interactable = canUse;
            if (heroPowerUsedOverlay != null)
                heroPowerUsedOverlay.SetActive(!canUse && heroPowerUsesThisTurn > 0);
        }

        // 공격 가능 표시
        if (attackGlow != null)
            attackGlow.enabled = CanAttackWithWeapon();

        // 프레임 색상
        if (portraitFrame != null)
        {
            if (CanAttackWithWeapon())
                portraitFrame.color = attackableFrameColor;
            else if (currentHealth < heroData.maxHealth / 3)
                portraitFrame.color = damagedFrameColor;
            else
                portraitFrame.color = normalFrameColor;
        }
    }
}
