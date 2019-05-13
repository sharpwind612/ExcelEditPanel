using System;
using System.Collections.Generic;
using System.Data;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using EditorTool;

namespace UnityEditor.ExcelTreeView
{
	class MultiColumnWindow : EditorWindow
	{
        #region 字段定义

        [NonSerialized] bool m_Initialized;
		[SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
        //[SerializeField] TreeViewState m_TreeViewState2;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
        //[SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState2;
        SearchField m_SearchField;
        MultiColumnTreeView m_TreeView;
        //MultiColumnTreeView m_TreeView2;
        List<string> m_nameList;   //TODO:暂时当做字符串，后续当成一个索引去查表
        List<string> m_discribeList;   //描述文本，如果有则优先使用
        List<ExcelTreeElement> m_excelData;
        DataTable m_dataTable; //缓存读取excel表格后的dataTable
        DataRow m_selectedData; //当前选中的数据

        //分属于三个不同分区的字段
        List<string> m_normalList;
        List<string> m_multipleList;
        Dictionary<string,bool> m_expressionDict;

        struct FieldInfo {
            public string name;
            public int type;
            public string describe;
            public int state;
            public bool bIsEnum;
        }
        Dictionary<string, FieldInfo> m_fieldDict;

        struct EnumInfo {
            public string name;
            public int value;

            public EnumInfo(string _name,int _value)
            {
                name = _name;
                value = _value;
            }
        }
        Dictionary<string, Dictionary<int,EnumInfo>> m_fieldEnumDict;

        //int m_selectedId;
        const int m_keyRow = 1; // start from 0
        const int m_discribeMaxLength = 7;
        const int m_DiscribeRowCount = 3;
        public List<string> nameList
        {
            get { return m_nameList; }
        }
        public List<string> discribeList
        {
            get { return m_discribeList; }
        }

        public List<ExcelTreeElement> excelData
        {
            get { return m_excelData; }
        }
        //MyTreeAsset m_MyTreeAsset;
        string m_excelPath;// = "D:/Projects/MultiColumnTreeView/Assets/Data/test.xlsx";
        string excelPath
        {
            get {
                if (m_excelPath == null)
                    m_excelPath = Application.dataPath + "/Data/test.xlsx";
                return m_excelPath;
            }
            set { m_excelPath = value; }
        }

        string m_fieldRulePath;
        string fieldRulePath
        {
            get
            {
                if (m_fieldRulePath == null)
                    m_fieldRulePath = Application.dataPath + "/Data/fieldRule.xlsx";
                return m_fieldRulePath;
            }
            set { m_fieldRulePath = value; }
        }

        [MenuItem("ExcelEditor/Open Panel")]
		public static MultiColumnWindow GetWindow ()
		{
			var window = GetWindow<MultiColumnWindow>();
			window.titleContent = new GUIContent("Multi Columns");
			window.Focus();
			window.Repaint();
			return window;
		}

        Rect filePathRect
        {
            get { return new Rect(20, 10, position.width - 40, position.height - 60); }
        }

        Rect toolbarRect
        {
            get { return new Rect(20f, 50f, position.width - 40f, 20f); }
        }

        Rect multiColumnTreeViewRect
		{
			get { return new Rect(20, 70, position.width-40, 200); }
		}

        Rect bottomToolbarRect
		{
			get { return new Rect(20f, position.height - 25f, position.width - 40f, 16f); }
		}

		public MultiColumnTreeView treeView
		{
			get { return m_TreeView; }
		}

        #endregion

        void OnGUI ()
		{
			InitIfNeeded();           
            FilePathBar(filePathRect);
            SearchBar(toolbarRect);
			DoTreeView(multiColumnTreeViewRect);
            DrawNormalEditZone();
            DrawMultipleEffectZone();
            DrawExpressionZone();
            BottomToolBar(bottomToolbarRect);
        }

        #region 主表格区

        void InitIfNeeded()
        {
            if (!m_Initialized)
            {
                // Check if it already exists (deserialized from window layout file or scriptable object)
                if (m_TreeViewState == null)
                    m_TreeViewState = new TreeViewState();

                bool firstInit = m_MultiColumnHeaderState == null;
                var headerState = MultiColumnTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width, nameList);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);
                m_MultiColumnHeaderState = headerState;

                var multiColumnHeader = new MyMultiColumnHeader(headerState);
                if (firstInit)
                    multiColumnHeader.ResizeToFit();

                TreeModel<ExcelTreeElement> treeModel;
                if (m_excelData == null)
                {
                    treeModel = new TreeModel<ExcelTreeElement>(GetEmptyData());
                }
                else
                {
                    treeModel = new TreeModel<ExcelTreeElement>(m_excelData);
                }

                m_TreeView = new MultiColumnTreeView(m_TreeViewState, multiColumnHeader, treeModel, this);

                m_SearchField = new SearchField();
                m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

                m_Initialized = true;
            }
        }

        void FilePathBar(Rect rect)
        {
            GUILayout.BeginArea(rect);
            using (new EditorGUILayout.HorizontalScope())
            { 
                var style = "miniButton";
                GUILayout.Label("请输入表格文件路径:");
                excelPath = GUILayout.TextField(excelPath, GUILayout.Width(400));

                if (GUILayout.Button("Load Excel", style, GUILayout.Width(80)))
                {
                    LoadExcelData();
                }
                if (GUILayout.Button("Save to Excel", style, GUILayout.Width(100)))
                {
                    SaveExcelData();
                }
                GUILayout.FlexibleSpace();
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                var style = "miniButton";
                GUILayout.Label("请输入规则文件所在目录:");
                fieldRulePath = GUILayout.TextField(fieldRulePath, GUILayout.Width(400));

                if (GUILayout.Button("Load Excel", style, GUILayout.Width(80)))
                {
                    LoadFieldRuleData();
                }
                if (m_normalList == null)
                    GUILayout.Label("未加载规则文件!");
                
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndArea();
        }

        void SearchBar (Rect rect)
		{
			treeView.searchString = m_SearchField.OnGUI (rect, treeView.searchString);
		}

		void DoTreeView (Rect rect)
		{
			m_TreeView.OnGUI(rect);
		}

        void BottomToolBar (Rect rect)
		{
			GUILayout.BeginArea (rect);

			using (new EditorGUILayout.HorizontalScope ())
			{

				var style = "miniButton";
                if (GUILayout.Button("加载技能预览场景", style))
                {
                    
                }
                GUILayout.FlexibleSpace();

                var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
                myColumnHeader.mode = MyMultiColumnHeader.Mode.MinimumHeaderWithoutSorting;
            }
			GUILayout.EndArea();
		}

        #endregion

        #region 表格数据操作

        IList<ExcelTreeElement> GetEmptyData()
        {
            List<ExcelTreeElement> dataList = new List<ExcelTreeElement>();
            dataList.Add(new ExcelTreeElement("root", -1, 0, new string[] { "root" }));
            return dataList;
        }

        //Load excel data from file
        void LoadExcelData()
        {
            Debug.Log("Start load excel data!!!");
            //使用NPOI库来读取Excel
            //DataTable dataTale = EditorTool.ExcelHelper.ReadExcel(excelPath);
            NPOIExcelHelper excelHelper = new NPOIExcelHelper(excelPath);
            DataTable dataTale = excelHelper.ExcelToDataTable("Sheet1", true, 1);
            if (dataTale == null)
            {
                return;
            }
            int columns = dataTale.Columns.Count;
            int rows = dataTale.Rows.Count;
            //取当做key的一行作为名字
            List<string> nameList = new List<string>();           
            for (int i = 0; i < columns; i++)
            {
                nameList.Add(dataTale.Rows[m_keyRow][i].ToString());
            }
            m_nameList = nameList;

            //取当做key的一行作为名字
            List<string> discribeList = new List<string>();
            string str = "";
            for (int i = 0; i < columns; i++)
            {
                str = dataTale.Rows[m_keyRow + 1][i].ToString();
                //限制一下描述的最大长度
                if (str.Length > m_discribeMaxLength)
                {
                    str = str.Substring(m_discribeMaxLength);
                }
                discribeList.Add(str);
            }
            m_discribeList = discribeList;
           
            List<ExcelTreeElement> dataList = new List<ExcelTreeElement>();
            dataList.Add(new ExcelTreeElement("root", -1, 0, new string[]{"root"}));
            int index = 0;
            //从第(m_DiscribeRowCount + 1)行开始读内容,从0开始
            for (int i = m_DiscribeRowCount; i < rows; i++)
            {
                string[] temp = new string[columns];
                for (int j = 0; j < columns; j++)
                {
                    temp[j] = dataTale.Rows[i][j].ToString();
                }
                dataList.Add(new ExcelTreeElement(temp[0], 0, ++index, temp));
            }
            m_excelData = dataList;
            m_dataTable = dataTale;
            m_Initialized = false;

            //TODO：Test Normal Select
            //m_normalList = new List<int>();
            //m_normalList.Add(0);
            //m_normalList.Add(1);
            //m_normalList.Add(2);
            //m_normalList.Add(3);
            //m_normalList.Add(7);

            //m_multipleList = new List<int>();
            //m_multipleList.Add(4);
            //m_multipleList.Add(5);

            Debug.Log("Load excel data success!!!");
        }

        //Save excel data to file
        void SaveExcelData()
        {
            Debug.Log("Start save excel data!!!");
            //使用NPOI库来保存Excel
            NPOIExcelHelper excelHelper = new NPOIExcelHelper(excelPath);
            excelHelper.DataTableToExcel(m_dataTable, "Sheet1", false);
            Debug.Log("Save excel data success!!!");
        }

        //读取字段规则数据
        void LoadFieldRuleData()
        {
            Debug.Log("Start load field rule data!!!");
            //使用NPOI库来读取Excel
            NPOIExcelHelper excelHelper = new NPOIExcelHelper(fieldRulePath);
            DataTable dataTale = excelHelper.ExcelToDataTable("Sheet1", true, 0);
            if (dataTale == null)
            {
                return;
            }
            int columns = dataTale.Columns.Count;
            int rows = dataTale.Rows.Count;
            //取当做key的一行作为名字
            m_fieldDict = new Dictionary<string, FieldInfo>();
            m_fieldEnumDict = new Dictionary<string, Dictionary<int, EnumInfo>>();
            m_normalList = new List<string>();
            m_multipleList = new List<string>();
            m_expressionDict = new Dictionary<string, bool>();

            DataRow dataRow;
            FieldInfo fieldInfo;
            //struct FieldInfo
            //{
            //    string name;
            //    int type;
            //    string describe;
            //    int state;
            //    bool bIsEnum;
            //}
            for (int i = 1; i < rows; i++)
            {
                dataRow = dataTale.Rows[i];
                fieldInfo = new FieldInfo();
                fieldInfo.name = dataRow[0].ToString();
                fieldInfo.type = int.Parse(dataRow[1].ToString());
                fieldInfo.describe = dataRow[2].ToString();
                fieldInfo.state = int.Parse(dataRow[3].ToString());
                if (dataRow[4].ToString().Equals(""))
                {
                    fieldInfo.bIsEnum = false;
                }
                else
                {
                    //如果是枚举类型，将枚举数值插入枚举字典
                    fieldInfo.bIsEnum = true;
                    Dictionary<int, EnumInfo> enumDict;
                    if (m_fieldEnumDict.TryGetValue(fieldInfo.name, out enumDict) == false)
                    {
                        enumDict = new Dictionary<int, EnumInfo>();
                        m_fieldEnumDict.Add(fieldInfo.name, enumDict);
                    }
                    string enum_name = dataRow[5].ToString();
                    int enum_value = int.Parse(dataRow[4].ToString());
                    EnumInfo info = new EnumInfo(enum_name, enum_value);
                    enumDict.Add(enum_value, info);
                }

                if (m_fieldDict.ContainsKey(fieldInfo.name) == false)
                    m_fieldDict.Add(fieldInfo.name, fieldInfo);

                //根据字段类型插入不同的列表
                if(fieldInfo.type == 0 && m_normalList.Contains(fieldInfo.name) == false)
                    m_normalList.Add(fieldInfo.name);
                if (fieldInfo.type == 1 && m_multipleList.Contains(fieldInfo.name) == false)
                    m_multipleList.Add(fieldInfo.name);
                if (fieldInfo.type == 2 && m_expressionDict.ContainsKey(fieldInfo.name) == false)
                    m_expressionDict.Add(fieldInfo.name,false);
            }

            Debug.Log("Load field rule success!!!");
        }

        public void ChangeDataTable(int rowIndex, int columnIndex, string value)
        {
            if (m_dataTable.Rows[rowIndex + m_DiscribeRowCount] != null)
            {
                if (m_dataTable.Rows[rowIndex + m_DiscribeRowCount][columnIndex] != null)
                {
                    //Debug.Log(value);
                    m_dataTable.Rows[rowIndex + m_DiscribeRowCount][columnIndex] = value;
                }
            }
        }

        public void SingleClickedItem(int id)
        {
            string str = "";
            for (int i = 0; i < m_dataTable.Columns.Count; ++i)
            {
                str = string.Format("{0}  {1}", str, m_dataTable.Rows[id + m_DiscribeRowCount - 1][i].ToString());
            }
            Debug.Log(str);
            m_selectedData = m_dataTable.Rows[id + m_DiscribeRowCount - 1];           
        }

        #endregion

        #region 普通字段区

        Rect normalLabelRect
        {
            get { return new Rect(20, 280, position.width / 2 - 30, 20); }
        }
        Rect normalZoneRect
        {
            get { return new Rect(20, 300, position.width / 2 - 30, 200); }
        }

        //Rect zoneRect
        //{
        //    get { return new Rect(20, 20, position.width / 2 - 40, 200); }
        //}

        Rect normalContentRect
        {
            get { return new Rect(20, 300, position.width / 2 - 60, 200); }
        }
        Vector2 normalSp;

        //string[] m_Type = {
        //    "Type0",
        //    "Type1",
        //    "Type2",
        //    "Type3",
        //};

        void DrawNormalEditZone()
        {          
            GUI.Label(normalLabelRect, "普通字段区");
            EditorGUI.DrawRect(normalZoneRect, new Color(0.2f, 0.2f, 0.2f));

            if (m_selectedData != null && m_normalList != null)
            {
                var contentRect = normalContentRect;
                contentRect.height = (m_normalList.Count + 1) * 17;
                normalSp = GUI.BeginScrollView(normalZoneRect, normalSp, contentRect);
                GUILayout.BeginArea(contentRect);
                //EditorGUILayout.LabelField("animal count", zoo.animals.Count.ToString());
                string name = "";
                FieldInfo fieldInfo;
                string oldStr = "";
                string curStr = "";
                for (int i = 0; i < m_dataTable.Columns.Count; i++)
                {
                    name = nameList[i];
                    if (m_normalList.Contains(name) == false)
                    {
                        continue;
                    }
                    fieldInfo = m_fieldDict[name];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(fieldInfo.describe, GUILayout.Width(120));

                    if (fieldInfo.bIsEnum == true)
                    {
                        var enumDict = m_fieldEnumDict[name];
                        var value = int.Parse(m_selectedData[i].ToString());
                        if (EditorGUILayout.DropdownButton(new GUIContent(enumDict[value].name), FocusType.Passive))
                        {
                            GenericMenu menu = new GenericMenu();
                            //for (int j = 0; j < enumDict.Count; j++)
                            //{
                            //    var info = new ClickInfo
                            //    {
                            //        index = i,
                            //        type = j
                            //    };
                            //    menu.AddItem(new GUIContent(m_Type[j]), false, ItemCallBack, info);
                            //}

                            foreach (KeyValuePair<int, EnumInfo> kvp in enumDict)
                            {
                                var info = new ClickInfo
                                {
                                    index = i,
                                    type = kvp.Value.value
                                };
                                menu.AddItem(new GUIContent(kvp.Value.name), false, ItemCallBack, info);
                            }
                            //menu.DropDown(GUILayoutUtility.GetLastRect());
                            menu.ShowAsContext();
                        }
                    }
                    else
                    {
                        oldStr = m_selectedData[i].ToString();
                        curStr = EditorGUILayout.TextField(oldStr);
                        m_selectedData[i] = curStr;
                        if (curStr.CompareTo(oldStr) != 0)
                        {
                            m_TreeView.UpdateContent(i, m_selectedData[i].ToString());
                        }
                    }
                    //if (GUILayout.Button("remove", GUILayout.Width(70)))
                    //{
                    //    zoo.animals.RemoveAt(i);
                    //}
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.EndArea();
                GUI.EndScrollView();
            }
        }


        private void ItemCallBack(object obj)
        {
            var clickInfo = (ClickInfo)obj;
            Debug.Log("Select:" + clickInfo.index + "," + clickInfo.type);
            m_selectedData[clickInfo.index] = (int)clickInfo.type;
            m_TreeView.UpdateContent(clickInfo.index, m_selectedData[clickInfo.index].ToString());
        }
        #endregion

        #region 多次生效字段区

        Rect multipleLabelRect
        {
            get { return new Rect(position.width / 2 + 10, 280, position.width / 2 - 30, 20); }
        }
        Rect multipleZoneRect
        {
            get { return new Rect(position.width / 2 + 10, 300, position.width / 2 - 30, 200); }
        }

        Rect multipleContentRect
        {
            get { return new Rect(position.width / 2 + 10, 300, position.width / 2 - 60, 200); }
        }

        Vector2 multipleSp;

        void DrawMultipleEffectZone()
        {
            GUI.Label(multipleLabelRect, "多次生效配置");
            EditorGUI.DrawRect(multipleZoneRect, new Color(0.2f, 0.2f, 0.2f));
            if (m_selectedData != null && m_multipleList != null)
            {
                var contentRect = multipleContentRect;
                contentRect.height = (m_multipleList.Count + 1) * 34;
                contentRect.width = 85 * 11;
                multipleSp = GUI.BeginScrollView(multipleZoneRect, multipleSp, contentRect);
                GUILayout.BeginArea(contentRect);
                //EditorGUILayout.LabelField("animal count", zoo.animals.Count.ToString());
                string name = "";
                for (int i = 0; i < m_dataTable.Columns.Count; i++)
                {
                    name = nameList[i];
                    string oldStr = "";
                    string curStr = "";
                    if (m_multipleList.Contains(name) == false)
                    {
                        continue;
                    }
                    var fieldInfo = m_fieldDict[name];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(fieldInfo.describe, GUILayout.Width(80));

                    string numberDiscribe = "";
                    for (int j = 1; j <= 10; j++)
                    {
                        numberDiscribe = string.Format("第{0}项", j);
                        EditorGUILayout.LabelField(numberDiscribe, GUILayout.Width(80));
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(80));

                    oldStr = m_selectedData[i].ToString();
                    string[] fields = oldStr.Split('#');
                    string[] tempFields = new string[10];
                    for (int j = 0; j < 10; j++)
                    {
                        if (j < fields.Length)
                            tempFields[j] = fields[j];
                        else
                            tempFields[j] = "";
                        tempFields[j] = EditorGUILayout.TextField(tempFields[j], GUILayout.Width(80));
                    }
                    if (fields[0] != null)
                    {
                        for (int j = 0; j < 10; j++)
                        {
                            if (tempFields[j] != null && tempFields[j].Equals("") == false)
                            {
                                if (j == 0)
                                    curStr += tempFields[j];
                                else
                                    curStr += ("#" + tempFields[j]);
                            }
                        }
                    }
                    m_selectedData[i] = curStr;
                    if (curStr.CompareTo(oldStr) != 0)
                    {
                        m_TreeView.UpdateContent(i, m_selectedData[i].ToString());
                    }
                    EditorGUILayout.EndHorizontal();
                }
                GUILayout.EndArea();
                GUI.EndScrollView();
            }
        }
        #endregion

        #region 表达式配置区

        Rect expressionLabelRect
        {
            get { return new Rect(20, 510, position.width - 40, 20); }
        }
        Rect expressionZoneRect
        {
            get { return new Rect(20, 530, position.width - 40, position.height - 510 - 50f); }
            
        }
        Rect expressionContentRect
        {
            get { return new Rect(20, 530, position.width - 70, position.height - 510 - 50f); }
        }
        Vector2 expressionSp;
        List<bool> expFoldList;

        class Styles
        {
            public static readonly GUIStyle categoryBox = new GUIStyle(EditorStyles.helpBox);
            static Styles()
            {
                categoryBox.padding.left = 14;
            }
        }

        bool fold = false;


        void DrawExpressionZone()
        {
            GUI.Label(expressionLabelRect, "表达式配置");
            EditorGUI.DrawRect(expressionZoneRect, new Color(0.2f, 0.2f, 0.2f));
            if (m_selectedData != null && m_expressionDict != null)
            {
                if (expFoldList == null)
                {
                    expFoldList = new List<bool>();
                    for (int i = 0; i < m_expressionDict.Count; i++)
                    {
                        expFoldList.Add(false);
                    }
                }
                var contentRect = expressionContentRect;
                contentRect.height = 200;
                //contentRect.height = (m_expressionList.Count + 1) * 34;
                //contentRect.width = 85 * 11;
                expressionSp = GUI.BeginScrollView(expressionZoneRect, expressionSp, contentRect);
                GUILayout.BeginArea(contentRect);
                //EditorGUILayout.LabelField("animal count", zoo.animals.Count.ToString());
                string name = "";
                for (int i = 0; i < m_dataTable.Columns.Count; i++)
                {
                    name = nameList[i];
                    string oldStr = "";
                    string curStr = "";
                    if (m_expressionDict.ContainsKey(name) == false)
                    {
                        continue;
                    }
                    var fieldInfo = m_fieldDict[name];
                    //EditorGUILayout.BeginHorizontal();
                    oldStr = m_selectedData[i].ToString();
                    EditorGUILayout.BeginVertical(Styles.categoryBox);
                    //EditorGUILayout.LabelField(fieldInfo.describe, GUILayout.Width(80));
                    //if (i + 1 > expFoldList.Count)
                    //{
                    //    expFoldList.Add(false);
                    //}

                    m_expressionDict[name] = EditorGUILayout.Foldout(m_expressionDict[name], fieldInfo.describe);
                    if (m_expressionDict[name])
                    {
                        EditorGUILayout.LabelField("自由和谐民主富强");
                        EditorGUILayout.LabelField("自由和谐民主富强");
                        EditorGUILayout.LabelField("自由和谐民主富强");
                    }
                    EditorGUILayout.LabelField(oldStr);
                    EditorGUILayout.EndVertical();
                    //string numberDiscribe = "";
                    //for (int j = 1; j <= 10; j++)
                    //{
                    //    numberDiscribe = string.Format("第{0}项", j);
                    //    EditorGUILayout.LabelField(numberDiscribe, GUILayout.Width(80));
                    //}
                    //EditorGUILayout.EndHorizontal();

                    //EditorGUILayout.BeginHorizontal();
                    //EditorGUILayout.LabelField("", GUILayout.Width(80));

                    //oldStr = m_selectedData[i].ToString();
                    //string[] fields = oldStr.Split('#');
                    //string[] tempFields = new string[10];
                    //for (int j = 0; j < 10; j++)
                    //{
                    //    if (j < fields.Length)
                    //        tempFields[j] = fields[j];
                    //    else
                    //        tempFields[j] = "";
                    //    tempFields[j] = EditorGUILayout.TextField(tempFields[j], GUILayout.Width(80));
                    //}
                    //if (fields[0] != null)
                    //{
                    //    for (int j = 0; j < 10; j++)
                    //    {
                    //        if (tempFields[j] != null && tempFields[j].Equals("") == false)
                    //        {
                    //            if (j == 0)
                    //                curStr += tempFields[j];
                    //            else
                    //                curStr += ("#" + tempFields[j]);
                    //        }
                    //    }
                    //}
                    //m_selectedData[i] = curStr;
                    //if (curStr.CompareTo(oldStr) != 0)
                    //{
                    //    m_TreeView.UpdateContent(i, m_selectedData[i].ToString());
                    //}
                    //EditorGUILayout.EndHorizontal();
                }
                GUILayout.EndArea();
                GUI.EndScrollView();
            }

            //GUILayout.BeginArea(expressionZoneRect);
            ////EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.BeginVertical(Styles.categoryBox);
            ////EditorGUILayout.BeginFadeGroup(1.0f);
            //fold = EditorGUILayout.Foldout(fold, "这是一个折叠文本");
            //if (fold)
            //{
            //    EditorGUILayout.LabelField("自由和谐民主富强");
            //    EditorGUILayout.LabelField("自由和谐民主富强");
            //    EditorGUILayout.LabelField("自由和谐民主富强");
            //}
            //EditorGUILayout.LabelField("自由和谐民主富强自由和谐民主富强自由和谐民主富强");
            //EditorGUILayout.EndVertical();
            ////EditorGUILayout.EndFadeGroup();
            ////EditorGUILayout.EndHorizontal();
            //GUILayout.EndArea();
        }

        #endregion
    }

    internal class MyMultiColumnHeader : MultiColumnHeader
    {
        Mode m_Mode;

        public enum Mode
        {
            LargeHeader,
            DefaultHeader,
            MinimumHeaderWithoutSorting
        }

        public MyMultiColumnHeader(MultiColumnHeaderState state)
            : base(state)
        {
            mode = Mode.DefaultHeader;
        }

        public Mode mode
        {
            get
            {
                return m_Mode;
            }
            set
            {
                m_Mode = value;
                switch (m_Mode)
                {
                    case Mode.LargeHeader:
                        canSort = true;
                        height = 37f;
                        break;
                    case Mode.DefaultHeader:
                        canSort = true;
                        height = DefaultGUI.defaultHeight;
                        break;
                    case Mode.MinimumHeaderWithoutSorting:
                        canSort = false;
                        height = DefaultGUI.minimumHeight;
                        break;
                }
            }
        }

        protected override void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            // Default column header gui
            base.ColumnHeaderGUI(column, headerRect, columnIndex);

            // Add additional info for large header
            if (mode == Mode.LargeHeader)
            {
                // Show example overlay stuff on some of the columns
                if (columnIndex > 2)
                {
                    headerRect.xMax -= 3f;
                    var oldAlignment = EditorStyles.largeLabel.alignment;
                    EditorStyles.largeLabel.alignment = TextAnchor.UpperRight;
                    GUI.Label(headerRect, 36 + columnIndex + "%", EditorStyles.largeLabel);
                    EditorStyles.largeLabel.alignment = oldAlignment;
                }
            }
        }
    }
}
