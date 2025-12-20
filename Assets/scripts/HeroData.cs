// HeroData.cs
// 영웅 데이터 ScriptableObject - 플레이어 및 적 영웅 정의

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Hero", menuName = "Kiwi Card Game/Hero Data")]
public class HeroData : ScriptableObject
{
    [Header("기본 정보")]
    public string heroId;
    public string heroName;
    [TextArea(2, 4)]
    public string description;

    [Header("아트워크")]
    public Sprite portrait;           // 영웅 초상화
    public Sprite portraitDamaged;    // 피해 입었을 때 초상화 (선택)
    public Sprite portraitSeduced;    // 유혹 상태 초상화 (선택, 플레이어용)

    [Header("기본 스탯")]
    public int maxHealth = 30;
    public int startingArmor = 0;     // 시작 방어력

    [Header("영웅 능력")]
    public HeroPowerData heroPower;   // 영웅 고유 능력

    [Header("적 영웅 전용 - 유혹 공격")]
    public bool canSeduceAttack = false;  // 유혹 공격 가능 여부
    public int seducePower = 5;           // 기본 유혹 공격력
    [TextArea(2, 3)]
    public string seduceDescription;      // 유혹 공격 설명

    [Header("시작 무기 (선택)")]
    public WeaponData startingWeapon;
}
