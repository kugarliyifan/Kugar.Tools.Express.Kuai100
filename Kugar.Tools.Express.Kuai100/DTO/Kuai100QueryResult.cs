using System;
using System.Collections.Generic;
using System.Text;

namespace Kugar.Tools.Express.DTO
{
    public class Kuai100QueryResult
    {
        /// <summary>
        /// 状态: 0=在途，1=揽收，2=疑难，3=签收，4=退签，5=派件，6=退回
        /// </summary>
        public int State { set; get; }

        /// <summary>
        /// 是否已签收
        /// </summary>
        public bool IsChecked { set; get; }

        /// <summary>
        /// 快递记录
        /// </summary>
        public LogItem[] Logs { set; get; }

        public class LogItem
        {
            public LogItem(DateTime logDt, string context)
            {
                LogDt = logDt;
                Context = context;
            }

            public DateTime LogDt { set; get; }

            public string Context { set; get; }
        }
    }
}
