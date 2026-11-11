
# Nghiên cứu Tổng quan: Kiến trúc Windows Internals và Cơ chế Thực thi Hệ thống

Tài liệu này tổng hợp các khái niệm cốt lõi về cách hệ điều hành Windows quản lý tiến trình, bộ nhớ, tệp thực thi và các cơ chế tầng thấp (low-level), phục vụ mục đích nghiên cứu học thuật và phân tích kỹ thuật hệ thống.

---

## 1. Tệp Batch (`.bat`) là gì và cách hoạt động?
*   **Bản chất:** `.bat` (Batch file) là một tệp kịch bản chứa danh sách các lệnh dòng lệnh (command-line instructions) được thực thi tuần tự bởi trình thông dịch lệnh của Windows là `cmd.exe`.
*   **Cách chạy:** Có thể chạy bằng cách mở Command Prompt và gọi tên tệp, hoặc **nhấn đúp chuột trực tiếp** trong File Explorer. Khi nhấn đúp, Windows tự động gọi `cmd.exe /c` để đọc và thực thi từng dòng lệnh bên trong tệp đó.

## 2. Lệnh `systeminfo` hoạt động như thế nào?
*   **Bản chất:** `systeminfo.exe` là một tiện ích dòng lệnh tích hợp sẵn trong Windows dùng để thu thập thông tin chi tiết về cấu hình hệ điều hành, phần cứng, phiên bản bản vá (hotfix) và thông tin mạng.
*   **Gọi hàm ở đâu:** Tiện ích này không tự thực hiện mọi thứ mà tương tác với tầng hệ điều hành thông qua:
    *   **Windows Management Instrumentation (WMI):** Truy vấn dữ liệu hệ thống từ các lớp như `Win32_OperatingSystem`, `Win32_ComputerSystem`.
    *   **Registry:** Đọc các nhánh hệ thống như `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion`.
    *   **Native/Win32 APIs:** Sử dụng các hàm như `GetSystemInfo`, `GlobalMemoryStatusEx`, và các hàm truy vấn thông tin phiên bản hệ thống.

## 3. Khái niệm "Built-in" là gì?
*   **Định nghĩa:** "Built-in" (tích hợp sẵn) chỉ các lệnh hoặc công cụ nằm sẵn trong bộ cài đặt gốc của hệ điều hành Windows mà không cần cài thêm phần mềm bên thứ ba (ví dụ: `cmd`, `powershell`, `net.exe`, `sc.exe`, `reg.exe`).
*   **Hoạt động:** Các lệnh built-in thường là các tệp thực thi nằm trong thư mục `%SystemRoot%\System32` (như `cmd.exe`, `net.exe`) hoặc là các lệnh nội tại (Internal commands) được xử lý trực tiếp bởi chính tiến trình `cmd.exe` (như `dir`, `cd`, `echo`, `set`).

## 4. Biến môi trường (Environment Variables) và cơ chế gọi tệp
*   **Bản chất:** Biến môi trường là các cặp khóa-giá trị (Key-Value) lưu trữ thông tin cấu hình hệ thống mà tiến trình có thể truy cập (ví dụ: `%PATH%`, `%USERNAME%`, `%SystemRoot%`).
*   **Gọi tệp ngoài thư mục chứa:** Khi bạn gọi một chương trình từ bất kỳ đâu trên dòng lệnh, Windows sẽ dựa vào biến môi trường `PATH`. Hệ thống sẽ lần lượt tìm kiếm tệp thực thi trong thư mục hiện tại, sau đó duyệt qua từng đường dẫn được định nghĩa trong `PATH`. Nếu tìm thấy, Windows sẽ nạp và thực thi mà không bắt buộc bạn phải đứng đúng thư mục chứa tệp đó.

## 5. Trực quan hóa thứ tự chạy của một Binary (PE File Loading)
Khi một tệp thực thi (PE - Portable Executable như `.exe`) khởi chạy, Windows thực hiện chuỗi các bước sau:
1.  **Process Creation:** Kernel tạo không gian địa chỉ ảo mới và tiến trình chính (`EPROCESS`, `PEB`).
2.  **Mapping:** Trình quản lý bộ nhớ (Memory Manager) ánh xạ tệp PE từ đĩa vào bộ nhớ ảo dựa trên thông tin trong PE Header.
3.  **Resolve Imports:** Trình tải (Loader) đọc bảng `Import Address Table (IAT)` để tìm các DLL phụ thuộc (như `kernel32.dll`, `ntdll.dll`), nạp chúng vào bộ nhớ và liên kết địa chỉ các hàm.
4.  **Entry Point Execution:** Luồng thực thi chính (Main Thread) bắt đầu chạy tại điểm nhập (Entry Point) của chương trình.

## 6. Sự khác biệt giữa x86 (32-bit) và x64 (64-bit)
*   **Kích thước con trỏ (Pointer Size):** x86 dùng không gian địa chỉ 32-bit ($2^{32}$ byte $\approx 4$ GB RAM tối đa), trong khi x64 dùng 64-bit (không gian địa chỉ lý thuyết cực lớn lên tới hàng Exabyte).
*   **Thanh ghi (Registers):** x64 có nhiều thanh ghi hơn x86 và kích thước mở rộng từ 32-bit (ví dụ: `EAX`) lên 64-bit (ví dụ: `RAX`), giúp tăng tốc độ xử lý dữ liệu lớn.
*   **Tương thích:** Windows x64 tích hợp cơ chế **WOW64** (Windows on Windows 64) để cho phép các ứng dụng x86 chạy độc lập trên nền tảng 64-bit.

## 7. Có thể dịch ngược (Decompile/Disassemble) một DLL hoặc EXE không?
*   **Có.** Bất kỳ tệp biên dịch nào cũng có thể được dịch ngược về dạng mã hợp ngữ (Disassembly) bằng các công cụ như Ghidra, IDA Pro, hoặc x64dbg.
*   Đối với các ngôn ngữ biên dịch ra mã máy gốc (Native như C, C++), việc dịch ngược thành mã nguồn gốc (C/C++) là dạng giả mã (Decompilation) sẽ có độ phức tạp cao, mất tên hàm (nếu bị strip symbol) nhưng hoàn toàn phân tích được logic.
*   Đối với tệp thuộc nền tảng **.NET** (C#, VB.NET), mã nguồn biên dịch thành Microsoft Intermediate Language (MSIL), có thể dịch ngược gần như **100% nguyên bản** mã nguồn C# bằng công cụ như dnSpy hoặc ILSpy.

## 8. Quyền `SYSTEM`, Kernel, Kill Process và Windows Defender
*   **Quyền SYSTEM (`NT AUTHORITY\SYSTEM`):** Là mức đặc quyền cao nhất trong hệ điều hành Windows, vượt qua cả Administrator. 
*   **Can thiệp Kernel:** Bản thân tiến trình chạy với quyền `SYSTEM` vẫn nằm ở **User-mode** (Ring 3), do đó nó không thể trực tiếp thay đổi mã nguồn hay cấu trúc của Kernel (Ring 0) trừ khi nó tải một **Kernel Driver** hợp lệ đã được ký số (Signed Driver).
*   **Kill Process & Windows Defender:** 
    *   Một tiến trình quyền `SYSTEM` thông thường (như Task Manager chạy dưới system) có thể kết thúc hầu hết các tiến trình khác.
    *   Tuy nhiên, các giải pháp bảo mật như **Windows Defender** (thông qua trình điều khiển kernel `WdFilter.sys`) có cơ chế tự bảo vệ (Self-Protection). Ngay cả khi bạn có quyền `SYSTEM`, nếu thao tác qua API thông thường (`TerminateProcess`) để can thiệp vào tiến trình của Defender, driver của Defender sẽ từ chối thao tác đó ở tầng Kernel.

## 9. Khả năng nạp trên RAM của .NET so với các ngôn ngữ khác
*   **.NET:** Có khả năng nạp và thực thi mã động trực tiếp trên RAM cực kỳ dễ dàng thông qua lớp `System.Reflection` và phương thức `Assembly.Load()` (nạp mảng byte thành một ứng dụng đang chạy mà không cần ghi tệp xuống ổ cứng).
*   **Ngôn ngữ khác (Native như C/C++):** Vẫn có thể thực thi mã hoàn toàn trên RAM thông qua kỹ thuật **Reflective DLL Injection** hoặc cấp phát bộ nhớ thực thi (`VirtualAlloc` với quyền `PAGE_EXECUTE_READWRITE`), sau đó trỏ con trỏ hàm vào vùng nhớ đó để chạy, nhưng phức tạp hơn do phải tự xử lý việc phân giải IAT và relocations.

## 10. Stager và Stageless khác nhau như thế nào?
*   **Stager:** Là một đoạn mã tải trọng siêu nhỏ (nhỏ gọn), có nhiệm vụ duy nhất là kết nối về máy chủ điều khiển để tải toàn bộ phần tải trọng thực sự (Full Payload) lớn hơn về RAM rồi chạy. Giúp qua mặt các giới hạn kích thước ban đầu.
*   **Stageless:** Là một tệp thực thi độc lập duy nhất chứa toàn bộ mọi thứ bên trong. Không cần kết nối tải thêm tệp nào khác khi khởi chạy.

## 11. Phân biệt Encode (Mã hóa dạng thức) và Encrypt (Mã hóa bảo mật)
*   **Encode (Mã hóa định dạng):** Biến đổi dữ liệu từ dạng này sang dạng khác theo một quy luật công khai (ví dụ: Base64, Hex, URL Encode). **Không dùng khóa (key)**, ai cũng có thể giải mã ngược lại ngay lập tức nếu biết thuật toán. Dùng để truyền tải dữ liệu an toàn qua các giao thức.
*   **Encrypt (Mã hóa bảo mật):** Biến đổi dữ liệu dựa trên một **Khóa bí mật (Key)** toán học (ví dụ: AES, RSA). Chỉ người có khóa chính xác mới khôi phục được dữ liệu gốc. Dùng để bảo mật thông tin.

## 12. Host Process là gì?
*   **Định nghĩa:** Host Process là những tiến trình chuẩn hợp pháp có sẵn của hệ điều hành Windows, được tận dụng để "dung nạp" (host) các đoạn mã hoặc thư viện chạy bên trong không gian bộ nhớ của chúng nhằm ngụy trang hoặc chia sẻ tài nguyên. 
*   Ví dụ điển hình: `svchost.exe` (chứa các Windows Services), `rundll32.exe` (chạy hàm bên trong một file DLL).

## 13. DLL chạy như thế nào? Có nhấn đúp chuột được không?
*   **Không thể nhấn đúp chuột:** Tệp `.dll` (Dynamic Link Library) không có điểm khởi chạy độc lập (`main` entry point theo kiểu EXE chuẩn) để hệ điều hành cấp phát tiến trình chạy trực tiếp.
*   **Cách chạy:** DLL chỉ chạy khi được **nạp (load)** vào không gian bộ nhớ của một tiến trình khác thông qua hàm `LoadLibrary` (hoặc lệnh gọi hệ thống), sau đó hệ thống sẽ gọi hàm khởi tạo `DllMain` bên trong DLL đó. (Muốn chạy thủ công qua dòng lệnh, người ta phải dùng `rundll32.exe`).

## 14. Shellcode chạy như thế nào?
*   **Bản chất:** Shellcode là một đoạn mã máy thuần túy (Raw Machine Code) dạng mảng byte (`\x90\x31\...`), không có PE Header, không có bảng import.
*   **Cách chạy:** Nó phải được đặt vào một vùng nhớ cấp phát có quyền thực thi (`PAGE_EXECUTE_READWRITE`), sau đó tạo một luồng mới (`CreateThread`) hoặc ép con trỏ lệnh (Instruction Pointer - RIP/EIP) nhảy thẳng tới địa chỉ vùng nhớ chứa mảng byte đó để CPU thực thi tuần tự từng tập lệnh máy.

## 15. DLL .NET khác gì DLL thường (Native DLL)?
*   **Native DLL:** Chứa mã máy biên dịch trực tiếp từ C/C++ phù hợp với kiến trúc CPU cụ thể (x86/x64).
*   **DLL .NET:** Chứa mã trung gian MSIL (Managed Code). Nó không chạy trực tiếp trên CPU mà cần thông qua **CLR (Common Language Runtime)** - môi trường thực thi của .NET Framework hoặc .NET Core để biên dịch tức thời (JIT - Just-In-Time) thành mã máy khi chạy.

## 16. Quyền truy cập bộ nhớ là gì? (Memory Protections)
*   Là các cờ thuộc tính do Windows Memory Manager áp đặt lên từng trang bộ nhớ ảo (Memory Page, mỗi trang thường có kích thước 4KB):
    *   `PAGE_READONLY`: Chỉ cho phép đọc dữ liệu.
    *   `PAGE_READWRITE`: Cho phép đọc và ghi dữ liệu.
    *   `PAGE_EXECUTE_READ`: Cho phép đọc và thực thi mã lệnh (nhưng không cho ghi đè, chống mã độc sửa code).
    *   `PAGE_EXECUTE_READWRITE`: Vừa đọc, vừa ghi, vừa thực thi (thường là mục tiêu của các cơ chế cấp phát mã động, nhưng dễ bị EDR giám sát chặt chẽ).

## 17. Process Injection và Remote Process Injection khác nhau thế nào?
*   **Process Injection (Nói chung):** Kỹ thuật đưa mã thực thi vào một không gian bộ nhớ tiến trình.
*   **Local Process Injection:** Tiến trình tự cấp phát bộ nhớ và tiêm mã vào chính không gian bộ nhớ của nó.
*   **Remote Process Injection:** Một tiến trình có quyền quản trị mở một tiến trình khác đang chạy (ví dụ: `notepad.exe`), cấp phát bộ nhớ trong tiến trình đích đó (`VirtualAllocEx`), ghi mã vào (`WriteProcessMemory`), và ra lệnh cho tiến trình đích chạy đoạn mã đó (`CreateRemoteThread`).

## 18. Chèn byte ở cuối tệp (Overlay Data) thì tệp có chạy được không?
*   **Có.** Cấu trúc PE Header của Windows định nghĩa rõ kích thước và vị trí của các phần (sections). Nếu bạn nối thêm một khối dữ liệu byte vào tận cùng của tệp (sau phần kết thúc thực tế của cấu trúc PE), hệ thống khi nạp tệp vẫn đọc đúng phần Header, bỏ qua phần dư thừa ở cuối. 
*   Ứng dụng vẫn chạy hoàn toàn bình thường, và phần byte chèn thêm thường dùng để lưu trữ tài nguyên phụ, cấu hình hoặc dữ liệu đính kèm.

## 19. Tổng quan chuỗi vận hành (Attack Chain) trong môi trường doanh nghiệp
Trong môi trường doanh nghiệp có hệ thống phòng thủ nhiều lớp (Defense-in-Depth), một tiến trình thực thi (loader) phải vượt qua các chốt chặn theo thứ tự từ ngoài vào trong:
1.  **Email Gateway / Web Proxy / Firewall:** Lọc và chặn tệp đính kèm độc hại hoặc kết nối tải xuống ban đầu.
2.  **Endpoint Protection / EDR (Microsoft Defender, CrowdStrike, v.v.):** Quét tĩnh tệp khi ghi xuống ổ đĩa và giám sát hành vi thời gian thực.
3.  **Application Control (AppLocker / WDAC):** Kiểm tra xem tệp thực thi có nằm trong danh sách cho phép (Allowlist) hay chữ ký số tin cậy hay không.
4.  **AMSI (Antimalware Scan Interface):** Kiểm tra nội dung kịch bản (PowerShell, VBScript, .NET) ngay trong bộ nhớ trước khi biên dịch/thực thi.
5.  **Behavioral Monitoring (EDR Kernel Callbacks):** Giám sát các hành vi bất thường như tiến trình lạ mở tiến trình khác (`OpenProcess`), tiêm mã bộ nhớ, hoặc tạo kết nối mạng hướng ngoại (Outbound connection) tới máy chủ điều khiển (C2).

## 20. Phân biệt Packing, Encoding và Encryption
*   **Packing (Đóng gói):** Nén tệp thực thi gốc lại thành một lớp vỏ bọc kèm theo một đoạn mã giải nén nhỏ (Stub). Khi chạy, Stub tự giải nén tệp gốc vào RAM rồi chuyển quyền điều khiển. Dùng để giảm dung lượng hoặc chống dịch ngược tĩnh cơ bản.
*   **Encoding:** Biến đổi dữ liệu sang dạng khác (như XOR, Base64) để qua mặt các bộ lọc chuỗi ký tự tĩnh (Static String Signatures). Không có tính năng bảo mật mật mã học.
*   **Encryption:** Mã hóa dữ liệu bằng thuật toán phức tạp kèm khóa giải mã. Dữ liệu hoàn toàn ngẫu nhiên và an toàn cho đến khi có khóa để giải mã.

## 21. API Hashing là gì, hoạt động và tác dụng ra sao?
*   **Bản chất:** Thay vì gọi trực tiếp tên hàm dạng chuỗi ký tự rõ ràng (ví dụ: `"VirtualAlloc"`) thông qua `GetProcAddress`, lập trình viên (hoặc các công cụ phân tích) sẽ chuyển đổi chuỗi tên hàm đó thành một mã băm cố định (ví dụ dùng thuật toán ROR13, CRC32, hoặc DJB2).
*   **Tác dụng:** 
    *   Ẩn hoàn toàn các tên hàm nhạy cảm khỏi bảng Import Table hoặc phần chuỗi tĩnh (Strings) của tệp.
    *   Khi chương trình chạy, nó sẽ tự động duyệt qua danh sách các hàm trong DLL (như `kernel32.dll`), tính mã băm của từng hàm cho đến khi trùng khớp với mã băm mục tiêu thì lấy địa chỉ thực thi. Giúp chống lại việc phân tích tĩnh (Static Analysis) bằng các công cụ tìm kiếm chuỗi ký tự.
