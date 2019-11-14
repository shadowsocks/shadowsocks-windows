/* ***************************************************************************

The component allows to read the environment variables of another process
running in a Windows system.

History:

 - v1.2.ss Add GetCommandLine for convenience.
 
 - v1.2: Added support for inspection of 64 bit processes from 32 bit host
 - v1.1: Fixed issue with environment block size detection
 - v1.0: Initial


******************************************************************************

The MIT License (MIT)

Copyright (c) 2011-2014 Oleksiy Gapotchenko
Copyright (c) 2018 Shadowsocks Project

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*************************************************************************** */

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Shadowsocks.Test
{
    static class ProcessEnvironment
    {
        public static StringDictionary ReadEnvironmentVariables(this Process process)
        {
            return _GetEnvironmentVariablesCore(process.Handle);
        }

        public static StringDictionary TryReadEnvironmentVariables(this Process process)
        {
            try
            {
                return ReadEnvironmentVariables(process);
            }
            catch
            {
                return null;
            }
        }

        public static string GetCommandLine(this Process process)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
            }

        }

        /// <summary>
        /// Universal pointer.
        /// </summary>
        struct UniPtr
        {
            public UniPtr(IntPtr p)
            {
                Value = p.ToInt64();
                Size = IntPtr.Size;
            }

            public UniPtr(long p)
            {
                Value = p;
                Size = sizeof(long);
            }

            public long Value;
            public int Size;

            public static implicit operator IntPtr(UniPtr p)
            {
                return new IntPtr(p.Value);
            }

            public static implicit operator UniPtr(IntPtr p)
            {
                return new UniPtr(p);
            }

            public override string ToString()
            {
                return Value.ToString();
            }

            public bool FitsInNativePointer
            {
                get
                {
                    return Size <= IntPtr.Size;
                }
            }

            public bool CanBeRepresentedByNativePointer
            {
                get
                {
                    int actualSize = Size;

                    if (actualSize == 8)
                    {
                        if (Value >> 32 == 0)
                            actualSize = 4;
                    }

                    return actualSize <= IntPtr.Size;
                }
            }

            public long ToInt64()
            {
                return Value;
            }
        }

        static StringDictionary _GetEnvironmentVariablesCore(IntPtr hProcess)
        {
            var penv = _GetPenv(hProcess);

            const int maxEnvSize = 32767;
            byte[] envData;

            if (penv.CanBeRepresentedByNativePointer)
            {
                int dataSize;
                if (!_HasReadAccess(hProcess, penv, out dataSize))
                    throw new Exception("Unable to read environment block.");

                if (dataSize > maxEnvSize)
                    dataSize = maxEnvSize;

                envData = new byte[dataSize];
                var res_len = IntPtr.Zero;
                bool b = WindowsApi.ReadProcessMemory(
                    hProcess,
                    penv,
                    envData,
                    new IntPtr(dataSize),
                    ref res_len);

                if (!b || (int)res_len != dataSize)
                    throw new Exception("Unable to read environment block data.");
            }
            else if (penv.Size == 8 && IntPtr.Size == 4)
            {
                // Accessing 64 bit process under 32 bit host.

                int dataSize;
                if (!_HasReadAccessWow64(hProcess, penv.ToInt64(), out dataSize))
                    throw new Exception("Unable to read environment block with WOW64 API.");

                if (dataSize > maxEnvSize)
                    dataSize = maxEnvSize;

                envData = new byte[dataSize];
                long res_len = 0;
                int result = WindowsApi.NtWow64ReadVirtualMemory64(
                    hProcess,
                    penv.ToInt64(),
                    envData,
                    dataSize,
                    ref res_len);

                if (result != WindowsApi.STATUS_SUCCESS || res_len != dataSize)
                    throw new Exception("Unable to read environment block data with WOW64 API.");
            }
            else
            {
                throw new Exception("Unable to access process memory due to unsupported bitness cardinality.");
            }

            return _EnvToDictionary(envData);
        }

        static StringDictionary _EnvToDictionary(byte[] env)
        {
            var result = new StringDictionary();

            int len = env.Length;
            if (len < 4)
                return result;

            int n = len - 3;
            for (int i = 0; i < n; ++i)
            {
                byte c1 = env[i];
                byte c2 = env[i + 1];
                byte c3 = env[i + 2];
                byte c4 = env[i + 3];

                if (c1 == 0 && c2 == 0 && c3 == 0 && c4 == 0)
                {
                    len = i + 3;
                    break;
                }
            }

            char[] environmentCharArray = Encoding.Unicode.GetChars(env, 0, len);

            for (int i = 0; i < environmentCharArray.Length; i++)
            {
                int startIndex = i;
                while ((environmentCharArray[i] != '=') && (environmentCharArray[i] != '\0'))
                {
                    i++;
                }
                if (environmentCharArray[i] != '\0')
                {
                    if ((i - startIndex) == 0)
                    {
                        while (environmentCharArray[i] != '\0')
                        {
                            i++;
                        }
                    }
                    else
                    {
                        string str = new string(environmentCharArray, startIndex, i - startIndex);
                        i++;
                        int num3 = i;
                        while (environmentCharArray[i] != '\0')
                        {
                            i++;
                        }
                        string str2 = new string(environmentCharArray, num3, i - num3);
                        result[str] = str2;
                    }
                }
            }

            return result;
        }

        static bool _TryReadIntPtr32(IntPtr hProcess, IntPtr ptr, out IntPtr readPtr)
        {
            bool result;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                int dataSize = sizeof(Int32);
                var data = Marshal.AllocHGlobal(dataSize);
                IntPtr res_len = IntPtr.Zero;
                bool b = WindowsApi.ReadProcessMemory(
                    hProcess,
                    ptr,
                    data,
                    new IntPtr(dataSize),
                    ref res_len);
                readPtr = new IntPtr(Marshal.ReadInt32(data));
                Marshal.FreeHGlobal(data);
                if (!b || (int)res_len != dataSize)
                    result = false;
                else
                    result = true;
            }
            return result;
        }

        static bool _TryReadIntPtr(IntPtr hProcess, IntPtr ptr, out IntPtr readPtr)
        {
            bool result;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                int dataSize = IntPtr.Size;
                var data = Marshal.AllocHGlobal(dataSize);
                IntPtr res_len = IntPtr.Zero;
                bool b = WindowsApi.ReadProcessMemory(
                    hProcess,
                    ptr,
                    data,
                    new IntPtr(dataSize),
                    ref res_len);
                readPtr = Marshal.ReadIntPtr(data);
                Marshal.FreeHGlobal(data);
                if (!b || (int)res_len != dataSize)
                    result = false;
                else
                    result = true;
            }
            return result;
        }

        static bool _TryReadIntPtrWow64(IntPtr hProcess, long ptr, out long readPtr)
        {
            bool result;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
            }
            finally
            {
                int dataSize = sizeof(long);
                var data = Marshal.AllocHGlobal(dataSize);
                long res_len = 0;
                int status = WindowsApi.NtWow64ReadVirtualMemory64(
                    hProcess,
                    ptr,
                    data,
                    dataSize,
                    ref res_len);
                readPtr = Marshal.ReadInt64(data);
                Marshal.FreeHGlobal(data);
                if (status != WindowsApi.STATUS_SUCCESS || res_len != dataSize)
                    result = false;
                else
                    result = true;
            }
            return result;
        }

        static UniPtr _GetPenv(IntPtr hProcess)
        {
            int processBitness = _GetProcessBitness(hProcess);

            if (processBitness == 64)
            {
                if (Environment.Is64BitProcess)
                {
                    // Accessing 64 bit process under 64 bit host.

                    IntPtr pPeb = _GetPeb64(hProcess);

                    IntPtr ptr;
                    if (!_TryReadIntPtr(hProcess, pPeb + 0x20, out ptr))
                        throw new Exception("Unable to read PEB.");

                    IntPtr penv;
                    if (!_TryReadIntPtr(hProcess, ptr + 0x80, out penv))
                        throw new Exception("Unable to read RTL_USER_PROCESS_PARAMETERS.");

                    return penv;
                }
                else
                {
                    // Accessing 64 bit process under 32 bit host.

                    var pPeb = _GetPeb64(hProcess);

                    long ptr;
                    if (!_TryReadIntPtrWow64(hProcess, pPeb.ToInt64() + 0x20, out ptr))
                        throw new Exception("Unable to read PEB.");

                    long penv;
                    if (!_TryReadIntPtrWow64(hProcess, ptr + 0x80, out penv))
                        throw new Exception("Unable to read RTL_USER_PROCESS_PARAMETERS.");

                    return new UniPtr(penv);
                }
            }
            else
            {
                // Accessing 32 bit process under 32 bit host.

                IntPtr pPeb = _GetPeb32(hProcess);

                IntPtr ptr;
                if (!_TryReadIntPtr32(hProcess, pPeb + 0x10, out ptr))
                    throw new Exception("Unable to read PEB.");

                IntPtr penv;
                if (!_TryReadIntPtr32(hProcess, ptr + 0x48, out penv))
                    throw new Exception("Unable to read RTL_USER_PROCESS_PARAMETERS.");

                return penv;
            }
        }

        static int _GetProcessBitness(IntPtr hProcess)
        {
            if (Environment.Is64BitOperatingSystem)
            {
                bool wow64;
                if (!WindowsApi.IsWow64Process(hProcess, out wow64))
                    return 32;
                if (wow64)
                    return 32;
                return 64;
            }
            else
            {
                return 32;
            }
        }

        static IntPtr _GetPeb32(IntPtr hProcess)
        {
            if (Environment.Is64BitProcess)
            {
                var ptr = IntPtr.Zero;
                int res_len = 0;
                int pbiSize = IntPtr.Size;
                int status = WindowsApi.NtQueryInformationProcess(
                    hProcess,
                    WindowsApi.ProcessWow64Information,
                    ref ptr,
                    pbiSize,
                    ref res_len);
                if (res_len != pbiSize)
                    throw new Exception("Unable to query process information.");
                return ptr;
            }
            else
            {
                return _GetPebNative(hProcess);
            }
        }

        static IntPtr _GetPebNative(IntPtr hProcess)
        {
            var pbi = new WindowsApi.PROCESS_BASIC_INFORMATION();
            int res_len = 0;
            int pbiSize = Marshal.SizeOf(pbi);
            int status = WindowsApi.NtQueryInformationProcess(
                hProcess,
                WindowsApi.ProcessBasicInformation,
                ref pbi,
                pbiSize,
                ref res_len);
            if (res_len != pbiSize)
                throw new Exception("Unable to query process information.");
            return pbi.PebBaseAddress;
        }

        static UniPtr _GetPeb64(IntPtr hProcess)
        {
            if (Environment.Is64BitProcess)
            {
                return _GetPebNative(hProcess);
            }
            else
            {
                // Get PEB via WOW64 API.
                var pbi = new WindowsApi.PROCESS_BASIC_INFORMATION_WOW64();
                int res_len = 0;
                int pbiSize = Marshal.SizeOf(pbi);
                int status = WindowsApi.NtWow64QueryInformationProcess64(
                    hProcess,
                    WindowsApi.ProcessBasicInformation,
                    ref pbi,
                    pbiSize,
                    ref res_len);
                if (res_len != pbiSize)
                    throw new Exception("Unable to query process information.");
                return new UniPtr(pbi.PebBaseAddress);
            }
        }

        static bool _HasReadAccess(IntPtr hProcess, IntPtr address, out int size)
        {
            size = 0;

            var memInfo = new WindowsApi.MEMORY_BASIC_INFORMATION();
            int result = WindowsApi.VirtualQueryEx(
                hProcess,
                address,
                ref memInfo,
                Marshal.SizeOf(memInfo));

            if (result == 0)
                return false;

            if (memInfo.Protect == WindowsApi.PAGE_NOACCESS || memInfo.Protect == WindowsApi.PAGE_EXECUTE)
                return false;

            try
            {
                size = Convert.ToInt32(memInfo.RegionSize.ToInt64() - (address.ToInt64() - memInfo.BaseAddress.ToInt64()));
            }
            catch (OverflowException)
            {
                return false;
            }

            if (size <= 0)
                return false;

            return true;
        }

        static bool _HasReadAccessWow64(IntPtr hProcess, long address, out int size)
        {
            size = 0;

            WindowsApi.MEMORY_BASIC_INFORMATION_WOW64 memInfo;
            var memInfoType = typeof(WindowsApi.MEMORY_BASIC_INFORMATION_WOW64);
            int memInfoLength = Marshal.SizeOf(memInfoType);
            const int memInfoAlign = 8;

            long resultLength = 0;
            int result;

            IntPtr hMemInfo = Marshal.AllocHGlobal(memInfoLength + memInfoAlign * 2);
            try
            {
                // Align to 64 bits.
                IntPtr hMemInfoAligned = new IntPtr(hMemInfo.ToInt64() & ~(memInfoAlign - 1L));

                result = WindowsApi.NtWow64QueryVirtualMemory64(
                    hProcess,
                    address,
                    WindowsApi.MEMORY_INFORMATION_CLASS.MemoryBasicInformation,
                    hMemInfoAligned,
                    memInfoLength,
                    ref resultLength);

                memInfo = (WindowsApi.MEMORY_BASIC_INFORMATION_WOW64)Marshal.PtrToStructure(hMemInfoAligned, memInfoType);
            }
            finally
            {
                Marshal.FreeHGlobal(hMemInfo);
            }

            if (result != WindowsApi.STATUS_SUCCESS)
                return false;

            if (memInfo.Protect == WindowsApi.PAGE_NOACCESS || memInfo.Protect == WindowsApi.PAGE_EXECUTE)
                return false;

            try
            {
                size = Convert.ToInt32(memInfo.RegionSize - (address - memInfo.BaseAddress));
            }
            catch (OverflowException)
            {
                return false;
            }

            if (size <= 0)
                return false;

            return true;
        }

        static class WindowsApi
        {
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct PROCESS_BASIC_INFORMATION
            {
                public IntPtr Reserved1;
                public IntPtr PebBaseAddress;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
                public IntPtr[] Reserved2;
                public IntPtr UniqueProcessId;
                public IntPtr Reserved3;
            }

            public const int ProcessBasicInformation = 0;
            public const int ProcessWow64Information = 26;

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern int NtQueryInformationProcess(
                IntPtr hProcess,
                int pic,
                ref PROCESS_BASIC_INFORMATION pbi,
                int cb,
                ref int pSize);

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern int NtQueryInformationProcess(
                IntPtr hProcess,
                int pic,
                ref IntPtr pi,
                int cb,
                ref int pSize);

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern int NtQueryInformationProcess(
                IntPtr hProcess,
                int pic,
                ref long pi,
                int cb,
                ref int pSize);

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct PROCESS_BASIC_INFORMATION_WOW64
            {
                public long Reserved1;
                public long PebBaseAddress;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
                public long[] Reserved2;
                public long UniqueProcessId;
                public long Reserved3;
            }

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern int NtWow64QueryInformationProcess64(
                IntPtr hProcess,
                int pic,
                ref PROCESS_BASIC_INFORMATION_WOW64 pbi,
                int cb,
                ref int pSize);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadProcessMemory(
              IntPtr hProcess,
              IntPtr lpBaseAddress,
              [Out] byte[] lpBuffer,
              IntPtr dwSize,
              ref IntPtr lpNumberOfBytesRead);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadProcessMemory(
              IntPtr hProcess,
              IntPtr lpBaseAddress,
              IntPtr lpBuffer,
              IntPtr dwSize,
              ref IntPtr lpNumberOfBytesRead);

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern int NtWow64ReadVirtualMemory64(
              IntPtr hProcess,
              long lpBaseAddress,
              IntPtr lpBuffer,
              long dwSize,
              ref long lpNumberOfBytesRead);

            [DllImport("ntdll.dll", SetLastError = true)]
            public static extern int NtWow64ReadVirtualMemory64(
              IntPtr hProcess,
              long lpBaseAddress,
              [Out] byte[] lpBuffer,
              long dwSize,
              ref long lpNumberOfBytesRead);

            public const int STATUS_SUCCESS = 0;

            public const int PAGE_NOACCESS = 0x01;
            public const int PAGE_EXECUTE = 0x10;

            [StructLayout(LayoutKind.Sequential)]
            public struct MEMORY_BASIC_INFORMATION
            {
                public IntPtr BaseAddress;
                public IntPtr AllocationBase;
                public int AllocationProtect;
                public IntPtr RegionSize;
                public int State;
                public int Protect;
                public int Type;
            }

            [DllImport("kernel32.dll")]
            public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, int dwLength);

            [StructLayout(LayoutKind.Sequential)]
            public struct MEMORY_BASIC_INFORMATION_WOW64
            {
                public long BaseAddress;
                public long AllocationBase;
                public int AllocationProtect;
                public long RegionSize;
                public int State;
                public int Protect;
                public int Type;
            }

            public enum MEMORY_INFORMATION_CLASS
            {
                MemoryBasicInformation
            }

            [DllImport("ntdll.dll")]
            public static extern int NtWow64QueryVirtualMemory64(
                IntPtr hProcess,
                long lpAddress,
                MEMORY_INFORMATION_CLASS memoryInformationClass,
                IntPtr lpBuffer, // MEMORY_BASIC_INFORMATION_WOW64, pointer must be 64-bit aligned
                long memoryInformationLength,
                ref long returnLength);

            [DllImport("kernel32.dll")]
            public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);
        }
    }
}
