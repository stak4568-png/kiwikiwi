using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 패배씬 관리자
/// 패배 조건에 맞는 씬 선택 및 재생
/// </summary>
public class DefeatSceneManager : MonoBehaviour
{
    public static DefeatSceneManager instance;

    [Header("패배씬 데이터베이스")]
    public List<DefeatSceneData> allDefeatScenes = new List<DefeatSceneData>();

    [Header("기본 씬")]
    public DefeatSceneData defaultHPDefeatScene;
    public DefeatSceneData defaultClimaxDefeatScene;
    public DefeatSceneData defaultSurrenderScene;

    [Header("UI 참조")]
    public DefeatSceneUI defeatUI;

    [Header("상태")]
    public bool isPlayingDefeatScene = false;

    // 이벤트
    public event Action<DefeatSceneData> OnDefeatSceneStarted;
    public event Action OnDefeatSceneCompleted;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 패배 처리 (GameManager에서 호출)
    /// </summary>
    public void OnPlayerDefeated(DefeatType type, HeroData enemy = null)
    {
        if (isPlayingDefeatScene) return;

        // 적절한 패배씬 찾기
        DefeatSceneData scene = FindBestDefeatScene(type, enemy);

        if (scene != null)
        {
            StartCoroutine(PlayDefeatScene(scene));
        }
        else
        {
            // 기본 처리
            Debug.Log("<color=red>패배...</color>");
            OnDefeatSceneCompleted?.Invoke();
        }
    }

    /// <summary>
    /// 조건에 맞는 최적의 패배씬 찾기
    /// </summary>
    DefeatSceneData FindBestDefeatScene(DefeatType type, HeroData enemy)
    {
        List<DefeatSceneData> candidates = new List<DefeatSceneData>();

        // 조건에 맞는 씬 필터링
        foreach (var scene in allDefeatScenes)
        {
            if (CheckSceneConditions(scene, type, enemy))
            {
                candidates.Add(scene);
            }
        }

        // 우선순위 정렬
        candidates.Sort((a, b) => b.priority.CompareTo(a.priority));

        if (candidates.Count > 0)
            return candidates[0];

        // 기본 씬 반환
        return type switch
        {
            DefeatType.HPZero => defaultHPDefeatScene,
            DefeatType.Climax => defaultClimaxDefeatScene,
            DefeatType.Surrender => defaultSurrenderScene,
            DefeatType.Masturbation => defaultSurrenderScene,
            _ => defaultHPDefeatScene
        };
    }

    /// <summary>
    /// 씬 조건 확인
    /// </summary>
    bool CheckSceneConditions(DefeatSceneData scene, DefeatType type, HeroData enemy)
    {
        // 패배 유형 체크
        if (scene.defeatType != DefeatType.Any && scene.defeatType != type)
            return false;

        // 특정 적 체크
        if (scene.specificEnemy != null && scene.specificEnemy != enemy)
            return false;

        // 페티시 체크
        if (scene.requiredFetish != FetishType.None)
        {
            if (PlayerFetishState.instance == null || !PlayerFetishState.instance.HasWeakness(scene.requiredFetish))
                return false;
        }

        // EP 체크
        if (scene.minimumEP > 0)
        {
            if (HeroPortrait.playerHero == null || HeroPortrait.playerHero.currentLust < scene.minimumEP)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 패배씬 재생
    /// </summary>
    IEnumerator PlayDefeatScene(DefeatSceneData scene)
    {
        isPlayingDefeatScene = true;
        OnDefeatSceneStarted?.Invoke(scene);

        Debug.Log($"<color=magenta>패배씬 재생: {scene.sceneTitle}</color>");

        // UI로 씬 재생
        if (defeatUI != null)
        {
            yield return defeatUI.PlayScene(scene);
        }

        // 결과 처리
        ProcessDefeatResults(scene);

        isPlayingDefeatScene = false;
        OnDefeatSceneCompleted?.Invoke();

        // 후속 이벤트
        if (scene.followUpEvent != null && StoryManager.instance != null)
        {
            StoryManager.instance.StartStory(scene.followUpEvent);
        }
    }

    /// <summary>
    /// 패배 결과 처리
    /// </summary>
    void ProcessDefeatResults(DefeatSceneData scene)
    {
        // 페티시 획득
        if (scene.acquiredFetish != FetishType.None)
        {
            PlayerFetishState.instance?.AcquireFetish(scene.acquiredFetish);
        }

        // 갤러리 해금
        if (!string.IsNullOrEmpty(scene.galleryUnlockId))
        {
            GalleryManager.instance?.UnlockEntry(scene.galleryUnlockId);
        }
    }

    /// <summary>
    /// 특정 씬 직접 재생
    /// </summary>
    public void PlaySceneById(string sceneId)
    {
        var scene = allDefeatScenes.Find(s => s.sceneId == sceneId);
        if (scene != null)
        {
            StartCoroutine(PlayDefeatScene(scene));
        }
    }
}

