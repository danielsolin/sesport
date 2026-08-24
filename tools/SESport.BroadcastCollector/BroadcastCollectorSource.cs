using SESport.Core.Domain;
using SESport.Core.Formatting;
using SESport.Core.Sources;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SESport.Tools.BroadcastCollector;

internal sealed class BroadcastCollectorSource : IBroadcastCollectorSource
{
   private const string SourceKey = "tvnu";
   private const string SiteRootUrl = "https://www.tv.nu";
   private const string BaseUrl = SiteRootUrl + "/sport";
   private const string ApiUrl = "https://web-api.tv.nu/sport/schedule";
   private const string SearchUrl = "https://web-api.tv.nu/search";
   private const string ChannelUrl = "https://web-api.tv.nu/channel";
   private const int MaxApiPages = 30;

   private static readonly JsonSerializerOptions CompactJsonOptions = new()
   {
      WriteIndented = false
   };

   private static readonly string[] SupplementalChannelQueries =
   [
      "V Sport Live",
      "V Sport Football Live",
      "TV4 Sport Live"
   ];

   public async Task<BroadcastCollectorDownloadResult> DownloadAsync(
      DateOnly date,
      string outputDirectory,
      CancellationToken cancellationToken
   )
   {
      var sourceOutputDirectory = ResolveSourceOutputDirectory(
         outputDirectory
      );
      Directory.CreateDirectory(sourceOutputDirectory);

      var outputPath = Path.Combine(
         sourceOutputDirectory,
         $"{DateDisplay.Format(date)}.html"
      );

      using var httpClient = CreateHttpClient(date);
      var url = $"{BaseUrl}?datum={DateDisplay.Format(date)}";
      var initialHtml = await httpClient.GetStringAsync(
         url,
         cancellationToken
      );

      var html = await BuildHtmlFromApiAsync(
         httpClient,
         initialHtml,
         date,
         cancellationToken
      );
      await File.WriteAllTextAsync(outputPath, html, cancellationToken);

      return new BroadcastCollectorDownloadResult(outputPath, html.Length);
   }

   private static HttpClient CreateHttpClient(DateOnly date)
   {
      var httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
         "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 " +
         "(KHTML, like Gecko) Chrome/137.0.0.0 Safari/537.36"
      );
      httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html");
      httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/plain, */*");
      httpClient.DefaultRequestHeaders.Referrer = new Uri(
         $"{BaseUrl}?datum={DateDisplay.Format(date)}"
      );

      return httpClient;
   }

   private static async Task<string> BuildHtmlFromApiAsync(
      HttpClient httpClient,
      string initialHtml,
      DateOnly date,
      CancellationToken cancellationToken
   )
   {
      var initialStateJson = ExtractInitialStateJson(initialHtml) ??
         throw new InvalidOperationException(
            "Unable to extract initial state."
         );

      var stateNode = JsonNode.Parse(initialStateJson)?.AsObject() ??
         throw new InvalidOperationException(
            "Unable to parse initial state."
         );

      var modules = ExtractStandardModules(initialStateJson);
      var scheduleItems = new JsonArray();
      var knownIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      var zeroStreak = 0;
      var lastPageNumber = 0;
      int? totalCount = null;

      for(var pageNumber = 1; pageNumber <= MaxApiPages; pageNumber++)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var apiPage = await FetchSchedulePageAsync(
            httpClient,
            date,
            modules,
            pageNumber,
            cancellationToken
         );
         var addedCount = AppendScheduleItems(
            apiPage.Items,
            scheduleItems,
            knownIds
         );

         lastPageNumber = pageNumber;
         totalCount ??= apiPage.TotalCount;

         Console.Error.WriteLine(
            $"[source={SourceKey}] api_page={pageNumber} " +
            $"added={addedCount} total={knownIds.Count} " +
            $"has_next={FormatNullable(apiPage.HasNext)} " +
            $"total_count={FormatNullable(totalCount)}"
         );

         if(apiPage.HasNext == false)
         {
            break;
         }

         if(addedCount == 0)
         {
            zeroStreak++;
            if(zeroStreak >= 2)
            {
               break;
            }
         }
         else
         {
            zeroStreak = 0;
         }
      }

      if(scheduleItems.Count == 0)
      {
         AddInitialScheduleFallback(
            stateNode,
            scheduleItems,
            knownIds
         );
      }

      await AppendSupplementalChannelSchedulesAsync(
         httpClient,
         date,
         scheduleItems,
         knownIds,
         cancellationToken
      );

      await EnrichPlayProviderUrlsAsync(
         httpClient,
         scheduleItems,
         cancellationToken
      );

      stateNode["sportPageSchedule"] = scheduleItems;
      var mergedStateJson = stateNode.ToJsonString(
         CompactJsonOptions
      );

      Console.Error.WriteLine(
         $"[source={SourceKey}] api_exit pages={lastPageNumber} " +
         $"items={knownIds.Count} total_count={FormatNullable(totalCount)}"
      );

      var html = ReplaceInitialStateJson(initialHtml, mergedStateJson) ??
         BuildSyntheticHtml(mergedStateJson);

      return InjectSyntheticRows(
         html,
         BuildSyntheticRows(scheduleItems)
      );
   }

   private static async Task<ScheduleApiPage> FetchSchedulePageAsync(
      HttpClient httpClient,
      DateOnly date,
      IReadOnlyList<string> modules,
      int pageNumber,
      CancellationToken cancellationToken
   )
   {
      var apiUrl = BuildScheduleApiUrl(date, modules, pageNumber);
      var responseText = await httpClient.GetStringAsync(
         apiUrl,
         cancellationToken
      );

      return ExtractSchedulePage(responseText, apiUrl);
   }

   private static void AddInitialScheduleFallback(
      JsonObject stateNode,
      JsonArray scheduleItems,
      ISet<string> knownIds
   )
   {
      if(stateNode["sportPageSchedule"] is not JsonArray initialSchedule)
      {
         return;
      }

      var addedCount = AppendScheduleItems(
         initialSchedule,
         scheduleItems,
         knownIds
      );

      Console.Error.WriteLine(
         $"[source={SourceKey}] api_fallback " +
         $"initial_state_added={addedCount}"
      );
   }

   private static int AppendScheduleItems(
      JsonArray sourceItems,
      JsonArray destinationItems,
      ISet<string> knownIds
   )
   {
      var addedCount = 0;

      foreach(var item in sourceItems.OfType<JsonObject>())
      {
         var added = AppendScheduleItem(
            item.DeepClone().AsObject(),
            destinationItems,
            knownIds
         );

         if(!added)
         {
            continue;
         }

         addedCount++;
      }

      return addedCount;
   }

   private static bool AppendScheduleItem(
      JsonObject item,
      JsonArray destinationItems,
      ISet<string> knownIds
   )
   {
      var itemId = item["id"]?.ToString();

      if(string.IsNullOrWhiteSpace(itemId))
      {
         itemId = item.ToJsonString();
      }

      if(!knownIds.Add(itemId))
      {
         MergeDuplicateScheduleItem(
            destinationItems,
            itemId,
            item
         );

         return false;
      }

      destinationItems.Add(item);
      return true;
   }

   private static void MergeDuplicateScheduleItem(
      JsonArray destinationItems,
      string itemId,
      JsonObject sourceItem
   )
   {
      var targetItem = destinationItems
         .OfType<JsonObject>()
         .FirstOrDefault(item => string.Equals(
            item["id"]?.ToString(),
            itemId,
            StringComparison.OrdinalIgnoreCase
         ));

      if(targetItem is null)
      {
         return;
      }

      MergeScheduleArray(targetItem, sourceItem, "broadcasts");
      MergeScheduleArray(targetItem, sourceItem, "playEpisodes");
   }

   private static void MergeScheduleArray(
      JsonObject targetItem,
      JsonObject sourceItem,
      string propertyName
   )
   {
      if(
         targetItem[propertyName] is not JsonArray targetArray ||
         sourceItem[propertyName] is not JsonArray sourceArray
      )
      {
         return;
      }

      var knownKeys = targetArray
         .OfType<JsonObject>()
         .Select(CreateScheduleEntryMergeKey)
         .ToHashSet(StringComparer.OrdinalIgnoreCase);

      foreach(var sourceEntry in sourceArray.OfType<JsonObject>())
      {
         if(!knownKeys.Add(CreateScheduleEntryMergeKey(sourceEntry)))
         {
            continue;
         }

         targetArray.Add(sourceEntry.DeepClone());
      }
   }

   private static string CreateScheduleEntryMergeKey(JsonObject item)
   {
      var provider =
         item["channel"] as JsonObject ??
         item["playProvider"] as JsonObject;
      var providerKey = provider?["slug"]?.ToString() ??
         provider?["name"]?.ToString() ??
         string.Empty;
      var startsAt = item["startTime"]?.ToString() ??
         item["streamStart"]?.ToString() ??
         string.Empty;

      return string.Join(
         "|",
         providerKey,
         startsAt,
         item["id"]?.ToString() ?? string.Empty
      );
   }

   private static async Task<int> AppendSupplementalChannelSchedulesAsync(
      HttpClient httpClient,
      DateOnly date,
      JsonArray scheduleItems,
      ISet<string> knownIds,
      CancellationToken cancellationToken
   )
   {
      var slugs = await FetchSupplementalChannelSlugsAsync(
         httpClient,
         cancellationToken
      );
      var totalAdded = 0;

      foreach(var slug in slugs)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var channelSchedule = await FetchChannelScheduleAsync(
            httpClient,
            slug,
            date,
            cancellationToken
         );
         var addedCount = AppendChannelScheduleItems(
            channelSchedule,
            date,
            scheduleItems,
            knownIds
         );

         totalAdded += addedCount;

         Console.Error.WriteLine(
            $"[source={SourceKey}] channel_schedule slug={slug} " +
            $"items={channelSchedule.Items.Count} added={addedCount} " +
            $"total={knownIds.Count}"
         );
      }

      Console.Error.WriteLine(
         $"[source={SourceKey}] channel_schedule_exit " +
         $"channels={slugs.Count} " +
         $"added={totalAdded}"
      );

      return totalAdded;
   }

   private static async Task<IReadOnlyList<string>>
      FetchSupplementalChannelSlugsAsync(
         HttpClient httpClient,
         CancellationToken cancellationToken
      )
   {
      var slugs = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

      foreach(var query in SupplementalChannelQueries)
      {
         cancellationToken.ThrowIfCancellationRequested();

         var responseText = await httpClient.GetStringAsync(
            BuildSearchUrl(query),
            cancellationToken
         );
         var addedCount = AddChannelSearchSlugs(responseText, slugs);

         Console.Error.WriteLine(
            $"[source={SourceKey}] channel_search query=\"{query}\" " +
            $"added={addedCount} total={slugs.Count}"
         );
      }

      return slugs.ToList();
   }

   private static int AddChannelSearchSlugs(
      string json,
      ISet<string> slugs
   )
   {
      var rootNode = JsonNode.Parse(json);

      if(
         rootNode is not JsonObject rootObject ||
         rootObject["data"] is not JsonArray items
      )
      {
         return 0;
      }

      var addedCount = 0;

      foreach(var item in items.OfType<JsonObject>())
      {
         if(!string.Equals(
            item["type"]?.ToString(),
            "channel",
            StringComparison.OrdinalIgnoreCase
         ))
         {
            continue;
         }

         if(item["mainItem"] is not JsonObject channel)
         {
            continue;
         }

         var slug = NormalizeOptionalText(channel["slug"]?.ToString());

         if(slug is null || !IsSupplementalChannelSlug(slug))
         {
            continue;
         }

         if(slugs.Add(slug))
         {
            addedCount++;
         }
      }

      return addedCount;
   }

   private static bool IsSupplementalChannelSlug(string slug)
   {
      return slug.Contains("sport-live", StringComparison.OrdinalIgnoreCase) ||
         slug.Contains("v-sport-live", StringComparison.OrdinalIgnoreCase);
   }

   private static async Task<ChannelSchedule> FetchChannelScheduleAsync(
      HttpClient httpClient,
      string slug,
      DateOnly date,
      CancellationToken cancellationToken
   )
   {
      var responseText = await httpClient.GetStringAsync(
         BuildChannelScheduleUrl(slug, date),
         cancellationToken
      );

      return ExtractChannelSchedule(responseText, slug);
   }

   private static ChannelSchedule ExtractChannelSchedule(
      string json,
      string sourceLabel
   )
   {
      var rootNode = JsonNode.Parse(json);

      if(
         rootNode is JsonObject rootObject &&
         rootObject["data"] is JsonObject dataObject &&
         dataObject["broadcasts"] is JsonArray broadcasts
      )
      {
         var channelName = NormalizeOptionalText(
            dataObject["name"]?.ToString()
         );
         var channelSlug = NormalizeOptionalText(
            dataObject["slug"]?.ToString()
         );

         if(
            string.IsNullOrWhiteSpace(channelName) ||
            string.IsNullOrWhiteSpace(channelSlug)
         )
         {
            throw new InvalidOperationException(
               "Unable to locate channel metadata in the channel schedule. " +
               $"Source: {sourceLabel}."
            );
         }

         return new ChannelSchedule(
            channelName,
            channelSlug,
            dataObject["themedLogo"]?.DeepClone(),
            broadcasts
         );
      }

      throw new InvalidOperationException(
         "Unable to locate broadcasts in the channel schedule. " +
         $"Source: {sourceLabel}. Prefix: {json[..Math.Min(json.Length, 200)]}"
      );
   }

   private static int AppendChannelScheduleItems(
      ChannelSchedule channelSchedule,
      DateOnly date,
      JsonArray scheduleItems,
      ISet<string> knownIds
   )
   {
      var addedCount = 0;

      foreach(var item in channelSchedule.Items.OfType<JsonObject>())
      {
         var scheduleItem = TryCreateScheduleItemFromChannelBroadcast(
            item,
            channelSchedule,
            date
         );

         if(scheduleItem is null)
         {
            continue;
         }

         if(AppendScheduleItem(scheduleItem, scheduleItems, knownIds))
         {
            addedCount++;
         }
      }

      return addedCount;
   }

   private static JsonObject? TryCreateScheduleItemFromChannelBroadcast(
      JsonObject item,
      ChannelSchedule channelSchedule,
      DateOnly date
   )
   {
      if(!string.Equals(
         item["type"]?.ToString(),
         "sport",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         return null;
      }

      var id = NormalizeOptionalText(item["id"]?.ToString());
      var title = NormalizeOptionalText(item["title"]?.ToString());

      if(
         string.IsNullOrWhiteSpace(id) ||
         string.IsNullOrWhiteSpace(title) ||
         item["broadcast"] is not JsonObject broadcast
      )
      {
         return null;
      }

      var startsAtValue = TryGetLong(broadcast["startTime"]);
      var endsAtValue = TryGetLong(broadcast["endTime"]);

      if(
         startsAtValue is null ||
         endsAtValue is null ||
         endsAtValue <= startsAtValue
      )
      {
         return null;
      }

      var categories = GetChannelScheduleCategories(item);

      if(categories.Count == 0)
      {
         return null;
      }

      var category = categories[0];
      var broadcastItem = new JsonObject
      {
         ["id"] = id,
         ["startTime"] = startsAtValue,
         ["endTime"] = endsAtValue,
         ["isRerun"] = TryGetBool(item["isRerun"]) ?? false,
         ["channel"] = CreateChannelNode(channelSchedule),
         ["type"] = "sport"
      };

      var broadcasts = new JsonArray
      {
         broadcastItem
      };

      return new JsonObject
      {
         ["type"] = "sport",
         ["id"] = id,
         ["isPlay"] = false,
         ["isMovie"] = false,
         ["isSeries"] = false,
         ["isRerun"] = TryGetBool(item["isRerun"]) ?? false,
         ["isRecurring"] = TryGetBool(item["isRecurring"]) ?? false,
         ["isLive"] = TryGetBool(item["isLive"]) ?? false,
         ["title"] = title,
         ["description"] = NormalizeOptionalText(
            item["description"]?.ToString()
         ) ?? string.Empty,
         ["genreNames"] = CloneArrayOrEmpty(item["genreNames"]),
         ["genres"] = CloneArrayOrEmpty(item["genres"]),
         ["genreGroupNames"] = CloneArrayOrEmpty(item["genreGroupNames"]),
         ["genreGroups"] = CloneArrayOrEmpty(item["genreGroups"]),
         ["playProviders"] = CloneArrayOrEmpty(item["playProviders"]),
         ["subtitle"] = category,
         ["sportGroup"] = category,
         ["sport"] = category,
         ["eventTime"] = startsAtValue,
         ["scheduleDate"] = DateDisplay.Format(date),
         ["tournament"] = string.Empty,
         ["tournamentSlug"] = string.Empty,
         ["odds"] = CloneArrayOrEmpty(item["odds"]),
         ["broadcasts"] = broadcasts,
         ["playEpisodes"] = new JsonArray(),
         ["tags"] = CloneArrayOrEmpty(item["tags"])
      };
   }

   private static IReadOnlyList<string> GetChannelScheduleCategories(
      JsonObject item
   )
   {
      var categories = new List<string>();

      AddTextCategory(item["sport"]?.ToString(), categories);
      AddTextCategory(item["sportGroup"]?.ToString(), categories);
      AddTextCategory(item["subtitle"]?.ToString(), categories);
      AddTextArrayCategories(item["genreNames"], categories);
      AddGenreObjectCategories(item["genres"], categories);

      return categories
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToArray();
   }

   private static void AddTextArrayCategories(
      JsonNode? node,
      ICollection<string> categories
   )
   {
      if(node is not JsonArray array)
      {
         return;
      }

      foreach(var item in array)
      {
         AddTextCategory(item?.ToString(), categories);
      }
   }

   private static void AddGenreObjectCategories(
      JsonNode? node,
      ICollection<string> categories
   )
   {
      if(node is not JsonArray array)
      {
         return;
      }

      foreach(var item in array.OfType<JsonObject>())
      {
         AddTextCategory(item["name"]?.ToString(), categories);
      }
   }

   private static void AddTextCategory(
      string? value,
      ICollection<string> categories
   )
   {
      var category = NormalizeOptionalText(value);

      if(category is not null && !string.Equals(
         category,
         "Sport",
         StringComparison.OrdinalIgnoreCase
      ))
      {
         categories.Add(category);
      }
   }

   private static JsonObject CreateChannelNode(
      ChannelSchedule channelSchedule
   )
   {
      var channel = new JsonObject
      {
         ["name"] = channelSchedule.Name,
         ["slug"] = channelSchedule.Slug
      };

      if(channelSchedule.ThemedLogo is not null)
      {
         channel["themedLogo"] = channelSchedule.ThemedLogo.DeepClone();
      }

      return channel;
   }

   private static JsonArray CloneArrayOrEmpty(JsonNode? node)
   {
      return node is JsonArray array ?
         array.DeepClone().AsArray() :
         new JsonArray();
   }

   private static ScheduleApiPage ExtractSchedulePage(
      string json,
      string sourceLabel
   )
   {
      var rootNode = JsonNode.Parse(json);

      if(TryExtractScheduleArray(rootNode, out var scheduleArray))
      {
         return new ScheduleApiPage(
            scheduleArray,
            TryExtractPaginationBool(rootNode, "hasNext"),
            TryExtractPaginationInt(rootNode, "totalCount")
         );
      }

      throw new InvalidOperationException(
         "Unable to locate a sport schedule array in the API response. " +
         $"Source: {sourceLabel}. Prefix: {json[..Math.Min(json.Length, 200)]}"
      );
   }

   private static bool TryExtractScheduleArray(
      JsonNode? node,
      out JsonArray scheduleArray
   )
   {
      if(node is null)
      {
         scheduleArray = [];
         return false;
      }

      if(node is JsonObject obj)
      {
         if(
            obj["data"] is JsonObject dataObject &&
            dataObject["broadcasts"] is JsonArray dataBroadcasts
         )
         {
            scheduleArray = dataBroadcasts;
            return true;
         }

         if(
            obj["sportPageSchedule"] is JsonArray directArray &&
            LooksLikeScheduleArray(directArray)
         )
         {
            scheduleArray = directArray;
            return true;
         }

         foreach(var property in obj)
         {
            if(TryExtractScheduleArray(property.Value, out scheduleArray))
            {
               return true;
            }
         }
      }

      if(node is JsonArray array)
      {
         if(LooksLikeScheduleArray(array))
         {
            scheduleArray = array;
            return true;
         }

         foreach(var item in array)
         {
            if(TryExtractScheduleArray(item, out scheduleArray))
            {
               return true;
            }
         }
      }

      scheduleArray = [];
      return false;
   }

   private static bool LooksLikeScheduleArray(JsonArray array)
   {
      return array
         .OfType<JsonObject>()
         .Any(item =>
            item["title"] is JsonValue &&
            item["id"] is JsonValue &&
            (
               item["broadcasts"] is JsonArray ||
               item["playEpisodes"] is JsonArray
            )
         );
   }

   private static bool? TryExtractPaginationBool(
      JsonNode? rootNode,
      string propertyName
   )
   {
      var value = TryExtractPaginationValue(rootNode, propertyName);

      if(value is JsonValue jsonValue &&
         jsonValue.TryGetValue<bool>(out var result))
      {
         return result;
      }

      return null;
   }

   private static int? TryExtractPaginationInt(
      JsonNode? rootNode,
      string propertyName
   )
   {
      var value = TryExtractPaginationValue(rootNode, propertyName);

      if(value is JsonValue jsonValue &&
         jsonValue.TryGetValue<int>(out var result))
      {
         return result;
      }

      return null;
   }

   private static JsonNode? TryExtractPaginationValue(
      JsonNode? rootNode,
      string propertyName
   )
   {
      if(
         rootNode is JsonObject rootObject &&
         rootObject["meta"] is JsonObject meta &&
         meta["pagination"] is JsonObject pagination
      )
      {
         return pagination[propertyName];
      }

      return null;
   }

   private static IReadOnlyList<string> ExtractStandardModules(
      string initialStateJson
   )
   {
      using var document = JsonDocument.Parse(initialStateJson);

      if(
         TryFindProperty(
            document.RootElement,
            "standardModules",
            out var value
         ) &&
         value.ValueKind == JsonValueKind.Array
      )
      {
         return value
            .EnumerateArray()
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToList();
      }

      return [];
   }

   private static bool TryFindProperty(
      JsonElement element,
      string propertyName,
      out JsonElement value
   )
   {
      if(element.ValueKind == JsonValueKind.Object)
      {
         if(element.TryGetProperty(propertyName, out value))
         {
            return true;
         }

         foreach(var property in element.EnumerateObject())
         {
            if(TryFindProperty(property.Value, propertyName, out value))
            {
               return true;
            }
         }
      }
      else if(element.ValueKind == JsonValueKind.Array)
      {
         foreach(var item in element.EnumerateArray())
         {
            if(TryFindProperty(item, propertyName, out value))
            {
               return true;
            }
         }
      }

      value = default;
      return false;
   }

   private static string BuildScheduleApiUrl(
      DateOnly date,
      IReadOnlyList<string> modules,
      int pageNumber
   )
   {
      var builder = new StringBuilder(ApiUrl);
      builder.Append('?');

      foreach(var module in modules)
      {
         builder.Append("modules[]=");
         builder.Append(Uri.EscapeDataString(module));
         builder.Append('&');
      }

      builder.Append("page=");
      builder.Append(pageNumber);
      builder.Append("&preset=sport&scheduleDate=");
      builder.Append(DateDisplay.Format(date));
      builder.Append("&viewAll=true");

      return builder.ToString();
   }

   private static string BuildSearchUrl(string query)
   {
      var builder = new StringBuilder(SearchUrl);
      builder.Append("?query=");
      builder.Append(Uri.EscapeDataString(query));

      return builder.ToString();
   }

   private static string BuildChannelScheduleUrl(
      string slug,
      DateOnly date
   )
   {
      var builder = new StringBuilder(ChannelUrl);
      builder.Append('/');
      builder.Append(Uri.EscapeDataString(slug));
      builder.Append("/schedule?date=");
      builder.Append(DateDisplay.Format(date));

      return builder.ToString();
   }

   private static string BuildSyntheticRows(JsonArray scheduleItems)
   {
      var rows = scheduleItems
         .OfType<JsonObject>()
         .Select(TryCreateSyntheticRow)
         .Where(row => row is not null)
         .Select(row => row!)
         .OrderBy(row => row.StartsAt)
         .ThenBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
         .ToList();

      if(rows.Count == 0)
      {
         return string.Empty;
      }

      var builder = new StringBuilder();
      builder.AppendLine();
      builder.AppendLine("<ul data-sesport-synthetic-broadcasts=\"true\">");

      foreach(var row in rows)
      {
         AppendSyntheticRow(builder, row);
      }

      builder.AppendLine("</ul>");
      return builder.ToString();
   }
   private static SyntheticRow? TryCreateSyntheticRow(JsonObject item)
   {
      var id = NormalizeOptionalText(item["id"]?.ToString());
      var title = NormalizeOptionalText(item["title"]?.ToString());
      var startsAt = TryGetEventTime(item) ?? TryGetFirstStart(item);
      var category = GetSyntheticCategory(item);
      var channelNames = GetSyntheticChannelNames(item);

      if(
         string.IsNullOrWhiteSpace(id) ||
         string.IsNullOrWhiteSpace(title) ||
         startsAt is null ||
         string.IsNullOrWhiteSpace(category) ||
         channelNames.Count == 0
      )
      {
         return null;
      }

      return new SyntheticRow(
         id,
         title,
         startsAt.Value,
         category,
         channelNames
      );
   }

   private static void AppendSyntheticRow(
      StringBuilder builder,
      SyntheticRow row
   )
   {
      var localStart = TimeZoneInfo.ConvertTime(
         row.StartsAt,
         StockholmTimeZone
      );
      var timeText = localStart.ToString(
         DateDisplay.DateTimeMinutesFormat,
         CultureInfo.InvariantCulture
      );
      var labelTime = localStart.ToString(
         "HH:mm",
         CultureInfo.InvariantCulture
      );
      var encodedTitle = WebUtility.HtmlEncode(row.Title);

      builder.AppendLine("   <li class=\"_37xCg nSLmX\">");
      builder.Append($"      <a href=\"{SiteRootUrl}/s/");
      builder.Append(WebUtility.HtmlEncode(row.Id));
      builder.AppendLine("\">");
      builder.Append("         <span aria-label=\"Link - ");
      builder.Append(labelTime);
      builder.Append(", ");
      builder.Append(encodedTitle);
      builder.AppendLine("\">");
      builder.Append("            ");
      builder.Append(encodedTitle);
      builder.AppendLine();
      builder.AppendLine("         </span>");
      builder.AppendLine("      </a>");
      builder.Append("      <time datetime=\"");
      builder.Append(timeText);
      builder.AppendLine("\"></time>");

      foreach(var channelName in row.ChannelNames)
      {
         builder.Append("      <span class=\"Oz76s\"></span></div>");
         builder.Append(WebUtility.HtmlEncode(channelName));
         builder.AppendLine("</div>");
      }

      builder.Append("      <div class=\"_2ZygK\"><div class=\"_2HFK6\">");
      builder.Append("</div>");
      builder.Append(WebUtility.HtmlEncode(row.Category));
      builder.AppendLine("<span class=\"ss5Ll\"></span></div>");
      builder.AppendLine("   </li>");
   }

   private static DateTimeOffset? TryGetEventTime(JsonObject item)
   {
      var eventTime = TryGetLong(item["eventTime"]);

      if(eventTime is null)
      {
         return null;
      }

      return DateTimeOffset.FromUnixTimeMilliseconds(eventTime.Value);
   }

   private static DateTimeOffset? TryGetFirstStart(JsonObject item)
   {
      var startTimes = new List<long>();

      AddStartTimes(item["broadcasts"], "startTime", startTimes);
      AddStartTimes(item["playEpisodes"], "streamStart", startTimes);

      if(startTimes.Count == 0)
      {
         return null;
      }

      return DateTimeOffset.FromUnixTimeMilliseconds(startTimes.Min());
   }

   private static void AddStartTimes(
      JsonNode? node,
      string propertyName,
      ICollection<long> startTimes
   )
   {
      if(node is not JsonArray array)
      {
         return;
      }

      foreach(var item in array.OfType<JsonObject>())
      {
         var value = TryGetLong(item[propertyName]);

         if(value is not null)
         {
            startTimes.Add(value.Value);
         }
      }
   }

   private static long? TryGetLong(JsonNode? node)
   {
      if(node is JsonValue value &&
         value.TryGetValue<long>(out var result))
      {
         return result;
      }

      return null;
   }

   private static bool? TryGetBool(JsonNode? node)
   {
      if(node is JsonValue value &&
         value.TryGetValue<bool>(out var result))
      {
         return result;
      }

      return null;
   }

   private static string? GetSyntheticCategory(JsonObject item)
   {
      var primaryParts = new[]
      {
         item["sport"]?.ToString(),
         item["tournament"]?.ToString()
      };
      var primaryCategory = JoinSyntheticCategory(primaryParts);

      if(!string.IsNullOrWhiteSpace(primaryCategory))
      {
         return primaryCategory;
      }

      return JoinSyntheticCategory(
         new[]
         {
            item["sportGroup"]?.ToString(),
            item["subtitle"]?.ToString()
         }
      );
   }

   private static string? JoinSyntheticCategory(
      IEnumerable<string?> parts
   )
   {
      var values = parts
         .Select(NormalizeOptionalText)
         .Where(value => !string.IsNullOrWhiteSpace(value))
         .Select(value => value!)
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();

      return values.Count == 0 ? null : string.Join(", ", values);
   }

   private static IReadOnlyList<string> GetSyntheticChannelNames(
      JsonObject item
   )
   {
      var channelNames = new List<string>();

      AddChannelNames(
         item["broadcasts"],
         "channel",
         channelNames
      );
      AddChannelNames(
         item["playEpisodes"],
         "playProvider",
         channelNames
      );

      return channelNames
         .Distinct(StringComparer.OrdinalIgnoreCase)
         .ToList();
   }

   private static void AddChannelNames(
      JsonNode? node,
      string providerPropertyName,
      ICollection<string> channelNames
   )
   {
      if(node is not JsonArray array)
      {
         return;
      }

      foreach(var item in array.OfType<JsonObject>())
      {
         if(item[providerPropertyName] is not JsonObject provider)
         {
            continue;
         }

         var channelName = NormalizeOptionalText(
            provider["name"]?.ToString()
         );

         if(channelName is not null)
         {
            channelNames.Add(channelName);
         }
      }
   }

   private static string? NormalizeOptionalText(string? value)
   {
      if(string.IsNullOrWhiteSpace(value))
      {
         return null;
      }

      return value.Replace("\\u0026", "&").Trim();
   }

   private static string InjectSyntheticRows(
      string html,
      string syntheticRows
   )
   {
      if(string.IsNullOrWhiteSpace(syntheticRows))
      {
         return html;
      }

      var bodyEndIndex = html.LastIndexOf(
         "</body>",
         StringComparison.OrdinalIgnoreCase
      );

      if(bodyEndIndex < 0)
      {
         return html + syntheticRows;
      }

      return html[..bodyEndIndex] +
         syntheticRows +
         html[bodyEndIndex..];
   }

   private static async Task EnrichPlayProviderUrlsAsync(
      HttpClient httpClient,
      JsonArray scheduleItems,
      CancellationToken cancellationToken
   )
   {
      var items = scheduleItems
         .OfType<JsonObject>()
         .Where(item =>
            !string.IsNullOrWhiteSpace(item["id"]?.ToString()) &&
            item["playEpisodes"] is JsonArray
         )
         .ToList();
      var attemptedCount = 0;
      var enrichedCount = 0;

      foreach(var item in items)
      {
         cancellationToken.ThrowIfCancellationRequested();

         if(item["playEpisodes"] is not JsonArray playEpisodes)
         {
            continue;
         }

         var missingEpisodes = playEpisodes
            .OfType<JsonObject>()
            .Where(episode =>
               episode["playProvider"] is JsonObject provider &&
               string.IsNullOrWhiteSpace(provider["url"]?.ToString())
            )
            .ToList();

         if(missingEpisodes.Count == 0)
         {
            continue;
         }

         attemptedCount++;
         var detailUrl = BuildDetailUrl(item["id"]!.ToString());
         string detailHtml;

         try
         {
            detailHtml = await httpClient.GetStringAsync(
               detailUrl,
               cancellationToken
            );
         }
         catch(HttpRequestException exception)
         {
            Console.Error.WriteLine(
               $"[source={SourceKey}] detail_fetch_failed " +
               $"id={item["id"]} error={exception.Message}"
            );
            continue;
         }

         var providerUrls = TryExtractDetailProviderUrls(detailHtml);

         foreach(var episode in missingEpisodes)
         {
            if(episode["playProvider"] is not JsonObject provider)
            {
               continue;
            }

            var providerKey = NormalizeOptionalText(
               provider["slug"]?.ToString()
            ) ?? NormalizeOptionalText(provider["name"]?.ToString());

            if(
               providerKey is null ||
               !providerUrls.TryGetValue(providerKey, out var url)
            )
            {
               continue;
            }

            provider["url"] = url;
            enrichedCount++;
         }
      }

      Console.Error.WriteLine(
         $"[source={SourceKey}] stream_link_enrichment " +
         $"pages={attemptedCount} links={enrichedCount}"
      );
   }

   private static IReadOnlyDictionary<string, string>
      TryExtractDetailProviderUrls(string html)
   {
      var initialStateJson = ExtractInitialStateJson(html);

      if(initialStateJson is null)
      {
         return new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
         );
      }

      try
      {
         var stateNode = JsonNode.Parse(initialStateJson);
         if(
            stateNode is not JsonObject stateObject ||
            stateObject["detail"] is not JsonObject detail ||
            detail["playProviders"] is not JsonArray providers
         )
         {
            return new Dictionary<string, string>(
               StringComparer.OrdinalIgnoreCase
            );
         }

         var urls = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
         );

         foreach(var provider in providers.OfType<JsonObject>())
         {
            if(!StreamLinkUrlNormalizer.TryNormalize(
               provider["url"]?.ToString(),
               out var normalizedUrl
            ))
            {
               continue;
            }

            foreach(var key in new[]
            {
               provider["slug"]?.ToString(),
               provider["name"]?.ToString()
            })
            {
               var normalizedKey = NormalizeOptionalText(key);
               if(normalizedKey is not null)
               {
                  urls[normalizedKey] = normalizedUrl;
               }
            }
         }

         return urls;
      }
      catch(JsonException)
      {
         return new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase
         );
      }
   }

   private static string BuildDetailUrl(string id)
   {
      return $"{SiteRootUrl}/s/{Uri.EscapeDataString(id)}";
   }

   private static string? ExtractInitialStateJson(string html)
   {
      const string marker = "__INITIAL_STATE__ = ";
      var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);

      if(markerIndex < 0)
      {
         return null;
      }

      var startIndex = markerIndex + marker.Length;

      while(
         startIndex < html.Length &&
         char.IsWhiteSpace(html[startIndex])
      )
      {
         startIndex++;
      }

      if(startIndex >= html.Length || html[startIndex] != '"')
      {
         return null;
      }

      var endIndex = startIndex + 1;
      var escaped = false;

      while(endIndex < html.Length)
      {
         var current = html[endIndex];

         if(escaped)
         {
            escaped = false;
         }
         else if(current == '\\')
         {
            escaped = true;
         }
         else if(current == '"')
         {
            break;
         }

         endIndex++;
      }

      if(endIndex >= html.Length)
      {
         return null;
      }

      var literal = html[startIndex..(endIndex + 1)];
      return JsonSerializer.Deserialize<string>(literal);
   }

   private static string ResolveSourceOutputDirectory(string outputDirectory)
   {
      var trimmedOutputDirectory = Path.TrimEndingDirectorySeparator(
         outputDirectory
      );
      var directoryName = Path.GetFileName(trimmedOutputDirectory);

      if(
         string.Equals(
            directoryName,
            SourceKey,
            StringComparison.OrdinalIgnoreCase
         )
      )
      {
         return outputDirectory;
      }

      return Path.Combine(outputDirectory, SourceKey);
   }

   private static string? ReplaceInitialStateJson(
      string html,
      string mergedStateJson
   )
   {
      const string marker = "__INITIAL_STATE__ = ";
      var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);

      if(markerIndex < 0)
      {
         return null;
      }

      var startIndex = markerIndex + marker.Length;

      while(
         startIndex < html.Length &&
         char.IsWhiteSpace(html[startIndex])
      )
      {
         startIndex++;
      }

      if(startIndex >= html.Length || html[startIndex] != '"')
      {
         return null;
      }

      var endIndex = startIndex + 1;
      var escaped = false;

      while(endIndex < html.Length)
      {
         var current = html[endIndex];

         if(escaped)
         {
            escaped = false;
         }
         else if(current == '\\')
         {
            escaped = true;
         }
         else if(current == '"')
         {
            break;
         }

         endIndex++;
      }

      if(endIndex >= html.Length)
      {
         return null;
      }

      var replacement = JsonSerializer.Serialize(mergedStateJson);
      return html[..startIndex] + replacement + html[endIndex..];
   }

   private static string BuildSyntheticHtml(string mergedStateJson)
   {
      var mergedStateLiteral = JsonSerializer.Serialize(mergedStateJson);

      return
         "<!doctype html>\n" +
         "<html>\n" +
         "<head>\n" +
         "   <meta charset=\"utf-8\">\n" +
         "</head>\n" +
         "<body>\n" +
         "   <script>__INITIAL_STATE__ = " + mergedStateLiteral +
         "</script>\n" +
         "</body>\n" +
         "</html>\n";
   }

   private static string FormatNullable<T>(T? value)
      where T : struct
   {
      return value?.ToString() ?? "n/a";
   }

   private static readonly TimeZoneInfo StockholmTimeZone =
      TimeZoneHelper.Resolve(SportDay.TimeZoneId);

   private sealed record ScheduleApiPage(
      JsonArray Items,
      bool? HasNext,
      int? TotalCount
   );

   private sealed record ChannelSchedule(
      string Name,
      string Slug,
      JsonNode? ThemedLogo,
      JsonArray Items
   );

   private sealed record SyntheticRow(
      string Id,
      string Title,
      DateTimeOffset StartsAt,
      string Category,
      IReadOnlyList<string> ChannelNames
   );
}
