using System;
using System.Collections.Generic;
using UnityEngine;

public enum DebtCollectionMethod
{
    None = 0,
    Gentle = 1,     // 温和催债
    Legal = 2,      // 法律催债
    Quell = 3,      // 平息
    Violent = 4     // 暴力讨债
}

public enum QuellMethod
{
    CalmDown = 5,     // 
    Permeation = 6,      // 
}

public enum TileType
{
    Default = 0,
    City = 1,       // 城市
    Suburb = 2,     // 郊区  
    Rural = 3,      // 农村
    Lake = 4,       // 湖泊（障碍）
    Mountain = 5,   // 山地（障碍）
    MaxNum = Mountain
}

public class HexTile : MonoBehaviour
{
    [Header("地块基本信息")]
    public int q; // 六边形网格坐标Q
    public int r; // 六边形网格坐标R
    public string tileName;
    public bool isUnlocked = false;
    public bool isRebelContinent = false;
    public bool isCalmedDown = false;

    [Header("Gameplay属性")]
    public float debtCost = 100.0f; // 初次借贷价格

    public float collectionCooldown = 8.0f; // 产出CD
    public float currentCollectionCooldown = 8.0f; // 当前CD

    public float baseCollectionValue = 8.0f; // 基础产出值
    public float currentCollectionRate = 0.8f; // 当前收账率 0-1
    public float baseCollectionRate = 0.3f; // 基础收账率 0-1
    public float targetCollectionRate = 0.3f; // 目标收账率 0-1

    public float resistanceLevel = 0.1f; // 反抗度 0-1
    public float supportLevel = 0.0f; // 支持度 0-1
    public float supportLevel_temp = 0.0f; // 不受联结度影响前的支持度
    public float unioLevel = 0.0f; // 联结度 0-1(用解锁百分比代替)
    public float unioLevelFractor = 0.5f; // 联结度乘数 0-1
    public float LeverageLevel = 0.0f; // 杠杆值 0-1

    public float resistanceLevelGrowth = -0.35f / 90f; // 反抗度自然增长
    public float baseResistanceLevelGrowth = -0.35f / 90f; // 初始反抗度自然增长
    public float regionOriginFuns = 420.0f; // 地区原始资金
    public float totalGain = 0f; // 总收益资金

    public float DebtCollectionMethodCooldown = 30.0f; // 催债方式CD
    public float currentDebtCollectionMethodCooldown = 0.0f; // 当前CD

    private GameManager gameManager_;
    private HexMapGenerator mapGenerator_;
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

    [Header("0.default 1.City 2.Suburb 3.Rural 4.Lake 5.Mountain")]
    public Sprite[] TileTypeSprites = { };

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

    // 技能消耗, 第一个索引是tileType号，第二个索引是技能号
    public static readonly float[,] abilityCost = new float[4, 7]
    {
        {0,0,0,0,0,0,0},
        {0,     120f,150f,300f,50f,   2000f,1200f},
        {0,     30f,40f,70f,10f,      600f,400f},
        {0,     15f,20f,35f,0f,       300f,200f}
    };

    private static readonly float collectionRestitutionFactor = 1f / 60f;
    private static readonly float rebelContinentLerpFactor = 0.05f;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (gameManager_ == null)
            gameManager_ = FindFirstObjectByType<GameManager>();
        if (mapGenerator_ == null)
            mapGenerator_ = FindFirstObjectByType<HexMapGenerator>();
        if (floatingTextController_ == null)
            floatingTextController_ = FindFirstObjectByType<FloatingTextController>();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        if (isUnlocked)
        {
            if (!isRebelContinent)
            {
                if (currentDebtCollectionMethodCooldown > 0)
                    currentDebtCollectionMethodCooldown -= Time.deltaTime;
                UpdateAttribute();
                CalculateProduct();
            }
            else
            {
                if (!isCalmedDown)
                    UpdateRebelContinent();
                currentDebtCollectionMethodCooldown = 0;
            }

            UpdateVisuals();
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

        // Update Sprite
        int len = TileTypeSprites.Length;
        int idx = (int)type;
        if (idx <= len - 1)
            spriteRenderer.sprite = TileTypeSprites[idx];
        else if (len > 0)
            spriteRenderer.sprite = TileTypeSprites[0];

        // Update Color
        switch (type)
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

        // Initialize TileType Attribute
        switch (type)
        {
            case TileType.Rural:
                debtCost = 100.0f; // 初次借贷价格
                baseCollectionValue = 8.0f; // 基础产出值
                baseCollectionRate = 0.3f; // 基础收账率 0-1
                resistanceLevel = 0.1f; // 反抗度 0-1
                supportLevel_temp = 0.0f; // 不受联结度影响前的支持度
                unioLevelFractor = 0.55f; // 联结度影响因子
                baseResistanceLevelGrowth = -0.3f / 90f; // 初始反抗度自然增长
                regionOriginFuns = 380.0f; // 地区原始资金
                break;
            case TileType.Suburb:
                debtCost = 200.0f; // 初次借贷价格
                baseCollectionValue = 20.0f; // 基础产出值
                baseCollectionRate = 0.2f; // 基础收账率 0-1
                resistanceLevel = 0.1f; // 反抗度 0-1
                supportLevel_temp = 0.0f; // 不受联结度影响前的支持度
                unioLevelFractor = 0.3f; // 联结度影响因子
                baseResistanceLevelGrowth = -0.0025f; // 初始反抗度自然增长
                regionOriginFuns = 8000.0f; // 地区原始资金
                break;
            case TileType.City:
                debtCost = 1000.0f; // 初次借贷价格
                baseCollectionValue = 80f; // 基础产出值
                baseCollectionRate = 0.1f; // 基础收账率 0-1
                resistanceLevel = 0.1f; // 反抗度 0-1
                supportLevel_temp = 0.0f; // 不受联结度影响前的支持度
                unioLevelFractor = 0.1f; // 联结度影响因子
                baseResistanceLevelGrowth = -0.001f; // 初始反抗度自然增长
                regionOriginFuns = 5000.0f; // 地区原始资金
                break;
            case 0:
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

        gameManager_.Spending(abilityCost[(int)tileType, (int)method]); // Cost

        switch (method)
        {
            case DebtCollectionMethod.Gentle:
                currentCollectionRate = Math.Max(0.80f, currentCollectionRate);
                supportLevel_temp += 0.10f;
                resistanceLevel += 0.05f;
                break;
            case DebtCollectionMethod.Legal:
                currentCollectionRate = Math.Max(1.00f, currentCollectionRate);
                supportLevel_temp += 0.03f;
                resistanceLevel += 0.15f;
                break;
            case DebtCollectionMethod.Quell:
                currentCollectionRate = Math.Max(0.60f, currentCollectionRate);
                supportLevel += 0.08f;
                resistanceLevel += -0.60f;
                break;
            case DebtCollectionMethod.Violent:
                currentCollectionRate = Math.Max(1.20f, currentCollectionRate);
                supportLevel_temp += -0.05f;
                resistanceLevel += 0.30f;
                break;
        }
    }

    /// <summary>
    /// 执行平复
    /// </summary>
    public void ExecuteQuell(QuellMethod method)
    {
        if (!isRebelContinent) return;

        gameManager_.Spending(abilityCost[(int)tileType, (int)method]); // Cost

        switch (method)
        {
            case QuellMethod.CalmDown:
                isCalmedDown = true;
                break;
            case QuellMethod.Permeation:
                isRebelContinent = false;
                OnRecoverRebel();
                break;
        }
    }

    private void UpdateAttribute()
    {
        // 联结度（简单用解锁百分比代替）
        unioLevel = (float)(mapGenerator_.unlockedTileTypeCounter[(int)tileType]) / (float)(mapGenerator_.tileTypeCounter[(int)tileType]);

        // 杠杆, 时间乘数
        LeverageLevel = totalGain / (regionOriginFuns + totalGain * Mathf.Max(1f, 2f - gameManager_.currentTime * 0.2f / 60f));

        float LeverageLevelMutiplier = 0.0487f;
        float supportLevelLevelMutiplier = -0.033f;
        // 反抗度（民怨值）自然增长
        resistanceLevelGrowth = baseResistanceLevelGrowth + LeverageLevel * LeverageLevelMutiplier + supportLevel * supportLevelLevelMutiplier;

        // 反抗度（民怨值）
        // remap: 60~100 -> 1~0.8, 民怨值较高时，减缓增长
        float resistanceLevelMutiplier = resistanceLevelGrowth < 0.6 ? 1.0f : Mathf.Lerp(1f, 0.7f, (resistanceLevel - 0.6f) / 0.4f);
        resistanceLevel = Math.Max(resistanceLevel + resistanceLevelGrowth * resistanceLevelMutiplier * Time.deltaTime, 0);
        // 反抗度满时，成为反抗地区
        if (resistanceLevel >= 1)
        {
            OnBeRebelContinent();
            resistanceLevel = 1;
        }

        // 支持度
        supportLevel = Math.Clamp(unioLevel * unioLevelFractor + supportLevel_temp, 0, 1);

        // 目标收账率
        targetCollectionRate = Math.Clamp(baseCollectionRate + supportLevel - resistanceLevel, 0, 1);

        // 当前收账率
        float alpha = collectionRestitutionFactor * Time.deltaTime;
        currentCollectionRate = Mathf.Lerp(currentCollectionRate, targetCollectionRate, alpha);
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
            if (isRebelContinent)
            {
                Color baseColor = unlockedColor;
                Color targetColor = Color.Lerp(baseColor, resistanceColor, 0.9f);
                spriteRenderer.color = targetColor;
            }
            else
            {
                // 根据民怨度混合颜色
                Color baseColor = unlockedColor;
                Color targetColor = Color.Lerp(baseColor, resistanceColor, Math.Max(resistanceLevel - 0.2f, 0f) * 0.8f);
                spriteRenderer.color = targetColor;
            }
        }
    }

    /// <summary>
    /// 成为反抗地区
    /// </summary>
    private void OnBeRebelContinent()
    {
        isCalmedDown = false;
        isRebelContinent = true;
        floatingTextController_.ShowText(transform.position, "Rebel!", new Color(1f, 0.1f, 0.1f, 1f));
    }

    /// <summary>
    /// 恢复正常
    /// </summary>
    private void OnRecoverRebel()
    {
        // 重置属性参数
        currentCollectionCooldown = 1.0f; // 当前CD

        currentCollectionRate = 0.8f; // 当前收账率 0-1

        resistanceLevel = 0.1f; // 反抗度 0-1
        supportLevel = 0.1f; // 支持度 0-1
        supportLevel_temp = 0.5f; // 不受联结度影响前的支持度

        totalGain = 0f; // 总收益资金
    }

    /// <summary>
    /// 反抗地区影响周围
    /// </summary>
    private void UpdateRebelContinent()
    {
        float alpha = rebelContinentLerpFactor * Time.deltaTime;
        foreach (var tile in neighbors)
        {
            tile.resistanceLevel = Mathf.Lerp(tile.resistanceLevel, 1f, alpha);
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