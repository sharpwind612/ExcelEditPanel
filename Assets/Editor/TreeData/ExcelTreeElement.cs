using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UnityEditor.ExcelTreeView
{
	[Serializable]
	internal class ExcelTreeElement: TreeElement
	{
        public List<string> data;

        public ExcelTreeElement(string name, int depth, int id, string[] input):base(name,depth,id)
		{
            data = new List<string>();
            //name = input[0];
            //depth = _depth;
            //id = index;
            for (int i = 1; i < input.Length; i++)
            {
                data.Add(input[i]);
            }
		}
	}
}

