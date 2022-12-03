using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Native
{
    public static class Enums
    {
        public enum ModuleFilter
        {
            ListModulesDefault = 0x0,
            ListModules32Bit = 0x01,
            ListModules64Bit = 0x02,
            ListModulesAll = 0x03,
        }

        public enum _THREAD_INFORMATION_CLASS
        {
            ThreadBasicInformation,
            ThreadTimes,
            ThreadPriority,
            ThreadBasePriority,
            ThreadAffinityMask,
            ThreadImpersonationToken,
            ThreadDescriptorTableEntry,
            ThreadEnableAlignmentFaultFixup,
            ThreadEventPair,
            ThreadQuerySetWin32StartAddress,
            ThreadZeroTlsCell,
            ThreadPerformanceCount,
            ThreadAmILastThread,
            ThreadIdealProcessor,
            ThreadPriorityBoost,
            ThreadSetTlsArrayAddress,
            ThreadIsIoPending,
            ThreadHideFromDebugger
        }

        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200),
        }

        [Flags]
        public enum MEM_PROTECTION : uint
        {
            PAGE_NOACCESS = 1,
            PAGE_READONLY = 2,
            PAGE_READWRITE = 4,
            PAGE_WRITECOPY = 8,
            PAGE_EXECUTE = 16, // 0x00000010
            PAGE_EXECUTE_READ = 32, // 0x00000020
            PAGE_EXECUTE_READWRITE = 64, // 0x00000040
            PAGE_EXECUTE_WRITECOPY = 128, // 0x00000080
            PAGE_GUARD = 256, // 0x00000100
            PAGE_NOCACHE = 512, // 0x00000200
            PAGE_WRITECOMBINE = 1024, // 0x00000400
            PAGE_ENCLAVE_UNVALIDATED = 536870912, // 0x20000000
            PAGE_TARGETS_INVALID = 1073741824, // 0x40000000
            PAGE_TARGETS_NO_UPDATE = PAGE_TARGETS_INVALID, // 0x40000000
            PAGE_ENCLAVE_THREAD_CONTROL = 2147483648, // 0x80000000
            PAGE_REVERT_TO_FILE_MAP = PAGE_ENCLAVE_THREAD_CONTROL, // 0x80000000
        }

        [Flags]
        public enum MEM_ALLOCATION_TYPE : uint
        {
            MEM_COMMIT = 4096, // 0x00001000
            MEM_RESERVE = 8192, // 0x00002000
            MEM_DECOMMIT = 16384, // 0x00004000
            MEM_RELEASE = 32768, // 0x00008000
            MEM_FREE = 65536, // 0x00010000
            MEM_PRIVATE = 131072, // 0x00020000
            MEM_MAPPED = 262144, // 0x00040000
            MEM_RESET = 524288, // 0x00080000
            MEM_TOP_DOWN = 1048576, // 0x00100000
            MEM_WRITE_WATCH = 2097152, // 0x00200000
            MEM_PHYSICAL = 4194304, // 0x00400000
            MEM_ROTATE = 8388608, // 0x00800000
            MEM_DIFFERENT_IMAGE_BASE_OK = MEM_ROTATE, // 0x00800000
            MEM_RESET_UNDO = 16777216, // 0x01000000
            MEM_LARGE_PAGES = 536870912, // 0x20000000
            MEM_4MB_PAGES = 2147483648, // 0x80000000
            MEM_64K_PAGES = MEM_LARGE_PAGES | MEM_PHYSICAL, // 0x20400000
        }

    }
}
