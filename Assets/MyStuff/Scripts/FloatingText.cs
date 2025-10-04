using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("组件引用")]
    public TextMeshProUGUI textComponent;
    public CanvasGroup canvasGroup;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float duration;
    private float elapsedTime;
    private AnimationCurve floatCurve;
    private AnimationCurve fadeCurve;

    void Awake()
    {
        // 自动获取组件引用
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    /// <summary>
    /// 初始化跳字
    /// </summary>
    public void Initialize(Vector3 worldPosition, string text, Color color, float duration,
                          float floatDistance, AnimationCurve floatCurve, AnimationCurve fadeCurve)
    {
        // 设置位置
        transform.position = worldPosition;
        startPosition = worldPosition;
        targetPosition = worldPosition + Vector3.up * floatDistance;

        // 设置文本和颜色
        textComponent.text = text;
        textComponent.color = color;

        // 设置动画参数
        this.duration = duration;
        this.floatCurve = floatCurve;
        this.fadeCurve = fadeCurve;
        elapsedTime = 0f;

        // 重置CanvasGroup
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 更新跳字状态
    /// </summary>
    /// <returns>是否还在活跃状态</returns>
    public bool UpdateText(float deltaTime)
    {
        elapsedTime += deltaTime;

        if (elapsedTime >= duration)
        {
            return false; // 动画结束
        }

        float progress = elapsedTime / duration;

        // 更新位置
        float floatProgress = floatCurve.Evaluate(progress);
        transform.position = Vector3.Lerp(startPosition, targetPosition, floatProgress);

        // 更新透明度
        if (canvasGroup != null)
        {
            canvasGroup.alpha = fadeCurve.Evaluate(progress);
        }

        return true;
    }
}