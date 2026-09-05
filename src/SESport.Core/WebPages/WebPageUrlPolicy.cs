using SESport.Core.Configuration;

using System.Net;
using System.Net.Sockets;

namespace SESport.AI.WebPages;

public static class WebPageUrlPolicy
{
   public static string GetCanonicalCacheKey(Uri absoluteUrl)
   {
      var builder = new UriBuilder(absoluteUrl)
      {
         Fragment = string.Empty
      };

      return builder.Uri.AbsoluteUri;
   }

   public static bool TryValidate(
      string url,
      out Uri absoluteUrl,
      out string error
   )
   {
      absoluteUrl = null!;
      error = string.Empty;

      if(string.IsNullOrWhiteSpace(url))
      {
         error = "Missing page URL.";
         return false;
      }

      if(url.Length > WebPageFetchDefaults.MaximumUrlLength)
      {
         error = "Page URL is too long.";
         return false;
      }

      if(!Uri.TryCreate(url, UriKind.Absolute, out var parsedUrl))
      {
         error = "Invalid page URL.";
         return false;
      }

      absoluteUrl = parsedUrl;

      if(absoluteUrl.Scheme is not ("http" or "https"))
      {
         error = "Page URL must use http or https.";
         return false;
      }

      if(string.IsNullOrWhiteSpace(absoluteUrl.Host))
      {
         error = "Page URL is missing a host.";
         return false;
      }

      if(IsBlockedHost(absoluteUrl.Host))
      {
         error = "Page URL host is not allowed.";
         return false;
      }

      return true;
   }

   internal static bool IsBlockedHost(string host)
   {
      var normalizedHost = host.TrimEnd('.');

      if(string.Equals(
         normalizedHost,
         "localhost",
         StringComparison.OrdinalIgnoreCase
      ) ||
         normalizedHost.EndsWith(
            ".localhost",
            StringComparison.OrdinalIgnoreCase
         ) ||
         string.Equals(
            normalizedHost,
            "metadata.google.internal",
            StringComparison.OrdinalIgnoreCase
         ))
      {
         return true;
      }

      return IPAddress.TryParse(normalizedHost, out var address) &&
         !IsPublicAddress(address);
   }

   private static bool IsPublicAddress(IPAddress address)
   {
      if(address.IsIPv4MappedToIPv6)
      {
         address = address.MapToIPv4();
      }

      if(address.AddressFamily == AddressFamily.InterNetwork)
      {
         return IsPublicIpv4Address(address);
      }

      if(address.AddressFamily != AddressFamily.InterNetworkV6 ||
         IPAddress.IPv6Any.Equals(address) ||
         IPAddress.IPv6Loopback.Equals(address) ||
         address.IsIPv6LinkLocal ||
         address.IsIPv6Multicast ||
         address.IsIPv6SiteLocal)
      {
         return false;
      }

      var bytes = address.GetAddressBytes();

      return (bytes[0] & 0xfe) != 0xfc;
   }

   private static bool IsPublicIpv4Address(IPAddress address)
   {
      var bytes = address.GetAddressBytes();
      var first = bytes[0];
      var second = bytes[1];

      return first is not 0 and not 10 and not 127 &&
         !(first == 100 && second is >= 64 and <= 127) &&
         !(first == 169 && second == 254) &&
         !(first == 172 && second is >= 16 and <= 31) &&
         !(first == 192 && second == 0) &&
         !(first == 192 && second == 168) &&
         !(first == 198 && second is 18 or 19) &&
         first is < 224;
   }
}
