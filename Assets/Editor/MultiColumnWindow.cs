using System;
using System.Collections.Generic;
using System.Data;
using UnityEditor.Callbacks;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


namespace UnityEditor.ExcelTreeView
{

	class MultiColumnWindow : EditorWindow
	{
		[NonSerialized] bool m_Initialized;
		[SerializeField] TreeViewState m_TreeViewState; // Serialized in the window layout file so it survives assembly reloading
		[SerializeField] MultiColumnHeaderState m_MultiColumnHeaderState;
		SearchField m_SearchField;
        MultiColumnTreeView m_TreeView;
        List<string> m_nameList;   //TODO:暂时当做字符串，后续当成一个索引去查表
        List<ExcelTreeElement> m_excelData;
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
			get { return new Rect(20, 50, position.width-40, position.height-80); }
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

                m_Initialized = true;
            }
        }

        void OnGUI ()
		{
			InitIfNeeded();           
            FilePathBar(filePathRect);
            SearchBar(toolbarRect);
			DoTreeView(multiColumnTreeViewRect);
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
                    GetExcelData();

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

		void BottomToolBar (Rect rect)
		{
			GUILayout.BeginArea (rect);

			using (new EditorGUILayout.HorizontalScope ())
			{

				var style = "miniButton";
				if (GUILayout.Button("Expand All", style))
				{
					treeView.ExpandAll ();
				}

				if (GUILayout.Button("Collapse All", style))
				{
					treeView.CollapseAll ();
				}

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
        void GetExcelData()
        {
            Debug.Log("Load Excel Data!!!");
            DataTable dataTale = EditorTool.ExcelHelper.ReadExcel(excelPath);
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
            m_Initialized = false;
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
