namespace Base
{
    using System;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Security;

    public sealed class ProcessJob : IDisposable
    {
        private readonly IntPtr jobHandle;

        private bool isDisposed;

        public ProcessJob()
        {
            this.jobHandle = CreateJobObject(IntPtr.Zero, null);
            if (this.jobHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Could not create job handle. Error code: {Marshal.GetLastWin32Error()}.");
            }

            var info = new JOBOBJECT_BASIC_LIMIT_INFORMATION { LimitFlags = JOBOBJECTLIMIT.KillOnJobClose };

            var extendedInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION { BasicLimitInformation = info };

            int length = Marshal.SizeOf(typeof(JOBOBJECT_EXTENDED_LIMIT_INFORMATION));
            IntPtr extendedInfoPtr = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(extendedInfo, extendedInfoPtr, false);

                if (!SetInformationJobObject(
                    this.jobHandle,
                    JOBOBJECTINFOCLASS.ExtendedLimitInformation,
                    extendedInfoPtr,
                    (uint)length))
                {
                    throw new InvalidOperationException(
                        $"Could not set information to job object. Error code: {Marshal.GetLastWin32Error()}.");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(extendedInfoPtr);
            }
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            CloseHandle(this.jobHandle);

            this.isDisposed = true;
        }

        public bool AddProcess(IntPtr processHandle)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(ProcessJob));
            }

            if (processHandle == IntPtr.Zero)
            {
                throw new ArgumentNullException(nameof(processHandle));
            }

            return AssignProcessToJobObject(this.jobHandle, processHandle);
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError = true)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateJobObject([In] IntPtr lpJobAttributes, string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool SetInformationJobObject(
            IntPtr hJob,
            JOBOBJECTINFOCLASS jobObjectInfoClass,
            IntPtr lpJobObjectInfo,
            uint cbJobObjectInfoLength);

        private enum JOBOBJECTINFOCLASS
        {
            AssociateCompletionPortInformation = 7,
            BasicLimitInformation = 2,
            BasicUIRestrictions = 4,
            EndOfJobTimeInformation = 6,
            ExtendedLimitInformation = 9,
            SecurityLimitInformation = 5,
            GroupInformation = 11
        }

        [Flags]
        private enum JOBOBJECTLIMIT : uint
        {
            // Basic Limits
            Workingset = 0x00000001,
            ProcessTime = 0x00000002,
            JobTime = 0x00000004,
            ActiveProcess = 0x00000008,
            Affinity = 0x00000010,
            PriorityClass = 0x00000020,
            PreserveJobTime = 0x00000040,
            SchedulingClass = 0x00000080,

            // Extended Limits
            ProcessMemory = 0x00000100,
            JobMemory = 0x00000200,
            DieOnUnhandledException = 0x00000400,
            BreakawayOk = 0x00000800,
            SilentBreakawayOk = 0x00001000,
            KillOnJobClose = 0x00002000,
            SubsetAffinity = 0x00004000,

            // Notification Limits
            JobReadBytes = 0x00010000,
            JobWriteBytes = 0x00020000,
            RateControl = 0x00040000
        }

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct IO_COUNTERS
        {
            private readonly ulong ReadOperationCount;
            private readonly ulong WriteOperationCount;
            private readonly ulong OtherOperationCount;
            private readonly ulong ReadTransferCount;
            private readonly ulong WriteTransferCount;
            private readonly ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            private readonly long PerProcessUserTimeLimit;
            private readonly long PerJobUserTimeLimit;
            public JOBOBJECTLIMIT LimitFlags;
            private readonly UIntPtr MinimumWorkingSetSize;
            private readonly UIntPtr MaximumWorkingSetSize;
            private readonly uint ActiveProcessLimit;
            private readonly long Affinity;
            private readonly uint PriorityClass;
            private readonly uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            private readonly IO_COUNTERS IoInfo;
            private readonly UIntPtr ProcessMemoryLimit;
            private readonly UIntPtr JobMemoryLimit;
            private readonly UIntPtr PeakProcessMemoryUsed;
            private readonly UIntPtr PeakJobMemoryUsed;
        }
    }
}
