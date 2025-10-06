using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("��Ϸ����")]
    public int maxFps = 120;

    public float initialFunds = 500f;
    public float currentFunds;
    public float currentTime = 0f;

    [Header("����")]
    public HexMapGenerator mapGenerator;
    public HexUIController uiController;

    private List<HexTile> unlockedTiles = new List<HexTile>();
    void Start()
    {
        Application.targetFrameRate = maxFps;

        currentTime = 0f;
        currentFunds = initialFunds;
        Debug.Log($"��Ϸ��ʼ����ʼ�ʽ�: {currentFunds}");
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
    /// �����ؿ�
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

                Debug.Log($"�����ؿ� ({q}, {r})������: {cost}��ʣ���ʽ�: {currentFunds}");
                return true;
            }
        }
        else
        {
            Debug.LogWarning($"�ʽ��㣡��Ҫ: {cost}����ǰ: {currentFunds}");
        }

        return false;
    }

    /// <summary>
    /// ����ʽ�ӿ�
    /// </summary>
    public void Income(float value)
    {
        currentFunds += value;
    }

    /// <summary>
    /// ֧���ʽ�ӿ�
    /// </summary>
    public void Spending(float value)
    {
        currentFunds -= value;
        if (currentFunds < 0)
            Debug.LogError($"�ʽ��㣡��ǰ�ʽ�Ϊ{currentFunds}��");
    }

    /// <summary>
    /// ����ʽ𣨲����ã�
    /// </summary>
    public void AddFunds(float amount)
    {
        currentFunds += amount;
        Debug.Log($"����ʽ�: {amount}����ǰ�ʽ�: {currentFunds}");
    }
}
