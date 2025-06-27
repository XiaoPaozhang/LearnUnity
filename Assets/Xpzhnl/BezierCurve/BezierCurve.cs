using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LearnUnity
{
  /// <summary>
  /// 贝塞尔曲线控制器
  /// 提供贝塞尔曲线箭头绘制功能，支持3D透视效果和动态视觉反馈
  /// </summary>
  public class BezierCurve : MonoBehaviour, IBezierCurve
  {
    #region 常量定义
    private const float DEFAULT_SCALE_FACTOR = 1f;
    private const float HIDE_POSITION_OFFSET = -10000f;
    private const int CONTROL_POINT_COUNT = 4;
    private const float SCALE_DECAY_FACTOR = 0.1f;
    #endregion

    #region 序列化字段
    [Header("箭头预制体")]
    [SerializeField] private GameObject arrowStartPrefab;
    [SerializeField] private GameObject arrowMiddlePrefab;
    [SerializeField] private GameObject arrowEndPrefab;

    [Header("终点图标")]
    [SerializeField] private Sprite arrowAvailableSprite;
    [SerializeField] private Sprite arrowUnavailableSprite;

    [Header("箭头配置")]
    [SerializeField] private int arrowNodeCount = 12;
    [SerializeField] private float scaleFactor = DEFAULT_SCALE_FACTOR;
    [SerializeField] private float arrowOffset = 0f;

    [Header("贝塞尔曲线设置")]
    [Range(0f, 5f)]
    [SerializeField] private float heightFactor = 1.2f;
    [SerializeField] private bool useVerticalParabola = true;
    [Range(0.1f, 2f)]
    [SerializeField] private float curveWidth = 0.3f;

    [Header("调试设置")]
    [SerializeField] private bool showRotationDebug = false;

    [Header("3D透视效果")]
    [Range(0f, 89f)]
    [SerializeField] private float perspectiveAngle = 25f;
    [Range(0.1f, 2f)]
    [SerializeField] private float depthScaleFactor = 0.85f;
    [Range(0f, 1f)]
    [SerializeField] private float perspectiveDistanceFactor = 0.5f;
    #endregion

    #region 私有字段
    private RectTransform startTransform;
    private List<RectTransform> arrowNodeList = new List<RectTransform>();

    /// <summary>
    /// 控制点列表,用于存储控制点位置
    /// 初始化时,默认添加4个控制点,每个控制点位置为Vector2.zero
    /// 控制点列表[0]为起始点,控制点列表[3]为结束点
    /// 控制点列表[1]和控制点列表[2]为中间控制点,初始位置为Vector2.zero
    /// </summary>
    private List<Vector2> controlPoints = new List<Vector2>();
    private readonly List<Vector2> controlPointFactors = new List<Vector2>
        {
            new Vector2(0.25f, 0.5f),
            new Vector2(0.75f, 0.8f)
        };
    private Camera mainCamera;
    private Canvas parentCanvas;
    private bool isInitialized = false;
    #endregion

    #region 属性
    /// <summary>
    /// 获取箭头是否可见
    /// </summary>
    public bool IsVisible { get; private set; }
    #endregion

    #region 事件
    /// <summary>
    /// 箭头显示事件
    /// </summary>
    public Action<Vector2> OnArrowShow { get; set; }

    /// <summary>
    /// 箭头隐藏事件
    /// </summary>
    public Action OnArrowHide { get; set; }

    /// <summary>
    /// 箭头位置更新事件
    /// </summary>
    public Action<Vector2> OnArrowPositionUpdate { get; set; }
    #endregion

    #region Unity生命周期
    private void Awake()
    {
      Debug.Log($"[Awake] {gameObject.name}通过 Camera.main获取主摄像机,获取不到就FindObjectOfType<Camera>()");
      mainCamera = Camera.main ?? FindObjectOfType<Camera>();
      Debug.Log($"[Awake] {gameObject.name}必须有父级canvas,通过 GetComponentInParent<Canvas>()获取父级画布");
      parentCanvas = GetComponentInParent<Canvas>();

      // 添加安全检查
      if (parentCanvas == null)
      {
        Debug.LogError("没有Canvas,贝塞尔曲线控制器无法工作");
        return;
      }

      InitializeControlPoints();
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 初始化贝塞尔曲线控制器，如果未分配则使用默认资源
    /// </summary>
    public void Initialize()
    {
      if (isInitialized) return;

      CheckResources();
      GenerateArrowNodes();
      HideArrow();
      isInitialized = true;
    }

    /// <summary>
    /// 显示从指定起始变换开始的箭头
    /// </summary>
    /// <param name="startPoint">箭头的起始点</param>
    public void ShowArrow(RectTransform startPoint)
    {
      if (!isInitialized) Initialize();

      startTransform = startPoint;
      Cursor.visible = false;
      gameObject.SetActive(true);
      IsVisible = true;

      // 触发显示事件
      OnArrowShow?.Invoke(startPoint.position);
    }

    /// <summary>
    /// 隐藏箭头并恢复鼠标光标可见性
    /// </summary>
    public void HideArrow()
    {
      Cursor.visible = true;
      // 只隐藏箭头节点，而不是整个GameObject
      HideAllNodes();
      IsVisible = false;

      // 触发隐藏事件
      OnArrowHide?.Invoke();
    }

    /// <summary>
    /// 根据当前鼠标位置更新箭头可视化
    /// 当箭头可见时应在Update中调用
    /// </summary>
    public void UpdateArrowVisualization()
    {
      if (!IsVisible || mainCamera == null || startTransform == null) return;

      UpdateControlPoints();
      UpdateArrowNodePositions();
      UpdateArrowNodeRotations();

      // 触发位置更新事件
      OnArrowPositionUpdate?.Invoke(GetTargetPosition());
    }

    /// <summary>
    /// 设置结束箭头图标的视觉状态
    /// </summary>
    /// <param name="isAvailable">目标位置是否有效</param>
    public void SetEndIconState(bool isAvailable)
    {
      if (arrowNodeList.Count == 0) return;

      var endNode = arrowNodeList[arrowNodeList.Count - 1];
      var imageComponent = endNode.GetComponent<Image>();

      if (imageComponent != null)
      {
        imageComponent.sprite = isAvailable ? arrowAvailableSprite : arrowUnavailableSprite;
      }
    }

    /// <summary>
    /// 获取当前世界空间中的目标位置
    /// </summary>
    /// <returns>箭头指向的目标位置</returns>
    public Vector2 GetTargetPosition()
    {
      if (controlPoints.Count > 3)
        return controlPoints[3];

      return mainCamera.ScreenToWorldPoint(Input.mousePosition);
    }
    #endregion

    #region 私有方法
    /// <summary>
    /// 如果需要则加载默认资源
    /// </summary>
    private void CheckResources()
    {
      if (arrowStartPrefab == null)
        Debug.LogError("箭头起始预制体为空");

      if (arrowMiddlePrefab == null)
        Debug.LogError("箭头中段预制体为空");

      if (arrowEndPrefab == null)
        Debug.LogError("箭头结束预制体为空");

      if (arrowAvailableSprite == null)
        Debug.LogError("箭头可用精灵为空");

      if (arrowUnavailableSprite == null)
        Debug.LogError("箭头不可用精灵为空");
    }

    /// <summary>
    /// 初始化控制点列表
    /// </summary>
    private void InitializeControlPoints()
    {
      controlPoints.Clear();
      for (int i = 0; i < CONTROL_POINT_COUNT; i++)
      {
        controlPoints.Add(Vector2.zero);
      }
    }

    /// <summary>
    /// 生成箭头节点
    /// </summary>
    private void GenerateArrowNodes()
    {
      CleanupExistingNodes();

      // 起始节点
      if (arrowStartPrefab != null)
      {
        var startNode = Instantiate(arrowStartPrefab, transform);
        arrowNodeList.Add(startNode.GetComponent<RectTransform>());
      }

      // 中段节点
      for (int i = 1; i < arrowNodeCount - 1; i++)
      {
        if (arrowMiddlePrefab != null)
        {
          var middleNode = Instantiate(arrowMiddlePrefab, transform);
          arrowNodeList.Add(middleNode.GetComponent<RectTransform>());
        }
      }

      // 结束节点
      if (arrowEndPrefab != null)
      {
        var endNode = Instantiate(arrowEndPrefab, transform);
        arrowNodeList.Add(endNode.GetComponent<RectTransform>());
      }

      // 初始隐藏所有节点
      HideAllNodes();
    }

    /// <summary>
    /// 清理现有节点
    /// </summary>
    private void CleanupExistingNodes()
    {
      foreach (var node in arrowNodeList)
      {
        if (node != null)
          DestroyImmediate(node.gameObject);
      }
      arrowNodeList.Clear();
    }

    /// <summary>
    /// 隐藏所有节点
    /// </summary>
    private void HideAllNodes()
    {
      var hidePosition = new Vector2(HIDE_POSITION_OFFSET, HIDE_POSITION_OFFSET);
      arrowNodeList.ForEach(node => node.position = hidePosition);
    }

    /// <summary>
    /// 更新控制点
    /// </summary>
    private void UpdateControlPoints()
    {
      if (startTransform == null) return;

      Vector2 startPoint;
      Vector2 endPoint;

      // 根据Canvas的渲染模式处理坐标转换
      if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
      {
        // 屏幕空间覆盖模式，直接使用屏幕坐标
        startPoint = startTransform.position;
        endPoint = Input.mousePosition;
      }
      else if (parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
      {
        // 屏幕空间摄像机模式
        startPoint = startTransform.position;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, parentCanvas.planeDistance));
        endPoint = mouseWorldPos;
      }
      else
      {
        // 世界空间模式
        startPoint = startTransform.position;
        endPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
      }

      controlPoints[0] = startPoint;
      controlPoints[3] = endPoint;

      if (useVerticalParabola)
      {
        UpdateVerticalParabolaControlPoints(startPoint, endPoint);
      }
      else
      {
        UpdateStandardBezierControlPoints(startPoint, endPoint);
      }
    }

    /// <summary>
    /// 更新垂直抛物线控制点
    /// </summary>
    private void UpdateVerticalParabolaControlPoints(Vector2 startPoint, Vector2 endPoint)
    {
      float distance = Vector2.Distance(startPoint, endPoint);
      float height = distance * heightFactor;

      // 改进中点计算，使曲线更自然
      Vector2 direction = (endPoint - startPoint).normalized;
      Vector2 perpendicular = new Vector2(-direction.y, direction.x); // 垂直于连线的方向

      Vector2 midPoint = new Vector2(
          Mathf.Lerp(startPoint.x, endPoint.x, 0.5f) + perpendicular.x * distance * curveWidth * 0.3f,
          Mathf.Max(startPoint.y, endPoint.y) + height
      );

      controlPoints[1] = midPoint;
      controlPoints[2] = endPoint;
    }

    /// <summary>
    /// 更新标准贝塞尔控制点
    /// </summary>
    private void UpdateStandardBezierControlPoints(Vector2 startPoint, Vector2 endPoint)
    {
      Vector2 direction = endPoint - startPoint;
      controlPoints[1] = startPoint + direction * controlPointFactors[0];
      controlPoints[2] = startPoint + direction * controlPointFactors[1];
    }

    /// <summary>
    /// 更新箭头节点位置
    /// </summary>
    private void UpdateArrowNodePositions()
    {
      Vector2 startPoint = controlPoints[0];
      Vector2 endPoint = controlPoints[3];
      float totalDistance = Vector2.Distance(startPoint, endPoint);

      // 避免除零错误
      if (totalDistance < 0.001f) return;

      for (int i = 0; i < arrowNodeList.Count; i++)
      {
        float t = CalculateNodeParameter(i);
        Vector2 position = CalculateBezierPoint(t);

        // 根据Canvas模式设置位置
        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
          arrowNodeList[i].position = new Vector3(position.x, position.y, 0);
        }
        else
        {
          arrowNodeList[i].position = new Vector3(position.x, position.y, startTransform.position.z);
        }

        // 应用3D透视效果
        float distanceRatio = Vector2.Distance(startPoint, position) / totalDistance;
        ApplyPerspectiveEffect(arrowNodeList[i], t, distanceRatio);
      }
    }

    /// <summary>
    /// 更新箭头节点旋转
    /// </summary>
    private void UpdateArrowNodeRotations()
    {
      if (arrowNodeList.Count < 2) return;

      Vector2 startPoint = controlPoints[0];
      Vector2 endPoint = controlPoints[3];
      float totalDistance = Vector2.Distance(startPoint, endPoint);

      // 使用精确的切线计算方法
      for (int i = 0; i < arrowNodeList.Count; i++)
      {
        Vector2 tangentDirection = CalculateExactTangentDirection(i);

        // 智能方向校正：检测是否需要反转切线方向
        if (ShouldReverseTangent(i, tangentDirection))
        {
          tangentDirection = -tangentDirection;
        }

        float baseRotationZ = Vector2.SignedAngle(Vector2.up, tangentDirection) - arrowOffset;

        // 计算透视旋转效果（可选）
        float distanceRatio = totalDistance > 0.001f ? Vector2.Distance(startPoint, arrowNodeList[i].position) / totalDistance : 0f;
        float perspectiveRotationX = Mathf.Lerp(0, perspectiveAngle, distanceRatio * perspectiveDistanceFactor);

        // 组合最终旋转
        Vector3 finalRotation = new Vector3(perspectiveRotationX, 0, baseRotationZ);
        arrowNodeList[i].rotation = Quaternion.Euler(finalRotation);

        // 详细调试信息
        if (showRotationDebug && Application.isPlaying)
        {
          AnalyzeRotationDifferences(i, tangentDirection, baseRotationZ);
        }
      }
    }

    /// <summary>
    /// 计算精确的切线方向（使用贝塞尔曲线导数）
    /// </summary>
    private Vector2 CalculateExactTangentDirection(int nodeIndex)
    {
      float t = CalculateNodeParameter(nodeIndex);
      Vector2 tangent;

      if (useVerticalParabola)
      {
        // 二次贝塞尔曲线 B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
        // 导数 B'(t) = -2(1-t)P₀ + 2(1-2t)P₁ + 2tP₂
        float oneMinusT = 1f - t;
        tangent = -2f * oneMinusT * controlPoints[0] +
              2f * (oneMinusT - t) * controlPoints[1] +
              2f * t * controlPoints[2];
      }
      else
      {
        // 三次贝塞尔曲线 B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
        // 导数 B'(t) = -3(1-t)²P₀ + 3(1-t)²P₁ - 6(1-t)tP₁ + 6(1-t)tP₂ - 3t²P₂ + 3t²P₃
        float oneMinusT = 1f - t;
        float oneMinusTSquared = oneMinusT * oneMinusT;
        float tSquared = t * t;

        tangent = -3f * oneMinusTSquared * controlPoints[0] +
              3f * oneMinusTSquared * controlPoints[1] -
              6f * oneMinusT * t * controlPoints[1] +
              6f * oneMinusT * t * controlPoints[2] -
              3f * tSquared * controlPoints[2] +
              3f * tSquared * controlPoints[3];
      }

      // 检查切线是否为零向量
      if (tangent.magnitude < 0.001f)
      {
        // 如果切线为零，使用相邻节点方法作为备选
        if (nodeIndex > 0)
        {
          tangent = arrowNodeList[nodeIndex].position - arrowNodeList[nodeIndex - 1].position;
        }
        else if (nodeIndex < arrowNodeList.Count - 1)
        {
          tangent = arrowNodeList[nodeIndex + 1].position - arrowNodeList[nodeIndex].position;
        }
        else
        {
          tangent = Vector2.up; // 默认向上
        }
      }

      return tangent.normalized;
    }

    /// <summary>
    /// 智能检测是否需要反转切线方向
    /// </summary>
    private bool ShouldReverseTangent(int nodeIndex, Vector2 tangentDirection)
    {
      // 使用相邻节点方向作为参考
      Vector2 referenceDirection = Vector2.zero;
      bool hasReference = false;

      // 优先使用前进方向（从前一个节点到当前节点）
      if (nodeIndex > 0)
      {
        referenceDirection = (arrowNodeList[nodeIndex].position - arrowNodeList[nodeIndex - 1].position).normalized;
        hasReference = true;
      }
      // 备选：使用前向方向（从当前节点到下一个节点）
      else if (nodeIndex < arrowNodeList.Count - 1)
      {
        referenceDirection = (arrowNodeList[nodeIndex + 1].position - arrowNodeList[nodeIndex].position).normalized;
        hasReference = true;
      }

      if (!hasReference) return false;

      // 计算切线方向与参考方向的点积
      float dotProduct = Vector2.Dot(tangentDirection, referenceDirection);

      // 如果点积为负，说明方向相反，需要反转
      bool shouldReverse = dotProduct < 0;

      if (showRotationDebug && Application.isPlaying)
      {
        Debug.Log($"节点{nodeIndex} - 切线方向: {tangentDirection}, 参考方向: {referenceDirection}, 点积: {dotProduct:F3}, 需要反转: {shouldReverse}");
      }

      return shouldReverse;
    }

    /// <summary>
    /// 分析不同旋转计算方法的差异
    /// </summary>
    private void AnalyzeRotationDifferences(int nodeIndex, Vector2 tangentDirection, float tangentRotation)
    {
      string nodeType = nodeIndex == 0 ? "起始" : (nodeIndex == arrowNodeList.Count - 1 ? "结束" : "中段");
      float t = CalculateNodeParameter(nodeIndex);

      // 方法1: 切线方向
      float tangentAngle = Vector2.SignedAngle(Vector2.up, tangentDirection);

      // 方法2: 相邻节点方向
      Vector2 adjacentDirection = Vector2.zero;
      float adjacentAngle = 0f;
      if (nodeIndex > 0)
      {
        adjacentDirection = (arrowNodeList[nodeIndex].position - arrowNodeList[nodeIndex - 1].position).normalized;
        adjacentAngle = Vector2.SignedAngle(Vector2.up, adjacentDirection);
      }

      // 方法3: 反向切线（测试是否需要反转）
      Vector2 reverseTangent = -tangentDirection;
      float reverseTangentAngle = Vector2.SignedAngle(Vector2.up, reverseTangent);

      // 方法4: 使用前向差分估算切线
      Vector2 forwardTangent = Vector2.zero;
      float forwardTangentAngle = 0f;
      if (nodeIndex < arrowNodeList.Count - 1)
      {
        forwardTangent = (arrowNodeList[nodeIndex + 1].position - arrowNodeList[nodeIndex].position).normalized;
        forwardTangentAngle = Vector2.SignedAngle(Vector2.up, forwardTangent);
      }

      Debug.Log($"=== 节点{nodeIndex}({nodeType}) t={t:F3} ===");
      Debug.Log($"切线方向: {tangentDirection} → 角度: {tangentAngle:F1}°");
      Debug.Log($"相邻方向: {adjacentDirection} → 角度: {adjacentAngle:F1}°");
      Debug.Log($"反向切线: {reverseTangent} → 角度: {reverseTangentAngle:F1}°");
      Debug.Log($"前向切线: {forwardTangent} → 角度: {forwardTangentAngle:F1}°");

      // 分析哪个角度更合理
      string suggestion = "";
      if (nodeIndex > 0 && Mathf.Abs(tangentAngle - adjacentAngle) > 90f)
      {
        suggestion += "切线方向与相邻节点方向差异>90°，可能需要反转；";
      }
      if (nodeIndex < arrowNodeList.Count - 1 && Mathf.Abs(tangentAngle - forwardTangentAngle) > 90f)
      {
        suggestion += "切线方向与前向方向差异>90°，可能需要反转；";
      }
      if (suggestion.Length > 0)
      {
        Debug.LogWarning($"节点{nodeIndex}建议: {suggestion}");
      }
    }

    /// <summary>
    /// 计算节点参数
    /// </summary>
    private float CalculateNodeParameter(int nodeIndex)
    {
      // 使用线性分布，确保节点间距均匀
      return (float)nodeIndex / (arrowNodeList.Count - 1);
    }

    /// <summary>
    /// 计算贝塞尔曲线上的点
    /// </summary>
    private Vector2 CalculateBezierPoint(float t)
    {
      if (useVerticalParabola)
      {
        // 二次贝塞尔用于抛物线
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * controlPoints[0] +
               2f * oneMinusT * t * controlPoints[1] +
               t * t * controlPoints[2];
      }
      else
      {
        // 三次贝塞尔用于复杂曲线
        return Mathf.Pow(1 - t, 3) * controlPoints[0] +
               3 * Mathf.Pow(1 - t, 2) * t * controlPoints[1] +
               3 * (1 - t) * Mathf.Pow(t, 2) * controlPoints[2] +
               Mathf.Pow(t, 3) * controlPoints[3];
      }
    }

    /// <summary>
    /// 应用3D透视效果
    /// </summary>
    private void ApplyPerspectiveEffect(RectTransform node, float t, float distanceRatio)
    {
      // 修复缩放计算错误 - 使用正确的参数t
      float baseScale = scaleFactor * (1f - SCALE_DECAY_FACTOR * t);

      // 应用透视缩放
      float perspectiveScale = Mathf.Lerp(1f, depthScaleFactor, distanceRatio * perspectiveDistanceFactor);
      float finalScale = Mathf.Max(0.1f, baseScale * perspectiveScale); // 防止缩放为0

      node.localScale = new Vector3(finalScale, finalScale, 1f);

      // 注意：不在这里设置旋转，避免覆盖正确的方向旋转
      // 透视旋转效果已在更新旋转方法中处理

      // 可选：应用透明度效果
      ApplyTransparencyEffect(node, distanceRatio);
    }

    /// <summary>
    /// 应用透明度效果
    /// </summary>
    private void ApplyTransparencyEffect(RectTransform node, float distanceRatio)
    {
      var spriteRenderer = node.GetComponent<SpriteRenderer>();
      if (spriteRenderer != null)
      {
        Color color = spriteRenderer.color;
        color.a = Mathf.Lerp(1f, 0.5f, distanceRatio * perspectiveDistanceFactor);
        spriteRenderer.color = color;
      }
    }
    #endregion

    #region 接口实现 - 配置方法

    /// <summary>
    /// 设置箭头节点数量
    /// </summary>
    /// <param name="count">箭头节点数量</param>
    public void SetArrowNodeCount(int count)
    {
      arrowNodeCount = Mathf.Max(3, count); // 最少3个节点
      if (isInitialized)
      {
        GenerateArrowNodes(); // 重新生成节点
      }
    }

    /// <summary>
    /// 设置缩放因子
    /// </summary>
    /// <param name="factor">缩放因子</param>
    public void SetScaleFactor(float factor)
    {
      scaleFactor = Mathf.Max(0.1f, factor);
    }

    /// <summary>
    /// 设置高度因子
    /// </summary>
    /// <param name="factor">高度因子</param>
    public void SetHeightFactor(float factor)
    {
      heightFactor = Mathf.Max(0f, factor);
    }

    /// <summary>
    /// 设置是否使用垂直抛物线
    /// </summary>
    /// <param name="use">是否使用垂直抛物线</param>
    public void SetVerticalParabolaMode(bool use)
    {
      useVerticalParabola = use;
    }

    /// <summary>
    /// 设置透视角度
    /// </summary>
    /// <param name="angle">透视角度（0-89度）</param>
    public void SetPerspectiveAngle(float angle)
    {
      perspectiveAngle = Mathf.Clamp(angle, 0f, 89f);
    }

    #endregion
  }
}