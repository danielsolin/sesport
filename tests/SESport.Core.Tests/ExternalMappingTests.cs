namespace SESport.Core.Tests;

public class ExternalMappingTests
{
   [Fact]
   public void NhlPlayerIdCanMapToInternalPersonId()
   {
      var nhl = new Source(new SourceId("source:nhl"), "NHL");
      var mapping = new ExternalMapping(
         nhl,
         new ExternalEntityId("player:8477406"),
         "person:william-karlsson"
      );

      Assert.Equal("source:nhl", mapping.Source.Id.Value);
      Assert.Equal("player:8477406", mapping.ExternalId.Value);
      Assert.Equal("person:william-karlsson", mapping.InternalId);
   }
}
