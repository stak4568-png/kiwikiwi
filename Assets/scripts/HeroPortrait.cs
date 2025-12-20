using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System; // Action 사용을 위해 필수

public class HeroPortrait : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public static HeroPortrait playerHero;
    public static HeroPortrait enemyHero;

    [Header("영웅 타입")]
    public bool isPlayerHero = true;

    [Header("영웅 데이터")]
    public HeroData heroData;

    [Header("실시간 상태 데이터")]
    public int currentHealth;
    public int currentArmor = 0;
    public int currentLust = 0;  // 플레이어 전용

    [Header("무기 및 능력 상태")]
    public WeaponState equippedWeapon;
    public bool canAttackWithWeapon = false;
    public int heroPowerUsesThisTurn = 0;

    [Header("UI 참조 - 초상화 및 스탯")]
    public Image portraitImage;
    public Image portraitFrame;
    public TMP_Text healthText;
    public TMP_Text armorText;
    public GameObject armorIcon;
    public TMP_Text lustText;       // 플레이어용
    public Slider lustSlider;       // 플레이어용

    [Header("UI 참조 - 무기 슬롯")]
    public GameObject weaponSlot;
    public Image weaponImage;
    public TMP_Text weaponAttackText;
    public TMP_Text weaponDurabilityText;

    [Header("UI 참조 - 영웅 능력")]
    public Button heroPowerButton;
    public Image heroPowerIcon;
    public TMP_Text heroPowerCostText;
    public GameObject heroPowerUsedOverlay;

    void Awake()
    {
        // 싱글톤 참조 설정
        if (isPlayerHero) playerHero = this;
        else enemyHero = this;
    }

    void Start()
    {
        InitializeHero();
    }

    /// <summary>
    /// 영웅 초기 데이터 설정
    /// </summary>
    public void InitializeHero()
    {
        if (heroData == null) return;

        currentHealth = heroData.maxHealth;
        currentArmor = heroData.startingArmor;
        currentLust = 0;
        heroPowerUsesThisTurn = 0;

        if (portraitImage != null) portraitImage.sprite = heroData.portrait;

        // 시작 무기가 있다면 장착
        if (heroData.startingWeapon != null) EquipWeapon(heroData.startingWeapon);

        SetupHeroPower();
        UpdateUI();
    }

    void SetupHeroPower()
    {
        if (heroData.heroPower == null) return;
        if (heroPowerIcon != null) heroPowerIcon.sprite = heroData.heroPower.icon;
        if (heroPowerCostText != null) heroPowerCostText.text = heroData.heroPower.manaCost.ToString();
        if (heroPowerButton != null) heroPowerButton.onClick.AddListener(OnHeroPowerClicked);
    }

    // === 턴 관리 ===

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

    public void OnTurnEnd()
    {
        canAttackWithWeapon = false;
        UpdateUI();
    }

    // === 전투 로직: 데미지 및 회복 ===

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;

        // 방어력 먼저 소모
        if (currentArmor > 0)
        {
            int armorDamage = Mathf.Min(currentArmor, amount);
            currentArmor -= armorDamage;
            amount -= armorDamage;
        }

        // 남은 데미지 체력 차감
        if (amount > 0)
        {
            currentHealth -= amount;
        }

        UpdateUI();

        // 사망 체크 (GameManager 호출)
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (GameManager.instance != null) GameManager.instance.CheckGameOver();
        }
    }

    /// <summary>
    /// 유혹 데미지 처리 (플레이어 전용)
    /// </summary>
    public void TakeLustDamage(int amount, bool ignoreMana = false)
    {
        if (!isPlayerHero) return;

        int finalLust = amount;
        if (!ignoreMana && GameManager.instance != null)
        {
            // 마나 수치만큼 유혹 공격 경감
            finalLust = Mathf.Max(0, amount - GameManager.instance.currentMana);
        }

        currentLust += finalLust;

        if (currentLust >= 100)
        {
            currentLust = 100;
            if (GameManager.instance != null) GameManager.instance.TriggerClimax();
        }
        UpdateUI();
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, heroData.maxHealth);
        UpdateUI();
    }

    // === 무기 시스템 ===

    public void EquipWeapon(WeaponData weapon)
    {
        equippedWeapon = new WeaponState(weapon);
        canAttackWithWeapon = !GameManager.instance.isEnemyTurn;
        UpdateUI();
    }

    public void DestroyWeapon()
    {
        equippedWeapon = null;
        canAttackWithWeapon = false;
        UpdateUI();
    }

    // === 영웅 능력 ===

    void OnHeroPowerClicked()
    {
        if (!CanUseHeroPower()) return;

        if (heroData.heroPower.requiresTarget)
        {
            // 타겟 지정 모드 (필요 시 구현)
            Debug.Log("능력 타겟을 선택하세요.");
        }
        else
        {
            ExecuteHeroPower(null);
        }
    }

    public bool CanUseHeroPower()
    {
        if (GameManager.instance.isEnemyTurn) return false;
        if (heroPowerUsesThisTurn >= heroData.heroPower.usesPerTurn) return false;
        if (GameManager.instance.currentMana < heroData.heroPower.manaCost) return false;
        return true;
    }

    public void ExecuteHeroPower(object target)
    {
        HeroPowerData power = heroData.heroPower;
        GameManager.instance.TrySpendMana(power.manaCost);
        heroPowerUsesThisTurn++;

        // 능력 타입별 실행
        switch (power.powerType)
        {
            case HeroPowerType.GainArmor:
                currentArmor += power.effectValue;
                break;
            case HeroPowerType.HealSelf:
                Heal(power.effectValue);
                break;
                // ... 다른 능력 타입 추가 가능
        }

        UpdateUI();
    }

    // === 적 영웅 전용: 유혹 공격 시퀀스 ===

    /// <summary>
    /// 적 영웅의 유혹 공격 실행 (패널 표시 후 대기)
    /// </summary>
    public void ExecuteSeduceAttack(Action onAttackComplete)
    {
        if (isPlayerHero || heroData == null || !heroData.canSeduceAttack)
        {
            onAttackComplete?.Invoke();
            return;
        }

        if (SeduceEventManager.instance != null)
        {
            // 유혹 이벤트 매니저에게 패널 표시 요청 및 완료 콜백 전달
            SeduceEventManager.instance.StartHeroSeduceEvent(this, onAttackComplete);
        }
        else
        {
            // 매니저 없을 시 즉시 데미지 처리 후 콜백 실행
            if (playerHero != null) playerHero.TakeLustDamage(heroData.seducePower);
            onAttackComplete?.Invoke();
        }
    }

    // === 마우스 상호작용 ===

    public void OnPointerClick(PointerEventData eventData)
    {
        // 효과 타겟 선택 모드일 때
        if (EffectManager.instance != null && EffectManager.instance.IsWaitingForTarget())
        {
            EffectManager.instance.OnHeroTargetSelected(this);
        }
    }

    /// <summary>
    /// 카드를 영웅에게 드롭했을 때 (공격 시도)
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (isPlayerHero) return; // 아군 공격 방지

        Draggable dragged = eventData.pointerDrag?.GetComponent<Draggable>();
        if (dragged == null) return;

        CardDisplay attacker = dragged.GetComponent<CardDisplay>();
        if (attacker != null && attacker.CanAttackNow())
        {
            // 도발 체크
            if (GameManager.instance.enemyField != null)
            {
                DropZone dz = GameManager.instance.enemyField.GetComponent<DropZone>();
                if (dz != null && dz.HasTaunt())
                {
                    Debug.Log("도발 하수인을 먼저 공격해야 합니다!");
                    return;
                }
            }

            // 공격 실행
            attacker.OnAttack(null);
            TakeDamage(attacker.currentAttack);
        }
    }

    // === UI 업데이트 ===

    public void UpdateUI()
    {
        if (healthText != null) healthText.text = currentHealth.ToString();
        if (armorText != null)
        {
            armorText.text = currentArmor.ToString();
            if (armorIcon != null) armorIcon.SetActive(currentArmor > 0);
        }

        if (isPlayerHero)
        {
            if (lustText != null) lustText.text = $"{currentLust}%";
            if (lustSlider != null) lustSlider.value = currentLust / 100f;
        }

        // 무기 UI 업데이트
        if (weaponSlot != null)
        {
            bool hasWeapon = equippedWeapon != null;
            weaponSlot.SetActive(hasWeapon);
            if (hasWeapon)
            {
                weaponAttackText.text = equippedWeapon.currentAttack.ToString();
                weaponDurabilityText.text = equippedWeapon.currentDurability.ToString();
                weaponImage.sprite = equippedWeapon.weaponData.weaponIcon;
            }
        }

        // 영웅 능력 버튼 업데이트
        if (heroPowerButton != null && heroData.heroPower != null)
        {
            heroPowerButton.interactable = CanUseHeroPower();
            if (heroPowerUsedOverlay != null)
                heroPowerUsedOverlay.SetActive(heroPowerUsesThisTurn >= heroData.heroPower.usesPerTurn);
        }
    }
}