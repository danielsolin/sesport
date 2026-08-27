using System.Text.Json;

using SESport.AI.Protocols;

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

   [Fact]
   public void ValidateStructuredOutputSupportsNullableSchemaTypes()
   {
      var schema = """
         {
            "type": "object",
            "required": ["birthdate", "height", "weight"],
            "properties": {
               "birthdate": { "type": ["string", "null"] },
               "height": { "type": ["integer", "null"] },
               "weight": { "type": ["integer", "null"] }
            },
            "additionalProperties": false
         }
         """;

      var validated = ResponsesOutputValidator.ValidateStructuredOutput(
         "{\"birthdate\":null,\"height\":201,\"weight\":null}",
         "json_schema",
         schema
      );

      Assert.Equal(
         "{\"birthdate\":null,\"height\":201,\"weight\":null}",
         validated
      );
   }

   [Fact]
   public void ValidateStructuredOutputRejectsEmptyArrayWhenMinItemsSet()
   {
      var exception = Assert.Throws<InvalidOperationException>(() =>
         ResponsesOutputValidator.ValidateStructuredOutput(
            """
            {"Sources":[],"Participants":[],"Participation":"No"}
            """,
            "json_schema",
            """
            {
               "type": "object",
               "properties": {
                  "Sources": {
                     "type": "array",
                     "minItems": 1
                  }
               }
            }
            """
         )
      );

      Assert.IsType<JsonException>(exception.InnerException);
      Assert.Contains("at least 1 item", exception.InnerException!.Message);
   }

   [Fact]
   public void ValidateStructuredOutputDescribesEmptyOutput()
   {
      var exception = Assert.Throws<InvalidOperationException>(() =>
         ResponsesOutputValidator.ValidateStructuredOutput(
            "",
            "json_schema",
            """{"type":"object"}"""
         )
      );

      Assert.Equal(
         ResponsesOutputValidator.EmptyOutputMessage,
         exception.Message
      );
   }
}
