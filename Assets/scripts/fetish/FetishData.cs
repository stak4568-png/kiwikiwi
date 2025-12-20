using UnityEngine;

/// <summary>
/// 페티시 정의 데이터
/// 유혹 공격에 속성 태그를 부여하고, 플레이어가 약점을 갖게 됨
/// </summary>
[CreateAssetMenu(fileName = "Fetish", menuName = "Kiwi Card Game/Fetish Data")]
public class FetishData : ScriptableObject
{
    [Header("기본 정보")]
    public FetishType fetishType;
    public string displayName;
    
    [TextArea(2, 4)]
    public string description;

    [Header("비주얼")]
    public Sprite icon;
    public Color themeColor = Color.magenta;

    [Header("효과")]
    [Tooltip("이 페티시에 약한 경우 추가 EP 상승량 (%)")]
    [Range(0, 100)]
    public int bonusLustPercent = 50;

    [Tooltip("이 페티시에 약한 경우 마나 방어 불가")]
    public bool disableManaDefense = true;

    [Tooltip("최대 강도 (0~3)")]
    public int maxIntensity = 3;

    [Header("획득 조건")]
    [Tooltip("같은 유혹을 몇 회 받으면 페티시 획득")]
    public int acquisitionThreshold = 3;

    [Header("정화")]
    [Tooltip("정화에 필요한 비용 (골드)")]
    public int purificationCost = 100;

    [Tooltip("정화 실패 확률 (%)")]
    [Range(0, 100)]
    public int purificationFailChance = 20;
}

