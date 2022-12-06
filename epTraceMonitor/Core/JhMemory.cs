using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using Core.Native;

namespace Core
{
    public class JhMemory
    {
        private readonly Process process;
        public readonly Dictionary<string, Structs.ModuleInformation> modulesDic;

        private ulong minAddress, maxAddress;

        public JhMemory(Process process)
        {
            this.process = process;
            if (process == null)
                throw new Exception("can not found process");
            if (process.MainModule == null)
                throw new Exception("failed get main module infomation");

            var comparer = StringComparer.OrdinalIgnoreCase;
            modulesDic = new Dictionary<string, Structs.ModuleInformation>(comparer);
            if (!MakeModuleDic())
                throw new Exception("failed make module list");

            SetScanRange();
        }
        //Init
        private bool MakeModuleDic()
        {
            IntPtr[] modulesPtr = new IntPtr[0];
            int bytesNeeded;

            if (!Psapi.EnumProcessModulesEx(process.Handle, modulesPtr, 0, out bytesNeeded, (uint)Enums.ModuleFilter.ListModulesAll))
                return false;

            int modulesCount = bytesNeeded / IntPtr.Size;

            modulesPtr = new IntPtr[modulesCount];

            if (Psapi.EnumProcessModulesEx(process.Handle, modulesPtr, bytesNeeded, out bytesNeeded, (uint)Enums.ModuleFilter.ListModulesAll))
            {
                for (int i = 0; i < modulesCount; i++)
                {
                    StringBuilder moduleFilePath = new StringBuilder(1024);
                    Psapi.GetModuleFileNameEx(process.Handle, modulesPtr[i], moduleFilePath, (uint)(moduleFilePath.Capacity));

                    Structs.ModuleInformation mi;
                    Psapi.GetModuleInformation(process.Handle, modulesPtr[i], out mi, (uint)(IntPtr.Size * (modulesPtr.Length)));
                    string moduleName = Path.GetFileName(moduleFilePath.ToString());

                    if (modulesDic.ContainsKey(moduleName))
                    {
                        modulesDic.Add("_" + moduleName, mi);
                    }
                    else
                    {
                        modulesDic.Add(moduleName, mi);
                    }
                }
            }
            return true;
        }

      
        //All cache
        private List<MemoryPage> GetMemoryPages()
        {
            Structs.MemoryBasicinformation mbi = new();
            List<MemoryPage> mpList = new();
            ulong minAddress = this.minAddress;
            ulong maxAddress = this.maxAddress;

            while (minAddress < maxAddress)
            {
                Kernel32.VirtualQueryEx(process.Handle, minAddress, out mbi, (uint)Marshal.SizeOf(mbi));
                if (mbi.Protect == (uint)Enums.MEM_PROTECTION.PAGE_READWRITE && mbi.State == (uint)Enums.MEM_ALLOCATION_TYPE.MEM_COMMIT)
                {
                    byte[] raw = new byte[mbi.RegionSize];
                    Read(mbi.BaseAddress, ref raw);
                    mpList.Add(new MemoryPage(mbi.BaseAddress, raw));
                }
                if (mbi.RegionSize == 0)
                    throw new Exception("maybe process terminated");
                minAddress += mbi.RegionSize;
            }
            return mpList;
        }

        //Setup
        public void SetScanRange()
        {
            Structs.SystemInformation si = new();
            Kernel32.GetSystemInfo(out si);
            this.minAddress = (ulong)si.minimumApplicationAddress;
            this.maxAddress = (ulong)si.maximumApplicationAddress;
        }


        //Memory Read
        public bool Read<T>(ulong address, ref T[] value) where T : unmanaged
        {
            ReadOnlySpan<byte> buffer = MemoryMarshal.Cast<T, byte>(value);
            return Kernel32.ReadProcessMemory(process.Handle, address, ref MemoryMarshal.GetReference(buffer), (UIntPtr)buffer.Length, out _);
        }
        public bool Read<T>(ulong address, ref Span<T> span) where T : unmanaged
        {
            ReadOnlySpan<byte> buffer = MemoryMarshal.Cast<T, byte>(span);
            return Kernel32.ReadProcessMemory(process.Handle, address, ref MemoryMarshal.GetReference(buffer), (UIntPtr)buffer.Length, out _);
        }

        //Scan
        public ulong? Scan(byte[] valueArr)
        {
            var patternData = new PatternData(valueArr);
            ulong? result = Compare(patternData);
            GC.Collect();
            return result;
        }

        private ulong? Compare(PatternData pattern)
        {
            List<MemoryPage> memoryPageList = GetMemoryPages();

            ulong? result = null;
            Parallel.ForEach(memoryPageList,
            (memoryPage, state) =>
            {
                for (int i = 0; i <= memoryPage.Raw.Length - pattern.RAW.Length;)
                {
                    for (int j = pattern.RAW.Length - 1; j >= 0; j--)
                        if (pattern.RAW[j] != memoryPage.Raw[i + j])
                            goto Pass;

                    result = memoryPage.BaseAddress + (uint)i;
                    state.Break();
                Pass:
                    i += pattern.JumpTable[memoryPage.Raw[i + pattern.RAW.Length - 1]];
                    continue;
                }
            });


            memoryPageList.Clear();
            return result;
        }
    }
    public struct PatternData
    {
        public PatternData(byte[] raw)
        {
            //00 ~ FF range
            JumpTable = new int[256];

            for (int i = 0; i < 256; i++)
                JumpTable[i] = raw.Length;

            //set jump index
            for(int i = 0; i < raw.Length; i++)
            {
                if (i < raw.Length - 1)
                    JumpTable[raw[i]] = raw.Length - i - 1;
            }

            RAW = raw;
        }

        public readonly byte[] RAW;
        public readonly int[] JumpTable;
    }
    public struct MemoryPage
    {
        public MemoryPage(ulong BaseAddress, byte[] Raw)
        {
            this.BaseAddress = BaseAddress;
            this.Raw = Raw;
        }
        public readonly ulong BaseAddress;
        public readonly byte[] Raw;
    }
}
