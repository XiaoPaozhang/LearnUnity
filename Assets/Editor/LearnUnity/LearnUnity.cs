using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System;

namespace XpzUtility
{
  public class LearnUnityTemplateGenerate : EditorWindow
  {
    private const string PrefsKey = "XpzUtility.LastSelectedFolder";
    private static string winTitle = "学习unity的模板生成";
    private string templateName;
    // 保存相对路径，如 "Assets/..."
    private string selectedFolderPath;
    private Vector2 scrollPosition;

    private string FormatPath => Path.Combine(selectedFolderPath, templateName);

    [MenuItem("小炮仗的妙妙工具/学习unity的模板生成 %#_z")]
    private static void ShowWindow()
    {
      // 创建并打开窗口
      var win = GetWindow<LearnUnityTemplateGenerate>(winTitle);
      win.minSize = new Vector2(600, 300);
      win.Center();

    }

    private void OnEnable()
    {
      // 在OnEnable中加载路径（窗口创建或脚本重编译时触发）

      // 初始化时加载上次保存的路径
      string savedPath = EditorPrefs.GetString(PrefsKey, Application.dataPath);
      // 尝试将保存的路径转换为相对路径
      selectedFolderPath = ConvertToRelativePath(savedPath);
    }

    private void OnGUI()
    {
      scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

      // 添加拖拽区域
      DrawDragArea();

      // 目录选择区域
      EditorGUILayout.BeginVertical(GUI.skin.box);
      {
        EditorGUILayout.LabelField("当前目标目录:", selectedFolderPath);
      }
      EditorGUILayout.EndVertical();

      templateName = EditorGUILayout.TextField("模板名称:", templateName);

      if (GUILayout.Button("生成模板", GUILayout.Height(30)))
      {
        if (ValidateInput())
        {
          GenerateTemplate();
        }
      }

      if (EditorGUILayout.LinkButton($"清除目录{selectedFolderPath}"))
      {
        bool confirm = EditorUtility.DisplayDialog(
            "警告",
            $"确定要删除{selectedFolderPath}吗？",
            "确认",
            "取消"
        );

        if (confirm) ClearDirectory();
      }

      EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// 将绝对路径转换为相对于工程的路径（Assets/...）
    /// </summary>
    private static string ConvertToRelativePath(string absolutePath)
    {
      if (absolutePath.StartsWith(Application.dataPath))
      {
        return "Assets" + absolutePath.Substring(Application.dataPath.Length);
      }
      return absolutePath;
    }

    /// <summary>
    /// 将相对路径转换为绝对路径，便于调用文件选择面板时定位初始目录
    /// </summary>
    private static string AbsolutePathFromRelative(string relativePath)
    {
      if (relativePath.StartsWith("Assets"))
      {
        return Path.Combine(Application.dataPath, relativePath.Substring("Assets".Length));
      }
      return relativePath;
    }

    /// <summary>
    /// 使用AssetDatabase.DeleteAsset来删除目录，确保删除meta文件等
    /// </summary>
    private void ClearDirectory()
    {
      // 使用相对路径，直接操作AssetDatabase
      if (AssetDatabase.IsValidFolder(selectedFolderPath))
      {
        bool isDeleted = AssetDatabase.DeleteAsset(selectedFolderPath);
        if (isDeleted)
        {
          AssetDatabase.Refresh();
          Debug.Log($"已删除文件夹: {selectedFolderPath}");
        }
        else
        {
          Debug.LogError($"删除文件夹失败: {selectedFolderPath}");
        }
      }
      else
      {
        Debug.LogError("目录不存在或路径错误");
      }
    }

    private bool ValidateInput()
    {
      if (string.IsNullOrEmpty(templateName))
      {
        EditorUtility.DisplayDialog("错误", "请输入模板名称", "确定");
        return false;
      }

      if (!AssetDatabase.IsValidFolder(selectedFolderPath))
      {
        EditorUtility.DisplayDialog("错误", "目标目录不存在或无效，请重新选择", "确定");
        return false;
      }

      return true;
    }
    private void GenerateTemplate()
    {
      try
      {
        // 检查目标目录（FormatPath）是否存在，FormatPath为 "Assets/TemplateName"
        if (!AssetDatabase.IsValidFolder(FormatPath))
        {
          // 使用AssetDatabase.CreateFolder创建文件夹
          string parentFolder = selectedFolderPath; // 比如 "Assets/Xpznl"
          string newFolderName = templateName;        // 模板名称作为新文件夹名称
          AssetDatabase.CreateFolder(parentFolder, newFolderName);

          // 创建场景并保存到新建的文件夹中
          Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);
          string scenePath = Path.Combine(FormatPath, $"{templateName}Scene.unity");
          // 创建Main对象并延迟挂载脚本
          var main = new GameObject("Main");
          EditorSceneManager.SaveScene(newScene, scenePath);

          // 创建脚本文件（File IO操作）
          CreateScriptFile(FormatPath);
          AssetDatabase.Refresh();
        }
        else
        {
          Debug.LogWarning("模板目录已存在！");
        }
      }
      catch (Exception e)
      {
        EditorUtility.DisplayDialog("错误", $"生成失败: {e.Message}", "确定");
        Debug.LogError(e);
      }
    }

    private void CreateScriptFile(string rootPath)
    {
      string scriptContent = $@"
using UnityEngine;

namespace Test
{{
  public class {templateName}Test : MonoBehaviour
  {{
    void Start()
    {{
      
    }}

    void Update()
    {{
      
    }}
  }}
}}

";
      string scriptPath = Path.Combine(rootPath, $"{templateName}Test.cs");
      File.WriteAllText(scriptPath, scriptContent);

      AssetDatabase.Refresh();
    }

    /// <summary>
    /// 绘制文件夹拖拽区域
    /// </summary>
    private void DrawDragArea()
    {
      Rect dropArea = GUILayoutUtility.GetRect(0.0f, 60.0f, GUILayout.ExpandWidth(true));
      GUI.Box(dropArea, "点击选择或拖拽文件夹到这里", EditorStyles.helpBox);

      Event evt = Event.current;
      switch (evt.type)
      {
        case EventType.MouseDown:
          if (dropArea.Contains(evt.mousePosition))
          {
            HandleDirectorySelection();
            evt.Use();
          }
          break;

        case EventType.DragUpdated:
        case EventType.DragPerform:
          if (!dropArea.Contains(evt.mousePosition))
            return;

          DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

          if (evt.type == EventType.DragPerform)
          {
            DragAndDrop.AcceptDrag();
            foreach (var draggedObject in DragAndDrop.objectReferences)
            {
              string path = AssetDatabase.GetAssetPath(draggedObject);
              if (AssetDatabase.IsValidFolder(path))
              {
                UpdateSelectedPath(path);
                break;
              }
            }
          }
          evt.Use();
          break;
      }
    }
    private void HandleDirectorySelection()
    {
      string newPath = "";
      if (Selection.activeObject != null)
      {
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (AssetDatabase.IsValidFolder(assetPath))
        {
          newPath = assetPath;
        }
      }

      if (string.IsNullOrEmpty(newPath))
      {
        newPath = EditorUtility.OpenFolderPanel("选择模板目录",
            AbsolutePathFromRelative(selectedFolderPath), "");
      }

      if (!string.IsNullOrEmpty(newPath)) UpdateSelectedPath(newPath);
    }

    private void UpdateSelectedPath(string newPath)
    {
      if (newPath.StartsWith(Application.dataPath) || newPath.StartsWith("Assets"))
      {
        selectedFolderPath = ConvertToRelativePath(newPath);
        EditorPrefs.SetString(PrefsKey, newPath);
      }
      else
      {
        EditorUtility.DisplayDialog("错误", "请选择工程内的目录（Assets下）", "确定");
      }
    }
    // 窗口居中方法
    private void Center()
    {
      var main = EditorGUIUtility.GetMainWindowPosition();
      position = new Rect(
          main.x + (main.width - position.width) * 0.5f,
          main.y + (main.height - position.height) * 0.5f,
          position.width,
          position.height
      );
    }
  }
}
