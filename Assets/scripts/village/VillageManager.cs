using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 마을 관리자
/// 층마다 있는 거점, 시설 이용 관리
/// </summary>
public class VillageManager : MonoBehaviour
{
    public static VillageManager instance;

    [Header("현재 상태")]
    public int currentVillageFloor = 1;
    public VillageData currentVillage;
    public bool isInVillage = false;

    [Header("UI 참조")]
    public VillageUI villageUI;

    // 이벤트
    public event Action<VillageData> OnVillageEntered;
    public event Action OnVillageExited;
    public event Action<FacilityType> OnFacilityUsed;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 마을 입장
    /// </summary>
    public void EnterVillage(int floor)
    {
        currentVillageFloor = floor;
        isInVillage = true;

        // 해당 층의 마을 데이터 가져오기
        FloorData floorData = TowerManager.instance?.CurrentFloorData;
        currentVillage = floorData?.villageData;

        OnVillageEntered?.Invoke(currentVillage);

        if (villageUI != null)
            villageUI.Show(currentVillage);

        Debug.Log($"<color=green>{floor}층 마을 입장</color>");
    }

    /// <summary>
    /// 마을 퇴장 (탐색 시작)
    /// </summary>
    public void ExitVillage()
    {
        isInVillage = false;
        OnVillageExited?.Invoke();

        if (villageUI != null)
            villageUI.Hide();

        // 맵 열기
        TowerManager.instance?.OpenMap();
    }

    /// <summary>
    /// 시설 이용
    /// </summary>
    public void UseFacility(FacilityType facility)
    {
        if (!isInVillage) return;

        OnFacilityUsed?.Invoke(facility);

        switch (facility)
        {
            case FacilityType.Inn:
                UseInn();
                break;

            case FacilityType.Shop:
                OpenShop();
                break;

            case FacilityType.Church:
                OpenChurch();
                break;

            case FacilityType.DeckEditor:
                OpenDeckEditor();
                break;

            case FacilityType.Gallery:
                OpenGallery();
                break;

            case FacilityType.Blacksmith:
                OpenBlacksmith();
                break;
        }
    }

    void UseInn()
    {
        // 체력 회복
        int healAmount = currentVillage?.innHealAmount ?? 10;
        int cost = currentVillage?.innCost ?? 50;

        // TODO: 비용 확인 및 차감
        if (HeroPortrait.playerHero != null)
        {
            HeroPortrait.playerHero.Heal(healAmount);
            Debug.Log($"<color=green>여관에서 휴식. 체력 +{healAmount}</color>");
        }
    }

    void OpenShop()
    {
        Debug.Log("상점 열기");
        // TODO: ShopUI.instance.Open();
    }

    void OpenChurch()
    {
        Debug.Log("교회 열기 (정화)");
        if (PurificationManager.instance != null)
        {
            PurificationManager.instance.OpenPurificationUI();
        }
    }

    void OpenDeckEditor()
    {
        Debug.Log("덱 편집기 열기");
        // TODO: DeckEditorUI 열기
    }

    void OpenGallery()
    {
        Debug.Log("갤러리 열기");
        if (GalleryManager.instance != null)
        {
            GalleryManager.instance.OpenGallery();
        }
    }

    void OpenBlacksmith()
    {
        Debug.Log("대장간 열기 (카드 강화)");
        // TODO: 카드 강화 UI
    }

    /// <summary>
    /// 특정 시설 이용 가능 여부
    /// </summary>
    public bool IsFacilityAvailable(FacilityType facility)
    {
        if (currentVillage == null) return false;
        return currentVillage.availableFacilities.Contains(facility);
    }
}

/// <summary>
/// 시설 타입
/// </summary>
public enum FacilityType
{
    Inn,            // 여관 (회복)
    Shop,           // 상점 (카드/아이템 구매)
    Church,         // 교회 (정화)
    DeckEditor,     // 덱 편집
    Gallery,        // 갤러리 (회상방)
    Blacksmith,     // 대장간 (카드 강화)
    Library,        // 도서관 (정보)
    Arena           // 투기장 (추가 전투)
}

