using System;
using System.Collections;
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
    private const int CONTROL_POINT_COUNT = 4;
    private const float SCALE_DECAY_FACTOR = 0.1f;
    private const int MAX_CANVAS_SEARCH_ATTEMPTS = 5;
    private const float CANVAS_SEARCH_RETRY_INTERVAL = 0.1f;
    private const float MIN_DISTANCE_THRESHOLD = 0.001f;
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
    private Vector2? customStartPosition;
    private List<RectTransform> arrowNodeList = new List<RectTransform>();
    private List<Vector2> controlPoints = new List<Vector2>();
    private readonly List<Vector2> controlPointFactors = new List<Vector2>
    {
      new Vector2(0.25f, 0.5f),
      new Vector2(0.75f, 0.8f)
    };

    // 核心组件引用
    private Camera mainCamera;
    private Canvas parentCanvas;
    private RectTransform canvasRectTransform;

    // 状态管理
    private bool isInitialized = false;
    private bool isCanvasSearching = false;
    private int canvasSearchAttempts = 0;

    // 性能优化缓存
    private Vector2 lastMousePosition;
    private Vector2 lastStartPosition;
    private bool needsUpdate = true;
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
      InitializeControlPoints();
    }

    private void Start()
    {
      StartCoroutine(DelayedInitialization());
    }

    private void OnEnable()
    {
      // 如果Canvas已经找到但组件被重新启用，确保状态正确
      if (parentCanvas != null && !isInitialized)
      {
        CompleteInitialization();
      }
    }
    #endregion

    #region 初始化逻辑
    /// <summary>
    /// 延迟初始化，解决生命周期顺序问题
    /// </summary>
    private IEnumerator DelayedInitialization()
    {
      // 等待一帧，确保所有UI组件都已初始化
      yield return new WaitForEndOfFrame();

      // 开始尝试查找Canvas
      yield return StartCoroutine(FindCanvasWithRetry());

      if (parentCanvas != null)
      {
        CompleteInitialization();
      }
      else
      {
        Debug.LogError($"[BezierCurve] {gameObject.name} 初始化失败：无法找到父级Canvas");
      }
    }

    /// <summary>
    /// 带重试机制的Canvas查找
    /// </summary>
    private IEnumerator FindCanvasWithRetry()
    {
      isCanvasSearching = true;
      canvasSearchAttempts = 0;

      while (canvasSearchAttempts < MAX_CANVAS_SEARCH_ATTEMPTS && parentCanvas == null)
      {
        canvasSearchAttempts++;
        parentCanvas = FindParentCanvas();

        if (parentCanvas == null)
        {
          Debug.LogWarning($"[BezierCurve] Canvas查找失败，第{canvasSearchAttempts}次尝试");
          yield return new WaitForSeconds(CANVAS_SEARCH_RETRY_INTERVAL);
        }
      }

      isCanvasSearching = false;
    }

    /// <summary>
    /// 完成初始化
    /// </summary>
    private void CompleteInitialization()
    {
      try
      {
        mainCamera = GetMainCamera();
        canvasRectTransform = parentCanvas.transform as RectTransform;

        Debug.Log($"[BezierCurve] {gameObject.name} 初始化成功，Canvas: {parentCanvas.name}, RenderMode: {parentCanvas.renderMode}");
      }
      catch (Exception e)
      {
        Debug.LogError($"[BezierCurve] 初始化过程中发生错误：{e.Message}");
      }
    }

    /// <summary>
    /// 获取主摄像机
    /// </summary>
    private Camera GetMainCamera()
    {
      Camera camera = Camera.main;
      if (camera == null)
      {
        camera = FindObjectOfType<Camera>();
        if (camera == null)
        {
          Debug.LogWarning("[BezierCurve] 未找到任何摄像机，某些功能可能无法正常工作");
        }
      }
      return camera;
    }
    #endregion

    #region Canvas查找逻辑（简化版）
    /// <summary>
    /// 简化的Canvas查找逻辑
    /// </summary>
    private Canvas FindParentCanvas()
    {
      // 方法1：直接查找父级Canvas（最常见情况）
      Canvas canvas = GetComponentInParent<Canvas>();
      if (canvas != null)
      {
        return canvas;
      }

      // 方法2：查找场景中的UI Canvas
      Canvas[] allCanvases = FindObjectsOfType<Canvas>();
      foreach (Canvas canvasComponent in allCanvases)
      {
        if (canvasComponent.renderMode == RenderMode.ScreenSpaceOverlay ||
            canvasComponent.renderMode == RenderMode.ScreenSpaceCamera)
        {
          return canvasComponent;
        }
      }

      // 方法3：返回第一个找到的Canvas作为fallback
      return allCanvases.Length > 0 ? allCanvases[0] : null;
    }
    #endregion

    #region 公共方法
    /// <summary>
    /// 初始化贝塞尔曲线控制器，如果未分配则使用默认资源
    /// </summary>
    public void Initialize()
    {
      if (isInitialized) return;

      // 如果Canvas还没找到，先尝试查找
      if (parentCanvas == null && !isCanvasSearching)
      {
        parentCanvas = FindParentCanvas();
        if (parentCanvas != null)
        {
          CompleteInitialization();
        }
      }

      // 如果Canvas已找到，继续初始化
      if (parentCanvas != null)
      {
        CheckResources();
        GenerateArrowNodes();
        HideArrow();
        isInitialized = true;
      }
    }

    /// <summary>
    /// 显示从指定起始变换开始的箭头
    /// </summary>
    /// <param name="startPoint">箭头的起始点</param>
    public void ShowArrow(RectTransform startPoint)
    {
      if (!EnsureInitialized()) return;

      startTransform = startPoint;
      customStartPosition = null;
      Cursor.visible = false;
      gameObject.SetActive(true);
      IsVisible = true;
      needsUpdate = true;

      OnArrowShow?.Invoke(startPoint.position);
    }

    /// <summary>
    /// 显示从指定坐标开始的箭头（重载方法，支持直接传入坐标）
    /// </summary>
    /// <param name="startPosition">箭头的起始坐标（UI坐标）</param>
    /// <param name="referenceTransform">参考变换（用于坐标系统参考，可为null）</param>
    public void ShowArrow(Vector2 startPosition, RectTransform referenceTransform = null)
    {
      if (!EnsureInitialized()) return;

      startTransform = referenceTransform;
      customStartPosition = startPosition;
      Cursor.visible = false;
      gameObject.SetActive(true);
      IsVisible = true;
      needsUpdate = true;

      OnArrowShow?.Invoke(startPosition);
    }

    /// <summary>
    /// 隐藏箭头并恢复鼠标光标可见性
    /// </summary>
    public void HideArrow()
    {
      Cursor.visible = true;
      HideAllNodes();
      IsVisible = false;
      OnArrowHide?.Invoke();
    }

    /// <summary>
    /// 根据当前鼠标位置更新箭头可视化
    /// 当箭头可见时应在Update中调用
    /// </summary>
    public void UpdateArrowVisualization()
    {
      if (!IsVisible || parentCanvas == null) return;

      // 性能优化：检查是否需要更新
      if (!ShouldUpdateVisualization()) return;

      try
      {
        EnsureNodesActive();
        UpdateControlPoints();
        UpdateArrowNodePositions();
        UpdateArrowNodeRotations();

        OnArrowPositionUpdate?.Invoke(GetTargetPosition());
        needsUpdate = false;
      }
      catch (Exception e)
      {
        Debug.LogError($"[BezierCurve] 更新箭头可视化时发生错误：{e.Message}");
      }
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
    /// 获取当前目标位置（使用统一的坐标系统）
    /// </summary>
    /// <returns>箭头指向的目标位置</returns>
    public Vector2 GetTargetPosition()
    {
      if (controlPoints.Count > 3)
        return controlPoints[3];
      return Input.mousePosition;
    }
    #endregion

    #region 私有工具方法
    /// <summary>
    /// 确保组件已初始化
    /// </summary>
    private bool EnsureInitialized()
    {
      if (!isInitialized)
      {
        Initialize();
      }
      return isInitialized;
    }

    /// <summary>
    /// 检查是否需要更新可视化
    /// </summary>
    private bool ShouldUpdateVisualization()
    {
      Vector2 currentMousePos = Input.mousePosition;
      Vector2 currentStartPos = GetCurrentStartPosition();

      bool mouseChanged = Vector2.Distance(currentMousePos, lastMousePosition) > MIN_DISTANCE_THRESHOLD;
      bool startChanged = Vector2.Distance(currentStartPos, lastStartPosition) > MIN_DISTANCE_THRESHOLD;

      if (mouseChanged || startChanged || needsUpdate)
      {
        lastMousePosition = currentMousePos;
        lastStartPosition = currentStartPos;
        return true;
      }

      return false;
    }

    /// <summary>
    /// 获取当前起始位置
    /// </summary>
    private Vector2 GetCurrentStartPosition()
    {
      if (customStartPosition.HasValue)
        return customStartPosition.Value;

      if (startTransform != null)
        return startTransform.position;

      return Vector2.zero;
    }

    /// <summary>
    /// 确保所有节点都是激活状态
    /// </summary>
    private void EnsureNodesActive()
    {
      foreach (var node in arrowNodeList)
      {
        if (node != null && !node.gameObject.activeInHierarchy)
        {
          node.gameObject.SetActive(true);
        }
      }
    }

    /// <summary>
    /// 检查资源（简化版）
    /// </summary>
    private void CheckResources()
    {
      if (arrowStartPrefab == null || arrowMiddlePrefab == null || arrowEndPrefab == null)
      {
        Debug.LogError("[BezierCurve] 缺少必要的箭头预制体资源");
      }
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

      try
      {
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

        HideAllNodes();
      }
      catch (Exception e)
      {
        Debug.LogError($"[BezierCurve] 生成箭头节点时发生错误：{e.Message}");
      }
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
      foreach (var node in arrowNodeList)
      {
        if (node != null)
        {
          node.gameObject.SetActive(false);
        }
      }
    }

    /// <summary>
    /// 更新控制点（优化版）
    /// </summary>
    private void UpdateControlPoints()
    {
      if (startTransform == null && !customStartPosition.HasValue) return;

      Vector2 startPoint = GetConvertedStartPoint();
      Vector2 endPoint = GetConvertedEndPoint();

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
    /// 获取转换后的起始点坐标
    /// </summary>
    private Vector2 GetConvertedStartPoint()
    {
      if (customStartPosition.HasValue)
      {
        return customStartPosition.Value;
      }

      if (startTransform == null) return Vector2.zero;

      switch (parentCanvas.renderMode)
      {
        case RenderMode.ScreenSpaceOverlay:
          return RectTransformUtility.WorldToScreenPoint(null, startTransform.position);

        case RenderMode.ScreenSpaceCamera:
          if (canvasRectTransform != null)
          {
            Vector3 localPos = canvasRectTransform.InverseTransformPoint(startTransform.position);
            return new Vector2(localPos.x, localPos.y);
          }
          return startTransform.anchoredPosition;

        default:
          RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            RectTransformUtility.WorldToScreenPoint(mainCamera, startTransform.position),
            mainCamera,
            out Vector2 localStartPoint
          );
          return localStartPoint;
      }
    }

    /// <summary>
    /// 获取转换后的终点坐标
    /// </summary>
    private Vector2 GetConvertedEndPoint()
    {
      Vector3 mousePosition = Input.mousePosition;

      switch (parentCanvas.renderMode)
      {
        case RenderMode.ScreenSpaceOverlay:
          return mousePosition;

        case RenderMode.ScreenSpaceCamera:
        case RenderMode.WorldSpace:
          RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            mousePosition,
            parentCanvas.worldCamera ?? mainCamera,
            out Vector2 localMousePos
          );
          return localMousePos;

        default:
          return mousePosition;
      }
    }

    private void UpdateVerticalParabolaControlPoints(Vector2 startPoint, Vector2 endPoint)
    {
      float distance = Vector2.Distance(startPoint, endPoint);
      float height = distance * heightFactor;

      Vector2 direction = (endPoint - startPoint).normalized;
      Vector2 perpendicular = new Vector2(-direction.y, direction.x);

      Vector2 midPoint = new Vector2(
        Mathf.Lerp(startPoint.x, endPoint.x, 0.5f) + perpendicular.x * distance * curveWidth * 0.3f,
        Mathf.Max(startPoint.y, endPoint.y) + height
      );

      controlPoints[1] = midPoint;
      controlPoints[2] = endPoint;
    }

    private void UpdateStandardBezierControlPoints(Vector2 startPoint, Vector2 endPoint)
    {
      Vector2 direction = endPoint - startPoint;
      controlPoints[1] = startPoint + direction * controlPointFactors[0];
      controlPoints[2] = startPoint + direction * controlPointFactors[1];
    }

    private void UpdateArrowNodePositions()
    {
      Vector2 startPoint = controlPoints[0];
      Vector2 endPoint = controlPoints[3];
      float totalDistance = Vector2.Distance(startPoint, endPoint);

      if (totalDistance < MIN_DISTANCE_THRESHOLD) return;

      for (int i = 0; i < arrowNodeList.Count; i++)
      {
        float t = CalculateNodeParameter(i);
        Vector2 position = CalculateBezierPoint(t);

        SetNodePosition(arrowNodeList[i], position);

        float distanceRatio = Vector2.Distance(startPoint, position) / totalDistance;
        ApplyPerspectiveEffect(arrowNodeList[i], t, distanceRatio);
      }
    }

    private void SetNodePosition(RectTransform node, Vector2 position)
    {
      switch (parentCanvas.renderMode)
      {
        case RenderMode.ScreenSpaceOverlay:
          node.position = new Vector3(position.x, position.y, 0);
          break;
        case RenderMode.ScreenSpaceCamera:
        case RenderMode.WorldSpace:
          node.anchoredPosition = position;
          break;
      }
    }

    private void UpdateArrowNodeRotations()
    {
      if (arrowNodeList.Count < 2) return;

      for (int i = 0; i < arrowNodeList.Count; i++)
      {
        Vector2 tangentDirection = CalculateExactTangentDirection(i);

        if (ShouldReverseTangent(i, tangentDirection))
        {
          tangentDirection = -tangentDirection;
        }

        float baseRotationZ = Vector2.SignedAngle(Vector2.up, tangentDirection) - arrowOffset;
        Vector3 finalRotation = new Vector3(0, 0, baseRotationZ);
        arrowNodeList[i].rotation = Quaternion.Euler(finalRotation);
      }
    }

    private Vector2 CalculateExactTangentDirection(int nodeIndex)
    {
      float t = CalculateNodeParameter(nodeIndex);
      Vector2 tangent;

      if (useVerticalParabola)
      {
        float oneMinusT = 1f - t;
        tangent = -2f * oneMinusT * controlPoints[0] +
                  2f * (oneMinusT - t) * controlPoints[1] +
                  2f * t * controlPoints[2];
      }
      else
      {
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

      if (tangent.magnitude < MIN_DISTANCE_THRESHOLD)
      {
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
          tangent = Vector2.up;
        }
      }

      return tangent.normalized;
    }

    private bool ShouldReverseTangent(int nodeIndex, Vector2 tangentDirection)
    {
      Vector2 referenceDirection = Vector2.zero;
      bool hasReference = false;

      if (nodeIndex > 0)
      {
        referenceDirection = (arrowNodeList[nodeIndex].position - arrowNodeList[nodeIndex - 1].position).normalized;
        hasReference = true;
      }
      else if (nodeIndex < arrowNodeList.Count - 1)
      {
        referenceDirection = (arrowNodeList[nodeIndex + 1].position - arrowNodeList[nodeIndex].position).normalized;
        hasReference = true;
      }

      if (!hasReference) return false;

      float dotProduct = Vector2.Dot(tangentDirection, referenceDirection);
      return dotProduct < 0;
    }

    private float CalculateNodeParameter(int nodeIndex)
    {
      return (float)nodeIndex / (arrowNodeList.Count - 1);
    }

    private Vector2 CalculateBezierPoint(float t)
    {
      if (useVerticalParabola)
      {
        float oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * controlPoints[0] +
                2f * oneMinusT * t * controlPoints[1] +
                t * t * controlPoints[2];
      }
      else
      {
        return Mathf.Pow(1 - t, 3) * controlPoints[0] +
                3 * Mathf.Pow(1 - t, 2) * t * controlPoints[1] +
                3 * (1 - t) * Mathf.Pow(t, 2) * controlPoints[2] +
                Mathf.Pow(t, 3) * controlPoints[3];
      }
    }

    private void ApplyPerspectiveEffect(RectTransform node, float t, float distanceRatio)
    {
      float baseScale = scaleFactor * (1f - SCALE_DECAY_FACTOR * t);
      float perspectiveScale = Mathf.Lerp(1f, depthScaleFactor, distanceRatio * perspectiveDistanceFactor);
      float finalScale = Mathf.Max(0.1f, baseScale * perspectiveScale);

      node.localScale = new Vector3(finalScale, finalScale, 1f);
    }
    #endregion

    #region 接口实现 - 配置方法
    /// <summary>
    /// 设置箭头节点数量
    /// </summary>
    /// <param name="count">箭头节点数量</param>
    public void SetArrowNodeCount(int count)
    {
      arrowNodeCount = Mathf.Max(3, count);
      if (isInitialized)
      {
        GenerateArrowNodes();
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