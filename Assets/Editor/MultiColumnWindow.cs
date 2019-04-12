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
		[NonSerialized] bool m_Initialized;
		[SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
        [SerializeField] TreeViewState m_TreeViewState2;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
        [SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState2;
        SearchField m_SearchField;
        MultiColumnTreeView m_TreeView;
        MultiColumnTreeView m_TreeView2;
        List<string> m_nameList;   //TODO:暂时当做字符串，后续当成一个索引去查表
        List<ExcelTreeElement> m_excelData;
        DataTable m_dataTable; //缓存读取excel表格后的dataTable
        const int m_keyRow = 1; // start from 0
        const int m_DiscribeRowCount = 3;
        public List<string> nameList
        {
            get { return m_nameList; }
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
            get { return new Rect(20f, 30f, position.width - 40f, 20f); }
        }

        Rect multiColumnTreeViewRect
		{
			get { return new Rect(20, 50, position.width-40, 200); }
		}

        Rect multiColumnTreeViewRect2
        {
            get { return new Rect(20, 270, position.width/2 - 30, 200); }
        }

        Rect multiColumnTreeViewRect3
        {
            get { return new Rect(position.width / 2 + 10, 270, position.width / 2 - 40, 200); }
        }

        Rect bottomToolbarRect
		{
			get { return new Rect(20f, position.height - 25f, position.width - 40f, 16f); }
		}

		public MultiColumnTreeView treeView
		{
			get { return m_TreeView; }
		}

		void InitIfNeeded ()
		{
			if (!m_Initialized)
			{
				// Check if it already exists (deserialized from window layout file or scriptable object)
				if (m_TreeViewState == null)
					m_TreeViewState = new TreeViewState();

				bool firstInit = m_MultiColumnHeaderState == null;
				var headerState = MultiColumnTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect.width,nameList);
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

                //普通字段框
                if (m_TreeViewState2 == null)
                    m_TreeViewState2 = new TreeViewState();

                bool firstInit2 = m_MultiColumnHeaderState2 == null;

                List<string> _nameList = new List<string>();
                var headerState2 = MultiColumnTreeView.CreateDefaultMultiColumnHeaderState(multiColumnTreeViewRect2.width, _nameList);
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState2, headerState2))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState2, headerState2);
                m_MultiColumnHeaderState2 = headerState2;

                var multiColumnHeader2 = new MyMultiColumnHeader(headerState2);
                if (firstInit2)
                    multiColumnHeader2.ResizeToFit();

                TreeModel<ExcelTreeElement> treeModel2;
                treeModel2 = new TreeModel<ExcelTreeElement>(GetEmptyData());
                //if (m_excelData == null)
                //{
                //    treeModel2 = new TreeModel<ExcelTreeElement>(GetEmptyData());
                //}
                //else
                //{
                //    treeModel2 = new TreeModel<ExcelTreeElement>(m_excelData);
                //}

                m_TreeView2 = new MultiColumnTreeView(m_TreeViewState2, multiColumnHeader2, treeModel2, this);


                m_Initialized = true;
            }
        }

        void OnGUI ()
		{
			InitIfNeeded();           
            FilePathBar(filePathRect);
            SearchBar(toolbarRect);
			DoTreeView(multiColumnTreeViewRect);
            DoTreeView2(multiColumnTreeViewRect2);
            //DoTreeView3(multiColumnTreeViewRect3);
            BottomToolBar(bottomToolbarRect);
        }

        void FilePathBar(Rect rect)
        {
            GUILayout.BeginArea(rect);
            using (new EditorGUILayout.HorizontalScope())
            { 
                var style = "miniButton";
                GUILayout.Label("Please input the excel file path:");
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

        void DoTreeView2(Rect rect)
        {
            m_TreeView2.OnGUI(rect);
        }

        void DoTreeView3(Rect rect)
        {
            m_TreeView2.OnGUI(rect);
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
                //if (GUILayout.Button("Expand All", style))
                //{
                //	treeView.ExpandAll ();
                //}

                //if (GUILayout.Button("Collapse All", style))
                //{
                //	treeView.CollapseAll ();
                //}

                GUILayout.FlexibleSpace();

				//GUILayout.Label (m_MyTreeAsset != null ? AssetDatabase.GetAssetPath (m_MyTreeAsset) : string.Empty);

				//GUILayout.FlexibleSpace ();

                var myColumnHeader = (MyMultiColumnHeader)treeView.multiColumnHeader;
                myColumnHeader.mode = MyMultiColumnHeader.Mode.MinimumHeaderWithoutSorting;

            }

			GUILayout.EndArea();
		}

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
            DataTable dataTale = excelHelper.ExcelToDataTable("Sheet1", true);
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
        }
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
