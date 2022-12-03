using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Core.Native
{
    public static class Structs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct ModuleInformation
        {
            public readonly UIntPtr lpBaseOfDll;
            public readonly uint SizeOfImage;
            public readonly UIntPtr EntryPoint;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryBasicinformation
        {
            public ulong BaseAddress;
            public ulong AllocationBase;
            public uint AllocationProtect;
            public uint __alignment1;
            public ulong RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
            public uint __alignment2;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ThreadBasicInformation
        {
            public int ExitStatus;
            public ulong TebBaseAddress;
            public ClientId ClientId;
            public ulong AffinityMask; // x86 == 4, x64 == 8
            public int Priority;
            public int BasePriority;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct ClientId
        {
            IntPtr UniqueProcess;
            IntPtr UniqueThread;
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct SystemInformation
        {
            public ushort processorArchitecture;
            //ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }
    }
}
