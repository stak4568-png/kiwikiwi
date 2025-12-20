using UnityEngine;

[CreateAssetMenu(fileName = "New Climax Event", menuName = "Kiwi Card Game/Special Event/Climax Data")]
public class ClimaxEventData : ScriptableObject
{
    [Header("1. 이벤트 기본 정보")]
    public string eventTitle;
    public Sprite climaxIllustration;
    [TextArea(2, 4)]
    public string[] dialogueLines;

    [Header("2. 운명의 선택지 버튼 텍스트")]
    public string acceptButtonText = "굴복한다";
    public string rejectButtonText = "저항한다";

    [Header("3. [수락] 시 연출 및 효과")]
    public Sprite acceptArt;
    [TextArea(2, 4)]
    public string[] acceptDialogues;
    public int acceptLustReduction = 100;
    public int acceptManaLockTurns = 2;     // 수락 시 마나 잠금 턴

    [Header("4. [거절] 시 연출 및 효과")]
    public Sprite rejectArt;
    [TextArea(2, 4)]
    public string[] rejectDialogues;
    public int rejectLustReduction = 30;
    public int rejectManaLockTurns = 1;     // ★ 거절 시 마나 잠금 턴 (체력 피해는 삭제됨)
}