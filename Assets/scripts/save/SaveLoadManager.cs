using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 저장/로드 시스템 관리자
/// 게임 진행 상태 저장 및 불러오기
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager instance;

    [Header("설정")]
    public int maxSaveSlots = 3;
    public string saveFileName = "kiwi_save";

    [Header("자동 저장")]
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300f;    // 5분

    private float _lastAutoSave;
    private bool _isSaving = false;  // 저장 중 플래그 (중복 저장 방지)
    private string SavePath => Application.persistentDataPath;

    // 이벤트
    public event Action OnSaveCompleted;
    public event Action OnLoadCompleted;
    public event Action<string> OnSaveError;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Update()
    {
        // 자동 저장 (비동기로 실행하여 프레임 블로킹 방지)
        if (autoSaveEnabled && !_isSaving && Time.time - _lastAutoSave > autoSaveInterval)
        {
            _lastAutoSave = Time.time;
            StartCoroutine(AutoSaveAsync()); // 비동기 자동 저장
        }
    }

    /// <summary>
    /// 비동기 자동 저장 (메인 스레드 블로킹 방지)
    /// </summary>
    IEnumerator AutoSaveAsync()
    {
        if (_isSaving) yield break;
        _isSaving = true;

        // 데이터 수집은 메인 스레드에서 (Unity API 접근 필요)
        SaveData data = CollectSaveData();
        string path = GetSavePath(0);

        // JSON 직렬화를 별도 프레임에서 처리
        yield return null;

        string json = null;
        bool success = false;

        // 백그라운드 스레드에서 JSON 직렬화 및 파일 쓰기
        Task saveTask = Task.Run(() =>
        {
            try
            {
                json = JsonUtility.ToJson(data, true);
                File.WriteAllText(path, json);
                success = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"자동 저장 실패: {e.Message}");
            }
        });

        // 저장 완료 대기 (프레임 블로킹 없이)
        while (!saveTask.IsCompleted)
        {
            yield return null;
        }

        if (success)
        {
            Debug.Log("<color=green>자동 저장 완료</color>");
            OnSaveCompleted?.Invoke();
        }

        _isSaving = false;
    }

    /// <summary>
    /// 현재 게임 상태를 SaveData로 수집
    /// </summary>
    public SaveData CollectSaveData()
    {
        SaveData data = new SaveData();
        data.saveTime = DateTime.Now.ToString();
        data.version = Application.version;

        // 탑 진행 상태
        if (TowerManager.instance != null)
        {
            data.currentFloor = TowerManager.instance.currentFloor;
            data.currentNodeIndex = TowerManager.instance.currentNodeIndex;
            data.visitedNodeIds = new List<string>(TowerManager.instance.visitedNodeIds);
            data.clearedBosses = new List<string>(TowerManager.instance.clearedBosses);
        }

        // 플레이어 상태
        if (HeroPortrait.playerHero != null)
        {
            data.playerMaxHP = HeroPortrait.playerHero.heroData != null ? HeroPortrait.playerHero.heroData.maxHealth : 30;
            data.playerCurrentHP = HeroPortrait.playerHero.currentHealth;
            data.playerCurrentLust = HeroPortrait.playerHero.currentLust;
        }

        // 컬렉션 및 덱
        if (PlayerCollection.instance != null)
        {
            data.ownedCards = new List<CollectionEntry>(PlayerCollection.instance.ownedCards);
            data.savedDecks = new List<DeckData>(PlayerCollection.instance.savedDecks);
            data.activeDeckIndex = PlayerCollection.instance.activeDeckIndex;
        }

        // 페티시 상태
        if (PlayerFetishState.instance != null)
        {
            data.hasSelectedInitialFetish = PlayerFetishState.instance.hasSelectedInitialFetish;
            data.initialFetish = PlayerFetishState.instance.initialFetish;
            data.fetishStates = new List<FetishState>(PlayerFetishState.instance.fetishStates);
        }

        // 스토리 플래그
        if (StoryManager.instance != null)
        {
            data.storyFlags = new Dictionary<string, int>(StoryManager.instance.storyFlags);
            data.completedEventIds = new List<string>(StoryManager.instance.completedEventIds);
        }

        // 갤러리 해금
        if (GalleryManager.instance != null)
        {
            data.unlockedGalleryIds = new List<string>(GalleryManager.instance.unlockedEntryIds);
        }

        // 정화 상태
        if (PurificationManager.instance != null)
        {
            data.playerGold = PurificationManager.instance.playerGold;
        }

        // 튜토리얼
        if (TutorialManager.instance != null)
        {
            data.tutorialCompleted = TutorialManager.instance.hasCompletedTutorial;
        }

        return data;
    }

    /// <summary>
    /// SaveData를 게임에 적용
    /// </summary>
    public void ApplySaveData(SaveData data)
    {
        if (data == null) return;

        // 탑 진행 상태
        if (TowerManager.instance != null)
        {
            TowerManager.instance.currentFloor = data.currentFloor;
            TowerManager.instance.currentNodeIndex = data.currentNodeIndex;
            TowerManager.instance.visitedNodeIds = new List<string>(data.visitedNodeIds ?? new List<string>());
            TowerManager.instance.clearedBosses = new List<string>(data.clearedBosses ?? new List<string>());
        }

        // 플레이어 상태
        if (HeroPortrait.playerHero != null)
        {
            if (HeroPortrait.playerHero.heroData != null)
                HeroPortrait.playerHero.heroData.maxHealth = data.playerMaxHP;
            HeroPortrait.playerHero.currentHealth = data.playerCurrentHP;
            HeroPortrait.playerHero.currentLust = data.playerCurrentLust;
            HeroPortrait.playerHero.UpdateUI();
        }

        // 컬렉션 및 덱
        if (PlayerCollection.instance != null)
        {
            PlayerCollection.instance.ownedCards = new List<CollectionEntry>(data.ownedCards ?? new List<CollectionEntry>());
            PlayerCollection.instance.savedDecks = new List<DeckData>(data.savedDecks ?? new List<DeckData>());
            PlayerCollection.instance.activeDeckIndex = data.activeDeckIndex;
        }

        // 페티시 상태
        if (PlayerFetishState.instance != null)
        {
            PlayerFetishState.instance.hasSelectedInitialFetish = data.hasSelectedInitialFetish;
            PlayerFetishState.instance.initialFetish = data.initialFetish;
            PlayerFetishState.instance.fetishStates = new List<FetishState>(data.fetishStates ?? new List<FetishState>());
        }

        // 스토리 플래그
        if (StoryManager.instance != null)
        {
            StoryManager.instance.storyFlags = new Dictionary<string, int>(data.storyFlags ?? new Dictionary<string, int>());
            StoryManager.instance.completedEventIds = new List<string>(data.completedEventIds ?? new List<string>());
        }

        // 갤러리 해금
        if (GalleryManager.instance != null)
        {
            GalleryManager.instance.unlockedEntryIds = new List<string>(data.unlockedGalleryIds ?? new List<string>());
        }

        // 정화 상태
        if (PurificationManager.instance != null)
        {
            PurificationManager.instance.playerGold = data.playerGold;
        }

        // 튜토리얼
        if (TutorialManager.instance != null)
        {
            TutorialManager.instance.hasCompletedTutorial = data.tutorialCompleted;
        }
    }

    /// <summary>
    /// 슬롯에 저장
    /// </summary>
    public bool SaveToSlot(int slotIndex)
    {
        try
        {
            SaveData data = CollectSaveData();
            string json = JsonUtility.ToJson(data, true);
            string path = GetSavePath(slotIndex);

            File.WriteAllText(path, json);

            Debug.Log($"<color=green>저장 완료: 슬롯 {slotIndex}</color>");
            OnSaveCompleted?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 실패: {e.Message}");
            OnSaveError?.Invoke(e.Message);
            return false;
        }
    }

    /// <summary>
    /// 슬롯에서 로드
    /// </summary>
    public bool LoadFromSlot(int slotIndex)
    {
        try
        {
            string path = GetSavePath(slotIndex);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"저장 파일이 없습니다: 슬롯 {slotIndex}");
                return false;
            }

            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            ApplySaveData(data);

            Debug.Log($"<color=cyan>로드 완료: 슬롯 {slotIndex}</color>");
            OnLoadCompleted?.Invoke();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"로드 실패: {e.Message}");
            OnSaveError?.Invoke(e.Message);
            return false;
        }
    }

    /// <summary>
    /// 저장 파일 존재 여부
    /// </summary>
    public bool HasSaveInSlot(int slotIndex)
    {
        return File.Exists(GetSavePath(slotIndex));
    }

    /// <summary>
    /// 슬롯 정보 가져오기
    /// </summary>
    public SaveSlotInfo GetSlotInfo(int slotIndex)
    {
        string path = GetSavePath(slotIndex);

        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            SaveData data = JsonUtility.FromJson<SaveData>(json);

            return new SaveSlotInfo
            {
                slotIndex = slotIndex,
                saveTime = data.saveTime,
                currentFloor = data.currentFloor,
                playerHP = data.playerCurrentHP,
                playerMaxHP = data.playerMaxHP
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 저장 삭제
    /// </summary>
    public void DeleteSave(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"저장 삭제: 슬롯 {slotIndex}");
        }
    }

    string GetSavePath(int slotIndex)
    {
        return Path.Combine(SavePath, $"{saveFileName}_{slotIndex}.json");
    }
}

/// <summary>
/// 저장 데이터 구조
/// </summary>
[System.Serializable]
public class SaveData
{
    // 메타 정보
    public string saveTime;
    public string version;

    // 탑 진행
    public int currentFloor;
    public int currentNodeIndex;
    public List<string> visitedNodeIds;
    public List<string> clearedBosses;

    // 플레이어 상태
    public int playerMaxHP;
    public int playerCurrentHP;
    public int playerCurrentLust;
    public int playerGold;

    // 컬렉션/덱
    public List<CollectionEntry> ownedCards;
    public List<DeckData> savedDecks;
    public int activeDeckIndex;

    // 페티시
    public bool hasSelectedInitialFetish;
    public FetishType initialFetish;
    public List<FetishState> fetishStates;

    // 스토리
    public Dictionary<string, int> storyFlags;
    public List<string> completedEventIds;

    // 갤러리
    public List<string> unlockedGalleryIds;

    // 튜토리얼
    public bool tutorialCompleted;
}

/// <summary>
/// 저장 슬롯 정보 (UI용)
/// </summary>
[System.Serializable]
public class SaveSlotInfo
{
    public int slotIndex;
    public string saveTime;
    public int currentFloor;
    public int playerHP;
    public int playerMaxHP;
}

