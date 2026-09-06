using ModelContextProtocol.Protocol;

namespace SESport.MCP.Support;

internal static class McpToolArgumentValidation
{
   internal static McpRequestFilter<
      CallToolRequestParams,
      CallToolResult
   > CreateFilter()
   {
      return next => async (request, cancellationToken) =>
      {
         if(TryGetMissingRequiredArgument(
            request,
            out var argumentName
         ))
         {
            return new CallToolResult
            {
               IsError = true,
               Content =
               [
                  new TextContentBlock
                  {
                     Text =
                        $"Missing required argument '{argumentName}'."
                  }
               ]
            };
         }

         return await next(
            request,
            cancellationToken
         );
      };
   }

   private static bool TryGetMissingRequiredArgument(
      RequestContext<CallToolRequestParams> request,
      out string argumentName
   )
   {
      argumentName = string.Empty;

      if(request.MatchedPrimitive is not McpServerTool tool ||
         !tool.ProtocolTool.InputSchema.TryGetProperty(
            "required",
            out var required
         ) ||
         required.ValueKind != JsonValueKind.Array)
      {
         return false;
      }

      var arguments = request.Params.Arguments;
      foreach(var requiredArgument in required.EnumerateArray())
      {
         if(requiredArgument.ValueKind != JsonValueKind.String)
         {
            continue;
         }

         var name = requiredArgument.GetString();
         if(name is not null &&
            (arguments is null || !arguments.ContainsKey(name)))
         {
            argumentName = name;
            return true;
         }
      }

      return false;
   }
}
