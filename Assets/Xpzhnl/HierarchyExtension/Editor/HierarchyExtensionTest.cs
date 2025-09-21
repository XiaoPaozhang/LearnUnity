
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Test
{
    /// <summary>
    /// Hierarchy窗口扩展测试类
    /// 用于自定义Unity编辑器的Hierarchy窗口显示效果
    /// </summary>
    public class HierarchyExtensionTest
    {
        // 设置显示图标和Toggle的最小窗口宽度（单位：像素）
        private const float MinWindowWidth = 240f;
        // 图标偏移量（单位：像素），方便统一修改
        private const float IconOffset = 18f;

        /// <summary>
        /// 编辑器启动时自动初始化方法
        /// 使用[InitializeOnLoadMethod]特性确保在编辑器启动时自动调用
        /// </summary>
        [InitializeOnLoadMethod]
        static void HierarchyExtensionIcon()
        {
            // 创建激活对象的样式（绿色文字）
            var activeStyle = new GUIStyle() { normal = { textColor = Color.green } };
            // 创建未激活对象的样式（半透明绿色文字）
            var inactiveStyle = new GUIStyle() { normal = { textColor = new Color(0, 1, 0, 0.5F) } };

            // 订阅Hierarchy窗口的GUI绘制事件
            // 每次在Hierarchy窗口中绘制游戏对象时都会触发这个回调
            EditorApplication.hierarchyWindowItemOnGUI += (int instanceID, Rect selectionRect) =>
            {
                // 第一步：将instanceID转换为GameObject对象
                GameObject go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (go == null) return; // 如果转换失败，直接返回

                // 第二步：初始化图标索引，用于计算图标的显示位置
                int index = 0;

                // 第三步：绘制对象激活状态切换按钮
                DrawActiveToggle(go, selectionRect, ref index);

                // 第四步：绘制静态标记（如果对象是静态的）
                DrawStatic(go, selectionRect, ref index);

                // 第五步：绘制组件图标（这里以BoxCollider为例）
                DrawRectIcon<BoxCollider>(go, selectionRect, ref index);

                // 第六步：重绘对象名称（使用不同的颜色显示激活状态）
                DrawGameObjectName(go, selectionRect, activeStyle, inactiveStyle);
            };
        }

        /// <summary>
        /// 获取 Hierarchy 窗口的宽度
        /// 通过反射获取Unity内部的Hierarchy窗口实例
        /// </summary>
        /// <returns>Hierarchy窗口的宽度，如果获取失败返回0</returns>
        private static float GetHierarchyWindowWidth()
        {
            // 第一步：使用反射获取UnityEditor.SceneHierarchyWindow类型的lastInteractedHierarchyWindow属性
            // 这个属性存储了最后交互的Hierarchy窗口实例
            PropertyInfo hierarchyInfo = typeof(Editor).Assembly
                .GetType("UnityEditor.SceneHierarchyWindow")
                ?.GetProperty("lastInteractedHierarchyWindow", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            // 第二步：获取Hierarchy窗口实例
            EditorWindow hierarchyWindow = (EditorWindow)hierarchyInfo?.GetValue(null);

            // 第三步：返回窗口宽度，如果窗口为null则返回0
            return hierarchyWindow?.position.width ?? 0;
        }

        /// <summary>
        /// 获取图标绘制区域的Rect
        /// 根据Hierarchy窗口宽度和图标索引计算图标的显示位置
        /// </summary>
        /// <param name="selectionRect">Unity提供的选择矩形区域</param>
        /// <param name="index">图标索引（从1开始，决定图标的横向位置）</param>
        /// <returns>计算后的图标绘制矩形</returns>
        private static Rect GetRect(Rect selectionRect, int index)
        {
            // 第一步：复制原始的selectionRect，避免修改原对象
            Rect rect = new Rect(selectionRect);

            // 第二步：根据窗口宽度决定图标的排列方向
            if (GetHierarchyWindowWidth() >= MinWindowWidth)
            {
                // 窗口足够宽：图标从右向左排列
                // 每个图标占用IconOffset像素宽度，乘以索引计算偏移量
                rect.x += rect.width - (IconOffset * index);
            }
            else
            {
                // 窗口较窄：图标从左向右排列（在对象名称之后）
                rect.x += rect.width + (IconOffset * index);
            }

            // 第三步：设置图标的固定宽度为IconOffset像素
            rect.width = IconOffset;

            // 第四步：返回计算后的矩形
            return rect;
        }

        /// <summary>
        /// 绘制激活状态的Toggle按钮
        /// 允许用户直接在Hierarchy窗口中切换游戏对象的激活状态
        /// 支持单选和多选对象批量操作
        /// </summary>
        /// <param name="go">要绘制激活状态的对象</param>
        /// <param name="selectionRect">Unity提供的绘制区域</param>
        /// <param name="index">图标索引（引用传递，会自动递增）</param>
        private static void DrawActiveToggle(GameObject go, Rect selectionRect, ref int index)
        {
            // 第一步：递增图标索引，为当前图标预留位置
            index++;

            // 第二步：计算当前Toggle按钮的绘制区域
            Rect rect = GetRect(selectionRect, index);

            // 第三步：获取当前对象的激活状态
            bool currentActiveState = go.activeSelf;

            // 第四步：检查鼠标点击事件
            // 只有当鼠标在Toggle按钮区域内按下时才处理点击
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                // 第五步：获取当前选中的所有游戏对象
                GameObject[] selectedObjects = Selection.gameObjects;

                // 第六步：检查当前对象是否在选中列表中
                if (!selectedObjects.Contains(go))
                {
                    // 如果当前对象未被选中，则将其设为活动对象
                    // 这样可以让未选中的对象也能响应激活状态切换
                    Selection.activeGameObject = go;
                    selectedObjects = new GameObject[] { go };
                }

                // 第七步：计算新的激活状态（取反当前状态）
                bool newActiveState = !currentActiveState;

                // 第八步：记录撤销操作
                // 这样用户可以通过Ctrl+Z撤销激活状态的更改
                Undo.RecordObjects(selectedObjects, "Toggle Active State");

                // 第九步：批量设置选中对象的激活状态
                foreach (GameObject selectedObject in selectedObjects)
                {
                    selectedObject.SetActive(newActiveState);
                }

                // 第十步：标记所有场景为已修改状态
                // 这样Unity会提示用户保存场景更改
                EditorSceneManager.MarkAllScenesDirty();

                // 第十一步：标记事件为已使用，防止事件继续传递给其他GUI元素
                Event.current.Use();
            }

            // 第十二步：绘制Toggle按钮
            // 使用GUI.Toggle绘制激活状态按钮（空字符串表示不显示文字，只显示复选框）
            GUI.Toggle(rect, currentActiveState, string.Empty);
        }
        /// <summary>
        /// 绘制静态标记
        /// 如果游戏对象被标记为静态，则在Hierarchy窗口中显示"S"标记
        /// 静态对象不会参与物理计算和动画，可以提高性能
        /// </summary>
        /// <param name="go">要检查和绘制的游戏对象</param>
        /// <param name="selectionRect">Unity提供的绘制区域</param>
        /// <param name="index">图标索引（引用传递，会自动递增）</param>
        private static void DrawStatic(GameObject go, Rect selectionRect, ref int index)
        {
            // 第一步：检查游戏对象是否为静态
            // 只有静态对象才会显示标记
            if (go.isStatic)
            {
                // 第二步：递增图标索引，为静态标记预留位置
                index++;

                // 第三步：计算静态标记的绘制区域
                Rect rect = GetRect(selectionRect, index);

                // 第四步：绘制"S"标签表示静态对象
                // 使用GUI.Label在指定区域绘制文字
                GUI.Label(rect, "S");
            }
            // 如果对象不是静态的，直接跳过，不占用图标位置
        }
        /// <summary>
        /// 绘制组件的Icon
        /// 使用泛型方法检查游戏对象是否包含指定类型的组件，如果有则显示该组件的图标
        /// </summary>
        /// <typeparam name="T">要检查的组件类型，必须继承自Component</typeparam>
        /// <param name="go">要检查和绘制的游戏对象</param>
        /// <param name="selectionRect">Unity提供的绘制区域</param>
        /// <param name="index">图标索引（引用传递，会自动递增）</param>
        private static void DrawRectIcon<T>(GameObject go, Rect selectionRect, ref int index) where T : Component
        {
            // 第一步：检查游戏对象是否包含指定类型的组件
            // 使用泛型方法GetComponent<T>()进行类型安全的组件检查
            if (go.GetComponent<T>() != null)
            {
                // 第二步：递增图标索引，为组件图标预留位置
                index++;

                // 第三步：计算组件图标的绘制区域
                Rect rect = GetRect(selectionRect, index);

                // 第四步：调用DrawIcon方法绘制具体的组件图标
                DrawIcon<T>(rect);
            }
            // 如果对象没有指定类型的组件，直接跳过，不占用图标位置
        }

        /// <summary>
        /// 绘制Unity原生Icon
        /// 获取Unity内置的组件图标并在指定区域绘制
        /// </summary>
        /// <typeparam name="T">组件类型，用于获取对应的图标</typeparam>
        /// <param name="rect">图标绘制区域</param>
        private static void DrawIcon<T>(Rect rect)
        {
            // 第一步：使用EditorGUIUtility.ObjectContent获取Unity内置的组件图标
            // 参数1：传入null表示不需要具体的对象实例
            // 参数2：传入组件类型，Unity会返回该类型的默认图标
            var icon = EditorGUIUtility.ObjectContent(null, typeof(T)).image;

            // 第二步：使用GUI.Label在指定区域绘制获取到的图标
            GUI.Label(rect, icon);
        }
        /// <summary>
        /// 绘制对象名称
        /// 重写游戏对象的名称显示，使用不同颜色表示激活状态
        /// 对于预制件对象跳过重绘（使用Unity默认的显示方式）
        /// </summary>
        /// <param name="go">要绘制名称的游戏对象</param>
        /// <param name="selectionRect">Unity提供的绘制区域</param>
        /// <param name="activeStyle">激活对象的文本样式（绿色）</param>
        /// <param name="inactiveStyle">未激活对象的文本样式（半透明绿色）</param>
        private static void DrawGameObjectName(GameObject go, Rect selectionRect, GUIStyle activeStyle, GUIStyle inactiveStyle)
        {
            // 第一步：调整绘制区域的X坐标，为图标留出IconOffset像素空间
            // 这样名称显示不会与左侧的图标重叠
            selectionRect.x += IconOffset;

            // 第二步：根据对象的激活状态选择合适的文本样式
            // 激活对象使用activeStyle（绿色），未激活对象使用inactiveStyle（半透明绿色）
            GUIStyle style = go.activeSelf ? activeStyle : inactiveStyle;

            // 第三步：检查对象是否为预制件的一部分
            // 如果是预制件，则跳过自定义绘制，使用Unity默认的预制件显示方式
            if (PrefabUtility.IsPartOfAnyPrefab(go)) return;

            // 第四步：使用GUI.Label绘制对象名称
            // 使用选定的样式显示名称，体现对象的激活状态
            GUI.Label(selectionRect, go.name, style);
        }

    }
}
