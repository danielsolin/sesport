using SESport.Core.AI;

namespace SESport.Core.Tests.AI;

public sealed class AiJobIdsTests
{
   [Fact]
   public void FindParticipantsStartTargetsActivities()
   {
      Assert.Equal(
         AiJobTargetType.Activity,
         AiJobIds.GetTargetType(AiJobIds.FindParticipantsStart)
      );
   }

   [Fact]
   public void FindParticipantsResultTargetsActivities()
   {
      Assert.Equal(
         AiJobTargetType.Activity,
         AiJobIds.GetTargetType(AiJobIds.FindParticipantsResult)
      );
   }

   [Fact]
   public void FindActivityGroupFactsTargetsActivityGroups()
   {
      Assert.Equal(
         AiJobTargetType.ActivityGroup,
         AiJobIds.GetTargetType(AiJobIds.FindActivityGroupFacts)
      );
   }
}
