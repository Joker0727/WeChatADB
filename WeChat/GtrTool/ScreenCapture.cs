using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace GamePic
{

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public RECT(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
        /// <summary>
        /// ×ª»»µ½RectangleÀà.
        /// </summary>
        /// <returns></returns>
        public Rectangle ToRectangle()
        {
            return new Rectangle(left, top, right - left, bottom - top);
        }
        public int Width
        {
            get { return right - left; }
        }

        public int Height
        {
            get { return bottom - top; }
        }

    }

    public class ScreenCapture
    {
        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDc);
        [DllImport("gdi32.dll")]
        static extern IntPtr DeleteObject(IntPtr hDc);
        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowDC(IntPtr hWnd);
        [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);
        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImportAttribute("user32.dll", EntryPoint = "PrintWindow")]
        public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);
        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hDC);
        [DllImportAttribute("Dbghelp.dll", EntryPoint = "MakeSureDirectoryPathExists")]
        public static extern bool MakeSureDirectoryPathExists(string DirPath);
        [DllImportAttribute("gdi32.dll")]
        private static extern bool BitBlt(
             IntPtr hdcDest, // handle to destination DC
             int nXDest, // x-coord of destination upper-left corner
             int nYDest, // y-coord of destination upper-left corner
             int nWidth, // width of destination rectangle
             int nHeight, // height of destination rectangle
             IntPtr hdcSrc, // handle to source DC
             int nXSrc, // x-coordinate of source upper-left corner
             int nYSrc, // y-coordinate of source upper-left corner
             Int32 dwRop // raster operation code
        );
        public static bool PrtWindowRET(IntPtr hWnd, string strFileName, RECT rect)
        {
            return PrtWindow(hWnd, strFileName, rect);
        }

        public static bool PrtWindowHWND(IntPtr hWnd, string strPictureDir, int picNo)
        {
            string strPicName = "";

            strPicName = string.Format("{0:00}", picNo % 100); 
            string strFileName = "";
            DateTime dt = DateTime.Now;
            strFileName = string.Format("{0}{1:00}{2:00}{3:00}{4}.jpg", strPictureDir, dt.Hour, dt.Minute, dt.Second, strPicName);
            
            RECT rect;
            GetWindowRect(hWnd, out rect);
            return PrtWindow(hWnd, strFileName, rect);
        }

        public static bool PrtWindow(IntPtr hWnd, string strFileName,RECT rect)
        {
            IntPtr hscrdc = GetWindowDC(hWnd);
            IntPtr hbitmap = CreateCompatibleBitmap(hscrdc, rect.right - rect.left, rect.bottom - rect.top);
            IntPtr hmemdc = CreateCompatibleDC(hscrdc);
            IntPtr hOldBmp = SelectObject(hmemdc,hbitmap);
            if (BitBlt(hmemdc, 0, 0, rect.right - rect.left, rect.bottom - rect.top, hscrdc, rect.left, rect.top, 13369376 | 1073741824))
            {
                Bitmap bmp = Image.FromHbitmap(hbitmap);
                SelectObject(hmemdc, hOldBmp);
                DeleteObject(hbitmap);
                DeleteDC(hmemdc);
                ReleaseDC(hWnd, hscrdc);
                bmp.Save(strFileName, ImageFormat.Jpeg);
                bmp.Dispose();
                return true;
            }
            return false;
        }
    }
}
