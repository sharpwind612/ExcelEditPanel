using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SerializationTest.Common;

namespace SerializationTest.Test5
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
            
            if (EditorGUI.DropdownButton(dropdownbuttonRect, new GUIContent(m_Type[selectedType]), FocusType.Passive))
            {
                GenericMenu menu = new GenericMenu();
                for (int i=0;i < m_Type.Length;i++)
                {
                    menu.AddItem(new GUIContent(m_Type[i]), false, ItemCallBack, i);
                }
                menu.DropDown(dropdownbuttonRect);
            }
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