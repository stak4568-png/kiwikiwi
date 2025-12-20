using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자위 항복 시스템 관리자
/// EP 70% 이상에서 적 하수인/영웅을 선택하여 자위 가능
/// </summary>
public class MasturbationManager : MonoBehaviour
{
    public static MasturbationManager instance;

    [Header("상태")]
    public bool isSelectingTarget = false;
    public bool isPlayingScene = false;

    [Header("기본 씬 (전용 씬이 없을 때 사용)")]
    public MasturbationSceneData defaultCardScene;
    public MasturbationSceneData defaultHeroScene;

    [Header("카드별 전용 씬")]
    [Tooltip("카드 ID를 키로 사용")]
    public List<MasturbationSceneData> cardScenes = new List<MasturbationSceneData>();

    [Header("영웅별 전용 씬")]
    public List<MasturbationSceneData> heroScenes = new List<MasturbationSceneData>();

    [Header("UI 참조")]
    public MasturbationSceneUI sceneUI;

    // 캐싱용 딕셔너리
    private Dictionary<string, MasturbationSceneData> _cardSceneCache;
    private Dictionary<string, MasturbationSceneData> _heroSceneCache;

    // 현재 선택된 대상
    private CardDisplay _selectedCard;
    private HeroPortrait _selectedHero;

    // 이벤트
    public event Action OnMasturbationStarted;
    public event Action OnMasturbationEnded;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        BuildSceneCache();
    }

    void BuildSceneCache()
    {
        _cardSceneCache = new Dictionary<string, MasturbationSceneData>();
        _heroSceneCache = new Dictionary<string, MasturbationSceneData>();

        foreach (var scene in cardScenes)
        {
            if (scene != null && scene.targetCard != null)
                _cardSceneCache[scene.targetCard.id] = scene;
        }

        foreach (var scene in heroScenes)
        {
            if (scene != null && scene.targetHero != null)
                _heroSceneCache[scene.targetHero.name] = scene;
        }
    }

    /// <summary>
    /// 자위 커맨드 사용 가능 여부
    /// </summary>
    public bool CanUseMasturbation()
    {
        if (isSelectingTarget || isPlayingScene) return false;
        return GameManager.instance != null && GameManager.instance.CanUseMasturbation();
    }

    /// <summary>
    /// 자위 버튼 클릭 - 타겟 선택 모드 진입
    /// </summary>
    public void OnMasturbationButtonClicked()
    {
        if (!CanUseMasturbation())
        {
            Debug.Log("자위 사용 불가: EP 70% 이상이어야 합니다.");
            return;
        }

        EnterTargetSelectionMode();
    }

    /// <summary>
    /// 타겟 선택 모드 진입
    /// </summary>
    public void EnterTargetSelectionMode()
    {
        isSelectingTarget = true;
        _selectedCard = null;
        _selectedHero = null;

        Debug.Log("<color=magenta>자위 대상을 선택하세요...</color>");

        // UI에 선택 모드 표시 (하이라이트 등)
        if (sceneUI != null)
            sceneUI.ShowTargetSelectionUI();
    }

    /// <summary>
    /// 타겟 선택 취소
    /// </summary>
    public void CancelTargetSelection()
    {
        isSelectingTarget = false;
        _selectedCard = null;
        _selectedHero = null;

        if (sceneUI != null)
            sceneUI.HideTargetSelectionUI();
    }

    /// <summary>
    /// 적 카드가 클릭됨 (CardDisplay에서 호출)
    /// </summary>
    public void OnEnemyCardSelected(CardDisplay card)
    {
        if (!isSelectingTarget) return;
        if (card == null || card.isMine) return;

        _selectedCard = card;
        isSelectingTarget = false;

        if (sceneUI != null)
            sceneUI.HideTargetSelectionUI();

        StartCoroutine(PlayMasturbationScene(card));
    }

    /// <summary>
    /// 적 영웅이 클릭됨 (HeroPortrait에서 호출)
    /// </summary>
    public void OnEnemyHeroSelected(HeroPortrait hero)
    {
        if (!isSelectingTarget) return;
        if (hero == null || hero.isPlayerHero) return;

        _selectedHero = hero;
        isSelectingTarget = false;

        if (sceneUI != null)
            sceneUI.HideTargetSelectionUI();

        StartCoroutine(PlayMasturbationSceneForHero(hero));
    }

    /// <summary>
    /// 카드 대상 자위씬 재생
    /// </summary>
    IEnumerator PlayMasturbationScene(CardDisplay card)
    {
        isPlayingScene = true;
        OnMasturbationStarted?.Invoke();

        // 씬 데이터 가져오기
        MasturbationSceneData sceneData = GetSceneForCard(card.data);
        if (sceneData == null)
        {
            Debug.LogWarning("자위씬 데이터가 없습니다.");
            isPlayingScene = false;
            yield break;
        }

        // 아트 해금 상태 확인
        bool isArtRevealed = card.isArtRevealed;
        MasturbationPage[] pages = sceneData.GetPages(isArtRevealed);

        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("자위씬 페이지가 없습니다.");
            isPlayingScene = false;
            yield break;
        }

        // UI로 씬 재생
        if (sceneUI != null)
        {
            yield return sceneUI.PlayScene(sceneData, pages, isArtRevealed);
        }

        // 결과 처리
        ApplyMasturbationResult(sceneData);

        isPlayingScene = false;
        OnMasturbationEnded?.Invoke();
    }

    /// <summary>
    /// 영웅 대상 자위씬 재생
    /// </summary>
    IEnumerator PlayMasturbationSceneForHero(HeroPortrait hero)
    {
        isPlayingScene = true;
        OnMasturbationStarted?.Invoke();

        // 씬 데이터 가져오기
        MasturbationSceneData sceneData = GetSceneForHero(hero.heroData);
        if (sceneData == null)
        {
            Debug.LogWarning("영웅 자위씬 데이터가 없습니다.");
            isPlayingScene = false;
            yield break;
        }

        // 영웅은 항상 아트 해금 상태로 처리 (또는 별도 체크)
        bool isArtRevealed = true;
        MasturbationPage[] pages = sceneData.GetPages(isArtRevealed);

        if (pages == null || pages.Length == 0)
        {
            Debug.LogWarning("자위씬 페이지가 없습니다.");
            isPlayingScene = false;
            yield break;
        }

        // UI로 씬 재생
        if (sceneUI != null)
        {
            yield return sceneUI.PlayScene(sceneData, pages, isArtRevealed);
        }

        // 결과 처리
        ApplyMasturbationResult(sceneData);

        isPlayingScene = false;
        OnMasturbationEnded?.Invoke();
    }

    /// <summary>
    /// 자위 결과 적용
    /// </summary>
    void ApplyMasturbationResult(MasturbationSceneData sceneData)
    {
        // EP 증가
        if (HeroPortrait.playerHero != null)
        {
            HeroPortrait.playerHero.TakeLustDamage(sceneData.lustGain, true);
        }

        // 페티시 획득
        if (sceneData.acquiredFetish != FetishType.None)
        {
            // TODO: PlayerFetishState에 페티시 추가
            Debug.Log($"<color=magenta>페티시 획득: {sceneData.acquiredFetish}</color>");
        }

        // 패배 처리
        if (sceneData.triggersDefeat)
        {
            Debug.Log("<color=red>자위 항복으로 패배...</color>");
            // TODO: DefeatSceneManager 호출
        }
    }

    /// <summary>
    /// 카드에 맞는 씬 데이터 가져오기
    /// </summary>
    MasturbationSceneData GetSceneForCard(CardData card)
    {
        if (card == null) return defaultCardScene;

        // 전용 씬 확인
        if (_cardSceneCache.TryGetValue(card.id, out var scene))
            return scene;

        // 기본 씬 반환
        return defaultCardScene;
    }

    /// <summary>
    /// 영웅에 맞는 씬 데이터 가져오기
    /// </summary>
    MasturbationSceneData GetSceneForHero(HeroData hero)
    {
        if (hero == null) return defaultHeroScene;

        // 전용 씬 확인
        if (_heroSceneCache.TryGetValue(hero.name, out var scene))
            return scene;

        // 기본 씬 반환
        return defaultHeroScene;
    }

    void Update()
    {
        // ESC로 타겟 선택 취소
        if (isSelectingTarget && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTargetSelection();
        }
    }
}

