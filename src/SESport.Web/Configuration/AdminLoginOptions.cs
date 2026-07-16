namespace SESport.Web.Configuration;

public sealed record AdminLoginOptions
{
   public string Password { get; init; } = string.Empty;
}
