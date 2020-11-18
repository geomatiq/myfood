using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using myfoodapp.Core.Model;
using myfoodapp.Core.Common;

namespace myfoodapp.Core.Business
{
    public class InternalHealthManager
    {
        private LogManager lg = LogManager.GetInstance;
        private static InternalHealthManager instance;

        public static InternalHealthManager GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new InternalHealthManager();
                }
                return instance;
            }
        }

        private InternalHealthManager()
        {
        }

        public string GetInternalTemperatureSignature()
        {
            StringBuilder rslt = new StringBuilder("EEE");

            var str = "sudo /opt/vc/bin/vcgencmd measure_temp".Bash();

            str = str.Replace("temp=","").Replace("'C","").Replace(".","").Replace(",","").Trim();

            if(str.Length > 2)
            {           
                rslt[1] = str[0];
                rslt[2] = str[1];
            }

            Console.WriteLine(rslt.ToString());
                      
            return rslt.ToString();
        }

        public string GetInternalThrottledSignature()
        {
            var str = "sudo /opt/vc/bin/vcgencmd get_throttled".Bash();

            str = str.Replace("throttled=0x","").Trim();

            Console.WriteLine(str);
                      
            return str;
        }
    }
}
