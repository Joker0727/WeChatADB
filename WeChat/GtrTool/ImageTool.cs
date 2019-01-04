using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
//using WinAPI;
//using WinAPI.Struct;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace AutoToolHelper
{
    public class ImageTool
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();
        private static IntPtr hDesk = GetDesktopWindow();
        public static RECT rt = GetRect(hDesk);
        //private static BitmapData pBData = null;
        public static byte[] pData = new byte[3000 * 1600 * 4];
        public static byte[] tData = new byte[512 * 512 * 3];

        private static int DistanceU = 0;
        private static int DistanceA = 0;
        private static string UFontName = "宋体";
        private static FontStyle UFontStyle = FontStyle.Regular;

        private static RECT GetRect(IntPtr hDesk)
        {
            RECT rect = new RECT();
            User32API.GetWindowRect(hDesk, out rect);
            return rect;

        }
        //private static void setDeskData()
        //{
        //    pData = new byte(rt.Height * rt.Width * 3);
        //}
        /// <summary>
        /// 二值化查找图片坐标
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="filePic">源图片路径</param>
        /// <param name="dstRect">图片匹配区域</param>
        /// <param name="nThreshold">二值化阈值</param>
        /// <returns>屏幕坐标</returns>
        public static Point FindBinarizationPic(IntPtr hWnd, string filePic, RECT dstRect,int nThreshold)
        {
            Point pt = Point.Empty;
            if (!File.Exists(filePic))
                return pt;
            #region set rect
            RECT rect = new RECT();
            User32API.GetWindowRect(hWnd, out rect);
            dstRect.left += rect.left;
            if (dstRect.right <= 0)
                dstRect.right = rect.right;
            else
                dstRect.right += rect.left;

            dstRect.top += rect.top;
            if (dstRect.bottom <= 0)
                dstRect.bottom = rect.bottom;
            else
                dstRect.bottom += rect.top;

            if (dstRect.left < 0) { dstRect.left = 0; }
            if (dstRect.top < 0) { dstRect.top = 0; }
            if ((dstRect.right > rect.right) || dstRect.right <= 0) { dstRect.right = rect.right; }
            if ((dstRect.bottom > rect.bottom) || dstRect.bottom <= 0) { dstRect.bottom = rect.bottom; }
            #endregion

            #region set bitmapData
            //源图片
            Bitmap srcBit = (Bitmap)Bitmap.FromFile(filePic, false);
            Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
            BitmapData srcBData = srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            //目标句柄图片
            Bitmap dstBit = GetScreenCapture(hDesk, dstRect);
            RGB2Binarization(dstBit, nThreshold);
            //dstBit.Save(@"E:\black.bmp");
            Rectangle dstRectN = new Rectangle(0, 0, dstBit.Width, dstBit.Height);
            BitmapData dstBData = dstBit.LockBits(dstRectN, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            IntPtr srcScan = srcBData.Scan0;
            IntPtr dstScan = dstBData.Scan0;
            #endregion

            #region unsafe operation
            unsafe
            {
                byte* srcP = (byte*)srcScan;
                byte* dstP = (byte*)dstScan;
                byte* dstPNew = dstP;
                byte bBG = srcP[0];
                byte gBG = srcP[1];
                byte rBG = srcP[2];

                bool flagComp;
                int dstOffset = dstBData.Stride - dstBData.Width * 3;
                int srcOffset = srcBData.Stride - srcBData.Width * 3;
                int dstNewOffset = dstBData.Stride - srcBData.Width * 3;

                for (int y = dstRect.top; y < dstRect.bottom - srcRect.Height; y++)
                {
                    for (int x = dstRect.left; x < dstRect.right - srcRect.Width; x++, dstP += 3)
                    {
                        flagComp = true;
                        srcP = (byte*)srcScan;
                        dstPNew = dstP;
                        for (int j = 0; j < srcRect.Height; j++)
                        {
                            if (!flagComp) { break; }

                            for (int i = 0; i < srcRect.Width; i++)
                            {
                                if (!flagComp) { break; }

                                if ( ((dstPNew[0] == srcP[0]) && (dstPNew[1] == srcP[1]) && (dstPNew[2] == srcP[2]))
                                    || ((srcP[0] == bBG) && (srcP[1] == gBG) && (srcP[2] == rBG)) )
                                    flagComp = true;
                                else
                                    flagComp = false;

                                dstPNew += 3;
                                srcP += 3;
                            }
                            srcP += srcOffset;
                            dstPNew += dstNewOffset;
                        }

                        if (flagComp)
                        {
                            dstBit.UnlockBits(dstBData);
                            pt.X = x;
                            pt.Y = y;
                            srcBit.UnlockBits(srcBData);
                            dstBit.Dispose();
                            srcBit.Dispose();
                            return pt;
                        }

                    }
                    dstP += dstOffset + srcRect.Width * 3;
                }

            }
            #endregion

            dstBit.UnlockBits(dstBData);
            srcBit.UnlockBits(srcBData);
            dstBit.Dispose();
            srcBit.Dispose();
            return pt;
        }
        public static Point FindPic(IntPtr hWnd, string filePic)
        {
            return FindPic(hWnd, filePic, new RECT(0, 0, 0, 0), 36);
        }
        /// <summary>
        /// 截图后查找窗口界面图片坐标
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="filePic">对比图片路径</param>
        /// <param name="dstRect">窗口中查找区域</param>
        /// <param name="errorValue">颜色误差值(0~128)</param>
        /// <returns>屏幕坐标</returns>
        public static Point FindPic(IntPtr hWnd, string filePic, RECT dstRect, int errorValue)
        {
            Point pt = Point.Empty;
            if (!File.Exists(filePic))
                return pt;
            #region set rect
            if (hWnd == (IntPtr)0)
                hWnd = hDesk;
            RECT rect = new RECT();
            User32API.GetWindowRect(hWnd, out rect);
            dstRect.left += rect.left;
            if (dstRect.right <= 0)
                dstRect.right = rect.right;
            else
                dstRect.right += rect.left;

            dstRect.top += rect.top;
            if (dstRect.bottom <= 0)
                dstRect.bottom = rect.bottom;
            else
                dstRect.bottom += rect.top;

            if (dstRect.left < 0) { dstRect.left = 0; }
            if (dstRect.top < 0) { dstRect.top = 0; }
            if ((dstRect.right > rect.right) || dstRect.right <= 0) { dstRect.right = rect.right; }
            if ((dstRect.bottom > rect.bottom) || dstRect.bottom <= 0) { dstRect.bottom = rect.bottom; }
            #endregion
            try
            {
                #region set bitmapData
                //源图片
                Bitmap srcBit = (Bitmap)Bitmap.FromFile(filePic, false);
                Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
                BitmapData srcBData = srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                //目标句柄图片
                Bitmap dstBit = GetScreenCapture(hDesk, dstRect);
                //dstBit.Save(@"E:\me.bmp");
                Rectangle dstRectN = new Rectangle(0, 0, dstBit.Width, dstBit.Height);
                BitmapData dstBData = dstBit.LockBits(dstRectN, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

                IntPtr srcScan = srcBData.Scan0;
                IntPtr dstScan = dstBData.Scan0;
                #endregion

                #region unsafe operation
                unsafe
                {
                    byte* srcP = (byte*)srcScan;
                    byte* dstP = (byte*)dstScan;
                    byte* dstPNew = dstP;
                    byte bBG = srcP[0];
                    byte gBG = srcP[1];
                    byte rBG = srcP[2];

                    bool flagComp;
                    int dstOffset = dstBData.Stride - dstBData.Width * 3;
                    int srcOffset = srcBData.Stride - srcBData.Width * 3;
                    int dstNewOffset = dstBData.Stride - srcBData.Width * 3;

                    for (int y = dstRect.top; y < dstRect.bottom - srcRect.Height; y++)
                    {
                        for (int x = dstRect.left; x < dstRect.right - srcRect.Width; x++, dstP += 3)
                        {
                            flagComp = true;
                            srcP = (byte*)srcScan;
                            dstPNew = dstP;
                            for (int j = 0; j < srcRect.Height; j++)
                            {
                                if (!flagComp) { break; }

                                for (int i = 0; i < srcRect.Width; i++)
                                {
                                    if (!flagComp) { break; }

                                    if (((Math.Abs(dstPNew[0] - srcP[0]) <= errorValue) &&
                                        (Math.Abs(dstPNew[1] - srcP[1]) <= errorValue) &&
                                        (Math.Abs(dstPNew[2] - srcP[2]) <= errorValue))
                                        || ((srcP[0] == bBG) && (srcP[1] == gBG) && (srcP[2] == rBG)))
                                        flagComp = true;
                                    else
                                        flagComp = false;

                                    dstPNew += 3;
                                    srcP += 3;
                                }
                                srcP += srcOffset;
                                dstPNew += dstNewOffset;
                            }

                            if (flagComp)
                            {
                                dstBit.UnlockBits(dstBData);
                                pt.X = x;
                                pt.Y = y;
                                srcBit.UnlockBits(srcBData);
                                dstBit.Dispose();
                                srcBit.Dispose();
                                return pt;
                            }

                        }
                        dstP += dstOffset + srcRect.Width * 3;
                    }

                }
                #endregion

                dstBit.UnlockBits(dstBData);
                srcBit.UnlockBits(srcBData);
                dstBit.Dispose();
                srcBit.Dispose();
            }
            catch
            { 
            }
            return pt;
        }

        public static bool GetTextData(string strText, int fontSize, int colorFont,ref int w, ref int h)
        {
            int r=colorFont % 256;
            int g=(colorFont / 256) % 256;
            int b=colorFont / 65536;
            return GetTextData(strText,fontSize,Color.FromArgb(r,g,b),ref w,ref h);
        }

        //int size,COLORREF color,int Interval,CString ZiTi,int width,int IntervalE)
        public static bool GetTextData(string strText, int fontSize, Color colorFont, ref int w, ref int h)
        {
            w = 0;
            h = 0;
            Bitmap srcBit;
            if (DistanceU !=0 || DistanceA !=0)
                srcBit = CreateTextBmp(strText, colorFont, UFontName, fontSize, UFontStyle, DistanceU, DistanceA);
            else
                srcBit = CreateTextBmp(strText, colorFont, UFontName, fontSize);//, DistanceU, DistanceA);
#if DEBUG
            srcBit.Save(@"E:\text.bmp");
#endif
            Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
            BitmapData pBData = srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            if (pBData.Stride * pBData.Height > 512 * 512 * 3)
                FileRW.WriteToFile("写入文字出错！");
            else
            {
                System.Runtime.InteropServices.Marshal.Copy(pBData.Scan0, tData, 0, pBData.Stride * pBData.Height);
                w = pBData.Stride;
                h = pBData.Height;
            }
            srcBit.UnlockBits(pBData);
            srcBit.Dispose();
            return true;
        }
        public static void SetFont(string fontName, FontStyle fontStyle, int distanceU, int distanceA)
        {
            UFontName = fontName;
            UFontStyle = fontStyle;
            DistanceU = distanceU;
            DistanceA = distanceA;
        }
        public static string MakeTheWordPic(string strText, int fontSize, int colorFont,Point pt,int x,int y)
        {
            int r = colorFont % 256;
            int g = (colorFont / 256) % 256;
            int b = colorFont / 65536;


            Bitmap txtBit, srcBit, outBit, WatermarkBit;
            ///////////////////////////////////////////////////
            if (DistanceU != 0 || DistanceA != 0)
                txtBit = CreateTextBmp(strText, Color.FromArgb(r, g, b), UFontName, fontSize, UFontStyle, DistanceU, DistanceA);
            else
                txtBit = CreateTextBmp(strText, Color.FromArgb(r, g, b), UFontName, fontSize);//, DistanceU, DistanceA);
#if DEBUG
            txtBit.Save(@"E:\text.bmp");
#endif
            int tHeight = txtBit.Height;
            Rectangle txtRect = new Rectangle(0, 0, txtBit.Width, txtBit.Height);
            BitmapData pBData = txtBit.LockBits(txtRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);


            byte[] aData = new byte[pBData.Stride * pBData.Height];
            System.Runtime.InteropServices.Marshal.Copy(pBData.Scan0, aData, 0, pBData.Stride * pBData.Height);
            txtBit.UnlockBits(pBData);
            txtBit.Dispose();
            ///////////////////////////////////////////////////
            r = 255;
            g = 255;
            b = 255;
            strText = "用鼠标点击";
            fontSize = 80;
            //if (DistanceU != 0 || DistanceA != 0)
            //    WatermarkBit = CreateTextBmp(strText, Color.FromArgb(r, g, b), UFontName, fontSize, UFontStyle, DistanceU, DistanceA);
            //else
            WatermarkBit = CreateTextBmp(strText, Color.FromArgb(r, g, b), UFontName, fontSize);//, DistanceU, DistanceA);
#if DEBUG
            WatermarkBit.Save(@"E:\Watermark.bmp");
#endif
            int wHeight = WatermarkBit.Height;
            Rectangle WatermarkRect = new Rectangle(0, 0, WatermarkBit.Width, WatermarkBit.Height);
            BitmapData wBData = WatermarkBit.LockBits(WatermarkRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);


            byte[] wData = new byte[wBData.Stride * wBData.Height];
            System.Runtime.InteropServices.Marshal.Copy(wBData.Scan0, wData, 0, wBData.Stride * wBData.Height);
            WatermarkBit.UnlockBits(wBData);
            WatermarkBit.Dispose();
            ///////////////////////////////////////////////////
            RECT rect = new RECT(pt.X,pt.Y,pt.X+x,pt.Y +y);
            srcBit = ImageTool.GetScreenCapture(rect);

#if DEBUG
            srcBit.Save(@"E:\SCR.bmp");
#endif
            int sHeight = srcBit.Height;
            Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
            BitmapData qBData = srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            byte[] bData = new byte[qBData.Stride * qBData.Height];
            System.Runtime.InteropServices.Marshal.Copy(qBData.Scan0, bData, 0, qBData.Stride * qBData.Height);
            srcBit.UnlockBits(qBData);
            srcBit.Dispose();

            int Width = (qBData.Stride > pBData.Stride) ? qBData.Stride : pBData.Stride;
            int Height = qBData.Height + pBData.Height;
            int Len = (Width * Height);
            byte[] cData = new byte[Len];

            for (int i = 0; i < sHeight; i++)
            {
                for (int j = 0; j < qBData.Stride; j++)
                {
                    cData[i * Width + j] = bData[i * qBData.Stride + j];
                }
            }


  　　      int Alpha   =  10;
            //int R = (R1 * (100 - Alpha) + R2 * Alpha) / 100;
            //int G = (G1 * (100 - Alpha) + G2 * Alpha) / 100;
            //int B = (B1 * (100 - Alpha) + B2 * Alpha) / 100;  
            for (int i = 0; i < wHeight; i++)
            {
                for (int j = 0; j < wBData.Stride; j++)
                {
                    byte[] c = System.BitConverter.GetBytes((cData[(i + Height / 2 - fontSize) * Width + j] * (100 - Alpha) + wData[i * wBData.Stride + j] * Alpha) / 100);
                    cData[(i + Height / 2 - fontSize) * Width + j] = c[0];
                }
            }
            for (int i = 0; i < tHeight; i++)
            {
                for (int j = 0; j < pBData.Stride; j++)
                {
                    cData[(i + sHeight) * Width + j] = aData[i * pBData.Stride + j];
                }
            }



            string strSavePath = @"E:\OUT.bmp";
            outBit = CreatBmpFromByte(cData, Width/3, Height);
            outBit.Save(strSavePath);
            outBit.Dispose();
            return strSavePath;



        }
        public static bool GetDcData(byte[] sData)
        {
            Bitmap dstBit = GetScreenCapture(hDesk,rt);
            //dstBit.Save(@"C:\Users\Joker\Desktop\me.bmp");
            Rectangle dstRectN = new Rectangle(0, 0, dstBit.Width, dstBit.Height);
            BitmapData pBData = dstBit.LockBits(dstRectN, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            if (pBData.Stride * pBData.Height > rt.Height * rt.Width * 4)
            {
                FileRW.WriteToFile("获取频幕数据出错！");
                dstBit.UnlockBits(pBData);
                dstBit.Dispose();
                return false;
            }
            System.Runtime.InteropServices.Marshal.Copy(pBData.Scan0, sData,0, pBData.Stride * pBData.Height);
            dstBit.UnlockBits(pBData);
            dstBit.Dispose();
           //FileRW.WriteToFile("获取数据成功！");
            return true;
        }
        public static bool GetPicData(string filePic,ref int w, ref int h)
        {
            if (!File.Exists(filePic))
            {
                w = 0;
                h = 0;
                FileRW.WriteToFile(filePic + "<< 文件不存在！");
                return false;
            }
            Bitmap srcBit = (Bitmap)Bitmap.FromFile(filePic, false);
            Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
            BitmapData pBData=srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            w = pBData.Stride;
            h = srcBit.Height;
            if (pBData.Stride * pBData.Height > 512 * 512 * 3)
            {
                srcBit.UnlockBits(pBData);
                srcBit.Dispose();
                FileRW.WriteToFile(filePic + "<< 图片过大！");
                return false;
            }
            System.Runtime.InteropServices.Marshal.Copy(pBData.Scan0, tData, 0, pBData.Stride * pBData.Height);
            srcBit.UnlockBits(pBData);
            srcBit.Dispose();
            return true;
        }
        public static bool GetPicData32(string filePic, ref int w, ref int h)
        {
            if (!File.Exists(filePic))
            {
                w = 0;
                h = 0;
                FileRW.WriteToFile(filePic + "<< 文件不存在！");
                return false;
            }
            Bitmap srcBit = (Bitmap)Bitmap.FromFile(filePic, false);
            Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
            BitmapData pBData = srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            w = pBData.Stride;
            h = srcBit.Height;
            if (pBData.Stride * pBData.Height > 512 * 512 * 4)
            {
                srcBit.UnlockBits(pBData);
                srcBit.Dispose();
                FileRW.WriteToFile(filePic + "<< 图片过大！");
                return false;
            }
            System.Runtime.InteropServices.Marshal.Copy(pBData.Scan0, tData, 0, pBData.Stride * pBData.Height);
            srcBit.UnlockBits(pBData);
            srcBit.Dispose();
            return true;
        }
        public static Point FTextBmp(string strText, int fontSize, int colorFont, IntPtr Hwnd)
        {
            RECT rect = new RECT();
            User32API.GetWindowRect(Hwnd, out rect);
            return FTextBmp(strText, fontSize, colorFont, rect.left, rect.top, rect.right, rect.bottom);
        }
        public static Point FTextBmp(string strText, int fontSize, int colorFont,RECT rect)
        {
            return FTextBmp(strText, fontSize, colorFont, rect.left, rect.top, rect.right, rect.bottom);
        }
        public static Point FTextBmp(string strText, int fontSize, int colorFont)
        {
            return FTextBmp(strText, fontSize, colorFont,0,0,rt.Width,rt.Height);
        }
        public static Point FTextBmp(string strText, int fontSize, int colorFont, int left, int top, int right, int bottom)
        {
            int r = colorFont % 256;
            int g = (colorFont / 256) % 256;
            int b = colorFont / 65536;
            return FTextBmp(strText, fontSize, Color.FromArgb(r, g, b),left,top,right,bottom); 
        }
        public static Point FTextBmp(string strText, int fontSize, Color colorFont, int left, int top, int right, int bottom)
        {
            Point PT = new Point(-1, -2);
            int w = 0;
            int h = 0;
            if (GetDcData(pData))
            {
                if (GetTextData(strText, fontSize, colorFont,ref w,ref h))
                {
                    return fPicBtoB(tData,w, h, left, top, right, bottom, 24);
                }
            }
            return PT;
        }

        public static Point fPicN(string Pic, RECT rect)
        {
            Point PT = new Point(-1, -2);
            int w = 0;
            int h = 0;
            if (GetPicData(Pic, ref w, ref h))
            {
                return fPicBtoB(tData,w, h, rect.left, rect.top, rect.right, rect.bottom, 24);
            }
            
            return PT;
        }

        public static Point FTextBmpN(string strText, int fontSize, int colorFont, RECT rect)
        {
            Point PT = new Point(-1, -2);
            int w = 0;
            int h = 0;
            if (GetTextData(strText, fontSize, colorFont, ref w, ref h))
            {
                return fPicBtoB(tData,w, h, rect.left, rect.top, rect.right, rect.bottom, 24);
            }
            return PT;
        }

        public static Point fPic(string Pic, RECT rect)
        {
            return fPic(Pic, rect.left, rect.top, rect.right, rect.bottom, 24);
        }
        public static Point fPic(string Pic, int left, int top, int right, int bottom)
        {
            return fPic(Pic,left,top,right,bottom,24);
        }
        public static Point fPic(string Pic, int left, int top, int right, int bottom, byte xiangshi)
        {
            Point PT = new Point(-1, -2);
            int w = 0;
            int h = 0;
            if (GetDcData(pData))
            {
                if (GetPicData(Pic, ref w, ref h))
                {
                    return fPicBtoB(tData, w, h, left, top, right, bottom,xiangshi);
                }
            }
            return PT;
        }
        //双正
        public static Point fPicBtoB1(byte[] ttData, int w, int h, int wx, int wy, int x1, int y1, int x2, int y2, byte xiangshi)
        {
            Point PT = new Point(-1, -2);
            bool fFalse = false;
            byte r, g, b, now_r, now_g, now_b;

            //wx = (x2 - x1) * 4;//(x2-x1)*4; //rt.Width * 3;
            //wy = y2-y1;//rt.Height;

            if (x1 < 0)
                x1 = 0;
            if (y1 < 0)
                y1 = 0;
            //循环比较
            if (w > wx || h > wy)
                return PT;
            // if ((x2 == 0) || (x2 > rt.Width))
            // x2 = rt.Width;
            // if ((y2 == 0) || (y2 > rt.Height))
            //  y2 = rt.Height;

            int m = 1;
            int n = 1;

            //去除无效点
            bool bfalse = false;
            for (m = 1; m < h - 1; m++)
            {
                if (bfalse)
                {
                    m--;
                    break;
                }
                for (n = 1; n < w / 3 - 1; n++)
                {
                    if (ttData[m * w + n * 3] != ttData[0] ||
                        ttData[m * w + n * 3 + 1] != ttData[1] ||
                        ttData[m * w + n * 3 + 2] != ttData[2])
                    {
                        bfalse = true;
                        break;
                    }
                }
            }
            if (!bfalse)
                return PT;
            //循环比较
            for (int y = y1; y < y2 - h - 1; y++)//y1
            {
                for (int x = x1; x < x2 - w / 3 - 1; x++)//x1
                {
                    try
                    {
                        if (Math.Abs(pData[wx * (y + m) + (x + n) * 4 + 2] - ttData[m * w + n * 3 + 2]) > xiangshi ||
                            Math.Abs(pData[wx * (y + m) + (x + n) * 4 + 1] - ttData[m * w + n * 3 + 1]) > xiangshi ||
                            Math.Abs(pData[wx * (y + m) + (x + n) * 4] - ttData[m * w + n * 3]) > xiangshi)
                            fFalse = false;
                        else
                            fFalse = true;
                    }
                    catch (Exception e)
                    {
                        AutoToolHelper.FileRW.WriteError(e.ToString());
                    }
                    for (int i = 1; i < h - 1; i++)
                    {
                        if (!fFalse)
                            break;
                        for (int j = 1; j < w / 3 - 1; j++)
                        {
                            if (!fFalse)
                                break;

                            b = ttData[i * w + j * 3];
                            g = ttData[i * w + j * 3 + 1];
                            r = ttData[i * w + j * 3 + 2];
                            now_r = pData[wx * (y + i) + (x + j) * 4 + 2];
                            now_g = pData[wx * (y + i) + (x + j) * 4 + 1];
                            now_b = pData[wx * (y + i) + (x + j) * 4];
                            if (((Math.Abs(now_r - r) < xiangshi) && (Math.Abs(now_g - g) < xiangshi) && (Math.Abs(now_b - b) < xiangshi)) || ((b == tData[0]) && (g == tData[1]) && (r == tData[2])))
                                fFalse = true;
                            else
                                fFalse = false;
                        }

                    }
                    if (fFalse == true)
                    {
                        PT.X = x;
                        PT.Y = y;
                        return PT;
                    }
                }
            }
            return PT;
        }

        public static Point fPicBtoB(byte[] ttData, int w, int h, int x1, int y1, int x2, int y2, byte xiangshi)
        {
            Point PT = new Point(-1, -2);
            bool fFalse;
            byte r, g, b, now_r, now_g, now_b;

            int wx = rt.Width * 3;
            int wy = rt.Height;

            if (x1 < 0)
                x1 = 0;
            if (y1 < 0)
                y1 = 0;
            //循环比较
            if (w > wx || h > wy)
                return PT;
            if ((x2 == 0) || (x2 > rt.Width))
                x2 = rt.Width;
            if ((y2 == 0) || (y2 > rt.Height))
                y2 = rt.Height;

            int m = 1;
            int n = 1;

            //去除无效点
            bool bfalse = false;
            for (m = 1; m < h - 1; m++)
            {
                if (bfalse)
                {
                    m--;
                    break;
                }
                for (n = 1; n < w / 3 - 1; n++)
                {
                    if (ttData[m * w + n * 3] != ttData[0] ||
                        ttData[m * w + n * 3 + 1] != ttData[1] ||
                        ttData[m * w + n * 3 + 2] != ttData[2])
                    {
                        bfalse = true;
                        break;
                    }
                }
            }
            if (!bfalse)
                return PT;
            //循环比较
            for (int y = y1; y < y2 - h - 1; y++)
            {
                for (int x = x1; x < x2 - w / 3 - 1; x++)
                {

                    if (Math.Abs(pData[wx * (y + m) + (x + n) * 3 + 2] - ttData[m * w + n * 3 + 2]) > xiangshi ||
                        Math.Abs(pData[wx * (y + m) + (x + n) * 3 + 1] - ttData[m * w + n * 3 + 1]) > xiangshi ||
                        Math.Abs(pData[wx * (y + m) + (x + n) * 3] - ttData[m * w + n * 3]) > xiangshi)
                        fFalse = false;
                    else
                        fFalse = true;

                    for (int i = 1; i < h - 1; i++)
                    {
                        if (!fFalse)
                            break;
                        for (int j = 1; j < w / 3 - 1; j++)
                        {
                            if (!fFalse)
                                break;

                            b = ttData[i * w + j * 3];
                            g = ttData[i * w + j * 3 + 1];
                            r = ttData[i * w + j * 3 + 2];
                            now_r = pData[wx * (y + i) + (x + j) * 3 + 2];
                            now_g = pData[wx * (y + i) + (x + j) * 3 + 1];
                            now_b = pData[wx * (y + i) + (x + j) * 3];
                            if (((Math.Abs(now_r - r) < xiangshi) && (Math.Abs(now_g - g) < xiangshi) && (Math.Abs(now_b - b) < xiangshi)) || ((b == tData[0]) && (g == tData[1]) && (r == tData[2])))
                                fFalse = true;
                            else
                                fFalse = false;
                        }

                    }
                    if (fFalse == true)
                    {
                        PT.X = x;
                        PT.Y = y;
                        return PT;
                    }
                }
            }
            return PT;
        }


        /// <summary>
        /// 截图后查找窗口界面文字坐标
        /// </summary>
        /// <param name="hWnd">窗口句柄</param>
        /// <param name="strText">文字</param>
        /// <param name="colorFont">文字颜色</param>
        /// <param name="fontName">字体名称</param>
        /// <param name="fontSize">字体大小（单位：像素）</param>
        /// <param name="fontStyle">字体样式：粗体，斜体等</param>
        /// <param name="distanceU">中文字间距</param>
        /// <param name="distanceA">非中文字间距</param>
        /// <param name="dstRect">窗口查找区域</param>
        /// <param name="errorValue">颜色误差值(0~128)</param>
        /// <returns>屏幕坐标</returns>
        public static Point FindText(IntPtr hWnd, string strText, Color colorFont, string fontName, int fontSize, FontStyle fontStyle, int distanceU, int distanceA, RECT dstRect, int errorValue)
        {
            Point pt = Point.Empty;
            if (strText == string.Empty) { return pt; }
            #region set rect
            RECT rect = new RECT();
            User32API.GetWindowRect(hWnd, out rect);
            dstRect.left += rect.left;
            if (dstRect.right <= 0)
                dstRect.right = rect.right;
            else
                dstRect.right += rect.left;

            dstRect.top += rect.top;
            if (dstRect.bottom <= 0)
                dstRect.bottom = rect.bottom;
            else
                dstRect.bottom += rect.top;

            if (dstRect.left < 0) { dstRect.left = 0; }
            if (dstRect.top < 0) { dstRect.top = 0; }
            if ((dstRect.right > rect.right) || dstRect.right <= 0) { dstRect.right = rect.right; }
            if ((dstRect.bottom > rect.bottom) || dstRect.bottom <= 0) { dstRect.bottom = rect.bottom; }
            #endregion

            #region set bitmapData
            //生成文字图片
            Bitmap srcBit = CreateTextBmp(strText, colorFont, fontName, fontSize, fontStyle, distanceU, distanceA);
            //srcBit.Save(@"E:\text.bmp");
            Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
            BitmapData srcBData = srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            //目标句柄图片
            Bitmap dstBit = GetScreenCapture(hDesk, dstRect);
            //dstBit.Save(@"E:\me.bmp");
            Rectangle dstRectN = new Rectangle(0, 0, dstBit.Width, dstBit.Height);
            BitmapData dstBData = dstBit.LockBits(dstRectN, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            IntPtr srcScan = srcBData.Scan0;
            IntPtr dstScan = dstBData.Scan0;
            #endregion

            #region unsafe operation
            unsafe
            {
                byte* srcP = (byte*)srcScan;
                byte* dstP = (byte*)dstScan;
                byte* dstPNew = dstP;
                byte bBG = srcP[0];
                byte gBG = srcP[1];
                byte rBG = srcP[2];

                bool flagComp;
                int dstOffset = dstBData.Stride - dstBData.Width * 3;
                int srcOffset = srcBData.Stride - srcBData.Width * 3;
                int dstNewOffset = dstBData.Stride - srcBData.Width * 3;

                for (int y = dstRect.top; y < dstRect.bottom - srcRect.Height; y++)
                {
                    for (int x = dstRect.left; x < dstRect.right - srcRect.Width; x++, dstP += 3)
                    {
                        flagComp = true;
                        srcP = (byte*)srcScan;
                        dstPNew = dstP;
                        for (int j = 0; j < srcRect.Height; j++)
                        {
                            if (!flagComp) { break; }

                            for (int i = 0; i < srcRect.Width; i++)
                            {
                                if (!flagComp) { break; }

                                if (((Math.Abs(dstPNew[0] - srcP[0]) <= errorValue) &&
                                    (Math.Abs(dstPNew[1] - srcP[1]) <= errorValue) &&
                                    (Math.Abs(dstPNew[2] - srcP[2]) <= errorValue))
                                    || ((srcP[0] == bBG) && (srcP[1] == gBG) && (srcP[2] == rBG)))
                                    flagComp = true;
                                else
                                    flagComp = false;

                                dstPNew += 3;
                                srcP += 3;
                            }
                            srcP += srcOffset;
                            dstPNew += dstNewOffset;
                        }

                        if (flagComp)
                        {
                            dstBit.UnlockBits(dstBData);
                            pt.X = x;
                            pt.Y = y;
                            srcBit.UnlockBits(srcBData);
                            dstBit.Dispose();
                            srcBit.Dispose();
                            return pt;
                        }

                    }
                    dstP += dstOffset + srcRect.Width * 3;
                }

            }
            #endregion

            dstBit.UnlockBits(dstBData);
            srcBit.UnlockBits(srcBData);
            dstBit.Dispose();
            srcBit.Dispose();
            return pt;
        }
        public static Point FindTextSave(string pic,string strText, Color colorFont, string fontName, int fontSize, FontStyle fontStyle, int distanceU, int distanceA)
        {
            Point pt =new Point( -1,-2);
            //生成文字图片
            Bitmap srcBit = CreateTextBmp(strText, colorFont, fontName, fontSize, fontStyle, distanceU, distanceA);
            srcBit.Save(pic);
            Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
            BitmapData srcBData = srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            srcBit.UnlockBits(srcBData);
            srcBit.Dispose();
            return pt;
        }
        /// <summary> 可能会错 汉字和字母混合时可能会错。
        /// 生成一个文字图片
        /// </summary>
        /// <param name="strText">前景文字</param>
        /// <param name="colorFont">文字颜色</param>
        /// <param name="fontName">字体名字</param>
        /// <param name="fontSize">字体大小单位（像素）</param>
        /// <param name="fontStyle">字体样式</param>
        /// <param name="distanceU">中文字体间距</param>
        /// <param name="distanceA">非中文字体间距</param>
        /// <returns>图片</returns>
        private static Bitmap CreateTextBmp(string strText, Color colorFont, string fontName, int fontSize, FontStyle fontStyle, int distanceU, int distanceA)
        {
            //图片中每个字x坐标
            int widthFont = -1;
            //图片中每个字所占宽度
            int widthTemp = 0;

            //134代表GB2312字体集合
            Font font = new Font(fontName, fontSize, fontStyle, GraphicsUnit.Pixel, 134, true);

            //bmp图片宽度
            int widthBmp = 0;
            int[] wfArr = new int[strText.Length];
            for (int i = 0; i < strText.Length; ++i)
            {
                //判断是否中文
                if (Convert.ToInt32(strText[i]) > Convert.ToInt32(Convert.ToChar(128)))
                    widthTemp = fontSize + distanceU;
                else
                    widthTemp = fontSize / 2 + distanceA;

                wfArr[i] = widthTemp;
                widthBmp += widthTemp;
            }
            widthBmp += 1;
            int HeightBmp = font.Height;
            Bitmap bm = new Bitmap(widthBmp, HeightBmp, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(bm);
            //g.TextRenderingHint = TextRenderingHint.SystemDefault;
            g.TextContrast = 0;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

            //这里的三个属性可以根据情况开放.
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //赋予图像一个背景色
            if (colorFont.R < 60 & colorFont.G < 60 & colorFont.B < 60)
                g.Clear(Color.White);
            else
                g.Clear(Color.Black);
            for (int i = 0; i < strText.Length; ++i)
            {
                g.DrawString(
                    strText[i].ToString(),
                    font,
                    new SolidBrush(colorFont),
                    widthFont,
                    1,
                    StringFormat.GenericDefault
                    );
                widthFont += wfArr[i];
            }
            g.Dispose();
            ImageTool.SetFont("宋体", FontStyle.Regular, 0, 0);
            return bm;
        }
        //一次性输出。
        public static Bitmap CreateTextBmp(string strText, Color colorFont, string fontName,int fontSize)
        {
            //134代表GB2312字体集合
            Font font = new Font(fontName, fontSize, UFontStyle, GraphicsUnit.Pixel, 134, true);
            //bmp图片宽度
            int len = System.Text.Encoding.GetEncoding("GB2312").GetByteCount(strText);

            //12号汉字后面跟字母间距+1   14号相反
            int widthBmp = (fontSize + 1) / 2 *len  + 1;
            //bool samestr=false;
            for (int i = 0; i < strText.Length-1; ++i)
            {
                if (Convert.ToInt32(strText[i]) > Convert.ToInt32(Convert.ToChar(128)))
                {
                    //samestr = !samestr;
                    if (Convert.ToInt32(strText[i + 1]) <= Convert.ToInt32(Convert.ToChar(128)))
                        widthBmp++;// i++;
                }
                else
                {
                    if (Convert.ToInt32(strText[i + 1]) > Convert.ToInt32(Convert.ToChar(128)))
                        widthBmp++;
                }
            }
            int HeightBmp = font.Height;

            Bitmap bm = new Bitmap(widthBmp, HeightBmp, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(bm);
            //g.TextRenderingHint = TextRenderingHint.SystemDefault;
            g.TextContrast = 0;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            //这里的三个属性可以根据情况开放.
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            //赋予图像一个背景色
            if (colorFont.R < 60 & colorFont.G < 60 & colorFont.B < 60)
                g.Clear(Color.White);
            else
                g.Clear(Color.Black);
            g.DrawString(strText,font,new SolidBrush(colorFont),-1, 1,StringFormat.GenericDefault );
            g.Dispose();
            //ImageTool.SetFont("宋体", FontStyle.Regular, 0, 0);
            return bm;
        }

        //地下城字体只管字与字间隔1像素
        public static Bitmap CreateTextBmpDNF(string strText, Color colorFont, int fontSize)
        {
            return CreateTextBmpDNF(strText, colorFont, fontSize,0);
        }
        public static Bitmap CreateTextBmpDNF(string strText, Color colorFont,int fontSize,int H_space)
        {
            //134代表GB2312字体集合
            Font font = new Font("宋体", fontSize, UFontStyle, GraphicsUnit.Pixel, 134, true);
            //bmp图片宽度
            int len = System.Text.Encoding.GetEncoding("GB2312").GetByteCount(strText);
            //12号汉字后面跟字母间距+1   14号相反
            int widthBmp = (fontSize + 1) / 2 * len + 1 + H_space;
            int HeightBmp = font.Height;
            Bitmap bm = new Bitmap(widthBmp, HeightBmp, PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(bm);
            g.TextContrast = 0;
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            //这里的三个属性可以根据情况开放.
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            //算间距
            Bitmap bmS = new Bitmap(fontSize + 1, HeightBmp, PixelFormat.Format24bppRgb);
            Graphics gS = Graphics.FromImage(bmS);
            gS.TextContrast = 0;
            gS.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            gS.CompositingQuality = CompositingQuality.HighQuality;
            gS.SmoothingMode = SmoothingMode.HighQuality;
            gS.InterpolationMode = InterpolationMode.HighQualityBicubic;

            //选择判断点
            int cC = 0;
            //赋予图像一个背景色
            if (colorFont.R < 60 & colorFont.G < 60 & colorFont.B < 60)
                g.Clear(Color.White);
            else
            {
                g.Clear(Color.Black);
                if (colorFont.B == 0)
                {
                    cC = 1;
                    if (colorFont.G == 0)
                        cC = 2;
                }
            }
            int wF = -1 + H_space;
            for (int i = 0; i < strText.Length; ++i)
            {
                if (colorFont.R < 60 & colorFont.G < 60 & colorFont.B < 60)
                    gS.Clear(Color.White);
                else
                    gS.Clear(Color.Black);
                gS.DrawString(strText[i].ToString(), font, new SolidBrush(colorFont), -1, 1, StringFormat.GenericDefault);
                Rectangle bmSRectN = new Rectangle(0, 0, bmS.Width, bmS.Height);
                BitmapData bmSBData = bmS.LockBits(bmSRectN, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                //计算前空
                int wS = 0;
                int wE = fontSize;
                //bmS.Save("t.bmp");
                unsafe
                {
                    byte* gSP = (byte*) bmSBData.Scan0;
                    bool fk = false;
                    for (int j = 0; j < bmSBData.Width; j++)
                    {
                        if (fk)
                            break;
                        for (int k = 1; k < bmSBData.Height; k++)
                        {
                            if (gSP[k * bmSBData.Stride + j * 3 + cC] != gSP[(k-1) * bmSBData.Stride + j * 3 + cC])
                            {
                                fk = true;
                                wS = 1 -j;
                                break;
                            }
                        }
                    }
                    fk = false;
                    //计算后空
                    for (int j = bmSBData.Width -1; j >= 0; j--)
                    {
                        if (fk)
                            break;
                        for (int k = 1; k < bmSBData.Height; k++)
                        {
                            if (gSP[k * bmSBData.Stride + j * 3 + cC] != gSP[(k-1) * bmSBData.Stride + j * 3 + cC])
                            {
                                fk = true;
                                wE = j + wS + 1;
                                break;
                            }
                        }
                    }
                    if (!fk)
                        wE = fontSize / 6;

                }
                wF += wS; 
                bmS.UnlockBits(bmSBData);
                g.DrawString( strText[i].ToString(), font, new SolidBrush(colorFont),wF, 1, StringFormat.GenericDefault );
                wF =wF+ - wS+ wE;
            }
            ImageTool.SetFont("宋体", FontStyle.Regular, 0, 0);
            gS.Dispose();
            g.Dispose();


            return bm;
        }
        //全匹配找图
        public static Point FindTextAll(string strText, int fontSize, int colorFont,RECT rect)
        {
            Point PT = new Point(-1, -2);
            int w = 0;
            int h = 0;
            if (GetDcData(pData))
            {
                if (GetTextData(strText, fontSize, colorFont, ref w, ref h))
                {
                    return FindRoleBtoB(tData, w, h, rect.left, rect.top, rect.right, rect.bottom);
                }
            }
            return PT;
        }

        //全匹配，防止找错角色
        public static Point FindRoleBtoB(byte[] roleData, int w, int h, RECT rect)
        {
            return FindRoleBtoB(roleData, w, h, rect.left, rect.top, rect.right, rect.bottom,8);
        }
        public static Point FindRoleBtoB(byte[] roleData, int w, int h, int x1, int y1, int x2, int y2)
        {
            return FindRoleBtoB(roleData, w, h, x1, y1, x2, y2, 8);
        }
        //全匹配，防止找错角色
        public static Point FindRoleBtoB(byte[] roleData, int w, int h, int x1, int y1, int x2, int y2,int xiangshi)
        {
            Point PT = new Point(-1, -2);
            //int xiangshi = 1;
            bool fFalse;
            byte r, g, b, now_r, now_g, now_b;
            int wx = rt.Width * 3;
            int wy = rt.Height;
            //循环比较
            if (w > wx || h > wy || x1 < 0 || y1 < 0)
                return PT;
            if ((x2 == 0) || (x2 > wx))
                x2 = wx;
            if ((y2 == 0) || (y2 > wy))
                y2 = wy;


            byte col_r = 0;
            byte col_g = 0;
            byte col_b = 0;

            int m = 1;
            int n = 1;
            //去除无效点
            bool bfalse = false;
            for (m = 1; m < h - 1; m++)
            {
                if (bfalse)
                {
                    m--;
                    break;
                }
                for (n = 1; n < w / 3 - 1; n++)
                {
                    if (roleData[m * w + n * 3] != roleData[0] ||
                        roleData[m * w + n * 3 + 1] != roleData[1] ||
                        roleData[m * w + n * 3 + 2] != roleData[2])
                    {
                        col_b = roleData[m * w + n * 3];
                        col_g = roleData[m * w + n * 3 + 1];
                        col_r = roleData[m * w + n * 3 + 2];
                        bfalse = true;
                        break;
                    }
                }
            }
            if (!bfalse)
                return PT;
            //循环比较
            for (int y = y1; y < y2 - h - 1; y++)
            {
                for (int x = x1; x < x2 - w / 3 - 1; x++)
                {

                    if (Math.Abs(pData[wx * (y + m) + (x + n) * 3 + 2] - roleData[m * w + n * 3 + 2]) > xiangshi ||
                        Math.Abs(pData[wx * (y + m) + (x + n) * 3 + 1] - roleData[m * w + n * 3 + 1]) > xiangshi ||
                        Math.Abs(pData[wx * (y + m) + (x + n) * 3] - roleData[m * w + n * 3]) > xiangshi)
                        fFalse = false;
                    else
                        fFalse = true;

                    for (int i = 1; i < h - 1; i++)
                    {
                        if (!fFalse)
                            break;
                        for (int j = 1; j < w / 3 - 2; j++)
                        {
                            if (!fFalse)
                                break;

                            b = roleData[i * w + j * 3];
                            g = roleData[i * w + j * 3 + 1];
                            r = roleData[i * w + j * 3 + 2];
                            now_r = pData[wx * (y + i) + (x + j) * 3 + 2];
                            now_g = pData[wx * (y + i) + (x + j) * 3 + 1];
                            now_b = pData[wx * (y + i) + (x + j) * 3];
                            if (((Math.Abs(now_r - r) < xiangshi) && (Math.Abs(now_g - g) < xiangshi) && (Math.Abs(now_b - b) < xiangshi)) ||
                                ((b == tData[0] && g == tData[1] && r == tData[2]) && (now_r != col_r || now_g != col_g || now_b != col_b)))
                                //((b == tData[0]) && (g == tData[1]) && (r == tData[2])))
                                fFalse = true;
                            else
                                fFalse = false;
                        }

                    }
                    if (fFalse == true)
                    {
                        PT.X = x;
                        PT.Y = y;
                        return PT;
                    }
                }
            }
            return PT;
        }
        
        /// <summary>
        /// 可以截图到非GDI界面
        /// </summary>
        /// <returns></returns>
        /// 
        public static Bitmap GetScreenCapture(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                hWnd = GetDesktopWindow();
            RECT rect;
            User32API.GetClientRect(hWnd, out rect);
            return GetScreenCapture(hWnd, rect);
        }
        //截图
        public static Bitmap GetScreenCapture(RECT dstRect)
        {
            if (dstRect.left < 0)
                dstRect.left = 0;
            if (dstRect.top < 0)
                dstRect.top = 0;
            if (dstRect.right >= rt.Width)
                dstRect.right = rt.Width - 1;
            if (dstRect.bottom >= rt.Height)
                dstRect.bottom = rt.Height - 1;
            const int SRCCOPY = 0x00CC0020;
            const int CAPTUREBLT = 0x40000000;
            RECT rect;
            IntPtr hWnd = User32API.GetDesktopWindow();
            Graphics grpScreen = Graphics.FromHwnd(hWnd);
            User32API.GetClientRect(hWnd, out rect);
            //创建一个以当前屏幕为模板的图象
            Image MyImage = new Bitmap(dstRect.Width, dstRect.Height, grpScreen);
            //创建以屏幕大小为标准的位图 
            Graphics g2 = Graphics.FromImage(MyImage);
            //得到屏幕的DC
            IntPtr dc1 = grpScreen.GetHdc();
            //得到Bitmap的DC                      
            IntPtr dc2 = g2.GetHdc();
            Gdi32API.BitBlt(dc2, 0, 0, dstRect.Width, dstRect.Height, dc1, dstRect.left, dstRect.top, SRCCOPY | CAPTUREBLT);
            grpScreen.ReleaseHdc(dc1);
            g2.ReleaseHdc(dc2);
            grpScreen.Dispose();
            g2.Dispose();
            return (Bitmap)MyImage;
        }
        public static Bitmap GetScreenCapture(IntPtr hWnd, RECT dstRect)
        {
            //获得当前屏幕的大小
            //int width = Screen.PrimaryScreen.Bounds.Width;
            //int height = Screen.PrimaryScreen.Bounds.Height;
            const int SRCCOPY = 0x00CC0020;
            const int CAPTUREBLT = 0x40000000;
            RECT rect;
            if ((hWnd == IntPtr.Zero)&&(!User32API.IsWindow(hWnd)))
                hWnd = User32API.GetDesktopWindow();
            User32API.GetClientRect(hWnd, out rect);
            //if (rect.Width < dstRect.Width || rect.Height < dstRect.Height)
            //{
            //    AppLog.WriteError(string.Format("截图目标窗口句柄大小【{0},{1}】",rect.Width,rect.Height));
            //    AppLog.WriteError(string.Format("截图矩形设置大小【{0},{1}】", dstRect.Width, dstRect.Height));
            //    AppLog.WriteError("截图矩形设置错误");
            //    return null;
            //}
            //int width = dstRect.right - dstRect.left;
            //int height = dstRect.bottom - dstRect.top;
            try
            {
                Graphics grpScreen = Graphics.FromHwnd(hWnd);
                //创建一个以当前屏幕为模板的图象
                Image MyImage = new Bitmap(dstRect.Width, dstRect.Height, grpScreen);
                //创建以屏幕大小为标准的位图 
                Graphics g2 = Graphics.FromImage(MyImage);
                //得到屏幕的DC
                IntPtr dc1 = grpScreen.GetHdc();
                //得到Bitmap的DC                      
                IntPtr dc2 = g2.GetHdc();
                Gdi32API.BitBlt(dc2, 0, 0, dstRect.Width, dstRect.Height, dc1, dstRect.left, dstRect.top, SRCCOPY | CAPTUREBLT);
                grpScreen.ReleaseHdc(dc1);
                g2.ReleaseHdc(dc2);
                grpScreen.Dispose();
                g2.Dispose();
                return (Bitmap)MyImage;
            }
            catch
            { 
            }
            return null;
        }
        /// <summary>
        /// 图片二值化
        /// </summary>
        /// <param name="srcBitmap">源图片</param>
        /// <param name="nThreshold">0~255</param>
        /// <returns>二值化后的图片</returns>
        private static void RGB2Binarization(Bitmap srcBitmap, int nThreshold)
        {
            int width = srcBitmap.Width;
            int height = srcBitmap.Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData srcData = srcBitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            IntPtr srcScan = srcData.Scan0;
            unsafe
            {
                byte* srcP = (byte*)(void*)srcScan;
                int srcOffset = srcData.Stride - width * 3;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++, srcP += 3)
                    {
                        //*dstP = (byte)((b * 15 + g * 75 + r * 38) >> 7);
                        srcP[0] = srcP[1] = srcP[2] =
                            (byte)((srcP[0] * 15 + srcP[1] * 75 + srcP[2] * 38) >> 7) > nThreshold ? (byte)255 : (byte)0;
                    }
                    srcP += srcOffset;
                }
            }
            srcBitmap.UnlockBits(srcData);
        }

        //
        public static Bitmap CreatBmpFromByte1(Byte[] tmpData, int weight, int height)
        {
            //Stream stream = new
            Bitmap bmp = new Bitmap(weight,height, PixelFormat.Format32bppRgb);
            Rectangle srcRect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData srcBData = bmp.LockBits(srcRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            //unsafe
            //{
            //    srcBData.Scan0 = tmpData.;
            //}

            //修改模板颜色
            for (int i = 1; i < height-1; i++)
            {
                for (int j = 1; j < weight -1; j++)
                {
                    tmpData[i * weight * 4 + j * 4] = 0;
                    tmpData[i * weight * 4 + j * 4 + 1] = 0;
                    tmpData[i * weight * 4 + j * 4 + 2] = 0;
                }
            }
            //////////
            System.Runtime.InteropServices.Marshal.Copy(tmpData,0, srcBData.Scan0, srcBData.Stride * srcBData.Height);
            bmp.UnlockBits(srcBData);
            return bmp;
            //bmp.Save("t.bmp");
        }
        public static Bitmap CreatBmpFromByte(Byte[] tmpData, int weight, int height)
        {
            //Stream stream = new
            Bitmap bmp = new Bitmap(weight, height, PixelFormat.Format32bppRgb);
            Rectangle srcRect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData srcBData = bmp.LockBits(srcRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            //unsafe
            //{
            //    srcBData.Scan0 = tmpData.;
            //}

           
            //////////
            System.Runtime.InteropServices.Marshal.Copy(tmpData, 0, srcBData.Scan0, srcBData.Stride * srcBData.Height);
            bmp.UnlockBits(srcBData);
            return bmp;
            //bmp.Save("t.bmp");
        }
        //Bitmap sbmp = new Bitmap("E:\\text.bmp",true);
        //Bitmap bbmp = new Bitmap("E:\\me.bmp",true);
        //ImageTool.BmpInsert(bbmp, sbmp, 100, 100).Save("t.bmp");
        public static Bitmap BmpInsert(Bitmap BigBmp, Bitmap smallBmp, int x, int y)
        {
            if (x + smallBmp.Width > BigBmp.Width || y + smallBmp.Height > BigBmp.Height)
                return BigBmp;
            Rectangle srcRect = new Rectangle(x, y, smallBmp.Width, smallBmp.Height);
            Rectangle smaRect = new Rectangle(0, 0, smallBmp.Width, smallBmp.Height);
            BitmapData bigBData = BigBmp.LockBits(srcRect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData smaBData = smallBmp.LockBits(smaRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            bigBData.Scan0 = smaBData.Scan0;
            BigBmp.UnlockBits(bigBData);
            smallBmp.UnlockBits(smaBData);
            return BigBmp;
        }
        public static Bitmap BmpInsert(Bitmap BigBmp, Bitmap sBmp, Point pt)
        {
            return BmpInsert(BigBmp, sBmp, pt.X, pt.Y);
        }
        public static Bitmap BmpInsert(Bitmap BigBmp, Bitmap sBmp, ref int x, ref int y, ref int bottom, string str)
        {
            if (str == "换行")
            {
                x = 0;
                y = bottom;
                return BigBmp;
            }
            if (str == "水印")
            {
                x = BigBmp.Width - sBmp.Width;
                y = BigBmp.Height - sBmp.Height;
            }
            bottom = Math.Max(bottom, y + sBmp.Height);
            if (BigBmp == sBmp)
                return BigBmp;
            if (x + sBmp.Width > BigBmp.Width || y + sBmp.Height > BigBmp.Height)
            {

                return BigBmp;
            }
            Rectangle srcRect = new Rectangle(x, y, sBmp.Width, sBmp.Height);
            Rectangle sRect = new Rectangle(0, 0, sBmp.Width, sBmp.Height);
            BitmapData bigBData = BigBmp.LockBits(srcRect, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);//Format24bppRgb
            BitmapData sBData = sBmp.LockBits(sRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            bigBData.Scan0 = sBData.Scan0;
            BigBmp.UnlockBits(bigBData);
            sBmp.UnlockBits(sBData);
            x += sBmp.Width;
            return BigBmp;
        }
        //用之前取到的屏幕数据查找。前面没取过要先 ImageTool.GetDCData();


        public static string ReadCoin(int left, int top, int right, int bottom, bool BWkey)
        {

            int tempX = left;
            int[,] a = new int[10,91] {
	            {0,0,1,1,0,0,0,
		         0,1,0,0,1,1,0,
		         1,0,0,0,0,1,0,
		         1,0,0,0,0,0,1,
		         1,0,0,0,0,0,1,
		         1,0,0,0,0,0,1,
		         1,0,0,0,0,0,1,
		         1,0,0,0,0,0,1,
		         1,0,0,0,0,0,1,
		         1,0,0,0,0,0,0,
		         0,1,0,0,0,1,0,
		         0,1,1,0,1,0,0,
		         0,0,0,0,0,0,0},//0
		        {0,0,0,0,1,0,0,
                 0,0,1,1,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,1,0,0,
                 0,0,0,0,0,0,0},//1
		        {0,0,1,1,0,0,0,0,1,0,0,1,1,0,1,0,0,0,0,1,0,1,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,1,1,0,0,0,0,0,0,0,0,0,0,0,0},//2++
		        {0,0,1,1,0,0,0,1,1,0,0,1,0,0,1,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,1,1,0,0,0,0,1,1,1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,1,0,0,0,0,1,0,0,1,0,0,1,1,0,0,0,0,0,0,0,0},//3
		        {0,0,0,0,1,0,0,0,0,0,1,1,0,0,0,0,0,1,0,1,0,0,0,1,0,0,1,0,0,0,1,0,0,1,0,0,1,0,0,0,1,0,0,1,0,0,0,1,0,1,0,0,0,0,1,0,1,1,1,1,1,1,1,0,0,0,0,1,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0},//4
		        {0,1,1,1,1,1,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,1,1,1,1,0,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,1,0,0,0,0,1,0,1,1,0,0,1,0,0,0,0,0,0,0,0,0},//5
		        {0,0,0,1,1,0,0,0,1,0,0,0,1,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,1,1,1,1,0,0,1,1,0,0,0,1,0,1,0,0,0,0,1,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,1,0,0,1,1,0,1,1,0,0,0,0,0,0,0,0},//6
		        {0,1,1,1,1,1,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0},//7
		        {0,0,1,1,0,0,0,0,1,0,0,1,1,0,1,0,0,0,0,1,0,1,0,0,0,0,1,0,0,1,0,0,0,1,0,0,1,1,0,1,0,0,0,1,1,1,1,0,0,1,1,0,0,0,1,0,1,0,0,0,0,0,0,1,0,0,0,0,0,0,1,0,0,0,0,1,0,0,1,0,0,1,1,0,0,0,0,0,0,0,0},//8
		        {0,0,1,1,0,0,0,0,1,0,0,1,1,0,1,0,0,0,0,1,0,1,0,0,0,0,0,0,1,0,0,0,0,0,1,1,0,0,0,0,1,0,0,1,0,0,0,1,0,0,0,1,1,1,1,0,0,0,0,0,0,0,0,1,0,0,0,0,1,0,1,0,0,0,0,1,0,0,1,0,0,1,0,0,0,0,0,0,0,0,0} //9
	        };
            //读屏幕
            byte now_r, now_g, now_b;
            int Height = rt.Height;
            int Width = rt.Width * 3;
            if (left < 0)
                left = 0;
            if (top < 0)
                top = 0;
            //循环比较
            if ((right == 0) || (right > rt.Width))
                right = rt.Width;
            if ((bottom == 0) || (bottom > rt.Height))
                bottom = rt.Height;


            //int WidthQ = 7;
            //int HeightQ = 13;
            if (!GetDcData(pData))
            {
                return "检测异常";
            }
            //屏幕装入数组

            bool flag = true;

            int key;
            if (BWkey)
                key = 120;
            else
                key = 60;
            int[] b = new int[91];//[91];
            string retunNo = "";
            //string AgCoin = "";
            //string AuCoin = "";

            //string sss = "";
            int num = 0;
            for (int y = top; y < bottom - 13; y++)
            {
                bool stop = true;
                for (int x = left; x < right - 7; x++)
                {
                    num = 0;
                   // sss="";
                    for (int i = 0; i < 13; i++)
                    {
                        for (int j = 0; j < 7; j++)
                        {
                            now_r = pData[Width * 3 * (Height - y - 1 - i) + (x + j) * 3 + 2];
                            now_g = pData[Width * 3 * (Height - y - 1 - i) + (x + j) * 3 + 1];
                            now_b = pData[Width * 3 * (Height - y - 1 - i) + (x + j) * 3 + 0];
                            if (BWkey)
                            {
                                if ((now_r > key) && (now_g > key) && (now_b > key))
                                {
                                    //sss+="1";
                                    b[num] = 1;
                                }
                                else
                                {
                                   // sss+="0";
                                    b[num] = 0;
                                }
                            }
                            else
                            {

                                if ((now_r < key) && (now_g < key) && (now_b < key))
                                {
                                   // sss+="1";
                                    b[num] = 1;
                                }
                                else
                                {
                                   // sss+="0";
                                    b[num] = 0;
                                }
                            }
                            num++;
                        }
                       // sss+="\r\n";

                    }
                   // sss+="\r\n";
                   // FileRW.WriteToFile(sss);
                    for (int m = 0; m < 10; m++)
                    {
                        flag = true;
                        int pointNum = 0;
                        for (int n = 0; n < 91; n++)
                        {
                            if ((b[n] == 0) && (a[m, n] == 1))
                            {
                                flag = false;
                            }
                            if (b[n] == 0)
                            {
                                pointNum++;
                            }
                            if (!flag && pointNum > 85)
                            {
                                n = 100;
                            }
                        }
                        if (flag && (pointNum <= 85))
                        {
                            string temp = string.Format("{0}", m);
                            if (x - tempX > 10)
                            {
                                temp = "X" + temp;
                            }
                            tempX = x;
                            retunNo = retunNo + temp;
                            stop = false;
                            m = 10;

                        }
                    }
                }
                if (!stop) break;
            }



            string strTemp = retunNo;
            //::MessageBox(hWnd,strTemp,"警告",MB_OK);
            int len = strTemp.Length;
            if (len <= 0)
            {
                return "0";
            }
            strTemp = strTemp.Substring(1, len - 1);//Right(len - 1);
            strTemp = strTemp + "X";
            int ipos = strTemp.IndexOf("X");
            len = strTemp.Length;
            string[] arr ={ "", "", "", "", "" };
           // int num = new int;
            num = 0;
            while (ipos >= 0)
            {
                num++;
                arr[num] = strTemp.Substring(0, ipos);
                strTemp = strTemp.Substring(ipos + 1, len - ipos - 1);
                ipos = strTemp.IndexOf("X");
                len = strTemp.Length;
            }
            if (arr[2].Length == 1)
            {
                arr[2] = "0" + arr[2];
            }
            if (arr[3].Length == 1)
            {
                arr[3] = "0" + arr[3];
            }
            if (num == 3)
            {
                retunNo = arr[1] + arr[2] + arr[3];
            }
            else
                if (num == 2)
                {
                    retunNo = arr[1] + arr[2];
                }
                else
                    if (num == 1)
                    {
                        retunNo = arr[1];
                    }
                    else
                    {
                        retunNo = "检测异常";
                    }

            return retunNo;

        }
        //用之前取到的屏幕数据查找。前面没取过要先 ImageTool.GetDCData();
        public static bool CheckRGB(int left, int top, int right, int bottom, int r,int g,int b,int key)
        {
           //读屏幕
            //int r = color % 256;
            //int g = (color / 256) % 256;
            //int b = color / 65536;


            RECT rect = new RECT(left, top, right, bottom);
            Bitmap srcBit = ImageTool.GetScreenCapture(rect);

#if DEBUG
            srcBit.Save(@"E:\SCR.bmp");
#endif
            int sHeight = srcBit.Height;
            Rectangle srcRect = new Rectangle(0, 0, srcBit.Width, srcBit.Height);
            BitmapData qBData = srcBit.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            byte[] sData = new byte[qBData.Stride * qBData.Height];
            System.Runtime.InteropServices.Marshal.Copy(qBData.Scan0, sData, 0, qBData.Stride * qBData.Height);
            srcBit.UnlockBits(qBData);
            srcBit.Dispose();
            //屏幕装入数组

            for (int i = 0; i < sHeight; i++)
            {
                for (int j = 0; j < qBData.Stride; j++)
                {
                    int rr = Math.Abs(sData[i * qBData.Stride + j + 2] - r);
                    int gg = Math.Abs(sData[i * qBData.Stride + j + 1] - g);
                    int bb = Math.Abs(sData[i * qBData.Stride + j] - b);
                    if ((rr <= key) && (gg <= key) && (bb <= key))
                        return true;
                }
            }
            return false;
        }
    }
}
