using System;
using System.Collections.Generic;
using UnityEngine;

public enum DebtCollectionMethod
{
    None,
    Gentle,     // 温和催债
    Legal,      // 法律催债
    Quell,      // 平息
    Violent     // 暴力讨债
}

public enum TileType
{
    City = 0,       // 城市
    Suburb = 1,     // 郊区  
    Rural = 2,      // 农村
    Lake = 3,       // 湖泊（障碍）
    Mountain = 4,   // 山地（障碍）
    MaxNum = Mountain
}

public class HexTile : MonoBehaviour
{
    [Header("地块基本信息")]
    public int q; // 六边形网格坐标Q
    public int r; // 六边形网格坐标R
    public string tileName;
    public bool isUnlocked = false;

    [Header("Gameplay属性")]
    public float debtCost = 100.0f; // 初次借贷价格

    public float collectionCooldown = 8.0f; // 产出CD
    private float currentCollectionCooldown = 8.0f; // 当前CD

    public float baseCollectionValue = 15.0f; // 基础产出值
    public float currentCollectionRate = 1.0f; // 当前收账率 0-1
    public float baseCollectionRate = 0.8f; // 基础收账率 0-1
    public float targetCollectionRate = 1f; // 目标收账率 0-1

    public float resistanceLevel = 0.3f; // 反抗度 0-1
    public float supportLevel = 0.1f; // 支持度 0-1
    public float supportLevel_temp = 0.1f; // 不受联结度影响前的支持度
    public float unioLevel = 0.0f; // 联结度 0-1
    public float LeverageLevel = 0.0f; // 杠杆值 0-1

    public float resistanceLevelGrowth = -1f / 180f; // 反抗度自然增长
    public float baseResistanceLevelGrowth = -1f / 180f; // 初始反抗度自然增长
    public float regionOriginFuns = 1000.0f; // 地区原始资金
    public float totalGain = 0f; // 总收益资金

    public float DebtCollectionMethodCooldown = 30.0f; // 催债方式CD
    public float currentDebtCollectionMethodCooldown = 0.0f; // 当前CD

    private GameManager gameManager_;
    private FloatingTextController floatingTextController_;

    [Header("相邻地块")]
    public List<HexTile> neighbors = new List<HexTile>();

    [Header("可视化组件")]
    public SpriteRenderer spriteRenderer;
    public Color unlockedColor = Color.white;
    public Color cityUnlockedColor = Color.white;
    public Color suburbUnlockedColor = Color.white;
    public Color ruralUnlockedColor = Color.white;
    public Color lakeUnlockedColor = Color.white;
    public Color mountainUnlockedColor = Color.white;

    public Color resistanceColor = Color.red;
    public Color lockedColor = Color.gray;

    [Header("TileType")]
    public TileType tileType = TileType.City;
    public Sprite[] TileTypeSprites = {};

    // 六边形方向向量 (Q, R 坐标)，对于q为偶数的tile
    private static readonly Vector2Int[] hexDirections_01 =
    {
        new Vector2Int(0, 1),   // 上
        new Vector2Int(1, 1),   // 右上
        new Vector2Int(1, 0),   // 右下
        new Vector2Int(0, -1),  // 下
        new Vector2Int(-1, 1),  // 左上
        new Vector2Int(-1, 0)   // 左下
    };

    // 六边形方向向量 (Q, R 坐标)，对于q为奇数的tile
    private static readonly Vector2Int[] hexDirections =
    {
        new Vector2Int(0, 1),    // 上
        new Vector2Int(1, 0),    // 右上
        new Vector2Int(1, -1),   // 右下
        new Vector2Int(0, -1),   // 下
        new Vector2Int(-1, 0),   // 左上
        new Vector2Int(-1, -1)   // 左下
    };

    private static readonly float collectionRestitutionFactor = 1f / (3f * 8.0f);
    

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (gameManager_ == null)
            gameManager_ = FindFirstObjectByType<GameManager>();
        if (floatingTextController_ == null)
            floatingTextController_ = FindFirstObjectByType<FloatingTextController>();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        if(isUnlocked)
        {
            if (currentDebtCollectionMethodCooldown > 0)
                currentDebtCollectionMethodCooldown -= Time.deltaTime;

            UpdateAttribute();
            UpdateVisuals();
            CalculateProduct();
        }
    }

    /// <summary>
    /// 初始化地块
    /// </summary>
    public void Initialize(int qCoord, int rCoord, string name = "")
    {
        q = qCoord;
        r = rCoord;
        tileName = string.IsNullOrEmpty(name) ? $"Tile_{q}_{r}" : name;

        // 设置位置
        Vector3 worldPos = HexToWorldPosition(q, r);
        transform.position = worldPos;
    }

    /// <summary>
    /// 初始化地块类型
    /// </summary>
    public void InitializeType(TileType type)
    {
        tileType = type;
        switch(type)
        {
            case TileType.City:
                unlockedColor = cityUnlockedColor;
                break;
            case TileType.Rural:
                unlockedColor = ruralUnlockedColor;
                break;
            case TileType.Suburb:
                unlockedColor = suburbUnlockedColor;
                break;
            case TileType.Mountain:
                unlockedColor = mountainUnlockedColor;
                break;
            case TileType.Lake:
                unlockedColor = lakeUnlockedColor;
                break;
        }
    }

    public bool isObstacle()
    {
        return tileType == TileType.Lake || tileType == TileType.Mountain;
    }

    /// <summary>
    /// 解锁地块
    /// </summary>
    public void UnlockTile()
    {
        if (!isUnlocked)
        {
            isUnlocked = true;
            UpdateVisuals();
            Debug.Log($"地块 {tileName} 已解锁");
        }
    }

    /// <summary>
    /// 执行催债
    /// </summary>
    public void ExecuteDebtCollection(DebtCollectionMethod method)
    {
        if (!isUnlocked || currentDebtCollectionMethodCooldown > 0) return;

        currentDebtCollectionMethodCooldown = DebtCollectionMethodCooldown; // 冷却

        switch (method)
        {
            //case DebtCollectionMethod.Violent:
            //    baseCollection = 0.7f;
            //    resistanceChange = 0.4f;
            //    break;
            //case DebtCollectionMethod.Gentle:
            //    baseCollection = 0.3f;
            //    resistanceChange = -0.1f;
            //    break;
            //case DebtCollectionMethod.Legal:
            //    baseCollection = 0.5f;
            //    resistanceChange = 0.1f;
            //    break;
            case DebtCollectionMethod.Gentle:
                Debug.Log("1");
                break;
            case DebtCollectionMethod.Legal:
                Debug.Log("2");
                break;
            case DebtCollectionMethod.Quell:
                Debug.Log("3");
                break;
            case DebtCollectionMethod.Violent:
                Debug.Log("4");
                break;
        }
    }

    /// <summary>
    /// 计算联结度加成
    /// </summary>
    //private float CalculateConnectionBonus()
    //{
    //    int connectedUnlockedNeighbors = 0;
    //    foreach (var neighbor in neighbors)
    //    {
    //        if (neighbor != null && neighbor.isUnlocked)
    //            connectedUnlockedNeighbors++;
    //    }

    //    // 每有一个相邻解锁地块，增加5%的收账率
    //    return connectedUnlockedNeighbors * 0.05f;
    //}

    /// <summary>
    /// 更新各种属性
    /// </summary>
    private void UpdateAttribute()
    {
        // 杠杆
        LeverageLevel = totalGain / (regionOriginFuns + totalGain);

        // 反抗度（民怨值）自然增长
        resistanceLevelGrowth = baseResistanceLevelGrowth + LeverageLevel * 7f/8f;

        // 反抗度（民怨值）
        resistanceLevel = Math.Max(resistanceLevel + resistanceLevelGrowth * Time.deltaTime, 0);

        // 支持度
        supportLevel = unioLevel + supportLevel_temp;

        // 目标收账率
        targetCollectionRate = Math.Clamp(baseCollectionRate + supportLevel - resistanceLevel, 0, 1);

        // 当前收账率
        float alpha = collectionRestitutionFactor * Time.deltaTime;
        currentCollectionRate = currentCollectionRate * (1f - alpha) + targetCollectionRate * alpha;

    }

    /// <summary>
    /// 计算产出
    /// </summary>
    private void CalculateProduct()
    {
        // 更新冷却时间
        currentCollectionCooldown -= Time.deltaTime;
        // 触发收入
        if (currentCollectionCooldown <= 0)
        {
            float needIncome = baseCollectionValue * currentCollectionRate;
            gameManager_.Income(needIncome);

            var selfPos = HexToWorldPosition(q, r);
            totalGain += needIncome;
            floatingTextController_.ShowMoneyText(selfPos, needIncome);

            // Reset CD
            currentCollectionCooldown += collectionCooldown;
        }
        
    }

    /// <summary>
    /// 更新地块外观
    /// </summary>
    public void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        if (!isUnlocked)
        {
            spriteRenderer.color = Color.Lerp(lockedColor, unlockedColor, 0.2f);
        }
        else
        {
            // 根据民怨度混合颜色
            Color baseColor = unlockedColor;
            Color targetColor = Color.Lerp(baseColor, resistanceColor, Math.Max(resistanceLevel-0.2f,0f)*0.8f);
            spriteRenderer.color = targetColor;
        }
    }

    /// <summary>
    /// 获取指定方向的相邻坐标
    /// </summary>
    public static Vector2Int GetNeighborCoordinate(Vector2Int coord, int direction)
    {
        if (direction < 0 || direction >= hexDirections.Length)
        {
            Debug.LogError($"无效的方向: {direction}");
            return coord;
        }

        return coord + ((coord.x % 2 == 0) ? hexDirections[direction] : hexDirections_01[direction]);
    }

    /// <summary>
    /// 将六边形坐标转换为世界坐标
    /// </summary>
    public static Vector3 HexToWorldPosition(int q, int r)
    {
        float x = q * 1.5f; // 水平间距
        float y = r * Mathf.Sqrt(3f) + (q % 2) * (Mathf.Sqrt(3f) / 2f); // 垂直间距，交错排列

        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// 获取所有相邻坐标
    /// </summary>
    public static List<Vector2Int> GetAllNeighborCoordinates(int q, int r)
    {
        List<Vector2Int> neighborCoords = new List<Vector2Int>();

        for (int i = 0; i < hexDirections.Length; i++)
        {
            Vector2Int neighborCoord = GetNeighborCoordinate(new Vector2Int(q, r), i);
            neighborCoords.Add(neighborCoord);
        }

        return neighborCoords;
    }

}