using Baidu.Aip.Ocr;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WeChat
{
    public class BaiduOCR
    {
        // 调用getAccessToken()获取的 access_token建议根据expires_in 时间 设置缓存
        // 返回token示例
        public static String TOKEN = "24.adda70c11b9786206253ddb70affdc46.2592000.1493524354.282335-1234567";

        // 百度云中开通对应服务应用的 API Key 建议开通应用的时候多选服务
        public String clientId = "IpxX29W1xPR1qV09Spke0ehP";
        // 百度云中开通对应服务应用的 Secret Key
        public String clientSecret = "Cdl8Wde4qaesEgmaxl9Veu1tLz6GfM1o";

        private Ocr client = null;

        public BaiduOCR(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            client = new Baidu.Aip.Ocr.Ocr(clientId, clientSecret);
        }
        public String GetAccessToken()
        {
            String authHost = "https://aip.baidubce.com/oauth/2.0/token";
            HttpClient client = new HttpClient();
            List<KeyValuePair<String, String>> paraList = new List<KeyValuePair<string, string>>();
            paraList.Add(new KeyValuePair<string, string>("grant_type", "client_credentials"));
            paraList.Add(new KeyValuePair<string, string>("client_id", clientId));
            paraList.Add(new KeyValuePair<string, string>("client_secret", clientSecret));

            HttpResponseMessage response = client.PostAsync(authHost, new FormUrlEncodedContent(paraList)).Result;
            String result = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine(result);
            return result;
        }

        public string GeneralBasicDemo(Bitmap bitmap)
        {
            client.Timeout = 60000;  // 修改超时时间          
            // var image = File.ReadAllBytes("图片文件路径");
            var image = Bitmap2Byte(bitmap);
            // 调用通用文字识别, 图片参数为本地图片，可能会抛出网络等异常，请使用try/catch捕获
            var result = client.GeneralBasic(image);
            //Console.WriteLine(result);
            // 如果有可选参数
            var options = new Dictionary<string, object>{
        {"language_type", "CHN_ENG"},
        {"detect_direction", "true"},
        {"detect_language", "true"},
        {"probability", "true"}
    };
            // 带参数调用通用文字识别, 图片参数为本地图片
            result = client.GeneralBasic(image, options);
            //Console.WriteLine(result);
            string word = string.Empty;
            if (result["words_result"].ToList().Count > 0)
            {
                word = result["words_result"][0]["words"].ToString();
                if (IsNumeric(word))
                {
                    if (word.Length < 11 && word.Substring(0, 1) != "1")
                    {
                        word = "1" + word;
                    }
                }
            }
            return word;
        }
        public void GeneralBasicUrlDemo()
        {
            var url = "https//www.x.com/sample.jpg";

            // 调用通用文字识别, 图片参数为远程url图片，可能会抛出网络等异常，请使用try/catch捕获
            var result = client.GeneralBasicUrl(url);
            Console.WriteLine(result);
            // 如果有可选参数
            var options = new Dictionary<string, object>{
        {"language_type", "CHN_ENG"},
        {"detect_direction", "true"},
        {"detect_language", "true"},
        {"probability", "true"}
    };
            // 带参数调用通用文字识别, 图片参数为远程url图片
            result = client.GeneralBasicUrl(url, options);
            Console.WriteLine(result);
        }

        public byte[] Bitmap2Byte(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Bmp);
                byte[] data = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(data, 0, Convert.ToInt32(stream.Length));
                return data;
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
    }
}
