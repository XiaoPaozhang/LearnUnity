using System;
using UnityEngine;

namespace LearnUnity
{
  /// <summary>
  /// 贝塞尔曲线接口
  /// 定义贝塞尔曲线箭头的基本功能
  /// </summary>
  public interface IBezierCurve
  {
    /// <summary>
    /// 获取箭头是否可见
    /// </summary>
    bool IsVisible { get; }

    /// <summary>
    /// 初始化贝塞尔曲线控制器
    /// </summary>
    void Initialize();

    /// <summary>
    /// 显示从指定起始变换开始的箭头
    /// </summary>
    /// <param name="startPoint">箭头的起始点</param>
    void ShowArrow(RectTransform startPoint);

    /// <summary>
    /// 显示从指定坐标开始的箭头
    /// </summary>
    /// <param name="startPosition">箭头的起始坐标（UI坐标）</param>
    /// <param name="referenceTransform">参考变换（用于坐标系统参考，可为null）</param>
    void ShowArrow(Vector2 startPosition, RectTransform referenceTransform = null);

    /// <summary>
    /// 隐藏箭头并恢复鼠标光标可见性
    /// </summary>
    void HideArrow();

    /// <summary>
    /// 根据当前鼠标位置更新箭头可视化
    /// </summary>
    void UpdateArrowVisualization();

    /// <summary>
    /// 设置结束箭头图标的视觉状态
    /// </summary>
    /// <param name="isAvailable">目标位置是否有效</param>
    void SetEndIconState(bool isAvailable);

    /// <summary>
    /// 获取当前世界空间中的目标位置
    /// </summary>
    /// <returns>箭头指向的目标位置</returns>
    Vector2 GetTargetPosition();

    /// <summary>
    /// 设置箭头节点数量
    /// </summary>
    /// <param name="count">箭头节点数量</param>
    void SetArrowNodeCount(int count);

    /// <summary>
    /// 设置缩放因子
    /// </summary>
    /// <param name="factor">缩放因子</param>
    void SetScaleFactor(float factor);

    /// <summary>
    /// 设置高度因子
    /// </summary>
    /// <param name="factor">高度因子</param>
    void SetHeightFactor(float factor);

    /// <summary>
    /// 设置是否使用垂直抛物线
    /// </summary>
    /// <param name="use">是否使用垂直抛物线</param>
    void SetVerticalParabolaMode(bool use);

    /// <summary>
    /// 设置透视角度
    /// </summary>
    /// <param name="angle">透视角度（0-89度）</param>
    void SetPerspectiveAngle(float angle);
  }
}