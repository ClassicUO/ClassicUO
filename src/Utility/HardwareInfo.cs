#region license

// Copyright (c) 2021, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Management;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using ClassicUO.Configuration;

namespace ClassicUO.Utility
{
    public static class HardwareInfo
    {
        private static byte m_Version;
        private static int m_InstanceID;
        private static int m_OSMajor, m_OSMinor, m_OSRevision;
        private static int m_CpuFamily, m_CpuModel, m_CpuClockSpeed;
        private static byte m_CpuManufacturer, m_CpuQuantity;
        private static int m_PhysicalMemory;
        private static int m_ScreenWidth, m_ScreenHeight, m_ScreenDepth;
        private static byte m_DXMajor, m_DXMinor;
        private static int m_VCVendorID, m_VCDeviceID, m_VCMemory;
        private static byte m_Distribution, m_ClientsRunning, m_ClientsInstalled, m_PartialInstalled;
        private static string m_VCDescription;
        private static string m_Language;
        private static string m_Unknown;

        public static byte Version { get { return m_Version; } }

        public static int CpuModel { get { return m_CpuModel; } }

        public static int CpuClockSpeed { get { return m_CpuClockSpeed; } }

        public static byte CpuQuantity { get { return m_CpuQuantity; } }

        public static int OSMajor { get { return m_OSMajor; } }

        public static int OSMinor { get { return m_OSMinor; } }

        public static int OSRevision { get { return m_OSRevision; } }

        public static int InstanceID { get { return m_InstanceID; } }

        public static int ScreenWidth { get { return m_ScreenWidth; } }

        public static int ScreenHeight { get { return m_ScreenHeight; } }

        public static int ScreenDepth { get { return m_ScreenDepth; } }

        public static int PhysicalMemory { get { return m_PhysicalMemory; } }

        public static byte CpuManufacturer { get { return m_CpuManufacturer; } }

        public static int CpuFamily { get { return m_CpuFamily; } }

        public static int VCVendorID { get { return m_VCVendorID; } }

        public static int VCDeviceID { get { return m_VCDeviceID; } }

        public static int VCMemory { get { return m_VCMemory; } }

        public static byte DXMajor { get { return m_DXMajor; } }

        public static byte DXMinor { get { return m_DXMinor; } }

        public static string VCDescription { get { return m_VCDescription; } }

        public static string Language { get { return m_Language; } }

        public static byte Distribution { get { return m_Distribution; } }

        public static byte ClientsRunning { get { return m_ClientsRunning; } }

        public static byte ClientsInstalled { get { return m_ClientsInstalled; } }

        public static byte PartialInstalled { get { return m_PartialInstalled; } }

        public static string Unknown { get { return m_Unknown; } }

        public static bool SendHardwareInfo()
        {   // OSI uses some yet unknown algo for sending HardwareInfo. It does seem to be fairly
            // regular after a ~day has passed, with something like a 1 in 5 chance to be sent.
            if (DateTime.Now > Settings.GlobalSettings.LastHardwareInfo + TimeSpan.FromHours(RandomHelper.GetValue(12, 24)))
            {   // once per day'ish
                if (RandomHelper.GetValue(0, 4) == 0)
                {   // random 1 in 5
                    Settings.GlobalSettings.LastHardwareInfo = DateTime.Now;
                    return true;
                }
            }
            return false;
        }

        public static void Initialize()
        {
            m_Version = 2;                          // 1: <4.0.1a, 2>=4.0.1a
            GetInstanceID(out m_InstanceID);        // Unique Instance ID of UO
            GetOperatingSystemInfo(out m_OSMajor, out m_OSMinor, out m_OSRevision);
            GetCPUInfo(out m_CpuClockSpeed, out m_CpuFamily, out m_CpuManufacturer, out m_CpuModel, out m_CpuQuantity);
            GetPhysicalMemory(out m_PhysicalMemory);
            GetScreenInfo(out m_ScreenWidth, out m_ScreenHeight, out m_ScreenDepth);
            GetDirectxVersion(out m_DXMajor, out m_DXMinor);
            GetVideoCardInfo(out m_VCDescription, out m_VCVendorID, out m_VCDeviceID, out m_VCMemory);

            // always 10. I tried 3 different client versions, all return 10
            m_Distribution = 10;

            GetClientsRunning(out m_ClientsRunning);

            m_ClientsInstalled = 1; // Unavailable: we know there is at least one client installed
            m_PartialInstalled = 0; // Unavailable: incomplete installs?
            GetLanguage(out m_Language);
            m_Unknown = string.Empty;
        }
        private static void GetLanguage(out string Language)
        {
            Language = string.Empty;
            CultureInfo ci = CultureInfo.InstalledUICulture;
            Language = ci.ThreeLetterWindowsLanguageName;
        }
        private static void GetClientsRunning(out byte ClientsRunning)
        {
            ClientsRunning = 0;
            Process[] processes = Process.GetProcesses();
            foreach (Process p in processes)
            {
                if (!String.IsNullOrEmpty(p.MainWindowTitle))
                {
                    ClientsRunning += (byte)(p.MainWindowTitle.ToLower().Contains("uo") ? 1 : 0);
                    ClientsRunning += (byte)(p.MainWindowTitle.ToLower().Contains("ultima online") ? 1 : 0);
                }
            }
        }
        private static void GetInstanceID(out int InstanceID)
        {
            InstanceID = 0;
            try
            {
                IntPtr key;
                int error;
                error = WOW6432Node.RegOpenKeyEx(WOW6432Node.HKEY_LOCAL_MACHINE, String.Format(@"SOFTWARE\Origin Worlds Online"),
                    0, WOW6432Node.KEY_READ | WOW6432Node.KEY_WOW64_32KEY, out key);

                if (error != 0)
                    return;
                try
                {
                    InstanceID = (int)WOW6432Node.RegQueryValue(key, "UniqueInstanceId");
                }
                finally
                {
                    WOW6432Node.RegCloseKey(key);
                }
            }
            catch
            {
            }
        }
        private static void GetVideoCardInfo(out string VCDescription, out int VCVendorID, out int VCDeviceID, out int VCMemory)
        {
            VCDescription = string.Empty; VCDeviceID = VCMemory = 0;
            string result = RunQuery("Win32_VideoController", "Name");
            VCDescription = result;

            // OSI's returned VendorID does not seem to have any relation in the actual data the video card produces.
            //  We'll therefore hash the name and use that. (Likely OSI has their own lookup table or hashing algo)
            result = RunQuery("Win32_VideoController", "Name"); // eg. "AMD FirePro W2100"
            VCVendorID = GetStableHashCode(result);

            // Again, OSI's returned VCDeviceID does not seem to have any relation in the actual data the video card produces.
            //  We'll therefore hash the real DeviceID and use that. (Likely OSI has their own lookup table or hashing algo)
            result = RunQuery("Win32_VideoController", "DeviceID"); // "VideoController1"
            VCDeviceID = GetStableHashCode(result);

            result = RunQuery("Win32_VideoController", "AdapterRAM");
            ulong ram;
            ulong.TryParse(result, out ram);
            VCMemory = (int)(ram / 1024 / 1024);
        }
        private static void GetDirectxVersion(out byte DXMajor, out byte DXMinor)
        {
            DXMajor = DXMinor = 0;

            int OSMajor, OSMinor, OSRevision;
            GetOperatingSystemInfo(out OSMajor, out OSMinor, out OSRevision);

            if (OSMajor == 10)
            {
                DXMajor = 12;
                DXMinor = 0;
            }
            else if (OSMajor == 8)
            {
                DXMajor = 11;
                DXMinor = 1;
            }
            else if (OSMajor == 7)
            {
                DXMajor = 11;
                DXMinor = 0;
            }
            else
            {
                DXMajor = 10;
                DXMinor = 0;
            }
        }
        private static void GetScreenInfo(out int ScreenWidth, out int ScreenHeight, out int ScreenDepth)
        {
            ScreenWidth = ScreenHeight = ScreenDepth = 0;
            string result = RunQuery("Win32_VideoController", "CurrentHorizontalResolution");
            ulong hRes;
            ulong.TryParse(result, out hRes);
            ScreenWidth = (int)(hRes);

            result = RunQuery("Win32_VideoController", "CurrentVerticalResolution");
            ulong vRes;
            ulong.TryParse(result, out vRes);
            ScreenHeight = (int)(vRes);

            result = RunQuery("Win32_VideoController", "CurrentBitsPerPixel");
            ulong depth;
            ulong.TryParse(result, out depth);
            ScreenDepth = (int)(depth);
        }
        private static void GetPhysicalMemory(out int PhysicalMemory)
        {
            PhysicalMemory = 0;
            string result = RunQuery("Win32_ComputerSystem", "TotalPhysicalMemory");
            ulong bytes;
            ulong.TryParse(result, out bytes);
            PhysicalMemory = (int)(bytes / 1024 / 1024);
        }
        static void GetCPUInfo(out int CpuClockSpeed, out int CpuFamily, out byte CpuManufacturer, out int CpuModel, out byte CpuQuantity)
        {
            CpuClockSpeed = CpuFamily = CpuModel = 0;
            CpuManufacturer = CpuQuantity = 0;
            var cpu =
                new ManagementObjectSearcher("select * from Win32_Processor")
                .Get()
                .Cast<ManagementObject>()
                        .First();

            CpuClockSpeed = GetCpuClockSpeed((string)cpu["Name"]);
            CpuFamily = GetCpuFamily((string)cpu["Caption"]);
            // There are two major manufacturers of computer processors, Intel and AMD 
            //  OSI returns '2' for my machine which is Intel, so we will assume '1' for all else
            CpuManufacturer = (byte)(((string)cpu["Caption"]).ToLower().Contains("intel") ? 2 : 1);
            CpuModel = GetCpuModel((string)cpu["Caption"]);
            CpuQuantity = (byte)((uint)cpu["NumberOfCores"]);
        }
        static int GetCpuModel(string caption)
        {
            if (string.IsNullOrEmpty(caption))
                return 0;
            string[] toks = caption.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool reading = false;
            int model = 0;
            foreach (string tok in toks)
            {
                if (reading)
                {
                    int.TryParse(tok, out model);
                    return model;
                }

                if (tok.ToLower() == "model")
                    reading = true;
            }

            return 0;
        }
        static int GetCpuClockSpeed(string name)
        {
            if (string.IsNullOrEmpty(name))
                return 0;
            // ensure "3.10Ghz" and "3.10 Ghz" are parsed correctly
            name = name.ToLower().Replace("ghz", " ghz");
            string[] toks = name.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Array.Reverse(toks);
            bool reading = false;
            int clockSpeed = 0;
            double temp;
            foreach (string tok in toks)
            {
                if (reading)
                {
                    double.TryParse(tok, out temp);
                    return clockSpeed = (int)(temp * 1000);
                }

                if (tok.ToLower().Contains("ghz"))
                    reading = true;
            }

            return 0;
        }
        static int GetCpuFamily(string caption)
        {
            if (string.IsNullOrEmpty(caption))
                return 0;
            string[] toks = caption.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            bool reading = false;
            int family = 0;
            foreach (string tok in toks)
            {
                if (reading)
                {
                    int.TryParse(tok, out family);
                    return family;
                }

                if (tok.ToLower() == "family")
                    reading = true;
            }

            return 0;
        }
        static void GetOperatingSystemInfo(out int OSMajor, out int OSMinor, out int OSRevision)
        {
            OSMajor = OSMinor = OSRevision = 0;
            var wmi =
                new ManagementObjectSearcher("select * from Win32_OperatingSystem")
                .Get()
                .Cast<ManagementObject>()
                .First();

            string Version = (string)wmi["Version"];
            string[] toks = Version.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            int.TryParse(toks[0], out OSMajor);
            int.TryParse(toks[1], out OSMinor);
            int.TryParse(toks[2], out OSRevision);
        }
        private static string RunQuery(string TableName, string MethodName)
        {
            ManagementObjectSearcher MOS = new ManagementObjectSearcher("Select * from " + TableName);
            foreach (ManagementObject MO in MOS.Get())
            {
                try
                {
                    return MO[MethodName].ToString();
                }
                catch (Exception e)
                {
                    return null;
                }
            }
            return "";
        }
        public static int GetStableHashCode(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }
    }
    public static class WOW6432Node
    {
        [DllImport("Advapi32.dll", EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode)]
        public static extern int RegOpenKeyEx(IntPtr hKey, [In] string lpSubKey, int ulOptions, int samDesired, out IntPtr phkResult);
        [DllImport("Advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode)]
        public static extern int RegQueryValueEx(IntPtr hKey, [In] string lpValueName, IntPtr lpReserved, out int lpType, [Out] byte[] lpData, ref int lpcbData);
        [DllImport("advapi32.dll")]
        public static extern int RegCloseKey(IntPtr hKey);

        static public readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(-2147483648);
        static public readonly IntPtr HKEY_CURRENT_USER = new IntPtr(-2147483647);
        static public readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(-2147483646);
        static public readonly IntPtr HKEY_USERS = new IntPtr(-2147483645);
        static public readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(-2147483644);
        static public readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(-2147483643);
        static public readonly IntPtr HKEY_DYN_DATA = new IntPtr(-2147483642);

        public const int KEY_READ = 0x20019;
        public const int KEY_WRITE = 0x20006;
        public const int KEY_QUERY_VALUE = 0x0001;
        public const int KEY_SET_VALUE = 0x0002;
        public const int KEY_WOW64_64KEY = 0x0100;
        public const int KEY_WOW64_32KEY = 0x0200;

        public const int REG_NONE = 0;
        public const int REG_SZ = 1;
        public const int REG_EXPAND_SZ = 2;
        public const int REG_BINARY = 3;
        public const int REG_DWORD = 4;
        public const int REG_DWORD_BIG_ENDIAN = 5;
        public const int REG_LINK = 6;
        public const int REG_MULTI_SZ = 7;
        public const int REG_RESOURCE_LIST = 8;
        public const int REG_FULL_RESOURCE_DESCRIPTOR = 9;
        public const int REG_RESOURCE_REQUIREMENTS_LIST = 10;
        public const int REG_QWORD = 11;

        public static object RegQueryValue(IntPtr key, string value)
        {
            return RegQueryValue(key, value, null);
        }
        public static object RegQueryValue(IntPtr key, string value, object defaultValue)
        {
            int error, type = 0, dataLength = 0xfde8;
            int returnLength = dataLength;
            byte[] data = new byte[dataLength];
            while ((error = RegQueryValueEx(key, value, IntPtr.Zero, out type, data, ref returnLength)) == 0xea)
            {
                dataLength *= 2;
                returnLength = dataLength;
                data = new byte[dataLength];
            }
            if (error == 2)
                return defaultValue; // value doesn't exist
            if (error != 0)
                throw new Win32Exception(error);

            switch (type)
            {
                case REG_NONE:
                case REG_BINARY:
                    return data;
                case REG_DWORD:
                    return (((data[0] | (data[1] << 8)) | (data[2] << 16)) | (data[3] << 24));
                case REG_DWORD_BIG_ENDIAN:
                    return (((data[3] | (data[2] << 8)) | (data[1] << 16)) | (data[0] << 24));
                case REG_QWORD:
                    {
                        uint numLow = (uint)(((data[0] | (data[1] << 8)) | (data[2] << 16)) | (data[3] << 24));
                        uint numHigh = (uint)(((data[4] | (data[5] << 8)) | (data[6] << 16)) | (data[7] << 24));
                        return (long)(((ulong)numHigh << 32) | (ulong)numLow);
                    }
                case REG_SZ:
                    return Encoding.Unicode.GetString(data, 0, returnLength);
                case REG_EXPAND_SZ:
                    return Environment.ExpandEnvironmentVariables(Encoding.Unicode.GetString(data, 0, returnLength));
                case REG_MULTI_SZ:
                    {
                        var strings = new List<string>();
                        string packed = Encoding.Unicode.GetString(data, 0, returnLength);
                        int start = 0;
                        int end = packed.IndexOf('\0', start);
                        while (end > start)
                        {
                            strings.Add(packed.Substring(start, end - start));
                            start = end + 1;
                            end = packed.IndexOf('\0', start);
                        }
                        return strings.ToArray();
                    }
                default:
                    throw new NotSupportedException();
            }
        }
    }
}