using UnityEngine;

namespace LearnUnity
{
    /// <summary>
    /// 贝塞尔曲线测试脚本
    /// 用于测试贝塞尔曲线控制器的功能
    /// </summary>
    public class BezierCurveTest : MonoBehaviour
    {
        [Header("测试设置")]
        [SerializeField] private BezierCurveController 贝塞尔曲线控制器;
        [SerializeField] private RectTransform 起始点;
        [SerializeField] private bool 自动测试 = false;
        [SerializeField] private bool 鼠标左键控制 = true;

        [Header("性能测试")]
        [SerializeField] private bool 显示性能统计 = false;
        private float 上次更新时间;
        private int 帧计数;
        private float 平均帧率;

        void Start()
        {
            // 获取贝塞尔曲线控制器组件
            if (贝塞尔曲线控制器 == null)
                贝塞尔曲线控制器 = GetComponent<BezierCurveController>();

            // 获取起始点
            if (起始点 == null)
                起始点 = GetComponent<RectTransform>();

            // 初始化控制器
            if (贝塞尔曲线控制器 != null)
                贝塞尔曲线控制器.初始化();

            // 自动测试
            if (自动测试 && 贝塞尔曲线控制器 != null && 起始点 != null)
            {
                贝塞尔曲线控制器.显示箭头(起始点);
            }

            上次更新时间 = Time.time;
        }

        void Update()
        {
            // 性能统计
            if (显示性能统计)
            {
                帧计数++;
                if (Time.time - 上次更新时间 >= 1f)
                {
                    平均帧率 = 帧计数 / (Time.time - 上次更新时间);
                    帧计数 = 0;
                    上次更新时间 = Time.time;
                }
            }

            // 鼠标控制
            if (鼠标左键控制)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    if (!贝塞尔曲线控制器.是否可见)
                        贝塞尔曲线控制器.显示箭头(起始点);
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    if (贝塞尔曲线控制器.是否可见)
                        贝塞尔曲线控制器.隐藏箭头();
                }
            }

            // 按空格键切换箭头显示状态
            if (Input.GetKeyDown(KeyCode.Space))
            {
                切换箭头显示();
            }

            // 按D键切换调试信息
            if (Input.GetKeyDown(KeyCode.D))
            {
                切换调试信息();
            }

            // 按P键切换性能统计
            if (Input.GetKeyDown(KeyCode.P))
            {
                显示性能统计 = !显示性能统计;
            }

            // 更新箭头可视化
            if (贝塞尔曲线控制器 != null && 贝塞尔曲线控制器.是否可见)
            {
                贝塞尔曲线控制器.更新箭头可视化();
                
                // 示例：基于鼠标位置设置结束图标状态
                bool 可使用 = Input.mousePosition.x > Screen.width * 0.5f;
                贝塞尔曲线控制器.设置结束图标状态(可使用);
            }
        }

        /// <summary>
        /// 切换箭头显示状态
        /// </summary>
        private void 切换箭头显示()
        {
            if (贝塞尔曲线控制器 == null || 起始点 == null) return;

            if (贝塞尔曲线控制器.是否可见)
            {
                贝塞尔曲线控制器.隐藏箭头();
            }
            else
            {
                贝塞尔曲线控制器.显示箭头(起始点);
            }
        }

        /// <summary>
        /// 切换调试信息显示
        /// </summary>
        private void 切换调试信息()
        {
            if (贝塞尔曲线控制器 == null) return;

            // 使用反射访问私有字段（仅用于调试）
            var 字段 = typeof(BezierCurveController).GetField("显示旋转调试信息", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (字段 != null)
            {
                bool 当前值 = (bool)字段.GetValue(贝塞尔曲线控制器);
                字段.SetValue(贝塞尔曲线控制器, !当前值);
                Debug.Log($"旋转调试信息: {(!当前值 ? "开启" : "关闭")}");
            }
        }

        /// <summary>
        /// 在编辑器中显示调试信息
        /// </summary>
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, 300));
            GUILayout.Label("=== 贝塞尔曲线测试控制台 ===", GUI.skin.GetStyle("box"));
            GUILayout.Space(5);
            
            GUILayout.Label($"控制器状态: {(贝塞尔曲线控制器?.是否可见 == true ? "显示" : "隐藏")}");
            
            GUILayout.Space(5);
            GUILayout.Label("=== 控制说明 ===");
            if (鼠标左键控制)
                GUILayout.Label("鼠标左键: 按住显示箭头，松开隐藏");
            GUILayout.Label("空格键: 切换箭头显示/隐藏");
            GUILayout.Label("D键: 切换旋转调试信息");
            GUILayout.Label("P键: 切换性能统计显示");
            
            if (贝塞尔曲线控制器?.是否可见 == true)
            {
                GUILayout.Space(5);
                GUILayout.Label("=== 实时信息 ===");
                var 目标位置 = 贝塞尔曲线控制器.获取目标位置();
                GUILayout.Label($"目标位置: ({目标位置.x:F1}, {目标位置.y:F1})");
                GUILayout.Label("鼠标移动到右半屏测试终点图标");
            }
            
            if (显示性能统计)
            {
                GUILayout.Space(5);
                GUILayout.Label("=== 性能统计 ===");
                GUILayout.Label($"帧率: {平均帧率:F1} FPS");
                GUILayout.Label($"当前时间: {Time.time:F1}s");
            }
            
            GUILayout.EndArea();
        }
    }
} 