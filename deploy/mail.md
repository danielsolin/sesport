# Direct outbound mail

The production and development web services send transactional mail through
a local Postfix instance. Postfix delivers directly to recipient MX servers;
HostUp's smarthost relay is not used by the application.

The VPS uses a public server address and sends as `sesport.se`.

## Mail flow

```text
SESport -> 127.0.0.1:25 -> Postfix -> recipient MX
                              |
                              -> OpenDKIM on 127.0.0.1:8891
```

Postfix is bound to loopback only. The VPS therefore does not expose an
inbound SMTP service and cannot be used as a public open relay. Inbound mail
continues to use the Cloudflare MX records.

## Packages

Install Postfix and OpenDKIM on the web host:

```bash
sudo apt-get update
sudo apt-get install -y postfix opendkim opendkim-tools
```

The Postfix installation may ask for a mail configuration type. The local
configuration below is authoritative.

## Application settings

Both `sesport.service` and `sesport-dev.service` load the repository-local
`.env` file. The SMTP settings must point to local Postfix:

```text
Smtp__Host=127.0.0.1
Smtp__Port=25
Smtp__UseSsl=false
Smtp__FromAddress=info@sesport.se
Smtp__FromName=sesport
```

The `.env` file is host-local and must not be committed.

## Postfix

Apply the important settings with `postconf`:

```bash
local_relay_restrictions='permit_mynetworks, reject_unauth_destination'

sudo postconf -e \
  'myhostname = sesport.se' \
  'mydomain = sesport.se' \
  'myorigin = sesport.se' \
  'mydestination = $myhostname, localhost.$mydomain, localhost' \
  'inet_interfaces = loopback-only' \
  'inet_protocols = ipv4' \
  'mynetworks = 127.0.0.0/8 [::1]/128' \
  'relayhost =' \
  'smtp_bind_address = <public-ip>' \
  'smtp_tls_security_level = may' \
  'smtp_tls_loglevel = 1' \
  'smtp_tls_CAfile = /etc/ssl/certs/ca-certificates.crt' \
  'smtpd_milters = inet:127.0.0.1:8891' \
  'non_smtpd_milters = inet:127.0.0.1:8891' \
  'milter_default_action = accept' \
  'milter_protocol = 6' \
  "smtpd_recipient_restrictions = $local_relay_restrictions" \
  "smtpd_relay_restrictions = $local_relay_restrictions" \
  'disable_vrfy_command = yes'
```

`smtp_bind_address` makes direct deliveries originate from the public server
address. Replace `<public-ip>` with the server's public address.
`relayhost` must remain empty.

## OpenDKIM

Generate one 2048-bit key for the sending domain:

```bash
sudo install -d -o opendkim -g opendkim -m 0750 \
  /etc/opendkim/keys/sesport.se
sudo opendkim-genkey -b 2048 -r -d sesport.se \
  -D /etc/opendkim/keys/sesport.se -s sesport
sudo chown opendkim:opendkim \
  /etc/opendkim/keys/sesport.se/sesport.private \
  /etc/opendkim/keys/sesport.se/sesport.txt
sudo chmod 600 /etc/opendkim/keys/sesport.se/sesport.private
sudo chmod 644 /etc/opendkim/keys/sesport.se/sesport.txt
```

The private key must remain on the VPS and must never be committed or
published. The public `sesport.txt` value is used for the DNS TXT record.
Depending on the package version, `opendkim-genkey` may write the public
record in BIND zone-file format. Copy only the text between the quotes,
concatenating any quoted fragments. Do not copy the selector, `IN TXT`,
parentheses, or comments.

Configure `/etc/opendkim.conf` with the following values:

```text
Syslog                  yes
SyslogSuccess           yes
Canonicalization        relaxed/simple
OversignHeaders         From
UserID                  opendkim
UMask                   007
Socket                  inet:8891@localhost
PidFile                 /run/opendkim/opendkim.pid
Mode                    sv
KeyTable               refile:/etc/opendkim/KeyTable
SigningTable            refile:/etc/opendkim/SigningTable
ExternalIgnoreList     refile:/etc/opendkim/TrustedHosts
InternalHosts          refile:/etc/opendkim/TrustedHosts
TrustAnchorFile        /usr/share/dns/root.key
```

`/etc/opendkim/KeyTable`:

```text
sesport._domainkey.sesport.se sesport.se:sesport:/etc/opendkim/keys/sesport.se/sesport.private
```

`/etc/opendkim/SigningTable`:

```text
*@sesport.se sesport._domainkey.sesport.se
```

`/etc/opendkim/TrustedHosts`:

```text
127.0.0.1
::1
localhost
```

Restart both mail services after changing their configuration:

```bash
sudo postfix check
sudo systemctl restart opendkim postfix
sudo systemctl enable opendkim postfix
```

## DNS

The following records are required in the authoritative DNS zone for
`sesport.se`:

| Name | Type | Value |
| --- | --- | --- |
| `sesport.se` | A | `<public-ip>` |
| `sesport.se` | TXT | SPF record below |
| `sesport._domainkey` | TXT | Public value from `sesport.txt` |
| `_dmarc` | TXT | `v=DMARC1; p=none` |

The current SPF record is:

```text
v=spf1 include:_spf.mx.cloudflare.net include:spf.hostup.se ip4:<public-ip> ~all
```

Keep one SPF TXT record for the domain. The HostUp include can be removed
later if the smarthost fallback is no longer needed.

The current reverse DNS is managed by HostUp and must point to:

```text
<public-ip> -> sesport.se
```

The `sesport.se` A record must resolve back to the same address before the
PTR value is saved at HostUp.

HostUp blocks outbound TCP port 25 by default. Request that port 25 be
opened for `<public-ip>` before enabling direct delivery. Verify outbound
connectivity to at least one recipient MX server after the change.

The `_hostup` TXT record is only used to authorize the HostUp smarthost. It
does not affect direct Postfix delivery and may remain as a fallback:

```text
_hostup TXT "v=mc1 auth=<public-ip>"
```

The MX records remain Cloudflare's. Do not point the domain MX at this VPS
unless inbound mail handling is added deliberately.

## Verification

Check the local listeners and service state:

```bash
systemctl is-active opendkim postfix sesport sesport-dev
systemctl is-enabled opendkim postfix
sudo ss -ltnp | rg '127\.0\.0\.1:(25|8891)\b'
sudo postfix check
```

Validate the local DKIM key and public DNS record:

```bash
sudo opendkim-testkey -d sesport.se -s sesport \
  -k /etc/opendkim/keys/sesport.se/sesport.private -vv
dig +short TXT sesport.se
dig +short TXT sesport._domainkey.sesport.se
dig +short TXT _dmarc.sesport.se
dig +short -x <public-ip>
```

The queue should normally be empty after delivery:

```bash
sudo postqueue -p
```

Inspect delivery and signing when troubleshooting:

```bash
sudo journalctl -u postfix -u opendkim -n 100 --no-pager
```

An accepted direct delivery normally includes a Postfix `status=sent` line,
TLS details, and an OpenDKIM line saying that a `DKIM-Signature` was added.
