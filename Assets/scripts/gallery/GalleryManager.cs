using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 갤러리 (회상방) 관리자
/// 해금된 일러스트, 씬, 이벤트 다시 보기
/// </summary>
public class GalleryManager : MonoBehaviour
{
    public static GalleryManager instance;

    [Header("갤러리 항목")]
    public List<GalleryEntry> allEntries = new List<GalleryEntry>();

    [Header("해금 상태")]
    public List<string> unlockedEntryIds = new List<string>();

    [Header("UI 참조")]
    public GalleryUI galleryUI;

    // 캐싱
    private Dictionary<string, GalleryEntry> _entryCache;

    // 이벤트
    public event Action<GalleryEntry> OnEntryUnlocked;
    public event Action<GalleryEntry> OnEntryViewed;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            BuildCache();
        }
        else Destroy(gameObject);
    }

    void BuildCache()
    {
        _entryCache = new Dictionary<string, GalleryEntry>();
        foreach (var entry in allEntries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.entryId))
            {
                _entryCache[entry.entryId] = entry;
            }
        }
    }

    /// <summary>
    /// 갤러리 열기
    /// </summary>
    public void OpenGallery()
    {
        if (galleryUI != null)
        {
            galleryUI.Show(allEntries, unlockedEntryIds);
        }
    }

    /// <summary>
    /// 항목 해금
    /// </summary>
    public void UnlockEntry(string entryId)
    {
        if (string.IsNullOrEmpty(entryId)) return;
        if (unlockedEntryIds.Contains(entryId)) return;

        unlockedEntryIds.Add(entryId);

        if (_entryCache.TryGetValue(entryId, out var entry))
        {
            OnEntryUnlocked?.Invoke(entry);
            Debug.Log($"<color=magenta>갤러리 해금: {entry.displayName}</color>");
        }

        // 자동 저장
        SaveUnlockedEntries();
    }

    /// <summary>
    /// 해금 여부 확인
    /// </summary>
    public bool IsUnlocked(string entryId)
    {
        return unlockedEntryIds.Contains(entryId);
    }

    /// <summary>
    /// 항목 보기
    /// </summary>
    public void ViewEntry(string entryId)
    {
        if (!IsUnlocked(entryId))
        {
            Debug.Log("이 항목은 아직 해금되지 않았습니다.");
            return;
        }

        if (_entryCache.TryGetValue(entryId, out var entry))
        {
            OnEntryViewed?.Invoke(entry);

            switch (entry.entryType)
            {
                case GalleryEntryType.Illustration:
                    if (galleryUI != null)
                        galleryUI.ShowIllustration(entry);
                    break;

                case GalleryEntryType.Scene:
                    PlayScene(entry);
                    break;

                case GalleryEntryType.StoryEvent:
                    PlayStoryEvent(entry);
                    break;

                case GalleryEntryType.DefeatScene:
                    PlayDefeatScene(entry);
                    break;

                case GalleryEntryType.MasturbationScene:
                    PlayMasturbationScene(entry);
                    break;
            }
        }
    }

    void PlayScene(GalleryEntry entry)
    {
        // VN 스타일 씬 재생
        if (entry.sceneData != null && StoryManager.instance != null)
        {
            StoryManager.instance.StartStory(entry.sceneData);
        }
    }

    void PlayStoryEvent(GalleryEntry entry)
    {
        if (entry.storyEvent != null && StoryManager.instance != null)
        {
            StoryManager.instance.StartStory(entry.storyEvent);
        }
    }

    void PlayDefeatScene(GalleryEntry entry)
    {
        if (entry.defeatScene != null && DefeatSceneManager.instance != null)
        {
            DefeatSceneManager.instance.PlaySceneById(entry.defeatScene.sceneId);
        }
    }

    void PlayMasturbationScene(GalleryEntry entry)
    {
        // TODO: MasturbationManager에서 직접 재생
        Debug.Log($"자위씬 재생: {entry.displayName}");
    }

    /// <summary>
    /// 카테고리별 항목 가져오기
    /// </summary>
    public List<GalleryEntry> GetEntriesByCategory(GalleryCategory category)
    {
        return allEntries.FindAll(e => e.category == category);
    }

    /// <summary>
    /// 해금률 계산
    /// </summary>
    public float GetUnlockProgress()
    {
        if (allEntries.Count == 0) return 0f;
        return (float)unlockedEntryIds.Count / allEntries.Count;
    }

    /// <summary>
    /// 해금 상태 저장 (PlayerPrefs)
    /// </summary>
    void SaveUnlockedEntries()
    {
        string json = JsonUtility.ToJson(new GalleryUnlockData { unlockedIds = unlockedEntryIds });
        PlayerPrefs.SetString("GalleryUnlocks", json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 해금 상태 로드
    /// </summary>
    public void LoadUnlockedEntries()
    {
        string json = PlayerPrefs.GetString("GalleryUnlocks", "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                GalleryUnlockData data = JsonUtility.FromJson<GalleryUnlockData>(json);
                unlockedEntryIds = data.unlockedIds ?? new List<string>();
            }
            catch
            {
                unlockedEntryIds = new List<string>();
            }
        }
    }

    void Start()
    {
        LoadUnlockedEntries();
    }
}

[System.Serializable]
public class GalleryUnlockData
{
    public List<string> unlockedIds;
}

/// <summary>
/// 갤러리 항목 데이터
/// </summary>
[CreateAssetMenu(fileName = "GalleryEntry", menuName = "Kiwi Card Game/Gallery Entry")]
public class GalleryEntry : ScriptableObject
{
    [Header("기본 정보")]
    public string entryId;
    public string displayName;
    
    [TextArea(2, 4)]
    public string description;

    [Header("분류")]
    public GalleryCategory category;
    public GalleryEntryType entryType;

    [Header("썸네일")]
    public Sprite thumbnail;

    [Header("일러스트")]
    public Sprite[] illustrations;

    [Header("씬 데이터")]
    public StoryData sceneData;
    public StoryData storyEvent;
    public DefeatSceneData defeatScene;

    [Header("조건")]
    public string unlockHint;           // 해금 힌트 (잠긴 상태에서 표시)
    public bool isHidden = false;       // 완전히 숨김 (해금 전까지)
}

public enum GalleryCategory
{
    Character,      // 캐릭터별
    Boss,           // 보스별
    Event,          // 이벤트
    Defeat,         // 패배씬
    Masturbation,   // 자위씬
    Ending,         // 엔딩
    Extra           // 기타
}

public enum GalleryEntryType
{
    Illustration,       // 일러스트만
    Scene,              // VN 스타일 씬
    StoryEvent,         // 스토리 이벤트
    DefeatScene,        // 패배씬
    MasturbationScene,  // 자위씬
    Movie               // 동영상 (미구현)
}

