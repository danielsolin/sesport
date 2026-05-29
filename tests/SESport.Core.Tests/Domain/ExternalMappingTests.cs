namespace SESport.Core.Tests.Domain;

public class ExternalMappingTests
{
   [Fact]
   public void NhlPlayerIdCanMapToInternalPersonId()
   {
      var nhl = new Source(new SourceId("source:nhl"), "NHL");
      var mapping = new ExternalMapping(
         nhl,
         new ExternalEntityId("player:external-william-karlsson"),
         new InternalEntityReference(
            InternalEntityKind.Person,
            "person:william-karlsson"
         )
      );

      Assert.Equal("source:nhl", mapping.Source.Id.Value);
      Assert.Equal(
         "player:external-william-karlsson",
         mapping.ExternalId.Value
      );
      Assert.Equal(InternalEntityKind.Person, mapping.InternalEntity.Kind);
      Assert.Equal("person:william-karlsson", mapping.InternalEntity.Id);
   }
}
