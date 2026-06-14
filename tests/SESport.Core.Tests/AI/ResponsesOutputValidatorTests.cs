using SESport.AI.Validation;

namespace SESport.Core.Tests.AI;

public class ResponsesOutputValidatorTests
{
   [Fact]
   public void NormalizeStructuredJsonOutputStripsJsonFences()
   {
      var output = """
         ```json
         {"ok":true}
         ```
         """;

      var normalized = ResponsesOutputValidator
         .NormalizeStructuredJsonOutput(output);

      Assert.Equal("{\"ok\":true}", normalized);
   }

   [Fact]
   public void NormalizeStructuredJsonOutputStripsInlineJsonFences()
   {
      var output = "```json {\"ok\":true} ```";

      var normalized = ResponsesOutputValidator
         .NormalizeStructuredJsonOutput(output);

      Assert.Equal("{\"ok\":true}", normalized);
   }

   [Fact]
   public void ValidateStructuredOutputStripsJsonFencesForJsonObject()
   {
      var output = """
         ```json
         {"ok":true}
         ```
         """;

      var validated = ResponsesOutputValidator.ValidateStructuredOutput(
         output,
         "json_object",
         null
      );

      Assert.Equal("{\"ok\":true}", validated);
   }

   [Fact]
   public void ValidateStructuredOutputStripsJsonFencesForSchema()
   {
      var output = """
         ```json
         {"ok":true}
         ```
         """;

      var validated = ResponsesOutputValidator.ValidateStructuredOutput(
         output,
         "json_schema",
         """{"type":"object"}"""
      );

      Assert.Equal("{\"ok\":true}", validated);
   }
}
