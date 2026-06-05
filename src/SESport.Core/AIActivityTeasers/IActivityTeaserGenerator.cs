namespace SESport.Core.AIActivityTeasers;

public interface IActivityTeaserGenerator
{
   Task<ActivityTeaserGenerationResult> GenerateAsync(
      ActivityTeaserRequest request,
      CancellationToken cancellationToken
   );
}
