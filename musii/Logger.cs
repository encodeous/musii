using System;
using System.Collections.Generic;
using System.Text;

namespace musii
{
    public static class Logger
    {
        public static void Log(this string s)
        {
            Console.WriteLine("["+DateTime.Now.ToString("h:mm:ss tt") + "]: " + s);
        }
    }
}
