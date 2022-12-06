using Core.Models;
using Core.Native;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Core
{
    public class EPTraceMonitor
    {
        public EPTraceMonitor(string[] args)
        {
            ConsoleSetting();
            Span<string> epmapFileSpan = LoadEPMapFile(args);
            this.headerArr = GetHeaderArray(epmapFileSpan);
            this.epmapDic = GenerateEPMapDic(epmapFileSpan);
            this.j = OpenProcess();
            this.traceTableStart = GettraceTableStart();
        }

        //Init
        private void ConsoleSetting()
        {
            IntPtr handle = Kernel32.GetConsoleWindow();
            IntPtr sysMenu = User32.GetSystemMenu(handle, false);

            IntPtr modeHandle = Kernel32.GetStdHandle(-10);
            UInt32 consoleMode;
            Kernel32.GetConsoleMode(modeHandle, out consoleMode);

            consoleMode &= ~((uint)0x0040);
            Kernel32.SetConsoleMode(modeHandle, consoleMode);
            Console.Title = $"epTraceMonitor";
            if (handle != IntPtr.Zero)
            {
                User32.DeleteMenu(sysMenu, (int)Enums.SysCommands.SC_MINIMIZE, (int)Enums.EnableMenuItem.MF_BYCOMMAND);
                User32.DeleteMenu(sysMenu, (int)Enums.SysCommands.SC_SIZE, (int)Enums.EnableMenuItem.MF_BYCOMMAND);
            }
            Console.SetWindowSize(180, 43);
        }
        private Span<string> LoadEPMapFile(string[] args)
        {
            if (args.Length == 0)
                ExitProgram("argument가 잘못되었습니다");

            string epmapFilePath = args[0];
            Span<string> epmapFileSpan = new();
            try
            {
                epmapFileSpan = File.ReadAllText(epmapFilePath).Split("\r\n").AsSpan();
            }
            catch
            {
                ExitProgram("경로가 잘못되었거나 epmap을 읽을 수 없습니다");
            }
            //remove \n
            epmapFileSpan = epmapFileSpan.Slice(0, epmapFileSpan.Length - 1);
            return epmapFileSpan;
        }
        private byte[] GetHeaderArray(Span<string> textSpan)
        {
            //header 2
            var headerStr = textSpan.Slice(0, 2);
            var header1 = headerStr[0].AsSpan().Slice(4);
            var header2 = headerStr[1].AsSpan().Slice(4);

            if (header1.Length != 32 || header2.Length != 32)
            {
                ExitProgram("헤더 길이가 잘못되었습니다");
            }

            //parse Hex
            var headerArr = new byte[header2.Length / 2];
            for (int i = 0; i < header2.Length; i = i + 2)
            {
                try
                {
                    headerArr[i / 2] = byte.Parse(header2.Slice(i, 2), System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    ExitProgram("헤더를 읽을 수 없습니다.");
                }
            }
            return headerArr;
        }
        private ConcurrentDictionary<uint, EPMap> GenerateEPMapDic(Span<string> epmapFileSpan)
        {
            epmapFileSpan = epmapFileSpan.Slice(2); //'2' means header area
            ConcurrentDictionary<uint, EPMap> epmapDic = new();
            for (int i = 0; i < epmapFileSpan.Length; i++)
            {
                uint identify = 0;
                try
                {
                    identify = uint.Parse(epmapFileSpan[i].AsSpan().Slice(3, 8), System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    ExitProgram("epmap 식별자를 읽을 수 없습니다");
                }
                string[] tmp = epmapFileSpan[i].AsSpan().Slice(14).ToString().Split("|");
                string? filePath = Path.GetDirectoryName(tmp[0]);
                if (filePath == null)
                {
                    ExitProgram("파일경로를 읽을 수 없습니다");
                }
                string fileName = Path.GetFileName(tmp[0]);
                if (fileName == null)
                {
                    ExitProgram("파일이름을 읽을 수 없습니다");
                }
                string functionName = tmp[1] + "()";
                uint line = 0;
                try
                {
                    line = uint.Parse(tmp[2]);
                }
                catch
                {
                    ExitProgram("라인 수 를 읽을 수 없습니다");
                }
                #pragma warning disable CS8604 // 가능한 null 참조 인수입니다.    위에서 처리 다해뒀음
                epmapDic[identify] = new EPMap(filePath, fileName, functionName, line);
                #pragma warning restore CS8604 // 가능한 null 참조 인수입니다.
            }
            return epmapDic;
        }
        private JhMemory OpenProcess()
        {
            //open process
            try
            {
                return new JhMemory(Process.GetProcessesByName("StarCraft")[0]);
            }
            catch
            {
                ExitProgram("스타크래프트를 먼저 실행하세요.");
                return new(Process.GetCurrentProcess()); //will never be execute

            }
        }     
        private ulong GettraceTableStart()
        {
            //scan header signature
            int tryCount = 1;
            ulong? traceTableStart = null;
            Console.WriteLine($"게임 입장을 기다리는 중입니다.");
            while (true)
            {
                Console.Write($"{tryCount}번째 시도중...");
                Console.SetCursorPosition(0, 1);
                traceTableStart = j.Scan(headerArr);
                if (traceTableStart != null)
                    break;
                Thread.Sleep(1000);
                tryCount++;
            }
            GC.Collect();

            return traceTableStart.Value;
        }


        //shortcut
        private void ExitProgram(string msg)
        {
            Console.WriteLine(msg);
            Console.WriteLine("epTraceMonitor가 종료됩니다.");
            Thread.Sleep(3000);
            Environment.Exit(0);
        }
        private void CleanConsoleWrite(string msg, int top)
        {
            Console.SetCursorPosition(0, top);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, top);
            Console.Write(msg);
        }



        private struct CountLog
        {
            public CountLog()
            {
                this.success = 0;
                this.fail = 0;
                this.traceCount = 0;
            }
            public ulong success;
            public ulong fail;
            public ulong traceCount;
            public void Reset()
            {
                this.success = 0;
                this.fail = 0;
                this.traceCount = 0;
            }
        }
        enum Filter
        {
            BlackList,
            WhiteList
        }
        enum Speed
        {
            Low,
            Mid,
            High
        }



        //init(w), main(r), displayer(r)
        private ConcurrentDictionary<uint, EPMap> epmapDic;

        //init(w), main(r)
        private JhMemory j;
        byte[] headerArr;
        private ulong traceTableStart;



        //main(r), commander(w)
        private readonly HashSet<string> blackList = new();
        private readonly HashSet<string> whiteList = new();
        private bool pause = false;

        //main(rw), displayer(r), commander(w)
        private ConcurrentDictionary<uint, int> sampleHitCount = new();

        //displayer(r), Commander(w)
        private Filter filter = Filter.BlackList;
        private Speed speed = Speed.High;

        //main(w), displayer(r), commander(r)
        private bool workerState = true;

        //main(w), displayer(r), commander(w)
        private CountLog countLog = new();

        //displayer(r), commander(r)
        const int commandPos = 40;



        public void Run()
        {
            Winmm.timeBeginPeriod(1);
            var workerDisplayer = new Thread(() => Displayer());
            var workerCommander = new Thread(() => Commander());
            workerDisplayer.Start();
            workerCommander.Start();




            Span<uint> stackTraceBuffer = stackalloc uint[2048 * 4];
            Span<byte> currentHeader = stackalloc byte[16];
            SortedSet<uint> sampledCheckpointSet = new();
            Queue<uint> lastCheckpointSet = new();
            int error = 0;
            while (true)
            {
                if (!j.Read((ulong)traceTableStart, ref currentHeader))
                {
                    error = 1;
                    break; //not found SC
                }
                if (!currentHeader.SequenceEqual(headerArr))
                {

                    error = 2;
                    break; // Game left
                }
                if (pause)
                    continue;
                if (j.Read(traceTableStart + 20, ref stackTraceBuffer))
                {
                    countLog.success++;
                    sampledCheckpointSet.Clear();
                    int stackDepth = 0;

                    while (stackTraceBuffer[stackDepth] != 0)
                    {
                        sampledCheckpointSet.Add(stackTraceBuffer[stackDepth]);
                        stackDepth++;
                        countLog.traceCount++;
                    }
                    foreach (uint checkPoint in sampledCheckpointSet)
                    {
                        if (sampleHitCount.ContainsKey(checkPoint))
                        {
                            sampleHitCount[checkPoint]++;
                            lastCheckpointSet.Enqueue(checkPoint);
                            if (lastCheckpointSet.Count > 30)
                                lastCheckpointSet.Dequeue();
                        }
                        else
                            sampleHitCount[checkPoint] = 1;
                    }
                }
                else
                {
                    countLog.fail++;
                }
                switch (speed)
                {
                    case Speed.High:
                        Thread.Sleep(0);
                        break;
                    case Speed.Mid:
                        Thread.Sleep(1);
                        break;
                    case Speed.Low:
                        Thread.Sleep(10);
                        break;
                }
            }
            workerState = false;

            Console.WriteLine(new string(' ', Console.WindowWidth));
            Console.WriteLine(new string(' ', Console.WindowWidth));
            var sb = new StringBuilder();
            foreach (uint checkPoint in lastCheckpointSet)
            {
                var dic = epmapDic[checkPoint];
                var log = $"            |{dic.FileName,20}|{dic.FunctionName,32}:{dic.Line,-5}|  {dic.Content}";
                sb.AppendLine(log);
            }
            if (lastCheckpointSet.Count != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.SetCursorPosition(0, Console.WindowHeight - 1);
                Console.WriteLine("Last update\n");
                Console.Write(sb.ToString());
                Console.ResetColor();
            }
            Console.WriteLine($"epTraceMonitor 중지됨({error})");
            //Console.WriteLine("아무키나 누르면 프로그램이 종료됩니다.");
            Winmm.timeEndPeriod(1);
        }

        private void Displayer()
        {
            StringBuilder sb = new();

            while (workerState)
            {
                var backupPos = Console.GetCursorPosition();
                sb.Clear();
                Console.SetCursorPosition(0, 0);

                var sortVar = from item in sampleHitCount
                              orderby item.Value descending
                              select item;
                int count = 0;
                string filterText = (filter == Filter.BlackList) ? "블랙리스트" : "화이트리스트";
                if (pause)
                    Console.Title = $"epTraceMonitor   {countLog.success:#,0} | {countLog.fail:#,0}   Update : {countLog.traceCount:#,0}  {filterText}  [일시중지]";
                else
                    Console.Title = $"epTraceMonitor   {countLog.success:#,0} | {countLog.fail:#,0}   Update : {countLog.traceCount:#,0}  {filterText}";

                foreach (var dic in sortVar)
                {
                    if (count < 40)
                    {
                        if (filter == Filter.BlackList)
                        {
                            if (blackList.Contains(epmapDic[dic.Key].FileName) || blackList.Contains($"{epmapDic[dic.Key].FunctionName}") || blackList.Contains($"{epmapDic[dic.Key].FunctionName}:{epmapDic[dic.Key].Line}"))
                                continue;
                        }
                        else
                        {
                            if (!whiteList.Contains(epmapDic[dic.Key].FileName) && !whiteList.Contains($"{epmapDic[dic.Key].FunctionName}") && !whiteList.Contains($"{epmapDic[dic.Key].FunctionName}:{epmapDic[dic.Key].Line}"))
                                continue;
                        }

                        var log = $"| {dic.Value,10} | {epmapDic[dic.Key].FileName,20} | {epmapDic[dic.Key].FunctionName,32}:{epmapDic[dic.Key].Line,-5} | {epmapDic[dic.Key].Content}";
                        sb.Append(log);
                        int padding = Encoding.Default.GetByteCount(log);

                        try
                        {
                            if (padding > Console.WindowWidth - 1)
                                Console.SetWindowSize(padding + 1, Console.WindowHeight);
                            sb.Append(' ', Console.WindowWidth - padding - 1);
                        }
                        catch { }
                        sb.Append(Environment.NewLine);
                    }
                    else
                        break;
                    count++;
                }
                while (count < 40)
                {
                    sb.Append(' ', Console.WindowWidth - 1);
                    sb.Append(Environment.NewLine);
                    count++;
                }


                Console.Write(sb.ToString());
                Console.SetCursorPosition(backupPos.Left, commandPos);
                Thread.Sleep(1000);
            }
        }

        private void Commander()
        {
            Thread.Sleep(200); // aot compile bug fix
            Console.SetCursorPosition(0, commandPos + 1);
            if (filter == Filter.BlackList)
                Console.Write("필터 - 블랙리스트");
            else
                Console.Write("필터 - 화이트리스트");
            Console.SetCursorPosition(0, commandPos);
            Console.Write("command>>");
            while (workerState)
            {
                string[] input = (Console.ReadLine() ?? "").Split(" ");
                if (!workerState)
                    break;
                switch (input[0])
                {
                    case "black":
                        {
                            if (input.Length != 1)
                                goto default;

                            filter = Filter.BlackList;
                            CleanConsoleWrite($"필터 - 블랙리스트", commandPos + 1);
                            break;
                        }
                    case "white":
                        {
                            if (input.Length != 1)
                                goto default;

                            filter = Filter.WhiteList;
                            CleanConsoleWrite($"필터 - 화이트리스트", commandPos + 1);
                            break;
                        }
                    case "speed":
                        {
                            if (input.Length != 2)
                                goto default;

                            if (input[1] == "high")
                            {
                                speed = Speed.High;
                                CleanConsoleWrite($"tick 갱신주기 빠름 (cpu성능 비례)", commandPos + 1);
                            }
                            else if (input[1] == "mid")
                            {
                                speed = Speed.Mid;
                                CleanConsoleWrite($"tick 갱신주기 보통 (1ms)", commandPos + 1);
                            }
                            else if (input[1] == "low")
                            {
                                speed = Speed.Low;
                                CleanConsoleWrite($"tick 갱신주기 느림 (10ms)", commandPos + 1);
                            }
                            else
                                goto default;

                            break;
                        }
                    case "fl":
                        {
                            if (input.Length != 2)
                                goto default;

                            if (filter == Filter.BlackList)
                            {
                                if (blackList.Contains(input[1]))
                                {
                                    blackList.Remove(input[1]);
                                    CleanConsoleWrite($"블랙리스트 : {input[1]} 를 제거했습니다.", commandPos + 1);
                                }
                                else
                                {
                                    blackList.Add(input[1]);
                                    CleanConsoleWrite($"블랙리스트 : {input[1]} 가 추가되었습니다.", commandPos + 1);
                                }
                            }
                            else
                            {
                                if (whiteList.Contains(input[1]))
                                {
                                    whiteList.Remove(input[1]);
                                    CleanConsoleWrite($"화이트리스트 : {input[1]} 를 제거했습니다.", commandPos + 1);
                                }
                                else
                                {
                                    whiteList.Add(input[1]);
                                    CleanConsoleWrite($"화이트리스트 : {input[1]} 가 추가되었습니다.", commandPos + 1);
                                }
                            }
                            break;
                        }
                    case "pp":
                        {
                            pause = !pause;
                            if (pause)
                                CleanConsoleWrite($"일시중지", commandPos + 1);
                            else
                                CleanConsoleWrite($"재개", commandPos + 1);
                            break;
                        }
                    case "reset":
                        {
                            if (input.Length != 2)
                                goto default;

                            if (input[1] == "black")
                            {
                                blackList.Clear();
                                CleanConsoleWrite($"블랙리스트 초기화", commandPos + 1);
                            }
                            else if (input[1] == "white")
                            {
                                whiteList.Clear();
                                CleanConsoleWrite($"화이트리스트 초기화", commandPos + 1);
                            }
                            else if (input[1] == "bw")
                            {
                                blackList.Clear();
                                whiteList.Clear();
                                CleanConsoleWrite($"블랙리스트, 화이트리스트 초기화", commandPos + 1);
                            }
                            else if (input[1] == "trace")
                            {
                                countLog.Reset();
                                sampleHitCount.Clear();
                                CleanConsoleWrite($"트레이스 초기화", commandPos + 1);
                            }
                            else
                                goto default;
                            break;
                        }
                    default:
                        CleanConsoleWrite($"{string.Join(" ", input)} 는 잘못된 명령어 입니다.", commandPos + 1);
                        break;
                }
                CleanConsoleWrite("command>>", commandPos);
            }
        }
    }
}
