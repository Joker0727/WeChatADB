using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

namespace AutoToolHelper
{
    class MyStr
    {
        public static string GetCut(string strSource)
        {
            char[] anychar = new char[1];
            anychar[0]='\0';
            int len = strSource.IndexOfAny(anychar);
            if (len > 0)
            {
                return strSource.Substring(0,len);
            }
            return strSource;
        }
        public static string FindStr(string strSource, string strKeyBegin, string strKeyEnd)
        {
            int iStart = 0;
            if (strKeyBegin != "")
            {
                iStart = strSource.IndexOf(strKeyBegin);
                if (iStart == -1)  { return ""; }
            }
            strSource = strSource.Substring(iStart);
            int iEnd = strSource.Length;
            if (strKeyEnd != "")
            {
                iEnd = strSource.IndexOf(strKeyEnd);
                if (iEnd == -1) { return ""; }
            }
            iStart = strKeyBegin.Length;
            if (iEnd - iStart < 0) { return ""; }
            
            return strSource.Substring(iStart, iEnd - iStart);
        }
        public static int firstIndexOf(string str, string pattern)
        {
            for (int i = 0; i < (str.Length - pattern.Length); i++)
            {
                int j = 0;
                while (j < pattern.Length)
                {
                    if (str.Substring(i + j, 1) != pattern.Substring(j, 1)) break;
                    j++;
                }
                if (j == pattern.Length) return i;
            }
            return -1;
        }
        public static string FindStr2(string strSource, string strKeyBegin, string strKeyEnd)
        {
            int iStart = 0;
            if (strKeyBegin != "")
            {
                iStart = firstIndexOf(strSource, strKeyBegin);
                if (iStart == -1) { return ""; }
            }
            strSource = strSource.Substring(iStart);
            int iEnd = strSource.Length;
            if (strKeyEnd != "")
            {
                iEnd = firstIndexOf(strSource, strKeyEnd);
                if (iEnd == -1) { return ""; }
            }
            iStart = strKeyBegin.Length;
            if (iEnd - iStart < 0) { return ""; }



            return strSource.Substring(iStart, iEnd - iStart);
        }


        
    }
    class FileRW
    {
        private static string _strPath = Application.ProductName + ".log";

        public static void InitLog()
        {

            File.Delete(_strPath);
            using (File.Create(_strPath))
            { }
        }
        public static void WriteToFile(string strLog)
        {
            if (_strPath.Trim() == "")
            {
                return;
            }

            if (!File.Exists(_strPath))
                InitLog();
            //strLog=String.Format(strLog, arg);
            StreamWriter sw=null;

            try
            {
                Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +"£º"+strLog);
               // sw = File.AppendText(_strPath);
                sw = new StreamWriter(_strPath, true, Encoding.Default);
               // sw.WriteLine("{0}[{1}] {2}", DateTime.Now.ToString("HH:mm:ss"), Application.ProductVersion, strLog);//´òÓ¡°æ±¾ºÅ
                sw.WriteLine("{0}£º{1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), strLog);
            }
            catch (Exception ex)
            {
                return;
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }
        }
        public static void WriteToFile(string strLog, string strPath)
        {
            StreamWriter Asw = null;
            try 
            {
                Asw = new StreamWriter(strPath, false, Encoding.Default);
                Asw.Write(strLog);
            }
            catch (Exception e)
            {
                throw e;
                //return;
            }
            finally
            {
                if (Asw != null)
                    Asw.Close();
            }
        }
        public static void WriteError(Exception ex)
        {
            WriteToFile(ex.ToString());
        }
        public static void WriteError(string strex)
        {
            WriteToFile(strex);
        }
        public static string ReadFile(string strPathName)
        {
            StreamReader sr;
            try
            {
                //sr = File.OpenText(strPathName);
                sr = new StreamReader(strPathName, Encoding.Default);
                string tmp = sr.ReadToEnd();
                sr.Close();
                return tmp;
            }
            catch (Exception ex)
            {
                WriteError(ex);
            }
            
            return "";
         
        }
    }

}
