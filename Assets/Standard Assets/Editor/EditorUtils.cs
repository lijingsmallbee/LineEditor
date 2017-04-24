/*  This file is part of the "Simple Waypoint System" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them directly or indirectly
 *  from Rebound Games. You shall not license, sublicense, sell, resell, transfer, assign,
 *  distribute or otherwise make available to any third party the Service or the Content. 
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
    /// <summary>
    /// Waypoint and path creation editor.
    /// <summary>
	public class EditorUtils
    {
		public static void SaveTextFile(string saveFilePath, string data)
		{
			string folder = System.IO.Path.GetDirectoryName(saveFilePath);
			System.IO.Directory.CreateDirectory(folder);

#if UNITY_WEBPLAYER && !UNITY_EDITOR
			LogManager.LogError("Current build target is set to Web Player. Cannot perform file input/output when in Web Player.");
#else
        // Unity's TextAsset.text borks when encoding used is UTF8 :(
        System.IO.StreamWriter write = new System.IO.StreamWriter(saveFilePath, false, new UTF8Encoding(false)); 
			write.Write(data);
			write.Flush();
			write.Close();
			write.Dispose();
			#endif
		}

    public static bool ReadCsvFile(string Filename, out string csvContent)
    {
        csvContent = "";
        try
        {
            string fullName = "table/" + Filename;
            //Debug.Log(fullName);
            StreamReader readerConfig = new StreamReader(fullName, Encoding.UTF8);
            csvContent = readerConfig.ReadToEnd();
            readerConfig.Close();
        }
        catch (Exception e)
        {
            e.GetType();
            return false;
        }
        return true;
    }

    public static List<string> readCsvLine(string CsvContent, ref int offset)
    {
        List<string> splitList = new List<string>();
        bool bInQuato = false;

        int len = CsvContent.Length;
        string word = "";
        while (offset < len)
        {
            if (!bInQuato && CsvContent[offset] == '"')
            {
                bInQuato = true;
                offset++;
                continue;
            }
            if (bInQuato && CsvContent[offset] == '"')
            {
                if (CsvContent[offset + 1] == '"')
                {
                    word += '"';
                    offset += 2;
                    continue;
                }
                bInQuato = false;
                offset++;
                continue;
            }

            if (!bInQuato)
            {
                if (CsvContent[offset] == ',')
                {
                    splitList.Add(word);
                    offset++;
                    word = "";
                    continue;
                }
                if (CsvContent[offset] == '\r')
                {
                    offset++;
                    continue;
                }
                if (CsvContent[offset] == '\n')
                {
                    splitList.Add(word);
                    offset++;
                    return splitList;
                }
            }
            word += CsvContent[offset];
            offset++;
        }
        return splitList;
    }

		public static string readLine( string Src,  ref int offsetPos )
		{
			char[] line = new char[1024];
			int pos = offsetPos;
			while( pos< Src.Length )
			{
				char ch = Src[pos];
				if( ch == '\r' || ch=='\n' )
				{
					pos ++;
					if( Src[pos] == '\n' )
						pos ++;
					break;
				}
				pos ++;
			}
            int length = pos - offsetPos;
			Src.CopyTo(offsetPos,line,0,pos-offsetPos);
			offsetPos = pos;
			return new string(line,0,length);
		}
		public static bool isSepChar( char ch )
		{
			if( ch == ' ' || 
			   ch == '\r' ||
			   ch == '\n' )
				return true;
			return false;
		}
		public static List<string> splitLine(string Src )
		{
			int pos = 0;
			List<string> values = new List<string>();
			StringBuilder sp = new StringBuilder();
			while( pos < Src.Length )
			{
				if( isSepChar( Src[pos] ) )
				{
					pos++;
					if( sp.Length>0)
						values.Add(sp.ToString());
					sp.Remove(0,sp.Length);
					continue;
				}
				sp.Append(Src[pos]);
				pos++;
			}
			return values;
		}
	public static void SetLayer(GameObject obj,int layer,bool recursive)
	{
		
		obj.layer = layer;
		if(recursive)
		{
			int count = obj.transform.childCount;
			for(int i = 0;i<count;++i)
			{
				SetLayer(obj.transform.GetChild(i).gameObject,layer,recursive);
			}
		}
	 
	}
}
