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

        // 최신 데이터 구조 적용 (art_full)
        if (portraitImage != null) portraitImage.sprite = heroData.art_full;

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

        // 마나 방어 로직은 이제 GameUIManager에서 처리되어 들어옵니다.
        // 여기서는 최종 계산된 결과값만 받아서 더해줍니다.
        currentLust += amount;

        UpdateUI(); // UI 먼저 갱신해서 100%를 보여줌

        if (currentLust >= 100)
        {
            currentLust = 100;
            // 유혹 패널 종료 시점과의 충돌 방지를 위해 약간의 지연 후 트리거
            if (GameManager.instance != null)
                GameManager.instance.StartCoroutine(DelayedTriggerClimax());
        }
    }

    public void ReduceLust(int amount)
    {
        currentLust = Mathf.Max(0, currentLust - amount);
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

    // === 적 영웅 전용: 유혹 공격 시퀀스 ===

    public void ExecuteSeduceAttack(Action onAttackComplete)
    {
        if (isPlayerHero || heroData == null || !heroData.canSeduceAttack)
        {
            onAttackComplete?.Invoke();
            return;
        }

        if (GameUIManager.instance != null)
        {
            // ★ 수정 포인트: 5개의 인자 전달 (title, art, power, mana_defense, callback) ★
            GameUIManager.instance.ShowSeduceEvent(
                heroData.title,
                heroData.seduce_event_art ?? heroData.art_full,
                heroData.seducePower,
                heroData.mana_defense, // 세분화된 방어 마나 수치 추가
                onAttackComplete
            );
        }
    }

    // === 마우스 상호작용 ===

    public void OnPointerClick(PointerEventData eventData)
    {
        // 효과 타겟 선택 모드
        if (EffectManager.instance != null && EffectManager.instance.IsWaitingForTarget())
        {
            EffectManager.instance.OnHeroTargetSelected(this);
            return;
        }

        // ★ 자위 대상 선택 모드 처리
        if (MasturbationManager.instance != null && MasturbationManager.instance.isSelectingTarget)
        {
            if (!isPlayerHero) // 적 영웅만 선택 가능
            {
                MasturbationManager.instance.OnEnemyHeroSelected(this);
                return;
            }
        }
    }

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
                    Debug.Log("도발 하수인이 있습니다!");
                    return;
                }
            }

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

        // 무기 UI
        if (weaponSlot != null)
        {
            weaponSlot.SetActive(equippedWeapon != null);
            if (equippedWeapon != null)
            {
                weaponAttackText.text = equippedWeapon.currentAttack.ToString();
                weaponDurabilityText.text = equippedWeapon.currentDurability.ToString();
                weaponImage.sprite = equippedWeapon.weaponData.weaponIcon;
            }
        }

        // 영웅 능력 버튼
        if (heroPowerButton != null && heroData.heroPower != null)
        {
            heroPowerButton.interactable = (GameManager.instance.playerCurrentMana >= heroData.heroPower.manaCost);
        }
    }

    // 영웅 능력 실행 로직
    public void ExecuteHeroPower(object target)
    {
        // 능력 관련 기존 로직...
    }

    // 버튼 이벤트 연결용 (인스펙터 호출 가능)
    void OnHeroPowerClicked()
    {
        // 타겟 선택이 필요한지 체크 후 실행
        ExecuteHeroPower(null);
    }

    // 지연된 클라이맥스 트리거
    System.Collections.IEnumerator DelayedTriggerClimax()
    {
        yield return new WaitForSeconds(0.1f);
        if (GameManager.instance != null)
            GameManager.instance.TriggerClimax();
    }
}