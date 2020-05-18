using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Fasterflect;
using Kugar.Core.BaseStruct;
using Kugar.Core.ExtMethod;
using Kugar.Core.Network;
using Kugar.Tools.Express.DTO;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kugar.Tools.Express
{
    /// <summary>
    /// 快递100物流查询功能
    /// </summary>
    public static class Kuai100
    {
        private static Lazy<Dictionary<string, string>> _compareNameMappers = new Lazy<Dictionary<string, string>>(readFile);

        //public static string MapJsonFilePath { set; get; } = "kuai100.json";

        /// <summary>
        /// 返回所有支持的物理公司名称
        /// </summary>
        public static string[] ExpressNames
        {
            get
            {
                return _compareNameMappers.Value.Keys.ToArrayEx();
            }
        }

        /// <summary>
        /// 查询快递单记录
        /// </summary>
        /// <param name="customerID">公司编号</param>
        /// <param name="key">key</param>
        /// <param name="expressName">物流公司名称</param>
        /// <param name="expressCode">物流单号</param>
        /// <param name="mobile">快递单号绑定的手机号码,部分物流公司,如顺丰等,需要传手机号</param>
        /// <returns></returns>
        public static async Task<ResultReturn<Kuai100QueryResult>> GetExpressLogsAsync(string customerID, string key, string expressName, string expressCode, string mobile = "")
        {
            const string url = "https://poll.kuaidi100.com/poll/query.do";

            var comName = getExpressNameToCode(expressName);

            if (string.IsNullOrEmpty(comName))
            {
                return new FailResultReturn<Kuai100QueryResult>("查找不到物流公司对应的编号,请检查名称是否正确");
            }

            var args = new JObject()
            {
                ["com"] = comName,
                ["num"] = expressCode,
                ["phone"] = mobile,
                ["resultv2"] = 1
            };

            var sign = $"{args.ToStringEx(Formatting.None)}{key}{customerID}".MD5_32(true).ToUpper();

            try
            {
                var resultStr = await WebHelper.Create(url)
                    .SetParamter("customer", customerID)
                    .SetParamter("sign", sign)
                    .SetParamter("param", args.ToStringEx(Formatting.None))
                    .Post_StringAsync();

                var resultJson = JObject.Parse(resultStr);

                var message = resultJson.GetString("message");

                if (!message.CompareTo("ok", true))
                {
                    return new FailResultReturn<Kuai100QueryResult>(message);
                }

                var state = resultJson.GetInt("state");

                var logsJson = resultJson.GetJArray("data");

                var lst = new List<Kuai100QueryResult.LogItem>(logsJson.Count);

                foreach (JObject json in logsJson)
                {
                    var dt = json.GetString("ftime").ToDateTime("yyyy-MM-dd HH:mm:ss");

                    var context = json.GetString("context");

                    lst.Add(new Kuai100QueryResult.LogItem(dt.Value, context));
                }

                return new SuccessResultReturn<Kuai100QueryResult>(new Kuai100QueryResult()
                {
                    State = state,
                    Logs = lst.ToArrayEx()
                });
            }
            catch (Exception e)
            {
                return new FailResultReturn<Kuai100QueryResult>(e);
            }
        }

        /// <summary>
        /// 订阅快递单号的推送
        /// </summary>
        /// <param name="customerID">公司编号</param>
        /// <param name="expressName">物流公司名称</param>
        /// <param name="expressCode">物流单号</param>
        /// <param name="callbackUrl">回调地址,回调后,将回调的body数据,调用DecodeSubscribeCallbackData函数解析出结果</param>
        /// <param name="phone">部分快递公司,如顺丰等,需要传入收件人手机号</param>
        /// <returns></returns>
        public static async Task<ResultReturn> SubscribeExpressCodeAsync(string customerID, string expressName,
            string expressCode, string callbackUrl, string phone = "")
        {
            const string url = "https://poll.kuaidi100.com/poll";

            var comName = getExpressNameToCode(expressName);

            if (string.IsNullOrEmpty(comName))
            {
                return new FailResultReturn<Kuai100QueryResult>("查找不到物流公司对应的编号,请检查名称是否正确");
            }

            var json = new JObject()
            {
                ["company"] = comName,
                ["number"] = expressCode,
                ["key"] = customerID,
                ["parameters"] = new JObject()
                {
                    ["callbackurl"] = callbackUrl,
                    ["salt"] = Guid.NewGuid().ToString("N"),
                    ["resultv2"] = "1",
                    ["autoCom"] = "1",
                    ["phone"] = phone
                }
            };

            var resultStr = await WebHelper.Create(url)
                .SetParamter("schema", "json")
                .SetParamter("param", json.ToStringEx(Formatting.None))
                .Encoding(Encoding.UTF8)
                .Post_StringAsync();

            var result = JObject.Parse(resultStr);

            var resultCode = result.GetInt("returnCode");

            if (json.GetBool("result") == true)
            {
                return SuccessResultReturn.Default;
            }

            var errorMsg = "";

            switch (resultCode)
            {
                case 200:
                    {
                        errorMsg = "提交成功";
                        break;
                    }

                case 701:
                    {
                        errorMsg = "拒绝订阅的快递公司";
                        break;
                    }

                case 700:
                    {
                        errorMsg = "订阅方的订阅数据存在错误（如不支持的快递公司、单号为空、单号超长等）或错误的回调地址";
                        break;
                    }

                case 702:
                    {
                        errorMsg = "POLL:识别不到该单号对应的快递公司";
                        break;
                    }

                case 600:
                    {
                        errorMsg = "您不是合法的订阅者（即授权Key出错）";
                        break;
                    }

                case 601:
                    {
                        errorMsg = "POLL:KEY已过期";
                        break;
                    }

                case 500:
                    {
                        errorMsg = "服务器错误";
                        break;
                    }

                case 501:
                    {
                        errorMsg = "重复订阅";
                        break;
                    }
            }

            return new FailResultReturn(errorMsg)
            {
                ReturnCode = resultCode
            };
        }

        //public static async Task<Kuai100QueryResult> GetExpressLogsFreeAsync(string expressName, string expressCode)
        //{

        //}

        /// <summary>
        /// 解码订阅推送收到的数据
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static Kuai100QueryResult DecodeSubscribeCallbackData(string data)
        {
            var json = JObject.Parse(data);

            var resultJson = json.GetJObject("lastResult");

            var decodeResult = new Kuai100QueryResult();

            decodeResult.State = resultJson.GetInt("state");
            decodeResult.IsChecked = resultJson.GetInt("ischeck") != 0;

            var logsJsonLst = resultJson.GetJArray("data");

            var lst = new List<Kuai100QueryResult.LogItem>(logsJsonLst.Count);

            foreach (var item in logsJsonLst)
            {
                var dt = json.GetString("ftime").ToDateTime("yyyy-MM-dd HH:mm:ss");

                var context = json.GetString("context");

                lst.Add(new Kuai100QueryResult.LogItem(dt.Value, context));

            }

            decodeResult.Logs = lst.ToArrayEx();

            return decodeResult;
        }

        private static string getExpressNameToCode(string expressName)
        {
            if (_compareNameMappers.Value.TryGetValue(expressName, out var code))
            {
                return code;
            }

            return "";
        }

        private static Dictionary<string, string> readFile()
        {
            var provider = new EmbeddedFileProvider(typeof(Kuai100).Assembly); //File.ReadAllText(MapJsonFilePath);

            using (var s = provider.GetFileInfo("kuai100.json").CreateReadStream())
            {
                var jsonStr = Encoding.UTF8.GetString(s.ReadAllBytes());

                var json = JObject.Parse(jsonStr);

                var dic = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

                foreach (var pair in json)
                {
                    dic.Add(pair.Key, pair.Value.ToStringEx());
                }

                return dic;
            }

        }
    }
}
