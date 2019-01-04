using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging; //ImageFormat.Jpeg
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;//md5
//using System.Collections;
using System.Windows.Forms;

namespace AutoToolHelper
{
    //struct ORDInfo
    //{

    //}


    class Game
    {

        public static string MD5(string strPwd)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.Default.GetBytes(strPwd);
            byte[] md5data = md5.ComputeHash(data);
            md5.Clear();
            string str = "";
            for (int i = 0; i < md5data.Length; i++)
            {
                str += md5data[i].ToString("x").PadLeft(2, '0');
            }
            return str;
        }
        public static bool processStay(string processName)
        {

            Process[] ps = Process.GetProcesses();
            if (processName.IndexOf(".") > 0)
                processName = processName.Substring(0, processName.LastIndexOf("."));
            foreach (Process item in ps)
            {
                if (item.ProcessName == processName)
                {
                    return true;
                }
            }
            return false;
        }
        public static void SendUnicode(ushort data)
        {
            INPUT[] input = new INPUT[2];
            input[0].Type = (UInt32)InputType.INPUT_KEYBOARD;
            input[0].Data.ki.wVk = 0;
            input[0].Data.ki.wScan = data;
            input[0].Data.ki.dwFlags = 0x4;//KEYEVENTF_UNICODE;

            input[0].Type = (UInt32)InputType.INPUT_KEYBOARD;
            input[1].Data.ki.wVk = 0;
            input[1].Data.ki.wScan = data;
            input[1].Data.ki.dwFlags = 0x2 | 0x4;//KEYEVENTF_UNICODE;
            //uint tt =(uint) Marshal.SizeOf(input[0]);
            User32API.SendInput(2, input, Marshal.SizeOf(input[0]) * 4);
            //User32API.SendInput(2, input, Marshal.SizeOf(input));
        }

        //public void SendInputKeys(string text)
        //{
        //    return;
        //}

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, ushort wParam, IntPtr lParam);
        //AppEnd=true,追加
        public static void SendString(IntPtr hWnd, string str, bool AppEnd)
        {
            //StringIterator
            if (str == null)
                return;
            byte[] bhr;// = new byte[2];
            //bhr = System.Text.Encoding.Default.GetBytes(str);
            if (!AppEnd)
                User32API.SendMessage(hWnd, 0x000C, IntPtr.Zero, IntPtr.Zero);//WM_SETTEXT
            for (int i = 0; i < str.Length; i++)
            {
                bhr = System.Text.Encoding.Default.GetBytes(str.Substring(i, 1));
                ushort sc = 0;
                for (int j = 0; j < bhr.GetLength(0); j++)
                    sc = (ushort)(sc * 256 + bhr[j]);
                SendMessage(hWnd, 0x0286, sc, IntPtr.Zero);//WM_IME_CHAR
                System.Threading.Thread.Sleep(10);
            }
        }

        public static void tskill(string processName)
        {

            Process[] ps = Process.GetProcesses();
            if (processName.IndexOf(".") > 0)
                processName = processName.Substring(0, processName.LastIndexOf("."));
            foreach (Process item in ps)
            {
                if (item.ProcessName.ToLower() == processName.ToLower())
                {
                    item.Kill();
                }
            }
        }

        public static void tskill(int[] pid)
        {
            if (pid[0] == 0)
                return;
            Process[] ps = Process.GetProcesses();
            foreach (Process item in ps)
            {
                if (Array.BinarySearch(pid, item.Id) >= 0)
                {
                    item.Kill();
                }
            }
        }

        public static string RunCmd(string cmd)
        {
            string[] strcmd = new string[] { cmd };
            return RunCmd(strcmd);
        }

        public static string RunCmd(string[] cmd)
        {
            Process process = new Process();

            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.WorkingDirectory = "";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.StandardInput.AutoFlush = true;
            //process.StandardInput.WriteLine("cd windows");
            process.WaitForExit(1000);
            for (int i = 0; i < cmd.Length; i++)
            {
                process.StandardInput.WriteLine(cmd[i].ToString());
            }
            process.StandardInput.WriteLine("exit");
            string strRst = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            process.Close();
            return strRst;
        }
        //MakeSureDirectoryPathExists
        //内网IP
        public static string GetLocalIp()
        {
            string hostname = Dns.GetHostName();
            IPHostEntry localhost = Dns.GetHostEntry(hostname);
            IPAddress localaddr = null;
            for (int i = 0; i < localhost.AddressList.Length; i++)
            {
                localaddr = localhost.AddressList[i];
                if (localaddr.ToString().Length >= 7)
                {
                    if (localaddr.ToString().Substring(0, 7) == "192.168")
                        break;
                }
            }
            return localaddr.ToString();
        }
        //外网IP
        public static string GetNetIp()
        {
            string tmp = GetUrlResult("http://www.ip138.com/ips138.asp");
            return MyStr.FindStr(tmp, "您的IP地址是：[", "]");
        }

        public static int StartProcess(string proName, string args)
        {
            Process p = new Process();
            //声明一个程序信息类
            ProcessStartInfo Info = new System.Diagnostics.ProcessStartInfo(proName, args.Trim());
            string strWorkDir = proName.Substring(0, proName.LastIndexOf("\\") + 1);
            Info.WorkingDirectory = strWorkDir;
            try
            {
                p = Process.Start(Info);

            }
            catch (System.ComponentModel.Win32Exception e)
            {
                //CommonClass.ShowTip("程序路径出错");
                FileRW.WriteError(string.Format("系统找不到指定的程序文件:{0}", e.ToString()));
                return 0;
            }
            return p.Id;
        }

        public static string GetUrlResult(string strUrl)
        {
            System.Net.WebClient myWC = new System.Net.WebClient();
            ///获取或设置用于对向Internet资源的请求进行身份验证的网络凭据。
            myWC.Credentials = System.Net.CredentialCache.DefaultCredentials;
            Uri u = new Uri(strUrl);
            //从指定网站下载数据
            Byte[] pageData = myWC.DownloadData(u);
            //如果获取网站页面采用的是GB2312
            string pageHtml = Encoding.Default.GetString(pageData);
            //如果获取网站页面采用的是UTF-8
            //string pageHtml = Encoding.UTF8.GetString(pageData);
            return pageHtml;
        }

        //public static void CaptureJpg(RECT rect)
        //{
        //    return;
        //    string tmp = SetPicPath(AutoToolHelper_dnf.m_strPictureDir, AutoToolHelper_dnf.OrdNo);
        //    ImageTool.GetScreenCapture(rect).Save(tmp, ImageFormat.Jpeg);
        //}
        public static void CaptureBmp(Bitmap BigBmp, RECT rect, string strPicName)
        {
            if (BigBmp == null)
                return;
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;
            Rectangle srcRect = new Rectangle(rect.left, rect.top, width, height);
            BitmapData bigBData = BigBmp.LockBits(srcRect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Bitmap bmp = new Bitmap(width, height, bigBData.Stride, PixelFormat.Format24bppRgb, bigBData.Scan0);
            ////下面是自己添加的,用来缩小图片
            //int defaultWidth = 880;
            //int defaultHeight = 656;
            //int newWidth = 0;
            //float newHeight = 0;
            //if (bmp.Width > defaultWidth)
            //{
            //    newWidth = defaultWidth;
            //    //newHeight = bmp.Height * (float)((float)bmp.Height / (float)bmp.Width);
            //    newHeight = 656;
            //}
            //else
            //{
            //    newWidth = bmp.Width;
            //    newHeight = bmp.Height;
            //}          
            //Size s = new Size(newWidth, (int)newHeight);//按比例缩小
            //Bitmap newBmp = new Bitmap(bmp, s);//从指定的现有图像并使用指定的大小初始化 System.Drawing.Bitmap 类的新实例。
            bmp.Save(strPicName);
            BigBmp.UnlockBits(bigBData);
            return;
            //ImageTool.GetScreenCapture(rect).Save(strPicName, ImageFormat.Bmp);
        }

        public static void CaptureJpg(Bitmap BigBmp, RECT rect, string strPicName)
        {
            //if (BigBmp == null)
            //    BigBmp = VGA.GetBmp(0);
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;
            Rectangle srcRect = new Rectangle(rect.left, rect.top, width, height);
            BitmapData bigBData = BigBmp.LockBits(srcRect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            Bitmap bmp = new Bitmap(width, height, bigBData.Stride, PixelFormat.Format24bppRgb, bigBData.Scan0);

            BigBmp.UnlockBits(bigBData);
            bmp.Save(strPicName, ImageFormat.Jpeg);
            return;

        }

        public static string SetPicPath(string strPictureDir, string strOrderNo)
        {
            string strPD, strFileName;
            DateTime dt = DateTime.Now;
            strPD = string.Format("{0}{1:0000}_{2:00}\\{3:00}\\{4}\\", strPictureDir, dt.Year, dt.Month, dt.Day, strOrderNo);
            User32API.MakeSureDirectoryPathExists(strPD);
            strFileName = string.Format("{0}{1:00}{2:00}{3:00}.jpg", strPD, dt.Hour, dt.Minute, dt.Second);
            return strFileName;
        }
        //不分大小写GetPid("dnf.exe") .net
        public static int GetPid(string strProcessName)
        {
            int pid = 0;
            //GetWindow(strProcessName,"", ref pid);
            //return pid;
            if (strProcessName.IndexOf(".") > 0)
                strProcessName = strProcessName.Substring(0, strProcessName.LastIndexOf("."));
            Process[] ps = Process.GetProcesses();
            foreach (Process item in ps)
            {
                if (item.ProcessName == strProcessName)
                {
                    return item.Id;
                }
            }
            return pid;

        }
        //避免重复开
        public static void KillSameProcess()
        {
            string tmp = Application.ExecutablePath;
            int len = tmp.LastIndexOf("\\");
            tmp = tmp.Substring(len + 1, tmp.Length - len - 1);
            tmp = tmp.Substring(0, tmp.LastIndexOf("."));
            Process[] ps = Process.GetProcesses();
            foreach (Process item in ps)
            {
                if (item.ProcessName == tmp)
                {
                    if (item.Id != Process.GetCurrentProcess().Id)
                        item.Kill();
                }
            }
        }

        public static int[] GetPidAll(string strProcessName)
        {
            List<int> aa = new List<int>();
            //int[] pid = new int[10];
            if (strProcessName.IndexOf(".") > 0)
                strProcessName = strProcessName.Substring(0, strProcessName.LastIndexOf("."));
            Process[] ps = Process.GetProcesses();
            foreach (Process item in ps)
            {
                if (item.ProcessName == strProcessName)
                {
                    aa.Add(item.Id);
                }
            }
            int[] array ={ 0 };
            if (aa.Count > 0)
            {
                array = aa.ToArray();
            }
            return array;
        }

        //user32.dll
        public static IntPtr GetWindow(uint pid, bool IsWD)
        {
            IntPtr hWnd = IntPtr.Zero;
            IntPtr lngProcId = IntPtr.Zero; ;
            hWnd = User32API.GetDesktopWindow();
            hWnd = User32API.GetWindow(hWnd, GetWindowEnums.GW_CHILD);
            while (hWnd != IntPtr.Zero)
            {
                //Console.WriteLine(str);
                User32API.GetWindowThreadProcessId(hWnd, ref lngProcId);
                if ((uint)lngProcId == pid)
                {
                    if (IsWD)
                    {
                        if (!User32API.IsWindowVisible(hWnd))
                        {
                            hWnd = User32API.GetWindow(hWnd, GetWindowEnums.GW_HWNDNEXT);
                            continue;
                        }
                    }
                    return hWnd;
                }
                hWnd = User32API.GetWindow(hWnd, GetWindowEnums.GW_HWNDNEXT);
            }
            return IntPtr.Zero;
        }
        public static bool IsPid(uint pid)
        {
            IntPtr hWnd = IntPtr.Zero;
            IntPtr lngProcId = IntPtr.Zero; ;
            hWnd = User32API.GetDesktopWindow();
            hWnd = User32API.GetWindow(hWnd, GetWindowEnums.GW_CHILD);
            while (hWnd != IntPtr.Zero)
            {
                //Console.WriteLine(str);
                User32API.GetWindowThreadProcessId(hWnd, ref lngProcId);
                if ((uint)lngProcId == pid)
                {
                    //if (IsWD)
                    //{
                    //    if (!User32API.IsWindowVisible(hWnd))
                    //    {
                    //        hWnd = User32API.GetWindow(hWnd, GetWindowEnums.GW_HWNDNEXT);
                    //        continue;
                    //    }
                    //}
                    return true;
                }
                hWnd = User32API.GetWindow(hWnd, GetWindowEnums.GW_HWNDNEXT);
            }
            return false;
        }
        //user32.dll
        public static IntPtr GetWindow(uint pid, string strTitle)
        {
            IntPtr hWnd = IntPtr.Zero;
            IntPtr lngProcId = IntPtr.Zero; ;
            hWnd = User32API.GetDesktopWindow();
            hWnd = User32API.GetWindow(hWnd, GetWindowEnums.GW_CHILD);
            string str = string.Empty;
            while (hWnd != IntPtr.Zero)
            {
                str = User32API.GetWindowText(hWnd);

                if (str.IndexOf(strTitle) != -1)
                {
                    //Console.WriteLine(str);
                    User32API.GetWindowThreadProcessId(hWnd, ref lngProcId);
                    if ((uint)lngProcId == pid)
                    {
                        return hWnd;
                    }
                }
                hWnd = User32API.GetWindow(hWnd, GetWindowEnums.GW_HWNDNEXT);
            }

            //MainWindowHandle

            return IntPtr.Zero;
        }
        //.net
        public static IntPtr GetIsWindow(string strProcessName)
        {
            if (strProcessName.IndexOf(".") > 0)
                strProcessName = strProcessName.Substring(0, strProcessName.LastIndexOf("."));
            Process[] ps = Process.GetProcesses();
            foreach (Process item in ps)
            {
                if (item.ProcessName.ToLower() == strProcessName.ToLower())
                {
                    if (User32API.IsWindowVisible(item.MainWindowHandle))
                        return item.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
        }
        //.net
        public static IntPtr GetWindow(string strProcessName)
        {
            if (strProcessName.IndexOf(".") > 0)
                strProcessName = strProcessName.Substring(0, strProcessName.LastIndexOf("."));
            Process[] ps = Process.GetProcesses();
            foreach (Process item in ps)
            {
                if (item.ProcessName.ToLower() == strProcessName.ToLower())
                    return item.MainWindowHandle;
            }
            return IntPtr.Zero;
        }
        //.net
        public static IntPtr GetWindow(string strProcessName, string strTitle, ref int pid)
        {
            IntPtr ptr = IntPtr.Zero;
            //IntPtr hSnapshot = Kernel32API.CreateToolhelp32Snapshot((uint)SnapshotFlags.TH32CS_SNAPPROCESS, 0);
            //if (hSnapshot == IntPtr.Zero)
            //{
            //    return IntPtr.Zero;
            //}
            //ProcessEntry32 pe = new ProcessEntry32();
            //pe.dwSize = (uint)Marshal.SizeOf(typeof(ProcessEntry32));

            //bool fok;
            //for (fok = Kernel32API.Process32First(hSnapshot, ref pe); fok; fok = Kernel32API.Process32Next(hSnapshot, ref pe))
            //{
            //    if (pe.szExeFile.ToUpper() == strProcessName.ToUpper())
            //    {
            //        pid = (int)pe.th32ProcessID;
            //        IntPtr hwnd = GetWindow(pe.th32ProcessID, strTitle);
            //        if (hwnd != IntPtr.Zero || strTitle== "")
            //            return hwnd;
            //        //if (GetWindow(pe.th32ProcessID, strTitle))
            //        //return GetWindow(pe.th32ProcessID, strTitle);
            //    }
            //}

            if (strProcessName.IndexOf(".") > 0)
                strProcessName = strProcessName.Substring(0, strProcessName.LastIndexOf("."));
            Process[] ps = Process.GetProcesses();
            foreach (Process item in ps)
            {
                if (item.ProcessName.ToLower() == strProcessName.ToLower())
                {
                    pid = item.Id;
                    ptr = item.MainWindowHandle;
                    if (item.MainWindowTitle == strTitle)
                    {
                        return item.MainWindowHandle;
                    }
                }
            }


            return ptr;
        }
        //user32.dll
        //public static IntPtr GetWindow(string strProcessName, List<string> listPID, string strTitle, ref uint pid)
        //{
        //    IntPtr hWnd = IntPtr.Zero;
        //    if (pid != 0)
        //    {
        //        return GetWindow(pid, strTitle);
        //    }
        //    IntPtr hSnapshot = Kernel32API.CreateToolhelp32Snapshot((uint)SnapshotFlags.TH32CS_SNAPPROCESS, 0);
        //    if (hSnapshot == IntPtr.Zero)
        //    {
        //        return IntPtr.Zero;
        //    }
        //    ProcessEntry32 pe = new ProcessEntry32();
        //    pe.dwSize = (uint)Marshal.SizeOf(typeof(ProcessEntry32));

        //    bool fok;
        //    for (fok = Kernel32API.Process32First(hSnapshot, ref pe); fok; fok = Kernel32API.Process32Next(hSnapshot, ref pe))
        //    {
        //        if (pe.szExeFile.ToUpper() == strProcessName.ToUpper())
        //        {
        //            foreach (string strPID in listPID)
        //            {
        //                if (Convert.ToUInt32(strPID) == pe.th32ProcessID)
        //                {
        //                    break;
        //                }
        //            }
        //            hWnd = GetWindow(pe.th32ProcessID, strTitle);
        //            if (hWnd == IntPtr.Zero)
        //            {
        //                continue;
        //            }
        //            pid = pe.th32ProcessID;
        //            Kernel32API.CloseHandle(hSnapshot);
        //            return hWnd;
        //        }
        //    }
        //    return IntPtr.Zero;
        //}
    }

}