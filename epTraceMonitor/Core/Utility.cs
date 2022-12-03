using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public static class Utility
    {
        public static byte[] StringToByteArray(string str, string encode)
        {
            byte[] value;
            encode = encode.ToLower();
            switch(encode)
            {
                case "utf8":
                    value = Encoding.UTF8.GetBytes(str);
                    break;
                case "ascii":
                    value = Encoding.ASCII.GetBytes(str);
                    break;
                case "utf16":
                case "unicode":
                    value = Encoding.Unicode.GetBytes(str);
                    break;
                default:
                    value = Encoding.UTF8.GetBytes(str);
                    break;
            }
            return value;
        }
    }
}
