using UnityEngine;

/// <summary>
/// 자위 항복 씬 데이터
/// 적 하수인/영웅을 선택하여 자위할 때 재생되는 씬
/// </summary>
[CreateAssetMenu(fileName = "MasturbationScene", menuName = "Kiwi Card Game/Masturbation Scene")]
public class MasturbationSceneData : ScriptableObject
{
    [Header("기본 정보")]
    public string sceneId;
    public string sceneTitle;

    [Header("연결 대상 (둘 중 하나만 설정)")]
    [Tooltip("하수인 카드와 연결")]
    public CardData targetCard;
    [Tooltip("영웅과 연결")]
    public HeroData targetHero;

    [Header("일반 씬 (아트 해금 상태)")]
    [Tooltip("전체 일러스트가 해금된 상태에서 보여지는 씬")]
    public MasturbationPage[] normalPages;

    [Header("검열 씬 (아트 미해금 상태)")]
    [Tooltip("실루엣 상태에서 보여지는 특수 씬")]
    public MasturbationPage[] censoredPages;
    [TextArea(2, 4)]
    public string censoredDescription = "정체를 알 수 없는 그 존재에게 굴복한다...";

    [Header("결과")]
    [Tooltip("EP 증가량")]
    public int lustGain = 30;
    [Tooltip("true면 패배 처리")]
    public bool triggersDefeat = true;
    [Tooltip("획득하는 페티시 (있다면)")]
    public FetishType acquiredFetish = FetishType.None;

    /// <summary>
    /// 아트 해금 상태에 따라 적절한 페이지 반환
    /// </summary>
    public MasturbationPage[] GetPages(bool isArtRevealed)
    {
        if (isArtRevealed && normalPages != null && normalPages.Length > 0)
            return normalPages;
        if (!isArtRevealed && censoredPages != null && censoredPages.Length > 0)
            return censoredPages;
        // 폴백: 있는 것 반환
        return normalPages ?? censoredPages;
    }
}

/// <summary>
/// 자위씬의 한 페이지
/// </summary>
[System.Serializable]
public class MasturbationPage
{
    [Tooltip("일러스트")]
    public Sprite illustration;

    [TextArea(3, 5)]
    [Tooltip("대사/나레이션")]
    public string dialogue;

    [Tooltip("화자 이름 (빈 값 = 나레이션)")]
    public string speakerName;

    [Tooltip("음성 (선택)")]
    public AudioClip voiceLine;
}

/// <summary>
/// 페티시 타입 정의 (임시 - 나중에 FetishData.cs로 이동)
/// </summary>
public enum FetishType
{
    None = 0,

    // 행위 계열
    Tentacle,       // 촉수
    Bondage,        // 구속/속박
    Hypnosis,       // 최면/세뇌
    Corruption,     // 타락/오염

    // 신체 계열
    Breast,         // 가슴
    Feet,           // 발
    Ass,            // 엉덩이

    // 상황 계열
    Public,         // 노출/공개
    Monster,        // 몬스터
    Femdom,         // 여성 지배
}

