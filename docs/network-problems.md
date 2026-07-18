# Network problems

## 2026-07-18: Intermittent access to sesport.se

The site was intermittently unreachable from the same machine and network
used by the local Codex environment. Browser access failed temporarily, while
other attempts succeeded.

Observed locally:

- DNS consistently resolved `sesport.se` to `207.2.120.181`.
- Successful HTTPS requests returned HTTP 200.
- Normal requests usually completed in about 0.12 seconds.
- Some requests timed out before the TCP connection was established.
- A retry could succeed after about four seconds, followed by normal response
  times.
- Only one IPv4 address was observed; no IPv6 address was published.

This points to an intermittent network, firewall, connection-limit, reverse
proxy, or server-side availability problem. It was not reproducible as a
continuous application failure, and was not specific to Chrome.

Recommended follow-up if the problem returns:

1. Correlate the exact timestamps with reverse-proxy and server logs.
2. Check CPU, memory, connection limits, firewall rules, and conntrack state.
3. Check whether the hosting provider reports packet loss or network events.
4. Consider monitoring from more than one external location.

The SSH log also showed an automated failed login for invalid user `admin`.
This appears unrelated to the web availability issue. Fail2ban should be
enabled for repeated SSH authentication failures if it is not already active.
