namespace SESport.Core.AIActivityTeasers;

public interface IActivityTeaserGenerator
{
   Task<string> GenerateAsync(
      ActivityTeaserRequest request,
      CancellationToken cancellationToken
   );
}
