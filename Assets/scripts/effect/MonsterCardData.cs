using UnityEngine;

[CreateAssetMenu(fileName = "NewMonster", menuName = "TCG/Cards/Monster")]
public class MonsterCardData : CardData // CardData를 상속받음
{
    [Header("3. 몬스터 스탯")]
    public int attack;
    public int health;
    public int lustAttack;

    [Header("4. 특수 능력")]
    public bool hasTaunt;
    public bool hasCharge;
}