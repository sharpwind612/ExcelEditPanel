using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.ExcelTreeView
{
    internal class MultiColumnTreeView : TreeViewWithTreeModel<ExcelTreeElement>
    {
        const float kRowHeights = 20f;
        const float kToggleWidth = 18f;
        public bool showControls = true;
        MultiColumnWindow m_parent;
        //public int selectedIndex = -1;

        //static Texture2D[] s_TestIcons =
        //{
        //    EditorGUIUtility.FindTexture ("Folder Icon"),
        //    EditorGUIUtility.FindTexture ("AudioSource Icon"),
        //    EditorGUIUtility.FindTexture ("Camera Icon"),
        //    EditorGUIUtility.FindTexture ("Windzone Icon"),
        //    EditorGUIUtility.FindTexture ("GameObject Icon")
        //};

        public enum SortOption
        {
            Name,
            Value1,
            Value2,
            Value3,
            Text
        }

        public static void TreeToList(TreeViewItem root, IList<TreeViewItem> result)
        {
            if (root == null)
                throw new NullReferenceException("root");
            if (result == null)
                throw new NullReferenceException("result");

            result.Clear();

            if (root.children == null)
                return;

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            for (int i = root.children.Count - 1; i >= 0; i--)
                stack.Push(root.children[i]);

            while (stack.Count > 0)
            {
                TreeViewItem current = stack.Pop();
                result.Add(current);

                if (current.hasChildren && current.children[0] != null)
                {
                    for (int i = current.children.Count - 1; i >= 0; i--)
                    {
                        stack.Push(current.children[i]);
                    }
                }
            }
        }

        public MultiColumnTreeView(TreeViewState state, MultiColumnHeader multicolumnHeader, TreeModel<ExcelTreeElement> model, MultiColumnWindow parent) : base(state, multicolumnHeader, model)
        {
            //Assert.AreEqual(m_SortOptions.Length , Enum.GetValues(typeof(MyColumns)).Length, "Ensure number of sort options are in sync with number of MyColumns enum values");

            // Custom setup
            rowHeight = kRowHeights;
            columnIndexForTreeFoldouts = 0;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            customFoldoutYOffset = (kRowHeights - EditorGUIUtility.singleLineHeight) * 0.5f; // center foldout in the row since we also center content. See RowGUI
                                                                                             //extraSpaceBeforeIconAndLabel = kToggleWidth;
                                                                                             //multicolumnHeader.sortingChanged += OnSortingChanged;
            m_parent = parent;
            Reload();
        }


        // Note we We only build the visible rows, only the backend has the full tree information. 
        // The treeview only creates info for the row list.
        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            //SortIfNeeded (root, rows);
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (TreeViewItem<ExcelTreeElement>)args.item;

            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), item, i, ref args);
            }
        }

        void CellGUI(Rect cellRect, TreeViewItem<ExcelTreeElement> item, int index, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);
            if (index == 0)
            {
                args.rowRect = cellRect;
                base.RowGUI(args);
            }
            else
            {
                string value = item.data.data[index - 1];
                //选中变为不可编辑
                var selections = GetSelection();
                if (selections.Count != 0 && selections[0] == item.id)
                {
                    GUI.Label(cellRect, value);
                }
                else
                {
                    item.data.data[index - 1] = GUI.TextField(cellRect, value);
                    //表格内容有变化，需要重新写入DataTable
                    if (item.data.data[index - 1].CompareTo(value) != 0)
                    {
                        m_parent.ChangeDataTable(args.row, index, item.data.data[index - 1]);
                        //item.data.data[index] = item.data.data[index - 1];
                        //m_parent.ChangeDataTable(args.row, index + 1, item.data.data[index]);
                        Debug.Log("Content Changed!!!");
                    }
                }
            }
        }

        public void UpdateContent(int columnIndex, string value)
        {
            var rowIndex = GetSelection()[0];
            var rows = GetRows();
            TreeViewItem<ExcelTreeElement> item = null;
            for (int i = 0; i < rows.Count; ++i)
            {
                if (rows[i].id == rowIndex)
                    item = (TreeViewItem<ExcelTreeElement>)rows[i];
            }
            if (item != null)
            {
                item.data.data[columnIndex - 1] = value;
            }
        }

        // Rename
        //--------

        protected override bool CanRename(TreeViewItem item)
        {
            // Only allow rename if we can show the rename overlay with a certain width (label might be clipped by other columns)
            Rect renameRect = GetRenameRect(treeViewRect, 0, item);
            return renameRect.width > 30;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            // Set the backend name and reload the tree to reflect the new model
            if (args.acceptedRename)
            {
                var element = treeModel.Find(args.itemID);
                element.name = args.newName;
                Reload();
            }
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            Rect cellRect = GetCellRectForTreeFoldouts(rowRect);
            CenterRectUsingSingleLineHeight(ref cellRect);
            return base.GetRenameRect(cellRect, row, item);
        }

        // Misc
        //--------

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }


        protected override void SingleClickedItem(int id)
        {
            Debug.Log("SingleClickedItem:" + id);
            m_parent.SingleClickedItem(id);
            //selectedIndex = id;
        }

        protected override void ContextClickedItem(int id)
        {
            Debug.Log("ContextClickedItem:" + id);
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float treeViewWidth, List<string> nameList)
        {
            List<MultiColumnHeaderState.Column> columnList = new List<MultiColumnHeaderState.Column>();
            if (nameList != null)
            {
                for (int i = 0; i < nameList.Count; i++)
                {
                    var item = new MultiColumnHeaderState.Column
                    {
                        headerContent = new GUIContent(nameList[i], "字段说明"),
                        headerTextAlignment = TextAlignment.Left,
                        sortedAscending = true,
                        sortingArrowAlignment = TextAlignment.Left,
                        width = 80,
                        minWidth = 80,
                        autoResize = true
                    };
                    columnList.Add(item);
                }
            }
            if (columnList.Count == 0)
            {
                var item = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("数据待加载", "字段说明"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 80,
                    minWidth = 80,
                    autoResize = true
                };
                columnList.Add(item);
            }
            var columns = columnList.ToArray();
            //Assert.AreEqual(columns.Length, Enum.GetValues(typeof(MyColumns)).Length, "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns);
            return state;
        }
    }
}