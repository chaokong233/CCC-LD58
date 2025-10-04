using System;
using System.Collections.Generic;
using UnityEngine;

public enum DebtCollectionMethod
{
    None,
    Violent,    // 暴力讨债
    Gentle,     // 温和催债
    Legal       // 法律催债
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
    public float currentCollectionCooldown = 8.0f; // 当前CD

    public float baseCollectionValue = 15.0f; // 基础产出值
    public float currentCollectionRate = 1.0f; // 当前收账率 0-1
    public float baseCollectionRate = 0.8f; // 基础收账率 0-1
    public float targetCollectionRate = 1f; // 目标收账率 0-1

    public float resistanceLevel = 0.3f; // 反抗度 0-1
    public float supportLevel = 0.1f; // 支持度 0-1
    public float supportLevel_temp = 0.1f; // 不受联结度影响前的支持度
    public float unioLevel = 0.0f; // 联结度 0-1


    public DebtCollectionMethod currentCollectionMethod = DebtCollectionMethod.None;
    private GameManager gameManager_;

    [Header("相邻地块")]
    public List<HexTile> neighbors = new List<HexTile>();

    [Header("可视化组件")]
    public SpriteRenderer spriteRenderer;
    public Color unlockedColor = Color.white;
    public Color lockedColor = Color.gray;

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
    private static readonly float resistanceReduceFactor = 100f / 180f;
    

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (gameManager_ == null)
            gameManager_ = FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        if(isUnlocked)
        {
            UpdateAttribute();
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
    //public void ExecuteDebtCollection(DebtCollectionMethod method)
    //{
    //    if (!isUnlocked) return;

    //    currentCollectionMethod = method;
    //    currentCollectionCooldown = 3f; // 3秒冷却

    //    float baseCollection = 0f;
    //    float resistanceChange = 0f;

    //    switch (method)
    //    {
    //        case DebtCollectionMethod.Violent:
    //            baseCollection = 0.7f;
    //            resistanceChange = 0.4f;
    //            break;
    //        case DebtCollectionMethod.Gentle:
    //            baseCollection = 0.3f;
    //            resistanceChange = -0.1f;
    //            break;
    //        case DebtCollectionMethod.Legal:
    //            baseCollection = 0.5f;
    //            resistanceChange = 0.1f;
    //            break;
    //    }

    //    // 计算联结度加成
    //    float connectionBonus = CalculateConnectionBonus();
    //    float finalCollectionRate = Mathf.Clamp(baseCollection + connectionBonus, 0.1f, 0.9f);

    //    // 更新反抗度
    //    resistanceLevel = Mathf.Clamp(resistanceLevel + resistanceChange, 0f, 1f);
    //}

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
        // 反抗度（民怨值）
        resistanceLevel = Math.Max(resistanceLevel - resistanceReduceFactor * Time.deltaTime, 0);

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
            spriteRenderer.color = lockedColor;
        }
        else
        {
            // 根据债务比例混合颜色
            //float debtRatio = debtAmount / Mathf.Max(investmentAmount, 1f);
            //Color baseColor = ;
            //Color targetColor = Color.Lerp(baseColor, debtColor, debtRatio);
            spriteRenderer.color = unlockedColor;
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