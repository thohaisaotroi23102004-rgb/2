# BÁO CÁO TỔNG HỢP NGHIÊN CỨU LOADER V4 VÀ AV/EDR
*Phiên bản tổng hợp khái niệm & phân tích hệ thống*

---

## MỤC 1: KIẾN THỨC NỀN TẢNG VỀ HỆ ĐIỀU HÀNH VÀ CHƯƠNG TRÌNH WINDOWS

Phần này giải thích cách một đoạn chữ do con người viết trở thành chương trình chạy trên máy tính, đồng thời làm rõ các thành phần cốt lõi của hệ điều hành Windows.

### 1.1 Các công cụ tự động hóa công việc
* **Batch (`.bat`)**: Danh sách các câu lệnh CMD viết sẵn chạy lần lượt.
  * *Ví dụ:* Giống như bạn ghi ra giấy các bước tạo thư mục rồi nhờ máy tính làm theo từng dòng.
* **Bash**: Tương tự Batch nhưng dành riêng cho hệ điều hành Linux.
  * *Ví dụ:* Công cụ chuyên duyệt qua các tệp nhật ký (log) trên Linux để đếm số lượng dòng chứa từ khóa lỗi.
* **PowerShell**: Công cụ quản trị mạnh mẽ của Windows, ngoài lệnh thông thường còn có khả năng xử lý đối tượng `.NET`.
  * *Ví dụ:* Lệnh `Get-Service` trả về danh sách dịch vụ cùng các thuộc tính để lọc trực tiếp các dịch vụ đang chạy.
* **Python**: Ngôn ngữ lập trình phổ biến dùng để đọc log, phân tích dữ liệu và tự động hóa.
  * *Ví dụ:* Viết script đọc tệp CSV xuất từ Sysmon để thống kê tần suất xuất hiện của các sự kiện.

### 1.2 Từ mã nguồn đến chương trình thực thi
* **Mã nguồn (Source code)**: Phần chữ do lập trình viên viết bằng C, C#, Python... CPU không thực thi trực tiếp dạng này.
* **Compiler (Trình biên dịch)**: Công cụ chuyển đổi mã nguồn thành mã máy hoặc mã trung gian.
* **Machine code (Mã máy)**: Dãy byte chứa tập lệnh mà CPU đọc và thực thi trực tiếp theo kiến trúc phần cứng.
* **Native code**: Mã máy được biên dịch riêng cho một kiến trúc CPU cụ thể (như x64) và chạy trực tiếp không cần qua môi trường trung gian.
* **Bộ ba IL, CLR và JIT (Trong mô hình .NET / C#)**:
  * *IL (Intermediate Language)*: Mã trung gian được tạo ra sau khi biên dịch C#.
  * *CLR (Common Language Runtime)*: Môi trường thực thi và quản lý bộ nhớ của ứng dụng .NET.
  * *JIT (Just-In-Time Compiler)*: Thành phần bên trong CLR dịch từng đoạn mã IL thành mã máy ngay tại thời điểm chạy.
* **Reflection**: Tính năng cho phép chương trình tự soi chiếu, tìm kiếm và gọi các class hoặc method (hàm) ngay khi đang chạy mà không cần định trước lúc biên dịch.
* **Runtime**: Môi trường hỗ trợ thực thi chương trình (như CLR của .NET hay JVM của Java).
* **So sánh đường thực thi các ngôn ngữ**:
  * *C/C++*: Biên dịch thẳng thành Native Code (EXE/DLL) cho CPU mục tiêu.
  * *Java*: Biên dịch ra Bytecode và chạy thông qua máy ảo JVM.
  * *Go*: Biên dịch ra Native Code kèm theo Runtime tối ưu quản lý bộ nhớ.
  * *Python*: Chạy thông qua Interpreter (trình thông dịch) xử lý trực tiếp bytecode Python.

### 1.3 CPU, bộ nhớ và các thành phần trong Windows
* **CPU (Central Processing Unit)**: Bộ não thực thi các lệnh máy theo các lượt được hệ điều hành lập lịch.
* **RAM (Random Access Memory)**: Bộ nhớ tạm thời lưu dữ liệu và mã lệnh trong suốt quá trình chương trình đang mở. Tắt máy sẽ mất dữ liệu chưa lưu.
* **Ổ cứng (SSD/HDD)**: Lưu trữ lâu dài các tệp tin (EXE, DLL, tài liệu).
* **Windows Loader**: Thành phần của Windows nhận tệp EXE/DLL từ ổ đĩa, cấp phát không gian bộ nhớ và chuẩn bị luồng thực thi.
* **Process (Tiến trình) & Thread (Luồng)**:
  * *Process*: Một ứng dụng đang hoạt động, sở hữu không gian bộ nhớ ảo riêng.
  * *Thread*: Đơn vị thực thi công việc nhỏ bên trong một tiến trình. Một tiến trình có thể chứa nhiều luồng.
* **Không gian địa chỉ ảo (Virtual Address Space)**: Vùng nhớ ảo riêng biệt cho từng tiến trình do Windows ánh xạ tới RAM thực, giúp các chương trình không can thiệp lẫn nhau.
* **Định dạng PE (Portable Executable)**: Cấu trúc đóng gói chuẩn của các tệp `.exe`, `.dll` hoặc driver trên Windows.
* **Kiến trúc (x86, x64, ARM)**: Chuẩn thiết kế phần cứng điều khiển cách xử lý dữ liệu và con trỏ bộ nhớ.
* **User mode & Kernel mode**:
  * *User mode*: Mức quyền hạn của ứng dụng người dùng, không được truy cập trực tiếp vào phần cứng hay bộ nhớ hệ thống.
  * *Kernel mode*: Mức quyền hạn cao nhất dành cho nhân hệ điều hành và các driver phần cứng.

### 1.4 Công cụ quan sát và giám sát hệ thống
* **Process Explorer**: Hiển thị cây tiến trình cha-con, danh sách DLL đã nạp và các tài nguyên tiến trình đang chiếm giữ.
* **Process Monitor (ProcMon)**: Theo dõi toàn bộ thao tác đọc/ghi tệp, truy cập Registry và các sự kiện hệ thống theo thời gian thực.
* **TCPView**: Giám sát danh sách tiến trình đang mở cổng kết nối mạng (IP nguồn/đích, Port).
* **Autoruns**: Liệt kê chi tiết mọi vị trí có khả năng tự động khởi động cùng Windows (Startup, Services, Scheduled Tasks).
* **Event Viewer & Sysmon**: Hệ thống đọc nhật ký Windows và công cụ Sysmon ghi nhận sâu các sự kiện tạo tiến trình, kết nối mạng, thay đổi tệp.

---

## MỤC 2: KHÁI NIỆM LOADER VÀ CÁC CƠ CHẾ NẠP MÃ

Phần này phân tích khái niệm "nạp mã" từ các chức năng hợp pháp của hệ điều hành cho đến các cách thức tổ chức thực thi.

### 2.1 Phân biệt Loader, Stager và Payload
* **Loader**: Chương trình chuẩn bị và nạp mã vào bộ nhớ để thực thi.
* **Stager**: Đoạn mã khởi động kích thước nhỏ, đóng vai trò tải về hoặc kích hoạt giai đoạn nạp chính tiếp theo.
* **Payload**: Đoạn mã thực hiện chức năng cuối cùng của chuỗi thực thi (có thể là lệnh kiểm tra hoặc kênh điều khiển từ xa).
* **Host process**: Tiến trình thực tế nơi đoạn mã đang được thực thi.
* **Nạp từ tệp (File-backed) vs Nạp từ bộ nhớ (In-memory)**:
  * *File-backed*: Nạp dữ liệu gắn liền với tệp tin vật lý lưu trên ổ đĩa.
  * *In-memory*: Nạp dữ liệu từ mảng byte nằm trực tiếp trên RAM, không tạo file EXE/DLL mới trên đĩa.
* **Shellcode**: Dãy byte mã máy thô dành cho kiến trúc CPU cụ thể, không cần cấu trúc định dạng PE đầy đủ.

### 2.2 Các môi trường thực thi và nạp mã phổ biến
* **LOLBins (Living off the Land Binaries)**: Các tệp thực thi hợp pháp có sẵn của hệ điều hành (như `powershell.exe`, `certutil.exe`) bị lạm dụng sai bối cảnh mục đích.
* **Môi trường Kịch bản (PowerShell, VBA, JScript, HTA)**: Các ngôn ngữ kịch bản có khả năng tương tác với hệ thống, bộ nhớ và các thư viện `.NET`.
* **Cơ chế nạp của Java & .NET**: Java dùng `Class Loader` nạp bytecode vào JVM; .NET dùng `Assembly Loader` của CLR nạp mã IL và metadata.

### 2.3 Các cơ chế nạp DLL và kỹ thuật liên quan
* **Static Import vs Dynamic Loading**:
  * *Static Import*: Khai báo thư viện cố định trong cấu trúc PE từ lúc biên dịch.
  * *Dynamic Loading*: Chương trình gọi hàm (như `LoadLibrary`) để nạp thư viện linh hoạt tại thời điểm chạy.
* **DLL Side-loading & Proxy DLL**:
  * *DLL Side-loading*: Tình huống ứng dụng nạp nhầm thư viện từ vị trí không an toàn do thứ tự tìm kiếm tệp của hệ thống.
  * *Proxy DLL*: Thư viện trung gian giữ nguyên giao diện hàm để chuyển tiếp lời gọi sang thư viện thật.
* **Reflective Loading**: Kỹ thuật tự chuẩn bị và nạp DLL vào bộ nhớ RAM mà không thông qua trình nạp chuẩn (`Windows Loader`).

### 2.4 Kỹ thuật can thiệp tiến trình
* **Process Hollowing**: Tạo một tiến trình hợp pháp ở trạng thái tạm dừng, sau đó thay thế nội dung bộ nhớ của nó bằng một mã thực thi khác trước khi cho chạy tiếp.
* **Remote Process Injection**: Tiến trình này ghi dữ liệu/mã thực thi vào không gian bộ nhớ của một tiến trình khác và kích hoạt luồng chạy.
* **So sánh điểm khác biệt**: `Assembly.Load` nạp mã ngay trong tiến trình hiện tại qua CLR; trong khi `Remote Injection` can thiệp qua ranh giới giữa hai tiến trình khác nhau.

---

## MỤC 3: CƠ CHẾ PHÁT HIỆN CỦA ANTIVIRUS VÀ EDR

Phần này giải thích cách các hệ thống bảo mật quét tệp trước khi chạy và giám sát hành vi khi chương trình đang hoạt động.

### 3.1 Phân vùng các giải pháp bảo vệ
* **Bảo vệ Endpoint (EPP/EDR)**:
  * *EPP (Endpoint Protection Platform)*: Nền tảng ngăn chặn mối đe dọa dựa trên mẫu nhận diện và chính sách.
  * *EDR (Endpoint Detection and Response)*: Giám sát, thu thập dữ liệu hành vi liên tục trên máy trạm để phát hiện và hỗ trợ điều tra sự cố.
* **Bảo vệ Mạng (NDR)**: Giám sát lưu lượng, giao thức và các kết nối bất thường qua thiết bị mạng.
* **Trung tâm điều hành (SIEM / SOC)**: Hệ thống quản lý tập trung nhật ký (SIEM) và đội ngũ nhân sự vận hành giám sát (SOC).

### 3.2 Phát hiện tĩnh (Static Detection - Trước khi chạy)
* **Hash (SHA-256)**: Dấu vân tay số của tệp. Thay đổi 1 byte trong tệp sẽ làm thay đổi hoàn toàn giá trị Hash.
* **Signature & YARA**: Quy tắc tìm kiếm các chuỗi ký tự, dãy byte hoặc đặc điểm cấu trúc đặc trưng trong tệp.
* **PE Metadata & Entropy**:
  * *PE Metadata*: Thông tin về nhà phát hành, kiến trúc, danh sách hàm import.
  * *Entropy*: Phép đo độ khó dự đoán của byte dữ liệu. Entropy cao thường gợi ý dữ liệu đã bị nén hoặc mã hóa.

### 3.3 Phát hiện động (Behavioral Detection - Khi đang chạy)
* **Phân tích hành vi (Behavioral Analysis)**: Giám sát chuỗi sự kiện thực tế khi chương trình chạy (tạo tiến trình con, sửa Registry, mở kết nối mạng).
* **Sandbox**: Môi trường ảo hóa cách ly hoàn toàn để thực thi tệp tin thử nghiệm và ghi lại toàn bộ tác động.
* **Heuristic & Machine Learning**: Kết hợp nhiều chỉ số đánh giá và mô hình học máy để chấm điểm mức độ nghi ngờ của chuỗi hành vi.
* **False Positive (Dương tính giả) & False Negative (Âm tính giả)**:
  * *False Positive*: Cảnh báo nhầm một phần mềm sạch là mã độc.
  * *False Negative*: Bỏ sót, không phát hiện được mã độc thực tế.

### 3.4 Các điểm quan sát nội bộ của EDR (Telemetry Sources)
* **User-mode Hooks**: Chèn điểm kiểm soát vào các hàm Windows API trong tiến trình ứng dụng để ghi nhận tham số truyền vào.
* **Kernel Callbacks**: Driver tầng nhân đăng ký với Windows để nhận thông báo tức thì khi có sự kiện tạo Process, Thread, Image load hoặc Handle.
* **File Mini-filter**: Driver chuyên trách giám sát mọi thao tác đọc, ghi, xóa tệp tin trên hệ thống tệp.
* **ETW (Event Tracing for Windows) & ETW-TI**: Hạ tầng thu thập sự kiện hệ thống chuyên sâu dành cho các giải pháp bảo mật.
* **WFP (Windows Filtering Platform)**: Nền tảng can thiệp và kiểm soát các luồng dữ liệu mạng.

---

## MỤC 4: NHỮNG KỸ THUẬT CHE GIẤU HÀNH VI VÀ GIỚI HẠN

Phần này phân tích các kỹ thuật thường được thảo luận trong việc né tránh sự quan sát và giới hạn thực tế của chúng.

### 4.1 Mã máy, biến đổi dữ liệu và che giấu
* **Encoding (Base64) vs XOR**:
  * *Base64*: Chuyển đổi dữ liệu byte sang dạng ký tự văn bản để truyền tải.
  * *XOR*: Biến đổi các byte dữ liệu dựa trên phép toán logic với một khóa đơn giản.
  * *Lưu ý:* Cả hai không phải là giải pháp mã hóa bảo mật bí mật, dữ liệu phải giải mã về dạng gốc trước khi sử dụng.
* **Encryption (Mã hóa thực sự)**: Sử dụng thuật toán và khóa bảo mật để bảo vệ tính bí mật của dữ liệu.
* **Packing & Obfuscation**: Đóng gói nén dữ liệu hoặc làm rối mã nguồn/API name để chống phân tích tĩnh đơn giản.

### 4.2 Né tránh giám sát tầng hệ thống và giới hạn
* **API Hashing & Dynamic Resolve**: Ẩn danh sách tên hàm API tĩnh trong tệp bằng cách chuyển sang dạng chuỗi băm Hash và chỉ tra cứu địa chỉ hàm khi chạy.
* **Anti-Hooking & Direct Syscall**: Cố gắng lách qua các điểm canh gác (`User-mode hooks`) bằng cách tự dựng lời gọi chuyển tiếp thẳng xuống Kernel (`Syscall`).
  * *Giới hạn thực tế:* Không thể xóa bỏ dấu vết vì EDR vẫn thu thập thông tin từ Kernel Callbacks, Mini-filter hệ thống tệp và các luồng mạng.
* **Giả mạo nguồn gốc (PPID & Command-line Spoofing)**: Thay đổi thông tin hiển thị của Tiến trình cha (Parent PID) hoặc chuỗi tham số truyền vào tiến trình để làm sai lệch cây tiến trình.
* **AMSI & ETW Patching**: Can thiệp vào bộ nhớ của tiến trình hiện tại nhằm làm gián đoạn khả năng quét kịch bản của AMSI hoặc khả năng phát sự kiện của ETW.

### 4.3 Nhận diện môi trường phân tích (Anti-Sandbox / Anti-Debug)
* **Anti-Sandbox**: Kiểm tra dung lượng RAM, số nhân CPU, thời gian máy chạy (Uptime) hoặc đo tốc độ tính toán để phát hiện môi trường máy ảo phân tích.
* **Anti-Debug**: Sử dụng các hàm hệ thống để kiểm tra xem chương trình có đang bị gắn vào bởi công cụ gỡ lỗi (Debugger) hay không.
* *Giới hạn:* Các phép kiểm tra môi trường bằng ngưỡng cứng rất dễ gây báo nhầm (khiến chương trình ngừng chạy ngay cả trên các máy tính người dùng thật có cấu hình thấp).

---

## MỤC 5: ĐÁNH GIÁ ĐỘ TIN CẬY CỦA CÔNG CỤ MÃ NGUỒN MỞ

* **Rủi ro từ bản phát hành (Artifact Risk)**: Tệp EXE/DLL biên dịch sẵn tải về từ Internet có thể chứa thêm các đoạn mã khác với mã nguồn công khai trên kho lưu trữ (Repository).
* **Rủi ro chuỗi cung ứng (Dependency Risk)**: Các thư viện bên ngoài mà công cụ phụ thuộc vào có thể bị thay đổi nội dung hoặc bị chiếm quyền quản lý.
* **Quy trình thử nghiệm an toàn**:
  * Đọc và kiểm tra mã nguồn (đặc biệt là các đoạn truy cập mạng, tệp tin và quyền hệ thống).
  * Cố định phiên bản thư viện (`Pin dependency`) khi tự biên dịch.
  * Thử nghiệm công cụ trong môi trường máy ảo (VM) có bật tính năng lưu điểm phục hồi (`Snapshot`) và giới hạn kết nối mạng.

---

## MỤC 6: PHÂN TÍCH THỰC NGHIỆM VỚI BỘ MẪU LOADER V4

Phần này phân tích kết quả thử nghiệm thực tế dựa trên bộ tệp mẫu (`Stager(1).ps1`, `FinalLoaderV4(1).cs`, `Payload.cs`, `PayloadRevShell(1).cs`...).

### 6.1 Mô hình luồng hoạt động
1. **Khởi động**: `Stager.ps1` thực thi trong môi trường PowerShell, tải thư viện DLL .NET vào RAM.
2. **Xử lý và Nạp mã**: `FinalLoaderV4.cs` thực hiện giải mã dữ liệu chuỗi XOR, áp dụng các bước kiểm tra anti-sandbox, sau đó sử dụng `Assembly.Load(byte[])` để CLR nạp assembly ngay trong không gian bộ nhớ của tiến trình PowerShell hiện tại.
3. **Thực thi Payload**:
  * *Bản Payload dạng Beacon*: Thực hiện kết nối TCP đến máy chủ thử nghiệm, gửi thông tin môi trường (tên máy, người dùng, OS) rồi ngắt kết nối.
  * *Bản Payload dạng Reverse Shell*: Tạo một tiến trình con `cmd.exe`, chuyển hướng 3 luồng dữ liệu (Input, Output, Error) qua kết nối TCP để nhận lệnh từ xa.

### 6.2 Bảng tổng hợp chức năng các hàm trong Loader V4

| Hàm / Thành phần | Chức năng đọc được từ mã nguồn | Ý nghĩa & Giới hạn thực tế |
| :--- | :--- | :--- |
| `Dec()` | Giải mã các chuỗi Hex bằng phép toán XOR theo khóa cố định. | Giúp ẩn chuỗi ký tự rõ lúc phân tích tĩnh; tuy nhiên chuỗi vẫn lộ ra RAM khi chạy. |
| `SetupVehAmsi()` | Đăng ký Handler xử lý ngoại lệ (VEH) liên quan đến hardware breakpoint. | Nhắm tới việc can thiệp đường quét AMSI; đòi hỏi kiểm chứng cấu trúc CONTEXT chính xác. |
| `EtwBypass()` | Ghi đè mã lệnh ở đầu hàm liên quan ETW trong thư viện `ntdll.dll`. | Tác động 1 điểm phát sự kiện ở User-mode; không vô hiệu hóa được các nguồn ETW từ Kernel. |
| `AntiSandbox()` | Kiểm tra dung lượng RAM và đo thời gian vòng lặp tính toán. | Phép thử dùng ngưỡng cứng, độ tin cậy không cao vì dễ nhận diện nhầm máy thật cấu hình yếu. |
| `XorBytes()` | Biến đổi mảng byte dữ liệu bằng phép toán XOR lặp lại. | Đổi biểu diễn byte để tránh mẫu nhận diện tĩnh; không phải chuẩn mã hóa bí mật an toàn. |
| `RunAssembly()` | Sử dụng CLR để nạp assembly byte và gọi hàm qua Reflection. | Nạp managed assembly trong RAM tiến trình hiện tại; không phải kỹ thuật tiêm mã remote native injection. |

### 6.3 Đánh giá đối chiếu Artifacts và kết quả thực nghiệm
* **Tính nhất quán của Mã nguồn & Binary**:
  * Trong tệp mã nguồn `FinalLoaderV4(1).cs` tồn tại ký tự thừa ở phần gán thuộc tính `Dr7`, gây lỗi cú pháp nếu biên dịch nguyên trạng. Điều này chứng minh tệp `FinalLoaderV4.dll` được gửi kèm được biên dịch từ một phiên bản khác.
  * Tệp `Payload.dll` và `payload.bin` có cùng kích thước nhưng khi XOR ngược với khóa trong `Stager.ps1` thì dữ liệu không khớp hoàn toàn, phản ánh sự lệch phiên bản giữa các tệp artifact.
* **Kết luận thực nghiệm**:
  * Chuỗi thực thi thử nghiệm đã ghi nhận kết nối mạng thành công về máy nhận trên hệ điều hành Windows 10 Pro và Windows 11.
  * Sự hiện diện của các đoạn mã kỹ thuật trong Loader V4 không đồng nghĩa với việc vô hiệu hóa được toàn bộ các lớp phòng thủ của hệ điều hành. Các sự kiện tạo tiến trình (`cmd.exe`), nạp module CLR và tạo kết nối TCP vẫn phát sinh dấu vết và có thể được ghi nhận bởi EDR/Sysmon tùy thuộc vào cấu hình chính sách (Policy) thực tế của môi trường.
```

An editable Markdown file `bao_cao_nghien_cuu_loader_v4_edr.md` has been generated containing the structured analysis and concepts. Let me know if you need any adjustments or exports to other formats!