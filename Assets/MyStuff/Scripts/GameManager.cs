using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("游戏设置")]
    public int maxFps = 120;

    public float initialFunds = 500f;
    public float currentFunds;
    public float currentTime = 0f;

    [Header("引用")]
    public HexMapGenerator mapGenerator;
    public HexUIController uiController;

    private List<HexTile> unlockedTiles = new List<HexTile>();
    void Start()
    {
        Application.targetFrameRate = maxFps;

        currentTime = 0f;
        currentFunds = initialFunds;
        Debug.Log($"游戏开始，初始资金: {currentFunds}");
    }

    void Update()
    {
        currentTime += Time.deltaTime;

        // Game Success
        int counter = 0;
        float threshold = 0.8f;
        int cityNumThreshold = 4;
        foreach (var city in mapGenerator.cityTiles)
        {
            if (city.Value.supportLevel > threshold)
                counter++;
        }
        if (counter >= cityNumThreshold)
            uiController.OnGameSuccess();

    }

    /// <summary>
    /// 解锁地块
    /// </summary>
    public bool UnlockTile(int q, int r, float cost)
    {
        if (currentFunds >= cost)
        {
            if (mapGenerator.UnlockTileAtCoord(q, r, cost))
            {
                currentFunds -= cost;
                HexTile tile = mapGenerator.GetTileAtCoord(q, r);
                if (tile != null)
                {
                    unlockedTiles.Add(tile);
                }

                Debug.Log($"解锁地块 ({q}, {r})，花费: {cost}，剩余资金: {currentFunds}");
                return true;
            }
        }
        else
        {
            Debug.LogWarning($"资金不足！需要: {cost}，当前: {currentFunds}");
        }

        return false;
    }

    /// <summary>
    /// 添加资金接口
    /// </summary>
    public void Income(float value)
    {
        currentFunds += value;
    }

    /// <summary>
    /// 支出资金接口
    /// </summary>
    public void Spending(float value)
    {
        currentFunds -= value;
        if (currentFunds < 0)
            Debug.LogError($"资金不足！当前资金为{currentFunds}。");
    }

    /// <summary>
    /// 添加资金（测试用）
    /// </summary>
    public void AddFunds(float amount)
    {
        currentFunds += amount;
        Debug.Log($"获得资金: {amount}，当前资金: {currentFunds}");
    }
}
