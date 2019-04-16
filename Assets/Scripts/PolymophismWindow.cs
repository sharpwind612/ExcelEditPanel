using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SerializationTest.Common;
using System.Reflection;

namespace UnityEditor
{
	[Serializable]
	public class Zoo {
		public List<Animal> animals = new List<Animal>();
	}

    public struct ClickInfo {
        public int index;
        public int type;
    }
		
	public class PolymophismWindow : EditorWindow {
		private const string WINDOW_TITLE = "Polymophism";

		private Zoo zoo;

		void OnEnable() {
			titleContent.text = WINDOW_TITLE;

			if(zoo == null) {
				zoo = new Zoo();
			}
		}

        Rect zoneRect
        {
            get { return new Rect(20, 20, position.width/2 - 40, 200); }
        }

        Rect contentRect
        {
            get { return new Rect(20, 20, position.width / 2 - 70, 200); }
        }

        Rect buttonRect
        {
            get { return new Rect(20, 230, position.width / 2 - 40, 25); }
        }

        Rect dropdownButtonRect
        {
            get { return new Rect(20, 265, position.width / 2 - 40, 25); }
        }

        int selectedType = 1;

        int selectedOption = 1;

        string[] m_Type = {
            "Type1",
            "Type2",
            "Type3",
        };

        Vector2 sp;
        void Start()
        {
            sp[0] = 20;
            sp[1] = 20;
        }

        void OnGUI() {
            EditorGUILayout.BeginHorizontal();
            Rect dropdownbuttonRect = new Rect(0, 0, 70, 20);
            var _contentRect = contentRect;
            _contentRect.height = (zoo.animals.Count + 1) * 20;

            EditorGUI.DrawRect(zoneRect, new Color(0.2f, 0.2f, 0.2f));
            //GUILayout.BeginArea(zoneRect, GUI.skin.box);
            sp = GUI.BeginScrollView(zoneRect, sp, _contentRect);
            //GUI.Label(new Rect(100, 200, Screen.width, 50), "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            GUILayout.BeginArea(_contentRect);
            EditorGUILayout.LabelField("animal count", zoo.animals.Count.ToString());
			for(int i = 0; i < zoo.animals.Count; i++) {
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(i.ToString(), zoo.animals[i].Species);

                if (EditorGUILayout.DropdownButton(new GUIContent(m_Type[zoo.animals[i].type]), FocusType.Passive))
                {
                    GenericMenu menu = new GenericMenu();
                    for (int j = 0; j < m_Type.Length; j++)
                    {
                        var info = new ClickInfo
                        {
                            index = i,
                            type = j
                        };
                        menu.AddItem(new GUIContent(m_Type[j]), false, ItemCallBack2, info);
                    }
                    //menu.DropDown(GUILayoutUtility.GetLastRect());
                    menu.ShowAsContext();
                }

                if (GUILayout.Button("remove", GUILayout.Width(70))) {
					zoo.animals.RemoveAt(i);
				}
				EditorGUILayout.EndHorizontal();
			}
            GUILayout.EndArea();
            GUI.EndScrollView();
            EditorGUILayout.EndHorizontal();
            //GUILayout.EndArea();

            GUILayout.BeginArea(buttonRect, GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("add Cat"))
            {
                zoo.animals.Add(new Cat());
            }
            if (GUILayout.Button("add Dog"))
            {
                zoo.animals.Add(new Dog());
            }
            if (GUILayout.Button("add Giraffe"))
            {
                zoo.animals.Add(new Giraffe());
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            GUILayout.BeginArea(dropdownButtonRect, GUI.skin.box);
            EditorGUILayout.BeginHorizontal();
            if (EditorGUILayout.DropdownButton(new GUIContent(m_Type[selectedType]), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                for (int i=0;i < m_Type.Length;i++)
                {
                    menu.AddItem(new GUIContent(m_Type[i]), false, ItemCallBack, i);
                }
                menu.ShowAsContext();
            }
            //GUILayout.FlexibleSpace();
            this.selectedOption = EditorGUILayout.Popup("Popup", (int)this.selectedOption, new string[] { "s1", "s2", "s3" });
            if (GUILayout.Button("Select Animation"))
            {
                AnimationClip fbxObj = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Data/Emotes--RussianDance.anim.fbx");                     
                Selection.activeObject = fbxObj;

                //Set certain model to the AvatarPreview window
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/Robot_A.fbx");
                Type t = typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimationClipEditor");                
                System.Reflection.MethodInfo[] methodInfos = t.GetMethods();
                //EditorWindow window = EditorWindow.GetWindow(t);
            }

            if (GUILayout.Button("Get Window"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Data/Robot_A.fbx");
                Type t = typeof(EditorWindow).Assembly.GetType("UnityEditor.AvatarPreview");
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo[] methodInfos = t.GetMethods(flags);
                var setPreviewFunc = t.GetMethod("SetPreview", flags);
                //m.Invoke()
                Type previewWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PreviewWindow");
                EditorWindow previewWindow = EditorWindow.GetWindow(previewWindowType);
                Type inspectorWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
                EditorWindow inspectorWindow = EditorWindow.GetWindow(inspectorWindowType);

                //IPreviewable[] editorsWithPreviews = GetEditorsWithPreviews(tracker.activeEditors);
                //IPreviewable editor = GetEditorThatControlsPreview(editorsWithPreviews);
                //var member = previewWindowType.GetField("m_PreviewWindow", flags).GetValue(previewWindow);
                setPreviewFunc.Invoke(previewWindow, new System.Object[] {prefab});
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void ItemCallBack(object typeIndex)
        {
            Debug.Log("Select:" + typeIndex);
            selectedType = (int)typeIndex;
        }

        private void ItemCallBack2(object obj)
        {           
            var clickInfo = (ClickInfo)obj;
            Debug.Log("Select:" + clickInfo.index + "," + clickInfo.type);
            zoo.animals[clickInfo.index].type = (int)clickInfo.type;
        }

        [MenuItem ("Window/Serialization Test/Test 5 - " + WINDOW_TITLE)]
		public static void  ShowWindow()
		{
			EditorWindow.GetWindow<PolymophismWindow>();
		}

	} // class TestWindow5

} // namespace SerializationTest.Test5