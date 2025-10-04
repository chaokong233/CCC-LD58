using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FloatingTextController : MonoBehaviour
{
    [Header("跳字设置")]
    public GameObject floatingTextPrefab;
    public Transform textParent;
    public float textDuration = 1.5f;
    public float floatDistance = 0.1f;
    public Color positiveColor = Color.green;
    public Color negativeColor = Color.red;

    [Header("动画曲线")]
    public AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Camera mainCamera;
    private Queue<FloatingText> textPool = new Queue<FloatingText>();
    private List<FloatingText> activeTexts = new List<FloatingText>();

    void Start()
    {
        mainCamera = Camera.main;

        // 如果没有指定父对象，使用当前对象
        if (textParent == null)
            textParent = FindFirstObjectByType<Canvas>().GetComponent<Transform>();

        // 预创建一些跳字对象
        PrewarmPool(10);
    }

    void Update()
    {
        // 更新所有活跃的跳字
        for (int i = activeTexts.Count - 1; i >= 0; i--)
        {
            if (!activeTexts[i].UpdateText(Time.deltaTime))
            {
                // 动画结束，回收对象
                ReturnToPool(activeTexts[i]);
                activeTexts.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 预创建对象池
    /// </summary>
    void PrewarmPool(int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateNewTextObject();
        }
    }

    /// <summary>
    /// 创建新的跳字对象
    /// </summary>
    void CreateNewTextObject()
    {
        if (floatingTextPrefab == null)
        {
            Debug.LogError("跳字预制体未分配！");
            return;
        }

        GameObject textObj = Instantiate(floatingTextPrefab, textParent);
        FloatingText floatingText = textObj.GetComponent<FloatingText>();

        if (floatingText != null)
        {
            textObj.SetActive(false);
            textPool.Enqueue(floatingText);
        }
    }

    /// <summary>
    /// 从对象池获取跳字对象
    /// </summary>
    FloatingText GetFromPool()
    {
        if (textPool.Count == 0)
        {
            CreateNewTextObject();
        }

        FloatingText text = textPool.Dequeue();
        text.gameObject.SetActive(true);
        return text;
    }

    /// <summary>
    /// 回收跳字对象到对象池
    /// </summary>
    void ReturnToPool(FloatingText text)
    {
        text.gameObject.SetActive(false);
        textPool.Enqueue(text);
    }

    /// <summary>
    /// 在世界位置显示金钱跳字
    /// </summary>
    public void ShowMoneyText(Vector3 worldPosition, float amount, string prefix = "")
    {
        FloatingText text = GetFromPool();

        // 设置文本内容
        string textContent = prefix + (amount >= 0 ? "+" : "-") + "$" + amount.ToString("F0");

        // 设置颜色
        Color textColor = amount >= 0 ? positiveColor : negativeColor;

        // 初始化跳字
        text.Initialize(worldPosition+new Vector3(0,0.2f,0), textContent, textColor, textDuration, floatDistance, floatCurve, fadeCurve);

        activeTexts.Add(text);
    }

    /// <summary>
    /// 在屏幕位置显示金钱跳字
    /// </summary>
    public void ShowMoneyTextAtScreen(Vector2 screenPosition, float amount, string prefix = "")
    {
        // 将屏幕坐标转换为世界坐标
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        worldPos.z = 0;

        ShowMoneyText(worldPos, amount, prefix);
    }

    /// <summary>
    /// 清除所有活跃的跳字
    /// </summary>
    public void ClearAllTexts()
    {
        foreach (var text in activeTexts)
        {
            ReturnToPool(text);
        }
        activeTexts.Clear();
    }
}