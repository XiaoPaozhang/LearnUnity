using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LearnUnity
{
    /// <summary>
    /// 贝塞尔曲线控制器
    /// 提供贝塞尔曲线箭头绘制功能，支持3D透视效果和动态视觉反馈
    /// </summary>
    public class BezierCurveController : MonoBehaviour
    {
        #region 常量定义
        private const float 默认缩放因子 = 1f;
        private const float 隐藏位置偏移 = -10000f;
        private const int 控制点数量 = 4;
        private const float 缩放衰减因子 = 0.1f;
        #endregion

        #region 序列化字段
        [Header("箭头预制体")]
        [SerializeField] private GameObject 箭头起始预制体;
        [SerializeField] private GameObject 箭头中段预制体;
        [SerializeField] private GameObject 箭头结束预制体;

        [Header("终点图标")]
        [SerializeField] private Sprite 箭头可用精灵;
        [SerializeField] private Sprite 箭头不可用精灵;

        [Header("箭头配置")]
        [SerializeField] private int 箭头节点数量 = 12;
        [SerializeField] private float 缩放因子 = 默认缩放因子;
        [SerializeField] private float 箭头偏移 = 0f;

        [Header("贝塞尔曲线设置")]
        [Range(0f, 5f)]
        [SerializeField] private float 高度因子 = 1.2f;
        [SerializeField] private bool 使用垂直抛物线 = true;
        [Range(0.1f, 2f)]
        [SerializeField] private float 曲线宽度 = 0.3f;

        [Header("调试设置")]
        [SerializeField] private bool 显示旋转调试信息 = false;

        [Header("3D透视效果")]
        [Range(0f, 89f)]
        [SerializeField] private float 透视角度 = 25f;
        [Range(0.1f, 2f)]
        [SerializeField] private float 深度缩放因子 = 0.85f;
        [Range(0f, 1f)]
        [SerializeField] private float 透视距离因子 = 0.5f;
        #endregion

        #region 私有字段
        private RectTransform 起始变换;
        private List<RectTransform> 箭头节点列表 = new List<RectTransform>();

        /// <summary>
        /// 控制点列表,用于存储控制点位置
        /// 初始化时,默认添加4个控制点,每个控制点位置为Vector2.zero
        /// 控制点列表[0]为起始点,控制点列表[3]为结束点
        /// 控制点列表[1]和控制点列表[2]为中间控制点,初始位置为Vector2.zero
        /// </summary>
        private List<Vector2> 控制点列表 = new List<Vector2>();
        private readonly List<Vector2> 控制点因子列表 = new List<Vector2> 
        { 
            new Vector2(0.25f, 0.5f), 
            new Vector2(0.75f, 0.8f) 
        };
        private Camera 主摄像机;
        private Canvas 父级画布;
        private bool 已初始化 = false;
        #endregion

        #region 属性
        /// <summary>
        /// 获取箭头是否可见
        /// </summary>
        public bool 是否可见 { get; private set; }
        #endregion

        #region Unity生命周期
        private void Awake()
        { 
            Debug.Log($"[Awake] {gameObject.name}通过 Camera.main获取主摄像机,获取不到就FindObjectOfType<Camera>()");
            主摄像机 = Camera.main ?? FindObjectOfType<Camera>();
            Debug.Log($"[Awake] {gameObject.name}必须有父级canvas,通过 GetComponentInParent<Canvas>()获取父级画布");
            父级画布 = GetComponentInParent<Canvas>();
            
            // 添加安全检查
            if (父级画布 == null)
            {
                Debug.LogError("没有Canvas,贝塞尔曲线控制器无法工作");
                return;
            }
            
            初始化控制点();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 初始化贝塞尔曲线控制器，如果未分配则使用默认资源
        /// </summary>
        public void 初始化()
        {
            Debug.Log($"[初始化]");
            Debug.Log($"[初始化] 检查资源,如果已初始化,则不进行初始化,结果为{(已初始化?"已初始化":"未初始化")}");
            if (已初始化) return;

            Debug.Log($"[初始化] 检查资源");
            检查资源(); 
            Debug.Log($"[初始化] 检查资源完成");
            Debug.Log($"[初始化] 生成箭头节点");
            生成箭头节点();
            隐藏箭头();
            已初始化 = true;
        }

        /// <summary>
        /// 显示从指定起始变换开始的箭头
        /// </summary>
        /// <param name="起始点">箭头的起始点</param>
        public void 显示箭头(RectTransform 起始点)
        { 
            if (!已初始化) 初始化();

            起始变换 = 起始点;
            Cursor.visible = false;
            gameObject.SetActive(true);
            是否可见 = true;
        }

        /// <summary>
        /// 隐藏箭头并恢复鼠标光标可见性
        /// </summary>
        public void 隐藏箭头()
        {
            Cursor.visible = true;
            gameObject.SetActive(false);
            是否可见 = false;
        }

        /// <summary>
        /// 根据当前鼠标位置更新箭头可视化
        /// 当箭头可见时应在Update中调用
        /// </summary>
        public void 更新箭头可视化()
        {
            if (!是否可见 || 主摄像机 == null || 起始变换 == null) return;

            更新控制点();
            更新箭头节点位置();
            更新箭头节点旋转();
        }

        /// <summary>
        /// 设置结束箭头图标的视觉状态
        /// </summary>
        /// <param name="可使用">目标位置是否有效</param>
        public void 设置结束图标状态(bool 可使用)
        {
            if (箭头节点列表.Count == 0) return;

            var 结束节点 = 箭头节点列表[箭头节点列表.Count - 1];
            var 图像组件 = 结束节点.GetComponent<Image>();
            
            if (图像组件 != null)
            {
                图像组件.sprite = 可使用 ? 箭头可用精灵 : 箭头不可用精灵;
            }
        }

        /// <summary>
        /// 获取当前世界空间中的目标位置
        /// </summary>
        /// <returns>箭头指向的目标位置</returns>
        public Vector2 获取目标位置()
        {
            if (控制点列表.Count > 3)
                return 控制点列表[3];
            
            return 主摄像机.ScreenToWorldPoint(Input.mousePosition);
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 如果需要则加载默认资源
        /// </summary>
        private void 检查资源()
        {
            if (箭头起始预制体 == null)
                Debug.LogError("箭头起始预制体为空");
            
            if (箭头中段预制体 == null)
                Debug.LogError("箭头中段预制体为空");
            
            if (箭头结束预制体 == null)
                Debug.LogError("箭头结束预制体为空");
            
            if (箭头可用精灵 == null)
                Debug.LogError("箭头可用精灵为空");
            
            if (箭头不可用精灵 == null)
                Debug.LogError("箭头不可用精灵为空");
        }

        /// <summary>
        /// 初始化控制点列表
        /// </summary>
        private void 初始化控制点()
        {
            Debug.Log($"[初始化控制点]");
            Debug.Log($"[初始化控制点] 清空控制点列表");
            控制点列表.Clear();
            Debug.Log($"[初始化控制点] 开始循环添加控制点,{控制点数量}个控制点");
            for (int i = 0; i < 控制点数量; i++)
            {
                Debug.Log($"[初始化控制点] 添加控制点{i},默认添加Vector2.zero");
                控制点列表.Add(Vector2.zero);
            }
            Debug.Log($"[初始化控制点] 控制点列表初始化完成");
        }

        /// <summary>
        /// 生成箭头节点
        /// </summary>
        private void 生成箭头节点()
        {
            清理现有节点();

            // 起始节点
            if (箭头起始预制体 != null)
            {
                var 起始节点 = Instantiate(箭头起始预制体, transform);
                箭头节点列表.Add(起始节点.GetComponent<RectTransform>());
            }

            // 中段节点
            for (int i = 1; i < 箭头节点数量 - 1; i++)
            {
                if (箭头中段预制体 != null)
                {
                    var 中段节点 = Instantiate(箭头中段预制体, transform);
                    箭头节点列表.Add(中段节点.GetComponent<RectTransform>());
                }
            }

            // 结束节点
            if (箭头结束预制体 != null)
            {
                var 结束节点 = Instantiate(箭头结束预制体, transform);
                箭头节点列表.Add(结束节点.GetComponent<RectTransform>());
            }

            // 初始隐藏所有节点
            隐藏所有节点();
        }

        /// <summary>
        /// 清理现有节点
        /// </summary>
        private void 清理现有节点()
        {
            foreach (var 节点 in 箭头节点列表)
            {
                if (节点 != null)
                    DestroyImmediate(节点.gameObject);
            }
            箭头节点列表.Clear();
        }

        /// <summary>
        /// 隐藏所有节点
        /// </summary>
        private void 隐藏所有节点()
        {
            var 隐藏位置 = new Vector2(隐藏位置偏移, 隐藏位置偏移);
            箭头节点列表.ForEach(节点 => 节点.position = 隐藏位置);
        }

        /// <summary>
        /// 更新控制点
        /// </summary>
        private void 更新控制点()
        {
            if (起始变换 == null) return;

            Vector2 起始点;
            Vector2 结束点;

            // 根据Canvas的渲染模式处理坐标转换
            if (父级画布.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // 屏幕空间覆盖模式，直接使用屏幕坐标
                起始点 = 起始变换.position;
                结束点 = Input.mousePosition;
            }
            else if (父级画布.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // 屏幕空间摄像机模式
                起始点 = 起始变换.position;
                Vector3 鼠标世界坐标 = 主摄像机.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 父级画布.planeDistance));
                结束点 = 鼠标世界坐标;
            }
            else
            {
                // 世界空间模式
                起始点 = 起始变换.position;
                结束点 = 主摄像机.ScreenToWorldPoint(Input.mousePosition);
            }

            控制点列表[0] = 起始点;
            控制点列表[3] = 结束点;

            if (使用垂直抛物线)
            {
                更新垂直抛物线控制点(起始点, 结束点);
            }
            else
            {
                更新标准贝塞尔控制点(起始点, 结束点);
            }
        }

        /// <summary>
        /// 更新垂直抛物线控制点
        /// </summary>
        private void 更新垂直抛物线控制点(Vector2 起始点, Vector2 结束点)
        {
            float 距离 = Vector2.Distance(起始点, 结束点);
            float 高度 = 距离 * 高度因子;

            // 改进中点计算，使曲线更自然
            Vector2 方向 = (结束点 - 起始点).normalized;
            Vector2 垂直方向 = new Vector2(-方向.y, 方向.x); // 垂直于连线的方向
            
            Vector2 中点 = new Vector2(
                Mathf.Lerp(起始点.x, 结束点.x, 0.5f) + 垂直方向.x * 距离 * 曲线宽度 * 0.3f,
                Mathf.Max(起始点.y, 结束点.y) + 高度
            );

            控制点列表[1] = 中点;
            控制点列表[2] = 结束点;
        }

        /// <summary>
        /// 更新标准贝塞尔控制点
        /// </summary>
        private void 更新标准贝塞尔控制点(Vector2 起始点, Vector2 结束点)
        {
            Vector2 方向 = 结束点 - 起始点;
            控制点列表[1] = 起始点 + 方向 * 控制点因子列表[0];
            控制点列表[2] = 起始点 + 方向 * 控制点因子列表[1];
        }

        /// <summary>
        /// 更新箭头节点位置
        /// </summary>
        private void 更新箭头节点位置()
        {
            Vector2 起始点 = 控制点列表[0];
            Vector2 结束点 = 控制点列表[3];
            float 总距离 = Vector2.Distance(起始点, 结束点);

            // 避免除零错误
            if (总距离 < 0.001f) return;

            for (int i = 0; i < 箭头节点列表.Count; i++)
            {
                float t = 计算节点参数(i);
                Vector2 位置 = 计算贝塞尔点(t);
                
                // 根据Canvas模式设置位置
                if (父级画布.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    箭头节点列表[i].position = new Vector3(位置.x, 位置.y, 0);
                }
                else
                {
                    箭头节点列表[i].position = new Vector3(位置.x, 位置.y, 起始变换.position.z);
                }

                // 应用3D透视效果
                float 距离比例 = Vector2.Distance(起始点, 位置) / 总距离;
                应用3D透视效果(箭头节点列表[i], t, 距离比例);
            }
        }

        /// <summary>
        /// 更新箭头节点旋转
        /// </summary>
        private void 更新箭头节点旋转()
        {
            if (箭头节点列表.Count < 2) return;

            Vector2 起始点 = 控制点列表[0];
            Vector2 结束点 = 控制点列表[3];
            float 总距离 = Vector2.Distance(起始点, 结束点);

            // 使用精确的切线计算方法
            for (int i = 0; i < 箭头节点列表.Count; i++)
            {
                Vector2 切线方向 = 计算精确切线方向(i);
                
                // 智能方向校正：检测是否需要反转切线方向
                if (需要反转切线方向(i, 切线方向))
                {
                    切线方向 = -切线方向;
                }
                
                float 基础旋转Z = Vector2.SignedAngle(Vector2.up, 切线方向) - 箭头偏移;
                
                // 计算透视旋转效果（可选）
                float 距离比例 = 总距离 > 0.001f ? Vector2.Distance(起始点, 箭头节点列表[i].position) / 总距离 : 0f;
                float 透视旋转X = Mathf.Lerp(0, 透视角度, 距离比例 * 透视距离因子);
                
                // 组合最终旋转
                Vector3 最终旋转 = new Vector3(透视旋转X, 0, 基础旋转Z);
                箭头节点列表[i].rotation = Quaternion.Euler(最终旋转);

                // 详细调试信息
                if (显示旋转调试信息 && Application.isPlaying)
                {
                    分析旋转差异(i, 切线方向, 基础旋转Z);
                }
            }
        }

        /// <summary>
        /// 计算精确的切线方向（使用贝塞尔曲线导数）
        /// </summary>
        private Vector2 计算精确切线方向(int 节点索引)
        {
            float t = 计算节点参数(节点索引);
            Vector2 切线;
            
            if (使用垂直抛物线)
            {
                // 二次贝塞尔曲线 B(t) = (1-t)²P₀ + 2(1-t)tP₁ + t²P₂
                // 导数 B'(t) = -2(1-t)P₀ + 2(1-2t)P₁ + 2tP₂
                float 一减t = 1f - t;
                切线 = -2f * 一减t * 控制点列表[0] +
                      2f * (一减t - t) * 控制点列表[1] +
                      2f * t * 控制点列表[2];
            }
            else
            {
                // 三次贝塞尔曲线 B(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃
                // 导数 B'(t) = -3(1-t)²P₀ + 3(1-t)²P₁ - 6(1-t)tP₁ + 6(1-t)tP₂ - 3t²P₂ + 3t²P₃
                float 一减t = 1f - t;
                float 一减t平方 = 一减t * 一减t;
                float t平方 = t * t;
                
                切线 = -3f * 一减t平方 * 控制点列表[0] +
                      3f * 一减t平方 * 控制点列表[1] -
                      6f * 一减t * t * 控制点列表[1] +
                      6f * 一减t * t * 控制点列表[2] -
                      3f * t平方 * 控制点列表[2] +
                      3f * t平方 * 控制点列表[3];
            }
            
            // 检查切线是否为零向量
            if (切线.magnitude < 0.001f)
            {
                // 如果切线为零，使用相邻节点方法作为备选
                if (节点索引 > 0)
                {
                    切线 = 箭头节点列表[节点索引].position - 箭头节点列表[节点索引 - 1].position;
                }
                else if (节点索引 < 箭头节点列表.Count - 1)
                {
                    切线 = 箭头节点列表[节点索引 + 1].position - 箭头节点列表[节点索引].position;
                }
                else
                {
                    切线 = Vector2.up; // 默认向上
                }
            }
            
            return 切线.normalized;
        }

        /// <summary>
        /// 智能检测是否需要反转切线方向
        /// </summary>
        private bool 需要反转切线方向(int 节点索引, Vector2 切线方向)
        {
            // 使用相邻节点方向作为参考
            Vector2 参考方向 = Vector2.zero;
            bool 有参考方向 = false;
            
            // 优先使用前进方向（从前一个节点到当前节点）
            if (节点索引 > 0)
            {
                参考方向 = (箭头节点列表[节点索引].position - 箭头节点列表[节点索引 - 1].position).normalized;
                有参考方向 = true;
            }
            // 备选：使用前向方向（从当前节点到下一个节点）
            else if (节点索引 < 箭头节点列表.Count - 1)
            {
                参考方向 = (箭头节点列表[节点索引 + 1].position - 箭头节点列表[节点索引].position).normalized;
                有参考方向 = true;
            }
            
            if (!有参考方向) return false;
            
            // 计算切线方向与参考方向的点积
            float 点积 = Vector2.Dot(切线方向, 参考方向);
            
            // 如果点积为负，说明方向相反，需要反转
            bool 需要反转 = 点积 < 0;
            
            if (显示旋转调试信息 && Application.isPlaying)
            {
                Debug.Log($"节点{节点索引} - 切线方向: {切线方向}, 参考方向: {参考方向}, 点积: {点积:F3}, 需要反转: {需要反转}");
            }
            
            return 需要反转;
        }

        /// <summary>
        /// 分析不同旋转计算方法的差异
        /// </summary>
        private void 分析旋转差异(int 节点索引, Vector2 切线方向, float 切线旋转)
        {
            string 节点类型 = 节点索引 == 0 ? "起始" : (节点索引 == 箭头节点列表.Count - 1 ? "结束" : "中段");
            float t = 计算节点参数(节点索引);
            
            // 方法1: 切线方向
            float 切线角度 = Vector2.SignedAngle(Vector2.up, 切线方向);
            
            // 方法2: 相邻节点方向
            Vector2 相邻节点方向 = Vector2.zero;
            float 相邻节点角度 = 0f;
            if (节点索引 > 0)
            {
                相邻节点方向 = (箭头节点列表[节点索引].position - 箭头节点列表[节点索引 - 1].position).normalized;
                相邻节点角度 = Vector2.SignedAngle(Vector2.up, 相邻节点方向);
            }
            
            // 方法3: 反向切线（测试是否需要反转）
            Vector2 反向切线 = -切线方向;
            float 反向切线角度 = Vector2.SignedAngle(Vector2.up, 反向切线);
            
            // 方法4: 使用前向差分估算切线
            Vector2 前向切线 = Vector2.zero;
            float 前向切线角度 = 0f;
            if (节点索引 < 箭头节点列表.Count - 1)
            {
                前向切线 = (箭头节点列表[节点索引 + 1].position - 箭头节点列表[节点索引].position).normalized;
                前向切线角度 = Vector2.SignedAngle(Vector2.up, 前向切线);
            }
            
            Debug.Log($"=== 节点{节点索引}({节点类型}) t={t:F3} ===");
            Debug.Log($"切线方向: {切线方向} → 角度: {切线角度:F1}°");
            Debug.Log($"相邻方向: {相邻节点方向} → 角度: {相邻节点角度:F1}°");
            Debug.Log($"反向切线: {反向切线} → 角度: {反向切线角度:F1}°");
            Debug.Log($"前向切线: {前向切线} → 角度: {前向切线角度:F1}°");
            
            // 分析哪个角度更合理
            string 建议 = "";
            if (节点索引 > 0 && Mathf.Abs(切线角度 - 相邻节点角度) > 90f)
            {
                建议 += "切线方向与相邻节点方向差异>90°，可能需要反转；";
            }
            if (节点索引 < 箭头节点列表.Count - 1 && Mathf.Abs(切线角度 - 前向切线角度) > 90f)
            {
                建议 += "切线方向与前向方向差异>90°，可能需要反转；";
            }
            if (建议.Length > 0)
            {
                Debug.LogWarning($"节点{节点索引}建议: {建议}");
            }
        }

        /// <summary>
        /// 计算节点参数
        /// </summary>
        private float 计算节点参数(int 节点索引)
        {
            // 使用线性分布，确保节点间距均匀
            return (float)节点索引 / (箭头节点列表.Count - 1);
        }

        /// <summary>
        /// 计算贝塞尔曲线上的点
        /// </summary>
        private Vector2 计算贝塞尔点(float t)
        {
            if (使用垂直抛物线)
            {
                // 二次贝塞尔用于抛物线
                float 一减t = 1f - t;
                return 一减t * 一减t * 控制点列表[0] +
                       2f * 一减t * t * 控制点列表[1] +
                       t * t * 控制点列表[2];
            }
            else
            {
                // 三次贝塞尔用于复杂曲线
                return Mathf.Pow(1 - t, 3) * 控制点列表[0] +
                       3 * Mathf.Pow(1 - t, 2) * t * 控制点列表[1] +
                       3 * (1 - t) * Mathf.Pow(t, 2) * 控制点列表[2] +
                       Mathf.Pow(t, 3) * 控制点列表[3];
            }
        }

        /// <summary>
        /// 应用3D透视效果
        /// </summary>
        private void 应用3D透视效果(RectTransform 节点, float t, float 距离比例)
        {
            // 修复缩放计算错误 - 使用正确的参数t
            float 基础缩放 = 缩放因子 * (1f - 缩放衰减因子 * t);
            
            // 应用透视缩放
            float 透视缩放 = Mathf.Lerp(1f, 深度缩放因子, 距离比例 * 透视距离因子);
            float 最终缩放 = Mathf.Max(0.1f, 基础缩放 * 透视缩放); // 防止缩放为0
            
            节点.localScale = new Vector3(最终缩放, 最终缩放, 1f);

            // 注意：不在这里设置旋转，避免覆盖正确的方向旋转
            // 透视旋转效果已在更新旋转方法中处理

            // 可选：应用透明度效果
            应用透明度效果(节点, 距离比例);
        }

        /// <summary>
        /// 应用透明度效果
        /// </summary>
        private void 应用透明度效果(RectTransform 节点, float 距离比例)
        {
            var 精灵渲染器 = 节点.GetComponent<SpriteRenderer>();
            if (精灵渲染器 != null)
            {
                Color 颜色 = 精灵渲染器.color;
                颜色.a = Mathf.Lerp(1f, 0.5f, 距离比例 * 透视距离因子);
                精灵渲染器.color = 颜色;
            }
        }
        #endregion

        #region 编辑器方法
        #if UNITY_EDITOR
        [ContextMenu("测试初始化")]
        private void 测试初始化()
        {
            初始化();
        }

        [ContextMenu("重置初始化状态")]
        private void 重置初始化状态()
        {
            已初始化 = false;
            清理现有节点();
            Debug.Log($"[重置初始化状态] 已将初始化状态重置为false，并清理了现有节点");
        }

        [ContextMenu("测试显示箭头")]
        private void 测试显示箭头()
        {
            if (起始变换 == null)
                起始变换 = GetComponent<RectTransform>();
            显示箭头(起始变换);
        }

        [ContextMenu("强制重新初始化")]
        private void 强制重新初始化()
        {
            重置初始化状态();
            初始化();
            Debug.Log($"[强制重新初始化] 已完成强制重新初始化");
        }
        #endif
        #endregion
    }
} 