using System.Text.Json;

using SESport.AI.Interfaces;
using SESport.Core.AI;
using SESport.Core.Domain;
using SESport.Data;

namespace SESport.Web.Services;

public sealed class PersonFactsService(
   AdminRepository repository,
   IAiJobRunner aiJobRunner
)
{
   public async Task<Guid?> QueueAsync(
      Guid entityId,
      CancellationToken cancellationToken
   )
   {
      var entity = await repository.GetEntityForEditAsync(
         entityId,
         cancellationToken
      );

      if(entity is null)
      {
         return null;
      }

      if(!string.Equals(
         entity.EntityTypeId,
         TrackedEntityTypeIds.Person,
         StringComparison.OrdinalIgnoreCase
      ))
      {
         throw new PersonFactsValidationException(
            "Person facts jobs can only be queued for person entities."
         );
      }

      var sports = await repository.GetReferenceRowsAsync(
         "sports",
         cancellationToken
      );
      var sport = sports.FirstOrDefault(option =>
         string.Equals(
            option.Id,
            entity.SportId,
            StringComparison.OrdinalIgnoreCase
         )
      )?.Label ?? entity.SportId;
      var linkedEntities = await repository.GetEntityLinkOptionsByIdsAsync(
         entity.LinkedEntityIds,
         entity.Id,
         cancellationToken
      );

      var inputPayloadJson = JsonSerializer.Serialize(
         new
         {
            name = entity.CanonicalName,
            sport,
            org_names = string.Join(
               ", ",
               linkedEntities.Select(linkedEntity => linkedEntity.Name)
            )
         }
      );

      return await aiJobRunner.QueueAsync(
         new AiJobRequest(
            AiJobIds.FindPersonFacts,
            inputPayloadJson,
            entityId.ToString()
         ),
         cancellationToken
      );
   }
}
