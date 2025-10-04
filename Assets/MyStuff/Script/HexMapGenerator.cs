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

    private Dictionary<Vector2Int, HexTile> hexMap = new Dictionary<Vector2Int, HexTile>();
    private List<HexTile> allTiles = new List<HexTile>();

    void Start()
    {
        GenerateHexMap();
        SetupNeighborConnections();
        UnlockStartTile();
    }

    /// <summary>
    /// 生成六边形地图
    /// </summary>
    void GenerateHexMap()
    {
        hexMap.Clear();
        allTiles.Clear();

        for (int q = 0; q < mapWidth; q++)
        {
            for (int r = 0; r < mapHeight; r++)
            {
                CreateHexTile(q, r);
            }
        }

        Debug.Log($"地图生成完成，共 {allTiles.Count} 个地块");
    }

    /// <summary>
    /// 创建单个六边形地块
    /// </summary>
    void CreateHexTile(int q, int r)
    {
        if (hexTilePrefab == null)
        {
            Debug.LogError("HexTile预制体未分配！");
            return;
        }

        Vector3 worldPosition = HexTile.HexToWorldPosition(q, r);
        GameObject tileObj = Instantiate(hexTilePrefab, worldPosition, Quaternion.identity, transform);
        tileObj.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1);

        HexTile hexTile = tileObj.GetComponent<HexTile>();
        if (hexTile != null)
        {
            hexTile.Initialize(q, r);
            hexMap[new Vector2Int(q, r)] = hexTile;
            allTiles.Add(hexTile);
        }
        else
        {
            Debug.LogError($"在位置 ({q}, {r}) 创建的地块没有HexTile组件");
            Destroy(tileObj);
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
        if (hexMap.TryGetValue(startTileCoord, out HexTile startTile))
        {
            startTile.UnlockTile();
            Debug.Log($"起始地块 {startTileCoord} 已解锁");
        }
        else
        {
            Debug.LogError($"找不到起始地块坐标: {startTileCoord}");
        }
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
                Gizmos.color = tile.isUnlocked ? Color.green : Color.gray;
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