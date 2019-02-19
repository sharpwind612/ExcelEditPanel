using System.Data;
using System.IO;
using System.Text;
using Excel;
using UnityEditor;
using UnityEngine;

namespace EditorTool
{
    public class ExcelHelper : Editor
    {
        /// <summary>
        /// 只读Excel方法
        /// </summary>
        /// <param name="ExcelPath"></param>
        /// <returns></returns>
        public static DataTable ReadExcel(string ExcelPath)
        {
            string fullPath = ExcelPath;// Application.dataPath + "/Data/" + ExcelPath;
            if (!File.Exists(fullPath))
            {
                Debug.LogError("未找到相关文件，请检查路径是否正确:" + fullPath);
                return null;
            }
            FileStream stream = File.Open(fullPath, FileMode.Open, FileAccess.Read);
            IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);

            DataSet result = excelReader.AsDataSet();
            return result.Tables[0];

            //int columns = result.Tables[0].Columns.Count;//获取列数
            //int rows = result.Tables[0].Rows.Count;//获取行数

            //StringBuilder sb = new StringBuilder();
            ////从第二行开始读
            //for (int i = 1; i < rows; i++)
            //{
            //    sb.Clear();
            //    for (int j = 0; j < columns; j++)
            //    {
            //        sb.Append(result.Tables[0].Rows[i][j].ToString());
            //        if (j != columns - 1)
            //        {
            //            sb.Append(",");
            //        }
            //    }
            //    Debug.Log(sb.ToString());
            //}

        }
    }
}