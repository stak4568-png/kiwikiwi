using UnityEngine;

/// </summary>
[CreateAssetMenu(fileName = "Temptation", menuName = "TcgEngine/TemptationData", order = 10)]
public class TemptationData : ScriptableObject
{
    [Header("기본 설정")]
    [Tooltip("유혹 발동까지 걸리는 시간(초)")]
    public float triggerTime = 3.0f;

    [Tooltip("강제 유혹까지 필요한 거절 횟수")]
    public int maxRejectCount = 3;

    [Header("단계별 일러스트")]
    [Tooltip("경고 단계 일러스트 (50% 진행 시)")]
    public Sprite warningArt;

    [Tooltip("유혹 제안 일러스트")]
    public Sprite temptationArt;

    [Tooltip("수락 시 일러스트")]
    public Sprite acceptArt;

    [Tooltip("강제 유혹 일러스트 (3회 거절 후)")]
    public Sprite forcedArt;

    [Header("대사")]
    [Tooltip("유혹 제안 대사")]
    [TextArea(2, 4)]
    public string temptationDialogue = "날 원하는 거 아니야...?";

    [Tooltip("거절 시 대사들 (횟수별로 다르게)")]
    [TextArea(2, 4)]
    public string[] rejectDialogues = new string[]
    {
        "아직 버틸 수 있다고 생각해...?",
        "점점 힘들어지고 있잖아...",
        "이제 더는 도망칠 수 없어♥"
    };

    [Tooltip("수락 시 대사")]
    [TextArea(2, 4)]
    public string acceptDialogue = "좋아, 잘 선택했어♥";

    [Tooltip("강제 유혹 대사")]
    [TextArea(2, 4)]
    public string forcedDialogue = "더 이상 참을 수 없게 만들어줄게♥";

    [Header("선택지 텍스트")]
    public string acceptButtonText = "받아들인다...";
    public string rejectButtonText = "거부한다!";
    public string confirmButtonText = "...";  // 수락/강제 후 확인 버튼

    [Header("효과")]
    [Tooltip("수락 시 Lust 증가량")]
    public int acceptLustGain = 15;

    [Tooltip("거절 성공 시 Lust 감소량")]
    public int rejectLustReduction = 5;

    [Header("강제 유혹 이벤트")]
    [Tooltip("강제 유혹 시 Lust 증가량")]
    public int forcedLustGain = 25;

    [Tooltip("강제 유혹 버튼 텍스트")]
    public string forcedButtonText = "저항할 수 없다...";

    [Tooltip("강제 유혹 후 대사들 (순차 표시)")]
    [TextArea(2, 4)]
    public string[] forcedAfterDialogues = new string[]
    {
        "그렇지... 순순히 받아들여...",
        "이제 넌 내 거야♥"
    };

    [Tooltip("강제 유혹 후 일러스트들 (대사와 매칭)")]
    public Sprite[] forcedAfterArts;
}
