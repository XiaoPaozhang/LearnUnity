using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LearnUnity
{
  /// <summary>
  /// 贝塞尔曲线管理器
  /// 完整的贝塞尔曲线箭头系统，包含输入处理和业务逻辑
  /// 可直接在项目中使用
  /// </summary>
  public class BezierCurveManager : MonoBehaviour
  {
    [Header("贝塞尔曲线控制器")]
    [SerializeField] private BezierCurve curveController;

    [Header("输入设置")]
    [SerializeField] private bool enableMouseControl = true;
    [SerializeField] private bool enableTouchControl = false;

    [Header("起始点设置")]
    [SerializeField] private RectTransform defaultStartPoint;

    // 当前状态
    private RectTransform currentStartPoint;
    private bool isDragging = false;

    // 外部事件（供其他系统订阅）
    public Action<Vector2> OnDragStart;
    public Action<Vector2> OnDragging;
    public Action<Vector2, bool> OnDragEnd; // 位置, 是否有效

    void Start()
    {
      // 获取或创建贝塞尔曲线控制器
      if (curveController == null)
        curveController = GetComponent<BezierCurve>();

      // 初始化控制器
      if (curveController != null)
        curveController.Initialize();

      // 设置默认起始点
      if (defaultStartPoint == null)
        defaultStartPoint = GetComponent<RectTransform>();
    }

    void Update()
    {
      if (enableMouseControl)
        HandleMouseInput();

      if (enableTouchControl)
        HandleTouchInput();

      // 更新箭头可视化
      if (isDragging && curveController?.IsVisible == true)
      {
        curveController.UpdateArrowVisualization();

        // 检查目标位置有效性
        bool isTargetValid = CheckTargetValidity();
        curveController.SetEndIconState(isTargetValid);

        // 触发拖拽中事件
        Vector2 currentPosition = curveController.GetTargetPosition();
        OnDragging?.Invoke(currentPosition);
      }
    }

    #region 输入处理

    private void HandleMouseInput()
    {
      // 鼠标按下开始
      if (Input.GetMouseButtonDown(0) && !isDragging)
      {
        // 检查是否点击在UI上
        if (!EventSystem.current.IsPointerOverGameObject())
        {
          ShowArrow();
        }
      }
      // 鼠标松开结束
      else if (Input.GetMouseButtonUp(0) && isDragging)
      {
        HideArrow();
      }
    }

    private void HandleTouchInput()
    {
      if (Input.touchCount == 1)
      {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began && !isDragging)
        {
          // 检查是否点击在UI上
          if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
          {
            ShowArrow();
          }
        }
        else if ((touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) && isDragging)
        {
          HideArrow();
        }
      }
    }

    #endregion

    #region 属性

    /// <summary>
    /// 当前是否正在显示箭头
    /// </summary>
    public bool IsArrowVisible => isDragging && curveController?.IsVisible == true;

    #endregion

    #region 公共方法

    /// <summary>
    /// 开始显示箭头（从指定起始点）
    /// </summary>
    /// <param name="startPoint">箭头起始位置</param>
    public void ShowArrow(RectTransform startPoint = null)
    {
      if (curveController == null) return;

      currentStartPoint = startPoint ?? defaultStartPoint;
      if (currentStartPoint == null) return;

      curveController.ShowArrow(currentStartPoint);
      isDragging = true;

      // 外部事件
      OnDragStart?.Invoke(currentStartPoint.position);
    }

    /// <summary>
    /// 停止显示箭头
    /// </summary>
    public void HideArrow()
    {
      if (curveController == null) return;

      Vector2 finalPosition = curveController.GetTargetPosition();
      bool isPositionValid = CheckTargetValidity();

      curveController.HideArrow();
      isDragging = false;

      // 外部事件
      OnDragEnd?.Invoke(finalPosition, isPositionValid);
    }

    /// <summary>
    /// 获取当前目标位置
    /// </summary>
    public Vector2 GetCurrentTargetPosition()
    {
      return curveController?.GetTargetPosition() ?? Vector2.zero;
    }

    #endregion

    #region 自定义逻辑

    /// <summary>
    /// 检查目标位置是否有效（可以根据项目需求自定义）
    /// </summary>
    private bool CheckTargetValidity()
    {
      // 示例：检查鼠标是否在屏幕右半部分
      Vector2 mouseScreenPosition = Input.mousePosition;
      return mouseScreenPosition.x > Screen.width * 0.5f;
    }

    #endregion
  }
}