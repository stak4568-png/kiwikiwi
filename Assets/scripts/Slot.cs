using UnityEngine;

/// <summary>
/// 필드 슬롯 좌표 시스템
/// 좌표 기반으로 슬롯을 관리하고 거리 계산을 제공합니다.
/// </summary>
[System.Serializable]
public struct Slot
{
    public int x; // 슬롯 위치 (1부터 시작, 1-5)
    public int y; // 행 위치 (현재는 1만 사용, 나중에 확장 가능)
    public int p; // 플레이어 ID (0: 플레이어, 1: 적)

    // 슬롯 범위 설정
    public static int x_min = 1; // 최소 x (0은 무효 슬롯)
    public static int x_max = 5; // 최대 x (슬롯 개수)
    public static int y_min = 1; // 최소 y
    public static int y_max = 1; // 최대 y (현재는 1행만 사용)

    public Slot(int x, int y, int p)
    {
        this.x = x;
        this.y = y;
        this.p = p;
    }

    public Slot(int playerId)
    {
        this.x = 0;
        this.y = 0;
        this.p = playerId;
    }

    /// <summary>
    /// X축 거리 체크
    /// </summary>
    public bool IsInRangeX(Slot slot, int range)
    {
        return Mathf.Abs(x - slot.x) <= range;
    }

    /// <summary>
    /// Y축 거리 체크
    /// </summary>
    public bool IsInRangeY(Slot slot, int range)
    {
        return Mathf.Abs(y - slot.y) <= range;
    }

    /// <summary>
    /// 플레이어 거리 체크
    /// </summary>
    public bool IsInRangeP(Slot slot, int range)
    {
        return Mathf.Abs(p - slot.p) <= range;
    }

    /// <summary>
    /// 직선 거리 체크 (대각선 = 2 거리)
    /// </summary>
    public bool IsInDistanceStraight(Slot slot, int dist)
    {
        int r = Mathf.Abs(x - slot.x) + Mathf.Abs(y - slot.y) + Mathf.Abs(p - slot.p);
        return r <= dist;
    }

    /// <summary>
    /// 거리 체크 (대각선 = 1 거리)
    /// </summary>
    public bool IsInDistance(Slot slot, int dist)
    {
        int dx = Mathf.Abs(x - slot.x);
        int dy = Mathf.Abs(y - slot.y);
        int dp = Mathf.Abs(p - slot.p);
        return dx <= dist && dy <= dist && dp <= dist;
    }

    /// <summary>
    /// 플레이어 슬롯인지 확인 (x=0, y=0은 특수 슬롯)
    /// </summary>
    public bool IsPlayerSlot()
    {
        return x == 0 && y == 0;
    }

    /// <summary>
    /// 유효한 슬롯인지 확인
    /// </summary>
    public bool IsValid()
    {
        return x >= x_min && x <= x_max && y >= y_min && y <= y_max && p >= 0;
    }

    /// <summary>
    /// 랜덤 슬롯 가져오기 (플레이어 측)
    /// </summary>
    public static Slot GetRandom(int playerId, System.Random rand = null)
    {
        if (rand == null) rand = new System.Random();
        int p = playerId;
        if (y_max > y_min)
            return new Slot(rand.Next(x_min, x_max + 1), rand.Next(y_min, y_max + 1), p);
        return new Slot(rand.Next(x_min, x_max + 1), y_min, p);
    }

    /// <summary>
    /// 랜덤 슬롯 가져오기 (모든 슬롯)
    /// </summary>
    public static Slot GetRandom(System.Random rand = null)
    {
        if (rand == null) rand = new System.Random();
        if (y_max > y_min)
            return new Slot(rand.Next(x_min, x_max + 1), rand.Next(y_min, y_max + 1), rand.Next(0, 2));
        return new Slot(rand.Next(x_min, x_max + 1), y_min, rand.Next(0, 2));
    }

    /// <summary>
    /// 특정 좌표의 슬롯 가져오기
    /// </summary>
    public static Slot Get(int x, int y, int p)
    {
        return new Slot(x, y, p);
    }

    /// <summary>
    /// 플레이어의 모든 슬롯 가져오기
    /// </summary>
    public static System.Collections.Generic.List<Slot> GetAll(int playerId)
    {
        var list = new System.Collections.Generic.List<Slot>();
        int p = playerId;
        for (int y = y_min; y <= y_max; y++)
        {
            for (int x = x_min; x <= x_max; x++)
            {
                list.Add(new Slot(x, y, p));
            }
        }
        return list;
    }

    /// <summary>
    /// 모든 유효한 슬롯 가져오기
    /// </summary>
    public static System.Collections.Generic.List<Slot> GetAll()
    {
        var list = new System.Collections.Generic.List<Slot>();
        for (int p = 0; p <= 1; p++)
        {
            for (int y = y_min; y <= y_max; y++)
            {
                for (int x = x_min; x <= x_max; x++)
                {
                    list.Add(new Slot(x, y, p));
                }
            }
        }
        return list;
    }

    public static bool operator ==(Slot slot1, Slot slot2)
    {
        return slot1.x == slot2.x && slot1.y == slot2.y && slot1.p == slot2.p;
    }

    public static bool operator !=(Slot slot1, Slot slot2)
    {
        return slot1.x != slot2.x || slot1.y != slot2.y || slot1.p != slot2.p;
    }

    public override bool Equals(object o)
    {
        if (o is Slot)
            return this == (Slot)o;
        return false;
    }

    public override int GetHashCode()
    {
        return x * 100 + y * 10 + p;
    }

    /// <summary>
    /// 무효 슬롯
    /// </summary>
    public static Slot None
    {
        get { return new Slot(0, 0, 0); }
    }

    public override string ToString()
    {
        return $"Slot(x:{x}, y:{y}, p:{p})";
    }
}

