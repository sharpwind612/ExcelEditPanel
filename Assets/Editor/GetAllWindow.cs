using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

class GetAllWindow : EditorWindow
{
    static List<Type> windowList = new List<Type>();
    [MenuItem("TestContextMenu/Open Window")]

    static void Init()
    {
        var window = GetWindow(typeof(GetAllWindow));
        window.titleContent = new GUIContent("ZZG");
        window.position = new Rect(200, 200, 400, 800);
        window.Show();
        windowList = getWindowAll();
    }

    [MenuItem("TestContextMenu/getWindowAll")]

    static void getWindow()
    {
        windowList = getWindowAll();
    }
    /// <summary>
    /// 获取所有窗口类型
    /// </summary>
    /// <returns></returns>
    static List<Type> getWindowAll()
    {
        Assembly assembly = typeof(EditorWindow).Assembly; //获取UnityEditor程序集，当然你也可以直接加载UnityEditor程序集来获取，我这里图方便,具体方法看一下程序集的加载Assembly.Load();
        Type[] types = assembly.GetTypes();
        List<Type> list = new List<Type>();
        for (int i = 0; i < types.Length; i++)
        {
            if (isEditorWindow(types[i]))
            {
                if (types[i].Name == "GameView")
                {
                    Debug.Log(types[i].FullName);
                }

                if (types[i].Name == "SceneView")
                {
                    Debug.Log(types[i].FullName);
                }
                list.Add(types[i]);
            }

        }
        list.Sort((a, b) => { return string.Compare(a.Name, b.Name); });  //排序
        return list;
    }
    /// <summary>
    /// 判断是否是编辑器窗口
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    static bool isEditorWindow(Type type)
    {
        int i = 0;
        Type temp = type;
        while (null != temp && i < 10000)
        {
            i++;
            if (temp.BaseType == typeof(EditorWindow))
            {
                return true;
            }
            temp = temp.BaseType;
        }
        return false;
    }
    /// <summary>
    /// 关闭所有窗口
    /// </summary>
    void closeWindowAll()
    {
        for (int i = 0; i < windowList.Count; i++)
        {
            try
            {
                EditorWindow editorWindow = EditorWindow.GetWindow(windowList[i]);
                if (editorWindow)
                {
                    editorWindow.Close();           //关闭窗口
                }
            }
            catch
            {

            }
        }
    }
    void showWindowAll()
    {
        for (int i = 0; i < windowList.Count; i++)
        {
            try
            {
                EditorWindow editorWindow = EditorWindow.GetWindow(windowList[i]);
                if (editorWindow)
                {
                    editorWindow.Show();        //打开窗口
                }
            }
            catch
            {

            }
        }
    }
    /// <summary>
    /// 显示指定类型窗口
    /// </summary>
    /// <param name="type"></param>
    void showWindow(Type type)
    {
        try
        {
            EditorWindow editorWindow = EditorWindow.GetWindow(type);
            if (editorWindow)
            {
                editorWindow.Show();
            }
        }
        catch
        {

        }
    }
    Vector2 pos = new Vector2(0, 0);
    void OnGUI()
    {
        if (GUILayout.Button("关闭所有窗口"))
        {
            closeWindowAll();
        }
        //if (GUILayout.Button("打开所有窗口"))
        //{
        //    showWindowAll();
        //}
        pos = GUILayout.BeginScrollView(pos);
        for (int i = 0; i < windowList.Count; i++)
        {
            if (GUILayout.Button(windowList[i].Name))
            {
                showWindow(windowList[i]);
            }
        }
        GUILayout.EndScrollView();
    }
}