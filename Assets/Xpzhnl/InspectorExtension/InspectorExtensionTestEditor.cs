
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Test
{
    [CustomEditor(typeof(InspectorExtensionTest))]
    public class InspectorExtensionTestEditor : Editor
    {
        private SerializedProperty atk;
        private SerializedProperty def;
        private SerializedProperty obj;
        private bool foldOut;

        public string[] strs;
        public int[] ints;
        public GameObject[] gameObjects;

        public List<GameObject> listObjs;
        private void OnEnable()
        {
            // 这样就得到与测试脚本对应的字段 注意传入的字符串自定义脚本中的成员名一致
            atk = serializedObject.FindProperty("atk");
            def = serializedObject.FindProperty("def");
            obj = serializedObject.FindProperty("obj");
        }
        public override void OnInspectorGUI()
        {
            //base.OnInspectorGUI();//注释掉父类调用后,Inspector窗口默认显示的atk def obj会消失

            serializedObject.Update();

            foldOut = EditorGUILayout.BeginFoldoutHeaderGroup(foldOut, "基础属性");
            if (foldOut)
            {
                if (GUILayout.Button("打印当前target对象"))
                {
                    Debug.Log("组件类型" + target.GetType());
                    Debug.Log("组件依附的游戏对象名" + target.name);
                }
                EditorGUILayout.IntSlider(atk, 0, 100, "攻击力");
                def.floatValue = EditorGUILayout.FloatField("防御力", def.floatValue);
                EditorGUILayout.ObjectField(obj, new GUIContent("敌对对象"));
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}

