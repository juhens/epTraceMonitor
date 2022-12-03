using Core.Native;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Core
{
    public class EPTraceMonitor
    {
        //TODO: enum
        private const int MF_BYCOMMAND = 0x00000000;
        private const int SC_CLOSE = 0xF060;
        private const int SC_MAXIMIZE = 0xF020;
        private const int SC_MINIMIZE = 0xF030;
        private const int SC_SIZE = 0xF000;//resize

        public EPTraceMonitor(string[] args)
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
                User32.DeleteMenu(sysMenu, SC_MINIMIZE, MF_BYCOMMAND);
                User32.DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);
            }
            Console.SetWindowSize(180, 43);

            //TODO: bad initalizer
            OpenEpMap(args);
            OpenProcess();
            traceTableStart = WaittingGame();
        }

        //Init
        private void OpenEpMap(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("argument가 잘못되었습니다");
                Console.WriteLine("epTraceMonitor가 종료됩니다.");
                Thread.Sleep(3000);
                Environment.Exit(0);
            }

            //read epmap
            string epmapPath = args[0];
            Span<string> textSpan = new();
            try
            {
                textSpan = File.ReadAllText(epmapPath).Split("\r\n").AsSpan();
            }
            catch
            {
                Console.WriteLine("epmap을 읽을 수 없습니다");
                Console.WriteLine("epTraceMonitor가 종료됩니다.");
                Thread.Sleep(3000);
                Environment.Exit(0);
            }
            //remove \n
            textSpan = textSpan.Slice(0, textSpan.Length - 1);

            //header 2
            var headerStr = textSpan.Slice(0, 2);
            var header1 = headerStr[0].AsSpan().Slice(4);
            var header2 = headerStr[1].AsSpan().Slice(4);

            if (header1.Length != 32 || header2.Length != 32)
            {
                Console.WriteLine("헤더 길이가 잘못되었습니다");
                Console.WriteLine("epTraceMonitor가 종료됩니다.");
                Thread.Sleep(3000);
                Environment.Exit(0);
            }


            //mapData
            var mapDataStr = textSpan.Slice(headerStr.Length);
            for (int i = 0; i < mapDataStr.Length; i++)
            {
                uint key = 0;
                try
                {
                    key = uint.Parse(mapDataStr[i].AsSpan().Slice(3, 8), System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    Console.WriteLine("epmap을 읽을 수 없습니다");
                    Console.WriteLine("epTraceMonitor가 종료됩니다.");
                    Thread.Sleep(3000);
                    Environment.Exit(0);
                }

                var mapData = new MapData(mapDataStr[i].AsSpan().Slice(14).ToString().Split("|"));
                mapDataDic[key] = mapData;
            }

            //parse Hex
            headerArr = new byte[header2.Length / 2];
            for (int i = 0; i < header2.Length; i = i + 2)
            {
                try
                {
                    headerArr[i / 2] = byte.Parse(header2.Slice(i, 2), System.Globalization.NumberStyles.HexNumber);
                }
                catch
                {
                    Console.WriteLine("헤더를 읽을 수 없습니다.");
                    Console.WriteLine("epTraceMonitor가 종료됩니다.");
                    Thread.Sleep(3000);
                    Environment.Exit(0);
                }
            }
        }
        private void OpenProcess()
        {
            //open process
            try
            {
                j = new JhMemory(Process.GetProcessesByName("StarCraft")[0]);
            }
            catch
            {
                Console.WriteLine("스타크래프트를 먼저 실행하세요.");
                Console.WriteLine("epTraceMonitor가 종료됩니다.");
                Thread.Sleep(3000);
                Environment.Exit(0);
            }
        }
        private ulong WaittingGame()
        {
            //scan header signature
            int tryCount = 1;
            ulong? result = null;
            while (result == null)
            {
                Console.Write($"{tryCount}번째 시도중...");
                Console.SetCursorPosition(0, 0);
                result = j.Scan(headerArr);
                Thread.Sleep(1000);
                tryCount++;
            }
            GC.Collect();

            return result.Value;
        }

        //TODO: change class?
        private struct MapData
        {
            public MapData(string[] strings)
            {
                FilePath = strings[0];
                FileName = Path.GetFileName(strings[0]);
                FunctionName = strings[1];
                Line = int.Parse(strings[2]);

                if (!epsRaw.ContainsKey(FilePath))
                {
                    epsRaw[FilePath] = File.ReadAllLines(FilePath, Encoding.Default);
                }
            }
            public string GetLineContent()
            {
                if (Content != null)
                    return Content;

                Content = epsRaw[FilePath][Line - 1].Trim().Replace(Environment.NewLine, string.Empty).Split("\t")[0];
                if (Content.Length > 80)
                    Content = Content.AsSpan().Slice(0, 80).ToString();
                return Content;
            }
            public string FilePath;
            public string FileName;
            public string FunctionName;
            public int Line;
            private string? Content;
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


        //1. 초기화
        //2. 


        
        //mapData's fk? cache?
        private readonly static ConcurrentDictionary<string, string[]> epsRaw = new();

        //init(w), main(r)
        private ConcurrentDictionary<uint, MapData> mapDataDic = new();

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

        //init(w), main(r)
        private JhMemory j = new(Process.GetCurrentProcess());
        byte[] headerArr = new byte[32];

        private ulong traceTableStart;

        public void Run()
        {
            Winmm.timeBeginPeriod(1);
            var workerDisplayer = new Thread(() => Displayer());
            var workerCommaner = new Thread(() => Commander());
            workerDisplayer.Start();
            workerCommaner.Start();


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
                var dic = mapDataDic[checkPoint];
                var log = $"            |{dic.FileName,20}|{dic.FunctionName,32}():{dic.Line,-5}|  {dic.GetLineContent()}";
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
                            if (blackList.Contains(mapDataDic[dic.Key].FileName) || blackList.Contains($"{mapDataDic[dic.Key].FunctionName}()") || blackList.Contains($"{mapDataDic[dic.Key].FunctionName}():{mapDataDic[dic.Key].Line}"))
                                continue;
                        }
                        else
                        {
                            if (!whiteList.Contains(mapDataDic[dic.Key].FileName) && !whiteList.Contains($"{mapDataDic[dic.Key].FunctionName}()") && !whiteList.Contains($"{mapDataDic[dic.Key].FunctionName}():{mapDataDic[dic.Key].Line}"))
                                continue;
                        }

                        var log = $"| {dic.Value,10} | {mapDataDic[dic.Key].FileName,20} | {mapDataDic[dic.Key].FunctionName,32}():{mapDataDic[dic.Key].Line,-5} | {mapDataDic[dic.Key].GetLineContent()}";
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
                            {
                                goto default;
                            }
                            Console.SetCursorPosition(0, commandPos + 1);
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.SetCursorPosition(0, commandPos + 1);
                            filter = Filter.BlackList;
                            Console.Write($"필터 - 블랙리스트");
                            break;
                        }
                    case "white":
                        {
                            if (input.Length != 1)
                            {
                                goto default;
                            }
                            Console.SetCursorPosition(0, commandPos + 1);
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.SetCursorPosition(0, commandPos + 1);
                            filter = Filter.WhiteList;
                            Console.Write($"필터 - 화이트리스트");
                            break;
                        }
                    case "speed":
                        {
                            if (input.Length != 2)
                            {
                                goto default;
                            }
                            Console.SetCursorPosition(0, commandPos + 1);
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.SetCursorPosition(0, commandPos + 1);
                            if (input[1] == "high")
                            {
                                speed = Speed.High;
                                Console.Write($"tick 갱신주기 빠름 (cpu성능 비례)");
                            }
                            else if (input[1] == "mid")
                            {
                                speed = Speed.Mid;
                                Console.Write($"tick 갱신주기 보통 (1ms)");
                            }
                            else if (input[1] == "low")
                            {
                                speed = Speed.Low;
                                Console.Write($"tick 갱신주기 느림 (10ms)");
                            }
                            else
                                goto default;

                            break;
                        }
                    case "fl":
                        {
                            if (input.Length != 2)
                            {
                                goto default;
                            }
                            Console.SetCursorPosition(0, commandPos + 1);
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.SetCursorPosition(0, commandPos + 1);

                            if (filter == Filter.BlackList)
                            {
                                if (blackList.Contains(input[1]))
                                {
                                    blackList.Remove(input[1]);
                                    Console.Write($"블랙리스트 : {input[1]} 를 제거했습니다.");
                                }
                                else
                                {
                                    blackList.Add(input[1]);
                                    Console.Write($"블랙리스트 : {input[1]} 가 추가되었습니다.");
                                }
                            }
                            else
                            {
                                if (whiteList.Contains(input[1]))
                                {
                                    whiteList.Remove(input[1]);
                                    Console.Write($"화이트리스트 : {input[1]} 를 제거했습니다.");
                                }
                                else
                                {
                                    whiteList.Add(input[1]);
                                    Console.Write($"화이트리스트 : {input[1]} 가 추가되었습니다.");
                                }
                            }
                            break;
                        }
                    case "pp":
                        {
                            pause = !pause;
                            Console.SetCursorPosition(0, commandPos + 1);
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.SetCursorPosition(0, commandPos + 1);
                            if (pause)
                                Console.Write($"일시중지");
                            else
                                Console.Write($"재개");
                            break;
                        }
                    case "reset":
                        {
                            if (input.Length != 2)
                            {
                                goto default;
                            }
                            Console.SetCursorPosition(0, commandPos + 1);
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.SetCursorPosition(0, commandPos + 1);

                            if (input[1] == "black")
                            {
                                blackList.Clear();
                                Console.Write($"블랙리스트 초기화");
                            }
                            else if (input[1] == "white")
                            {
                                whiteList.Clear();
                                Console.Write($"화이트리스트 초기화");
                            }
                            else if (input[1] == "bw")
                            {
                                blackList.Clear();
                                whiteList.Clear();
                                Console.Write($"블랙리스트, 화이트리스트 초기화");
                            }
                            else if (input[1] == "trace")
                            {
                                countLog.Reset();
                                sampleHitCount.Clear();
                                Console.Write($"트레이스 초기화");
                            }
                            else
                                goto default;
                            break;
                        }
                    default:
                        Console.SetCursorPosition(0, commandPos + 1);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, commandPos + 1);
                        Console.Write($"{string.Join(" ", input)} 는 잘못된 명령어 입니다.");
                        break;
                }
                Console.SetCursorPosition(0, commandPos);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, commandPos);
                Console.Write("command>>");
            }
        }
    }
}
