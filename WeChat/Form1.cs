using AutoToolHelper;
using Sipo;
using SqlLiteHelperDemo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace WeChat
{
    public partial class Form1 : Form
    {
        public static string basePath = AppDomain.CurrentDomain.BaseDirectory;
        public string capturePicPath = AppDomain.CurrentDomain.BaseDirectory;
        public string resultPath, resultFullPath = string.Empty;
        public string pptClassName = "LDPlayerMainFrame";
        public IntPtr m_hGameWnd = IntPtr.Zero;
        public RECT rt = new RECT();
        public string oldPhoneNumber = string.Empty;
        public int total = 0;
        public BaiduOCR baidu = null;
        public string clientId, clientSecret = string.Empty;
        public string result = string.Empty;
        public string defaultPath = AppDomain.CurrentDomain.BaseDirectory + "DefaultPath.ini";
        public VerifyCode vfc = null;
        public string workId = "ww-0030";
        public string settime = "2019-06-09";
        public static string sqlitePath = AppDomain.CurrentDomain.BaseDirectory + @"sqlite3.db";
        public SQLiteHelper sqlLiteHelper = null;
        public int singleCount = 0;
        public string phoneNumberPath = basePath + "phoneNumber.vcf";
        public int times = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.MaximizeBox = false;//使最大化窗口失效
            InitPath();
            clientId = "IpxX29W1xPR1qV09Spke0ehP";
            clientSecret = "Cdl8Wde4qaesEgmaxl9Veu1tLz6GfM1o";
            baidu = new BaiduOCR(clientId, clientSecret);
            sqlLiteHelper = new SQLiteHelper(sqlitePath);
            GetPhoneNumber();
        }
        private void textBox1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog targetFolder = new FolderBrowserDialog();

            if (targetFolder.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = targetFolder.SelectedPath;//选定目录
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (!IsAuthorised())
            {
                MessageBox.Show("使用权限到期，请联系开发者！", "WeChatFilter");
                return;
            }

            string path1 = this.textBox1.Text;
            string path2 = this.textBox2.Text;
            string countStr = this.textBox3.Text;

            if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2) || string.IsNullOrEmpty(countStr))
            {
                MessageBox.Show("导出目录不能为空！", "WeChatFilter");
                return;
            }
            string defaultStr = path2 + "\r\n" + path1 + "\r\n" + countStr;

            File.WriteAllText(defaultPath, defaultStr, Encoding.Default);
            times = 0;
            Thread th = new Thread(StartWork);
            th.IsBackground = true;
            th.Start();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            string sqlStr = "delete from WeChatFilter where IsFilter = 1";
            int resuleLine = sqlLiteHelper.RunSql(sqlStr);
            if (resuleLine > 0)
                MessageBox.Show("已下载数据删除成功！", "WeChatFilter");
        }
        private void textBox2_Click(object sender, EventArgs e)
        {
            OpenFileDialog file = new OpenFileDialog();
            file.Filter = "所有文件(*.txt)|*.txt";
            file.ShowDialog();
            string phoneNumberPath = file.FileName;
            if (!File.Exists(phoneNumberPath))
            {
                MessageBox.Show("手机号码路径无效！", "WeChatFilter");
                return;
            }
            this.button1.Enabled = false;//禁止按钮点击
            this.button2.Enabled = false;
            this.button3.Enabled = false;
            this.textBox2.Enabled = false;

            this.textBox2.Text = phoneNumberPath;
            string[] phoneNumberArr = ReadPhoneNumber(phoneNumberPath);
            Thread th = new Thread(new ParameterizedThreadStart(InsterPhoneNumber));
            th.IsBackground = true;
            th.Start(phoneNumberArr);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            resultPath = this.textBox1.Text;
            if (string.IsNullOrEmpty(resultPath))
            {
                MessageBox.Show("导出目录不能为空！", "WeChatFilter");
                return;
            }

            string sqlStr = "select * from WeChatFilter where IsFilter =1";
            List<PhoneFilterDto> pfList = sqlLiteHelper.GetReaderSchema(sqlStr);
            int index = 0;
            foreach (var pf in pfList)
            {
                try
                {
                    if (string.IsNullOrEmpty(pf.Sex))
                        continue;
                    WriteResult(pf.PhoneNumber, pf.Sex);
                    index++;
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }
            }
            MessageBox.Show("成功导出" + index + "条记录！", "WeChatFilter");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string countStr = this.textBox3.Text;
            singleCount = int.Parse(!string.IsNullOrEmpty(countStr) ? countStr : "0");
            if (string.IsNullOrEmpty(countStr))
            {
                MessageBox.Show("导入数量不能为空！", "WeChatFilter");
                return;
            }

            AdbOperation();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            string sqlStr = "delete from WeChatFilter";
            int resuleLine = sqlLiteHelper.RunSql(sqlStr);
            if (resuleLine > 0)
                MessageBox.Show("所有数据删除成功！", "WeChatFilter");
        }
        /// <summary>
        /// 开始工作
        /// </summary>
        public void StartWork()
        {
            resultFullPath = resultPath + @"\result.txt";

            Thread.Sleep(1000 * 3);
            m_hGameWnd = User32API.FindWindow(pptClassName, null);
            string phoneNumberStr, sex = string.Empty;

            SimulationOperate();
            MessageBoxButtons message = MessageBoxButtons.OKCancel;
            DialogResult dr = MessageBox.Show("确认已经进入“新的朋友”界面，并且已经加载完毕！", "WeChat", message);
            if (dr == DialogResult.OK)
            {
                Thread.Sleep(1000 * 2);
                m_hGameWnd = User32API.FindWindow(pptClassName, null);
                if (m_hGameWnd != IntPtr.Zero)
                {
                    User32API.SwitchToThisWindow(m_hGameWnd, true);
                    User32API.GetWindowRect(m_hGameWnd, out rt);
                    User32API.MoveWindow(m_hGameWnd, 0, 0, 390, 728, true);
                }
                else
                {
                    MessageBox.Show("未找到指定句柄，无法继续操作！", "WeChatFilter");
                    return;
                }
                FirstPage();
                int nt = 0;

                while (true)
                {
                    try
                    {
                        for (int i = 0; i < nt; i++)
                        {
                            User32API.Keybd_event(VirtualKey.DOWN, 0, 0, 0);
                            User32API.Keybd_event(VirtualKey.DOWN, 0, KeyEvent.KEYEVENTF_KEYUP, 0);
                        }
                        Thread.Sleep(500);
                        phoneNumberStr = Screenshot();
                        if (!IsNumeric(phoneNumberStr) || phoneNumberStr.Length != 11)
                        {
                            nt = 1;
                            Thread.Sleep(1000);
                            continue;
                        }

                        if (phoneNumberStr == oldPhoneNumber)
                        {
                            times++;
                            if (times > 5)
                            {
                                WriteLog("结束！");
                                MessageBox.Show("结束！");
                                break;
                            }
                            m_hGameWnd = User32API.FindWindow(pptClassName, null);
                            if (m_hGameWnd != IntPtr.Zero)
                            {
                                User32API.SwitchToThisWindow(m_hGameWnd, true);
                                User32API.GetWindowRect(m_hGameWnd, out rt);
                                User32API.MoveWindow(m_hGameWnd, 0, 0, 390, 728, true);
                            }
                            nt = 1;
                            Thread.Sleep(1000);
                            continue;
                        }
                        else
                        {
                            times = 0;
                            nt = 2;
                        }

                        oldPhoneNumber = phoneNumberStr;
                        sex = LoopOperation();
                        result = phoneNumberStr + "：" + sex;
                        UpdateState(phoneNumberStr, sex);
                        total++;
                        UpdataText();
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex.ToString() + "\r\n");
                    }
                }
            }
        }
        /// <summary>
        /// 识别第一页
        /// </summary>
        public void FirstPage()
        {
            string phoneStr, sexStr = string.Empty;
            int nt = 2;
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < nt; j++)
                {
                    User32API.Keybd_event(VirtualKey.DOWN, 0, 0, 0);
                    User32API.Keybd_event(VirtualKey.DOWN, 0, KeyEvent.KEYEVENTF_KEYUP, 0);
                }

                phoneStr = FirstPageScreenshot(i);

                if (!IsNumeric(phoneStr) || phoneStr.Length != 11 || string.IsNullOrEmpty(phoneStr))
                {
                    nt = 1;
                    Thread.Sleep(1000);
                    continue;
                }
                nt = 2;
                sexStr = LoopOperation(i);
                result = phoneStr + "：" + sexStr;
                UpdateState(phoneStr, sexStr);
                total++;
                UpdataText();
            }
        }
        /// <summary>
        /// 更新面板数据
        /// </summary>
        public void UpdataText()
        {

            if (this.label2.InvokeRequired)// 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
            {
                this.label2.Invoke(new Action(() => { this.label2.Text = total.ToString(); }));
            }
            else
            {
                this.label2.Text = total.ToString();
            }
            if (this.listBox1.InvokeRequired)// 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
            {
                this.listBox1.Invoke(new Action(() => { this.listBox1.Items.Add(result); }));
            }
            else
            {
                this.listBox1.Items.Add(result);
            }
        }
        /// <summary>
        /// 模拟点击操作
        /// </summary>
        public void SimulationOperate()
        {
            if (m_hGameWnd != IntPtr.Zero)
            {
                User32API.SwitchToThisWindow(m_hGameWnd, true);
                User32API.GetWindowRect(m_hGameWnd, out rt);
                //User32API.MoveWindow(m_hGameWnd, 0, 0, rt.Width, rt.Height, true);
                User32API.MoveWindow(m_hGameWnd, 0, 0, 390, 728, true);
            }
            else
            {
                MessageBox.Show("未找到指定句柄，无法继续操作！", "WeChatFilter");
                return;
            }
            Win32ApiMouseClick(147, 714);//点击微信底部通讯录图标
            Thread.Sleep(1000);
            Win32ApiMouseClick(147, 714);//点击微信底部通讯录图标
            Thread.Sleep(1000);
            Win32ApiMouseClick(200, 110);//点击微信通讯录 -> 新的朋友      
            Thread.Sleep(1000);
        }
        /// <summary>
        /// 循环操作
        /// </summary>
        public string LoopOperation()
        {
            string sex = string.Empty;
            for (int i = 0; i < 2; i++)
            {
                Thread.Sleep(500);
                Win32ApiMouseClick(280, 700, 1);
                Thread.Sleep(1500);
                rt = new RECT(75, 100, 370, 130);
                Bitmap numberSexPic = ImageTool.GetScreenCapture(m_hGameWnd, rt);
                sex = CheckSex(numberSexPic);
                numberSexPic.Dispose();

                if (sex == "未知")
                {
                    rt = new RECT(130, 230, 250, 550);
                    Bitmap bit = ImageTool.GetScreenCapture(m_hGameWnd, rt);
                    bool isDetail = IsDetail(bit);
                    bit.Dispose();
                    if (isDetail)
                        break;
                    else
                        continue;
                }
                else
                    break;
            }
        back:
            User32API.Keybd_event(VirtualKey.ESCAPE, 0, 0, 0);
            User32API.Keybd_event(VirtualKey.ESCAPE, 0, KeyEvent.KEYEVENTF_KEYUP, 0);
            Thread.Sleep(800);
            rt = new RECT(130, 230, 250, 550);
            Bitmap bit1 = ImageTool.GetScreenCapture(m_hGameWnd, rt);
            bool isDetail1 = IsDetail(bit1);
            bit1.Dispose();
            if (isDetail1)
                goto back;
            return sex;
        }
        /// <summary>
        /// 循环操作
        /// </summary>
        public string LoopOperation(int i)
        {
            string sex = string.Empty;
            for (int j = 0; j < 2; j++)
            {
                Thread.Sleep(500);
                Win32ApiMouseClick(280, 210 + (i * 55), 1);
                Thread.Sleep(1500);

                rt = new RECT(75, 100, 370, 130);
                Bitmap numberSexPic = ImageTool.GetScreenCapture(m_hGameWnd, rt);
                //numberSexPic.Save(resultPath + @"\sex.bmp");
                sex = CheckSex(numberSexPic);
                numberSexPic.Dispose();
                if (sex == "未知")
                {
                    rt = new RECT(130, 230, 250, 550);
                    Bitmap bit = ImageTool.GetScreenCapture(m_hGameWnd, rt);
                    bool isDetail = IsDetail(bit);
                    bit.Dispose();
                    if (isDetail)
                        break;
                    else
                        continue;
                }
                else
                    break;
            }
        back:
            User32API.Keybd_event(VirtualKey.ESCAPE, 0, 0, 0);
            User32API.Keybd_event(VirtualKey.ESCAPE, 0, KeyEvent.KEYEVENTF_KEYUP, 0);
            Thread.Sleep(800);
            rt = new RECT(130, 230, 250, 550);
            Bitmap bit1 = ImageTool.GetScreenCapture(m_hGameWnd, rt);
            bool isDetail1 = IsDetail(bit1);
            bit1.Dispose();
            if (isDetail1)
                goto back;
            return sex;
        }
        public bool IsDetail(Bitmap bit)
        {
            int xx = 10;//误差值
            int greenCount = 0;
            bool isDetail = false;

            for (int i = 0; i < bit.Width; i++)
            {
                for (int j = 0; j < bit.Height; j++)
                {
                    Color pixelColor = bit.GetPixel(i, j);

                    int r = pixelColor.R;//颜色的 RED 分量值                
                    int g = pixelColor.G;//颜色的 GREEN 分量值                   
                    int b = pixelColor.B;//颜色的 BLUE 分量值 

                    if ((89 - xx) < r && r < (89 + xx) &&
                        (109 - xx) < g && g < (109 + xx) &&
                       (150 - xx) < b && b < (150 + xx))
                        greenCount++;
                }
            }
            if (greenCount > 50)
                isDetail = true;
            else
                isDetail = false;
            return isDetail;
        }
        /// <summary>
        /// 第一页截图
        /// </summary>
        public string FirstPageScreenshot(int i)
        {
            int y = 210 + (58 * i);
            rt = new RECT(131, y, 204, y + 15);
            m_hGameWnd = User32API.GetDesktopWindow();
            Bitmap numberSexPic = ImageTool.GetScreenCapture(m_hGameWnd, rt);
            //vfc = new VerifyCode(numberSexPic);
            //vfc.BitmapTo1Bpp(0.83);//二值化
            //vfc.GetPicValidByValue(128/, 11);/得到有效空间     
            numberSexPic.Save(capturePicPath + "number" + i + ".bmp");
            return baidu.GeneralBasicDemo(numberSexPic);
        }
        /// <summary>
        /// 截图
        /// </summary>
        /// <returns></returns>
        public string Screenshot()
        {
            rt = new RECT(131, 697, 204, 712);
            m_hGameWnd = User32API.GetDesktopWindow();
            //m_hGameWnd = User32API.FindWindow(pptClassName, null);
            Bitmap numberSexPic = ImageTool.GetScreenCapture(m_hGameWnd, rt);
            //vfc = new VerifyCode(numberSexPic);
            //vfc.BitmapTo1Bpp(0.82);//二值化
            //vfc.GetPicValidByValue(128, 11);//得到有效空间     
            //numberSexPic.Save(basePath + @"number.bmp");
            return baidu.GeneralBasicDemo(numberSexPic);
        }
        /// <summary>
        /// 鼠标点击
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="option">1：鼠标左键点击，2：鼠标右键点击</param>
        public void Win32ApiMouseClick(int x, int y, int option = 1)
        {
            User32API.SetCursorPos(x, y);//设置鼠标位置（相对于整个桌面）；
            Thread.Sleep(100);
            switch (option)
            {
                case 1:
                    {
                        User32API.MouseEvent(MouseEventType.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
                        Thread.Sleep(100);
                        User32API.MouseEvent(MouseEventType.MOUSEEVENTF_LEFTUP, x, y, 0, 0);
                        Thread.Sleep(100);
                        break;
                    }
                case 2:
                    {
                        User32API.MouseEvent(MouseEventType.MOUSEEVENTF_RIGHTDOWN, x, y, 0, 0);
                        Thread.Sleep(100);
                        User32API.MouseEvent(MouseEventType.MOUSEEVENTF_RIGHTUP, x, y, 0, 0);
                        Thread.Sleep(100);
                        break;
                    }
                default:
                    break;
            }
        }
        /// <summary>
        /// 鼠标点击
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="index">点击次数</param>
        public void LeftMouseClick(int x, int y, int index = 1)
        {
            User32API.SetCursorPos(x, y);//设置鼠标位置（相对于整个桌面）；
            Thread.Sleep(100);
            for (int i = 0; i < index; i++)
            {
                User32API.MouseEvent(MouseEventType.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
                Thread.Sleep(100);
                User32API.MouseEvent(MouseEventType.MOUSEEVENTF_LEFTUP, x, y, 0, 0);
                Thread.Sleep(100);
            }
        }
        /// <summary>
        /// Y轴华滑动
        /// </summary>
        public void SlideY(int x, int y, int yd)
        {
            int lastL = y - yd;
            int index = yd / 2;
            User32API.SetCursorPos(x, y);//设置鼠标位置（相对于整个桌面）
            User32API.MouseEvent(MouseEventType.MOUSEEVENTF_LEFTDOWN, x, y, 0, 0);
            for (int i = 0; i < index; i++)
            {
                User32API.SetCursorPos(x, y - (2 * i));//设置鼠标位置（相对于整个桌面）
                Thread.Sleep(50);
            }
            User32API.MouseEvent(MouseEventType.MOUSEEVENTF_LEFTUP, x, lastL, 0, 0);
        }
        /// <summary>
        /// 日志打印
        /// </summary>
        /// <param name="log"></param>
        public static void WriteLog(string log)
        {
            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + "log\\";//日志文件夹
                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists)//判断文件夹是否存在
                    dir.Create();//不存在则创建

                FileInfo[] subFiles = dir.GetFiles();//获取该文件夹下的所有文件
                foreach (FileInfo f in subFiles)
                {
                    string fname = Path.GetFileNameWithoutExtension(f.FullName); //获取文件名，没有后缀
                    DateTime start = Convert.ToDateTime(fname);//文件名转换成时间
                    DateTime end = Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd"));//获取当前日期
                    TimeSpan sp = end.Subtract(start);//计算时间差
                    if (sp.Days > 30)//大于30天删除
                        f.Delete();
                }

                string logName = DateTime.Now.ToString("yyyy-MM-dd") + ".log";//日志文件名称，按照当天的日期命名
                string fullPath = path + logName;//日志文件的完整路径
                string contents = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " -> " + log + "\r\n";//日志内容

                File.AppendAllText(fullPath, contents, Encoding.UTF8);//追加日志

            }
            catch (Exception ex)
            {

            }
        }
        /// <summary>
        /// 判断字符串是不是数字类型的 true是数字
        /// </summary>
        /// <param name="value">需要检测的字符串</param>
        /// <returns>true是数字</returns>
        public bool IsNumeric(string value)
        {
            return Regex.IsMatch(value, @"^\d(\.\d+)?|[1-9]\d+(\.\d+)?$");
        }
        /// <summary>
        /// 判断性别
        /// </summary>
        /// <param name="bitSex"></param>
        public string CheckSex(Bitmap bitSex)
        {
            //男 88，155，238
            //女 238，115，87
            int man = 0;
            int woman = 0;
            string sex = string.Empty;
            int xx = 20;//误差值
            ///取出每个像素的颜色值，并记录下来
            for (int i = 0; i < bitSex.Width; i++)
            {
                for (int j = 0; j < bitSex.Height; j++)
                {
                    Color pixelColor = bitSex.GetPixel(i, j);

                    int r = pixelColor.R;//颜色的 RED 分量值                
                    int g = pixelColor.G;//颜色的 GREEN 分量值                   
                    int b = pixelColor.B;//颜色的 BLUE 分量值 

                    if ((88 - xx) < r && r < (88 + xx) &&
                        (155 - xx) < g && g < (155 + xx) &&
                       (238 - xx) < b && b < (238 + xx))
                    {
                        man++;
                    }
                    if ((238 - xx) < r && r < (238 + xx) &&
                       (115 - xx) < g && g < (115 + xx) &&
                       (87 - xx) < b && b < (87 + xx))
                    {
                        woman++;
                    }
                }
            }

            if (man > 30 && woman < 30)
                sex = "男";
            else if (woman > 30 && man < 30)
                sex = "女";
            else
                sex = "未知";
            return sex;
        }

        public void WriteResult(string result, string sex)
        {
            if (!Directory.Exists(resultPath))
                Directory.CreateDirectory(resultPath);
            result += "\r\n";
            switch (sex)
            {
                case "男":
                    {
                        resultFullPath = resultPath + @"\男.txt";
                        File.AppendAllText(resultFullPath, result);
                        break;
                    }
                case "女":
                    {
                        resultFullPath = resultPath + @"\女.txt";
                        File.AppendAllText(resultFullPath, result);
                        break;
                    }
                case "未知":
                    {
                        resultFullPath = resultPath + @"\未知.txt";
                        File.AppendAllText(resultFullPath, result);
                        break;
                    }
                default:
                    break;
            }
        }
        public bool IsAuthorised()
        {
            //string conStr = "Server=111.230.149.80;DataBase=MyDB;uid=sa;pwd=1add1&one";
            //using (SqlConnection con = new SqlConnection(conStr))
            //{
            //    string sql = string.Format("select count(*) from MyWork Where WorkId ='{0}'", workId);
            //    using (SqlCommand cmd = new SqlCommand(sql, con))
            //    {
            //        con.Open();
            //        int count = int.Parse(cmd.ExecuteScalar().ToString());
            //        if (count > 0)
            //            return true;
            //    }
            //}
            DateTime ETime = Convert.ToDateTime(settime);

            DateTime newTime = DateTime.Now;
            TimeSpan tsTime = newTime - ETime;
            if (tsTime.Days >= 1)
                return false;
            return true;
        }
        public void InitPath()
        {
            try
            {
                if (File.Exists(defaultPath))
                {
                    string pathStr = File.ReadAllText(defaultPath, Encoding.Default);
                    string[] pathArr = Regex.Split(pathStr, "\r\n", RegexOptions.IgnoreCase);

                    string path1 = string.Empty;
                    string path2 = string.Empty;
                    string count = string.Empty;

                    if (pathArr.Length >= 2)
                        path1 = pathArr[1];
                    if (pathArr.Length >= 1)
                        path2 = pathArr[0];
                    if (pathArr.Length >= 2)
                        count = pathArr[2];

                    this.textBox1.Text = path1;
                    this.textBox2.Text = path2;
                    this.textBox3.Text = count;
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
        }

        /// <summary>
        /// 读取号码
        /// </summary>
        /// <returns></returns>
        public string[] ReadPhoneNumber(string phoneNumberPath)
        {
            string phoneNumberStr = File.ReadAllText(phoneNumberPath);
            string[] phoneNumberArr = Regex.Split(phoneNumberStr, "\r\n", RegexOptions.IgnoreCase);
            return phoneNumberArr;
        }

        /// <summary>
        /// 插入手机号码
        /// </summary>
        public void InsterPhoneNumber(object phoneNumberObj)
        {
            string[] phoneNumberArr = phoneNumberObj as string[];
            if (!File.Exists(sqlitePath))
            {
                MessageBox.Show("数据故障！", "WeChatFilter");
                return;
            }

            int index = 0;
            foreach (var phoneNumber in phoneNumberArr)
            {
                try
                {
                    if (phoneNumber.Trim().Length != 11)
                        continue;
                    if (!IsNumeric(phoneNumber.Trim()))
                        continue;
                    string sql = string.Format("insert into WeChatFilter (PhoneNumber,Sex,IsFilter) values ('{0}','{1}','{2}')", phoneNumber, "", 0);
                    int qwe = sqlLiteHelper.ExeSqlOut(sql);

                    index++;

                    if (this.label6.InvokeRequired)// 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                        this.label6.Invoke(new Action(() => { this.label6.Text = index.ToString(); }));
                    else
                        this.label6.Text = index.ToString();
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }
            }

            MessageBox.Show("导入成功！", "WeChatFilter");

            if (this.label6.InvokeRequired)// 当一个控件的InvokeRequired属性值为真时，说明有一个创建它以外的线程想访问它
                this.label6.Invoke(new Action(() => { this.button1.Enabled = true; this.button2.Enabled = true; this.button3.Enabled = true; }));
            else
            { this.button1.Enabled = true; this.button2.Enabled = true; this.button3.Enabled = true; }
        }
        /// <summary>
        /// 执行adb命令
        /// </summary>
        /// <param name="commandStr">执行的命令参数</param>
        /// <returns></returns>
        public string RunAdbCommand(string commandStr)
        {
            string adbResult = string.Empty;
            try
            {
                String cmd = Application.StartupPath + "\\adb\\adb.exe";
                Process p = new Process();
                p.StartInfo.FileName = cmd;           //设定程序名   
                p.StartInfo.Arguments = commandStr;    //设定程式执行參數   
                p.StartInfo.UseShellExecute = false;        //关闭Shell的使用   
                p.StartInfo.RedirectStandardInput = true;   //重定向标准输入   
                p.StartInfo.RedirectStandardOutput = true;  //重定向标准输出   
                p.StartInfo.RedirectStandardError = true;   //重定向错误输出   
                p.StartInfo.CreateNoWindow = true;          //设置不显示窗口   
                p.Start();
                adbResult = p.StandardOutput.ReadToEnd();
                p.Close();
            }
            catch (Exception ex)
            {
                adbResult = ex.ToString();
            }
            return adbResult;
        }
        /// <summary>
        /// adb执行命令
        /// </summary>
        public void AdbOperation()
        {
            string commandStr1 = "-s emulator-5554 shell pm clear com.android.providers.contacts";//清除手机号
            string commandStr2 = "-s emulator-5554 push " + phoneNumberPath + " /sdcard/phoneNumber.vcf";//将手机号导入模拟器
            string commandStr3 = "-s emulator-5554 shell am start -t \"text/x-vcard\" -d \"file:///sdcard/phoneNumber.vcf\" -a android.intent.action.VIEW com.android.contacts";//导入手机号
            string commandStr4 = "shell rm /sdcard/phoneNumber.vcf";//删除模拟器中的文件
            string commandStr5 = "shell am start com.tencent.mm/com.tencent.mm.ui.LauncherUI";//打开微信界面
            string commandStr6 = "shell am force-stop com.tencent.mm";//关闭微信界面

            RunAdbCommand(commandStr6);//关闭微信界面
            Thread.Sleep(1000 * 2);
            RunAdbCommand(commandStr1);//清除手机号
            CreatVcf();
            Thread.Sleep(1000 * 1);
            RunAdbCommand(commandStr4);//删除模拟器中的文件
            Thread.Sleep(1000 * 1);
            RunAdbCommand(commandStr2);//将手机号导入模拟器
            Thread.Sleep(1000 * 2);
            RunAdbCommand(commandStr3);//导入手机号
            Thread.Sleep(1000 * 10);
            //RunAdbCommand(commandStr5);//打开微信界面
            //Thread.Sleep(1000 * 10);
            MessageBox.Show("号码导入成功！");
        }

        /// <summary>
        /// 生成vcf文件
        /// </summary>
        public void CreatVcf()
        {
            string sqlStr = "select * from WeChatFilter where IsFilter=0 order by id limit 0," + singleCount + ";";
            List<PhoneFilterDto> pfList = sqlLiteHelper.GetReaderSchema(sqlStr);
            if (File.Exists(phoneNumberPath))
                File.Delete(phoneNumberPath);
            foreach (var pf in pfList)
            {
                try
                {
                    if (pf.PhoneNumber.Length != 11)
                        continue;
                    if (!IsNumeric(pf.PhoneNumber))
                        continue;
                    CreateVCard(pf.PhoneNumber);
                }
                catch (Exception ex)
                {
                    WriteLog(ex.ToString());
                }
            }
        }
        /// <summary>
        /// 获取并更新为筛选数量
        /// </summary>
        public void GetPhoneNumber()
        {
            string sqlStr = "select count(*) from WeChatFilter where IsFilter=0";
            try
            {
                Object obj = sqlLiteHelper.GetScalar(sqlStr);
                if (obj != null)
                {
                    this.label9.Text = obj.ToString();
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
        }

        /// <summary>
        /// 更新状态
        /// </summary>
        /// <param name="pn"></param>
        /// <param name="sex"></param>
        public void UpdateState(string pn, string sex)
        {
            string sqlStr = "UPDATE WeChatFilter SET IsFilter = 1,Sex = '" + sex + "' WHERE PhoneNumber='" + pn + "'";
            int resuleLine = sqlLiteHelper.RunSql(sqlStr);
        }

        #region 生成VCard

        /// <summary>
        /// 生成VCard
        /// </summary>
        public void CreateVCard(string mobilePhone)
        {
            try
            {
                StreamWriter stringWrite = new StreamWriter(phoneNumberPath, true, System.Text.Encoding.Default);
                stringWrite.WriteLine("BEGIN:VCARD");
                stringWrite.WriteLine("VERSION:3.0");
                stringWrite.WriteLine("N;CHARSET=UTF-8:" + mobilePhone);
                stringWrite.WriteLine("FN;CHARSET=UTF-8:" + mobilePhone);
                stringWrite.WriteLine("TEL;TYPE=CELL:" + mobilePhone);
                stringWrite.WriteLine("END:VCARD");
                stringWrite.Close();
                stringWrite.Dispose();

            }
            catch (Exception ex) { WriteLog(ex.ToString()); }
        }

        #endregion
    }
}
