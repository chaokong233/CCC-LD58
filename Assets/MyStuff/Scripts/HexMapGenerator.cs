using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    [Header("地图设置")]
    public int mapWidth = 10;
    public int mapHeight = 10;
    public GameObject hexTilePrefab;

    [Header("起始设置")]
    public Vector2Int startTileCoord = new Vector2Int(5, 5);

    [Header("Tile 大小")]
    public Vector2 tileSize = new Vector2(5, 5);

    [Header("生成参数")]
    [Range(1, 10)]
    public int cityClusterCount = 3;
    [Range(1, 5)]
    public int cityClusterSize = 3;
    [Range(1, 10)]
    public int obstacleClusterCount = 4;
    [Range(1, 8)]
    public int obstacleClusterSize = 4;
    [Range(1, 3)]
    public int suburbRingCount = 2;
    [Range(1, 4)]
    public int minCityCount = 1;

    [Header("地图计数")]
    public List<int> tileTypeCounter = new List<int>(((int)TileType.MaxNum)+1);
    public List<int> unlockedTileTypeCounter = new List<int>(((int)TileType.MaxNum)+1);

    private Dictionary<Vector2Int, HexTile> hexMap = new Dictionary<Vector2Int, HexTile>();
    [HideInInspector]
    public Dictionary<Vector2Int, HexTile> cityTiles = new Dictionary<Vector2Int, HexTile>();
    private List<HexTile> allTiles = new List<HexTile>();
    private System.Random random;

    void Start()
    {
        for (int i = 0; i < ((int)TileType.MaxNum) + 1; i++)
        {
            tileTypeCounter.Add(0);
            unlockedTileTypeCounter.Add(0);
        }
        GenerateHexMap();
        SetupNeighborConnections();
        UnlockStartTile();
    }

    /// <summary>
    /// 生成六边形地图
    /// </summary>
    void GenerateHexMap()
    {
        do
        {
            random = new System.Random(System.DateTime.Now.Millisecond);

            hexMap.Clear();
            cityTiles.Clear();
            foreach (var item in allTiles)
            {
                Destroy(item.gameObject);
            }
            allTiles.Clear();

            for (int q = 0; q < mapWidth; q++)
            {
                for (int r = 0; r < mapHeight; r++)
                {
                    CreateHexTile(q, r, TileType.Rural);
                }
            }

            //第二阶段：生成障碍区域（湖泊和山地）
            GenerateObstacleClusters();
            // 第三阶段：生成城市集群
            GenerateCityClusters();
            // 第四阶段：确保连通性
            EnsureConnectivity();

            // Counter
            for (int i = 0; i < tileTypeCounter.Count; i++)
            {
                tileTypeCounter[i] = 0;
            }

            foreach (var tile in hexMap)
            {
                if (tile.Value.tileType == TileType.City)
                {
                    cityTiles.Add(tile.Key, tile.Value);
                }
                tileTypeCounter[(int)tile.Value.tileType]++;
            }

        } while (tileTypeCounter[(int)TileType.City] < minCityCount);

        Debug.Log($"地图生成完成，共 {allTiles.Count} 个地块");
    }

    /// <summary>
    /// 创建指定类型的地块
    /// </summary>
    void CreateHexTile(int q, int r, TileType type, string customName = "")
    {
        if (hexTilePrefab == null)
        {
            Debug.LogError("HexTile预制体未分配！");
            return;
        }

        // 如果该坐标已有地块，先移除
        Vector2Int coord = new Vector2Int(q, r);
        if (hexMap.ContainsKey(coord))
        {
            // 如果是障碍地块，不能覆盖
            if (hexMap[coord].isObstacle())
                return;

            Destroy(hexMap[coord].gameObject);
            hexMap.Remove(coord);
            allTiles.RemoveAll(t => t.q == q && t.r == r);
        }

        GameObject tileObj = Instantiate(hexTilePrefab, new Vector3(), Quaternion.identity, transform);
        tileObj.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1);

        HexTile hexTile = tileObj.GetComponent<HexTile>();
        if (hexTile != null)
        {
            hexTile.Initialize(q, r, customName);
            hexTile.InitializeType(type);
            hexMap[coord] = hexTile;
            allTiles.Add(hexTile);
        }
        else
        {
            Debug.LogError($"在位置 ({q}, {r}) 创建的地块没有HexTile组件");
            Destroy(tileObj);
        }
    }

    bool IsValidCoordinate(Vector2Int coord)
    {
        return coord.x >= 0 && coord.y >= 0 && coord.x < mapWidth && coord.y < mapHeight;
    }

    /// <summary>
    /// 生成障碍区域（湖泊和山地）
    /// </summary>
    void GenerateObstacleClusters()
    {
        int clustersGenerated = 0;

        while (clustersGenerated < obstacleClusterCount)
        {
            // 随机选择障碍类型
            TileType obstacleType = random.Next(2) == 0 ? TileType.Lake : TileType.Mountain;

            // 随机选择起始点（确保不在边缘，避免孤立的障碍）
            int startQ = random.Next(2, mapWidth - 2);
            int startR = random.Next(2, mapHeight - 2);

            Vector2Int startCoord = new Vector2Int(startQ, startR);

            // 检查起始点是否已经是障碍
            if (hexMap.ContainsKey(startCoord) && hexMap[startCoord].isObstacle())
                continue;

            // 生成障碍集群
            if (GenerateCluster(startCoord, obstacleType, obstacleClusterSize))
            {
                clustersGenerated++;
                //Debug.Log($"生成{GetTileTypeData(obstacleType).displayName}集群 #{clustersGenerated} 在 ({startQ}, {startR})");
            }
        }
    }

    /// <summary>
    /// 生成城市集群
    /// </summary>
    void GenerateCityClusters()
    {
        int clustersGenerated = 0;

        while (clustersGenerated < cityClusterCount)
        {
            // 随机选择起始点（确保不在边缘且不是障碍）
            int startQ = random.Next(2, mapWidth - 2);
            int startR = random.Next(2, mapHeight - 2);

            Vector2Int startCoord = new Vector2Int(startQ, startR);

            // 检查起始点是否适合
            if (!IsValidForCity(startCoord))
                continue;

            // 生成城市集群
            if (GenerateCityCluster(startCoord))
            {
                clustersGenerated++;
                Debug.Log($"生成城市集群 #{clustersGenerated} 在 ({startQ}, {startR})");
            }
        }
    }

    /// <summary>
    /// 生成单个城市集群（包括城市中心和郊区环）
    /// </summary>
    bool GenerateCityCluster(Vector2Int centerCoord)
    {
        // 生成城市中心
        List<Vector2Int> cityTiles = FloodFill(centerCoord, cityClusterSize,
            coord => !hexMap[coord].isObstacle() && hexMap[coord].tileType != TileType.City);

        if (cityTiles.Count == 0) return false;

        // 将选中的地块设置为城市
        foreach (Vector2Int coord in cityTiles)
        {
            CreateHexTile(coord.x, coord.y, TileType.City, $"城市_{coord.x}_{coord.y}");
        }

        // 生成郊区环
        for (int ring = 1; ring <= suburbRingCount; ring++)
        {
            List<Vector2Int> ringTiles = GetRingTiles(cityTiles, ring);

            foreach (Vector2Int coord in ringTiles)
            {
                if (hexMap.ContainsKey(coord) && !hexMap[coord].isObstacle() &&
                    hexMap[coord].tileType != TileType.City && hexMap[coord].tileType != TileType.Suburb)
                {
                    CreateHexTile(coord.x, coord.y, TileType.Suburb, $"郊区_{coord.x}_{coord.y}");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 生成集群（通用方法）
    /// </summary>
    bool GenerateCluster(Vector2Int centerCoord, TileType type, int targetSize)
    {
        List<Vector2Int> clusterTiles = FloodFill(centerCoord, targetSize,
            coord => !hexMap[coord].isObstacle() && hexMap[coord].tileType != type);

        if (clusterTiles.Count < targetSize / 2) // 如果集群太小，放弃
            return false;

        // 将选中的地块设置为指定类型
        foreach (Vector2Int coord in clusterTiles)
        {
            CreateHexTile(coord.x, coord.y, type);
        }

        return true;
    }

    /// <summary>
    /// 洪水填充算法生成连续区域
    /// </summary>
    List<Vector2Int> FloodFill(Vector2Int startCoord, int maxSize, System.Func<Vector2Int, bool> isValid)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(startCoord);
        visited.Add(startCoord);

        while (queue.Count > 0 && result.Count < maxSize)
        {
            Vector2Int current = queue.Dequeue();

            if (isValid(current))
            {
                result.Add(current);

                // 获取相邻坐标
                List<Vector2Int> neighbors = HexTile.GetAllNeighborCoordinates(current.x, current.y);

                foreach (Vector2Int neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor) && hexMap.ContainsKey(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定距离的环状地块
    /// </summary>
    List<Vector2Int> GetRingTiles(List<Vector2Int> centerTiles, int distance)
    {
        HashSet<Vector2Int> ringTiles = new HashSet<Vector2Int>();

        foreach (Vector2Int center in centerTiles)
        {
            // 获取距离为distance的所有地块
            List<Vector2Int> tilesAtDistance = GetTilesAtDistance(center, distance);
            foreach (Vector2Int tile in tilesAtDistance)
            {
                if (!centerTiles.Contains(tile) && hexMap.ContainsKey(tile))
                {
                    ringTiles.Add(tile);
                }
            }
        }

        return new List<Vector2Int>(ringTiles);
    }

    /// <summary>
    /// 获取指定距离的所有地块
    /// </summary>
    List<Vector2Int> GetTilesAtDistance(Vector2Int center, int distance)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        // 简单的六边形距离计算
        for (int q = -distance; q <= distance; q++)
        {
            for (int r = -distance; r <= distance; r++)
            {
                if (Mathf.Abs(q + r) <= distance)
                {
                    Vector2Int coord = new Vector2Int(center.x + q, center.y + r);
                    if (IsValidCoordinate(coord))
                    {
                        result.Add(coord);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 检查坐标是否适合建立城市
    /// </summary>
    bool IsValidForCity(Vector2Int coord)
    {
        if (!hexMap.ContainsKey(coord) || hexMap[coord].isObstacle())
            return false;

        // 检查周围是否有足够的空间
        int validNeighbors = 0;
        List<Vector2Int> neighbors = HexTile.GetAllNeighborCoordinates(coord.x, coord.y);

        foreach (Vector2Int neighbor in neighbors)
        {
            if (hexMap.ContainsKey(neighbor) && !hexMap[neighbor].isObstacle())
            {
                validNeighbors++;
            }
        }

        return validNeighbors >= 4; // 至少4个有效相邻地块
    }

    /// <summary>
    /// 确保所有可通行地块的连通性
    /// </summary>
    void EnsureConnectivity()
    {
        // 找到所有可通行地块
        List<Vector2Int> passableTiles = new List<Vector2Int>();
        foreach (var pair in hexMap)
        {
            if (!pair.Value.isObstacle())
            {
                passableTiles.Add(pair.Key);
            }
        }

        if (passableTiles.Count == 0) return;

        // 使用BFS检查连通性
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // 从起始点开始
        queue.Enqueue(startTileCoord);
        visited.Add(startTileCoord);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();

            List<Vector2Int> neighbors = HexTile.GetAllNeighborCoordinates(current.x, current.y);
            foreach (Vector2Int neighbor in neighbors)
            {
                if (hexMap.ContainsKey(neighbor) && !hexMap[neighbor].isObstacle() &&
                    !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // 将未连通的地块转换为农村
        int convertedCount = 0;
        foreach (Vector2Int tile in passableTiles)
        {
            if (!visited.Contains(tile))
            {
                CreateHexTile(tile.x, tile.y, TileType.Rural);
                convertedCount++;
            }
        }

        if (convertedCount > 0)
        {
            Debug.Log($"确保连通性: 转换了 {convertedCount} 个未连通地块为农村");
        }
    }

    /// <summary>
    /// 设置所有地块的相邻关系
    /// </summary>
    void SetupNeighborConnections()
    {
        foreach (var tile in allTiles)
        {
            tile.neighbors.Clear();

            // 获取所有相邻坐标
            List<Vector2Int> neighborCoords = HexTile.GetAllNeighborCoordinates(tile.q, tile.r);

            foreach (Vector2Int coord in neighborCoords)
            {
                if (hexMap.TryGetValue(coord, out HexTile neighborTile))
                {
                    tile.neighbors.Add(neighborTile);
                }
            }
        }

        Debug.Log("相邻关系设置完成");
    }

    /// <summary>
    /// 解锁起始地块
    /// </summary>
    void UnlockStartTile()
    {
        HexTile startTile = FindSuitableStartTile();

        if (startTile != null)
        {
            startTileCoord = new Vector2Int(startTile.q, startTile.r);
            unlockedTileTypeCounter[(int)startTile.tileType]++;

            startTile.UnlockTile();
            Debug.Log($"智能解锁起始地块: {startTile.tileName} ({startTile.q}, {startTile.r})");
        }
        else
        {
            Debug.LogError("无法找到合适的农村起始地块！");
        }
    }

    /// <summary>
    /// 寻找合适的起始地块
    /// </summary>
    HexTile FindSuitableStartTile()
    {
        List<HexTile> ruralTiles = new List<HexTile>();

        // 收集所有农村地块
        foreach (var tile in allTiles)
        {
            if (tile.tileType == TileType.Rural && !tile.isObstacle())
            {
                ruralTiles.Add(tile);
            }
        }

        if (ruralTiles.Count == 0)
        {
            Debug.LogWarning("没有找到任何农村地块，尝试寻找其他可通行地块");
            // 如果没有农村地块，寻找任何可通行地块
            foreach (var tile in allTiles)
            {
                if (!tile.isObstacle() && tile.tileType != TileType.Lake && tile.tileType != TileType.Mountain)
                {
                    ruralTiles.Add(tile);
                }
            }
        }

        if (ruralTiles.Count == 0)
        {
            Debug.LogError("没有找到任何可通行的起始地块！");
            return null;
        }

        // 如果指定了起始坐标且该坐标是农村，优先使用
        if (hexMap.ContainsKey(startTileCoord))
        {
            HexTile specifiedTile = hexMap[startTileCoord];
            if (specifiedTile.tileType == TileType.Rural && !specifiedTile.isObstacle())
            {
                Debug.Log($"使用指定的农村起始地块: ({startTileCoord.x}, {startTileCoord.y})");
                return specifiedTile;
            }
        }

        // 随机选取
        HexTile randomTile = ruralTiles[random.Next(ruralTiles.Count)];
        Debug.Log($"随机选择农村地块: ({randomTile.q}, {randomTile.r})");
        return randomTile;
    }


    /// <summary>
    /// 解锁指定坐标的地块
    /// </summary>
    public bool UnlockTileAtCoord(int q, int r, float cost = 0f)
    {
        Vector2Int coord = new Vector2Int(q, r);
        if (hexMap.TryGetValue(coord, out HexTile tile))
        {
            // 检查是否已经有相邻的解锁地块
            bool hasUnlockedNeighbor = false;
            foreach (var neighbor in tile.neighbors)
            {
                if (neighbor.isUnlocked)
                {
                    hasUnlockedNeighbor = true;
                    break;
                }
            }

            if (hasUnlockedNeighbor || (q == startTileCoord.x && r == startTileCoord.y))
            {
                tile.UnlockTile();
                unlockedTileTypeCounter[(int)tile.tileType]++;

                return true;
            }
            else
            {
                Debug.LogWarning($"无法解锁地块 ({q}, {r})，没有相邻的解锁地块");
                return false;
            }
        }

        Debug.LogWarning($"找不到坐标 ({q}, {r}) 的地块");
        return false;
    }

    /// <summary>
    /// 获取指定坐标的地块
    /// </summary>
    public HexTile GetTileAtCoord(int q, int r)
    {
        Vector2Int coord = new Vector2Int(q, r);
        hexMap.TryGetValue(coord, out HexTile tile);
        return tile;
    }

    /// <summary>
    /// 获取所有地块
    /// </summary>
    public List<HexTile> GetAllTiles()
    {
        return new List<HexTile>(allTiles);
    }

    /// <summary>
    /// 重新生成地图（编辑器调用）
    /// </summary>
    [ContextMenu("重新生成地图")]
    public void RegenerateMap()
    {
        // 清除现有地块
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        GenerateHexMap();
        SetupNeighborConnections();
        UnlockStartTile();
    }

    /// <summary>
    /// 在Scene视图中绘制调试信息
    /// </summary>
    private void OnDrawGizmos()
    {
        if (allTiles == null) return;

        foreach (var tile in allTiles)
        {
            if (tile != null)
            {
                // 绘制相邻连接线
                Gizmos.color = Color.green;
                foreach (var neighbor in tile.neighbors)
                {
                    if (neighbor != null)
                    {
                        Gizmos.DrawLine(tile.transform.position, neighbor.transform.position);
                    }
                }

                // 绘制坐标标签
#if UNITY_EDITOR
                // UnityEditor.Handles.Label(tile.transform.position + Vector3.up * 0.3f, $"({tile.q},{tile.r})");
#endif
            }
        }
    }
}