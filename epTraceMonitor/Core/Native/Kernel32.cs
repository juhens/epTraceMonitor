using System;
using System.Runtime.InteropServices;
using System.Security;


namespace Core.Native
{
    public static class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, ulong lpBaseAddress, ref byte lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, ulong lpBaseAddress, ref byte lpBuffer, UIntPtr nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern int IsWow64Process(IntPtr hProcess, out bool bWow64Process);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(Enums.ThreadAccess dwDesiredAccess, bool bInherihThread, int dwThreadId);

        [DllImport("ntdll.dll", SetLastError = true, EntryPoint = "NtQueryInformationThread")]
        public static extern int NtQueryInformationThread(IntPtr pHandle, Enums._THREAD_INFORMATION_CLASS infoClass, ref Structs.ThreadBasicInformation instance, int sizeOfInstance, out int length);

        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out Structs.SystemInformation lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(IntPtr hProcess, ulong lpAddress, out Structs.MemoryBasicinformation lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern ulong VirtualAlloc([In] ulong lpAddress, UIntPtr dwSize, Enums.MEM_ALLOCATION_TYPE flAllocationType, Enums.MEM_PROTECTION flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFree([In] ulong lpAddress, UIntPtr dwSize, Enums.MEM_ALLOCATION_TYPE dwFreeType);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    }
}
