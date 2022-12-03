using Core;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace epTraceMonitor
{
    internal class Program
    {
        static void Main(string[] args)
        {
            EPTraceMonitor e = new(args);
            e.Run();
        }
    }
}
