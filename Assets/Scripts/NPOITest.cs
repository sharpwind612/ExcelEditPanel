using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using EditorTool;

public class NPOITest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DataTable tempTable = TestExcelRead("D:/Projects/ExcelEditPanel/Assets/Data/test.xlsx");
        //EditDataTable(ref dt);
        //TestExcelWrite("D:/Projects/ExcelEditPanel/Assets/Data/test.xlsx", tempTable);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    DataTable TestExcelRead(string file)
    {
        try
        {
            using (NPOIExcelHelper excelHelper = new NPOIExcelHelper(file))
            {
                DataTable dt = excelHelper.ExcelToDataTable("Sheet1", true);               
                Debug.Log("Load excel file success!");
                EditDataTable(ref dt);
                PrintData(dt);
                excelHelper.DataTableToExcel(dt, "Sheet1", false);
                Debug.Log("Save excel file  success!");
                return dt;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception: " + ex.Message);
            return null;
        }
    }

    void TestExcelWrite(string file, DataTable dt)
    {
        try
        {
            using (NPOIExcelHelper excelHelper = new NPOIExcelHelper(file))
            {
                excelHelper.DataTableToExcel(dt, "Sheet1", false);
                Debug.Log("Save excel file  success!");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception: " + ex.Message);
        }
    }

    void EditDataTable(ref DataTable dt)
    {
        dt.Rows[4][4] = "4#12356";
        dt.Rows[5][4] = "特朗普";
        dt.Rows[6][4] = "杜美心";
    }

    void PrintData(DataTable data)
    {
        if (data == null) return;
        for (int i = 0; i < data.Rows.Count; ++i)
        {
            string str = "";
            for (int j = 0; j < data.Columns.Count; ++j)
            {
                str = string.Format("{0}  {1}", str, data.Rows[i][j].ToString());
            }
            Debug.Log(str);
        }
    }
}
