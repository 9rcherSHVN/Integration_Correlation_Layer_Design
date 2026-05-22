using Azure;
using Azure.Data.Tables;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace ShipmentIntegration.Functions;

#region Request/Response Models

public record ResolveCorrelationRequest(
    string EntityId,
    string ShpNum,
    string Loc,
    string Destination,
    string DueDate,
    string? Origin,
    string? OperationType
);

public record ResolveCorrelationResponse(
    string CorrelationId,
    string CorrelationGuid,
    bool IsNew,
    string? TenderId,
    string Status
);

public record LookupCorrelationRequest(
    string? CorrelationGuid,
    string? TenderId,
    string? ShpNumRef,
    string? EntityId
);

public record LookupCorrelationResponse(
    bool Found,
    string? LookupMethod,
    string? CorrelationId,
    string? CorrelationGuid,
    string? TenderId,
    string? Status,
    string? Error
);

public record LinkTenderRequest(
    string CorrelationGuid,
    string TenderId,
    string EntityId
);

public record LinkTenderResponse(
    bool Success,
    string? CorrelationId,
    string? CorrelationGuid,
    string? TenderId,
    DateTime? LinkedTimestamp,
    string? Error
);

#endregion

#region Table Entities

public class CorrelationMappingEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;  // EntityId
    public string RowKey { get; set; } = string.Empty;        // {shpnum}-{loc}-{dest}-{duedate}
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string CorrelationId { get; set; } = string.Empty;
    public string CorrelationGuid { get; set; } = string.Empty;
    public string ShpNum { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Loc { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public string? TenderId { get; set; }
    public string Status { get; set; } = "Active";
    public string? SupersededBy { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime LastUpdatedTimestamp { get; set; }
}

public class GuidLookupEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;  // First 8 chars of GUID
    public string RowKey { get; set; } = string.Empty;        // Full GUID
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string CorrelationId { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ShpNum { get; set; } = string.Empty;
    public DateTime CreatedTimestamp { get; set; }
}

public class ThirdPartyLookupEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;  // EntityId
    public string RowKey { get; set; } = string.Empty;        // TenderId
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string CorrelationId { get; set; } = string.Empty;
    public string CorrelationGuid { get; set; } = string.Empty;
    public string ShpNum { get; set; } = string.Empty;
    public DateTime LinkedTimestamp { get; set; }
}

#endregion

public class CorrelationManager
{
    private readonly TableClient _correlationTable;
    private readonly TableClient _guidLookupTable;
    private readonly TableClient _thirdPartyTable;
    private readonly TelemetryClient _telemetry;
    private readonly ILogger<CorrelationManager> _logger;

    public CorrelationManager(
        TableServiceClient tableService,
        TelemetryClient telemetry,
        ILogger<CorrelationManager> logger)
    {
        _correlationTable = tableService.GetTableClient("CorrelationMapping");
        _guidLookupTable = tableService.GetTableClient("GuidLookup");
        _thirdPartyTable = tableService.GetTableClient("ThirdPartyLookup");
        _telemetry = telemetry;
        _logger = logger;
    }

    /// <summary>
    /// Resolves or creates a Correlation ID and GUID for outbound messages
    /// </summary>
    [Function("ResolveCorrelation")]
    public async Task<HttpResponseData> ResolveCorrelation(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "correlation/resolve")]
        HttpRequestData req)
    {
        var request = await JsonSerializer.DeserializeAsync<ResolveCorrelationRequest>(
            req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (request == null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
        }

        // Build composite key
        var rowKey = $"{request.ShpNum}-{request.Loc}-{request.Destination}-{request.DueDate}";
        var correlationId = $"{request.EntityId}-{rowKey}";

        using var operation = _telemetry.StartOperation<RequestTelemetry>("ResolveCorrelation");
        operation.Telemetry.Properties["CorrelationId"] = correlationId;

        try
        {
            // Step 1: Try to find existing record
            var existingResponse = await _correlationTable.GetEntityIfExistsAsync<CorrelationMappingEntity>(
                request.EntityId,
                rowKey);

            if (existingResponse.HasValue && existingResponse.Value != null)
            {
                // Existing shipment - return existing IDs
                var existing = existingResponse.Value;

                _logger.LogInformation(
                    "Found existing correlation: {CorrelationId}, GUID: {CorrelationGuid}",
                    existing.CorrelationId,
                    existing.CorrelationGuid);

                operation.Telemetry.Properties["IsNew"] = "false";
                operation.Telemetry.Properties["CorrelationGuid"] = existing.CorrelationGuid;

                var existingResult = new ResolveCorrelationResponse(
                    existing.CorrelationId,
                    existing.CorrelationGuid,
                    false,
                    existing.TenderId,
                    existing.Status);

                return await CreateJsonResponse(req, HttpStatusCode.OK, existingResult);
            }

            // Step 2: Generate new IDs
            var correlationGuid = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            _logger.LogInformation(
                "Creating new correlation: {CorrelationId}, GUID: {CorrelationGuid}",
                correlationId,
                correlationGuid);

            // Step 3: Store in CorrelationMapping
            var correlationEntity = new CorrelationMappingEntity
            {
                PartitionKey = request.EntityId,
                RowKey = rowKey,
                CorrelationId = correlationId,
                CorrelationGuid = correlationGuid,
                ShpNum = request.ShpNum,
                EntityId = request.EntityId,
                Loc = request.Loc,
                Destination = request.Destination,
                DueDate = request.DueDate,
                Origin = request.Origin,
                Status = "Active",
                CreatedTimestamp = now,
                LastUpdatedTimestamp = now
            };

            await _correlationTable.UpsertEntityAsync(correlationEntity);

            // Step 4: Store in GuidLookup
            var guidEntity = new GuidLookupEntity
            {
                PartitionKey = correlationGuid.Substring(0, 8),
                RowKey = correlationGuid,
                CorrelationId = correlationId,
                EntityId = request.EntityId,
                ShpNum = request.ShpNum,
                CreatedTimestamp = now
            };

            await _guidLookupTable.UpsertEntityAsync(guidEntity);

            operation.Telemetry.Properties["IsNew"] = "true";
            operation.Telemetry.Properties["CorrelationGuid"] = correlationGuid;

            var newResult = new ResolveCorrelationResponse(
                correlationId,
                correlationGuid,
                true,
                null,
                "Active");

            return await CreateJsonResponse(req, HttpStatusCode.Created, newResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving correlation for {CorrelationId}", correlationId);
            operation.Telemetry.Success = false;
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Looks up Correlation ID from inbound message identifiers
    /// Uses priority: CorrelationGuid → TenderId → ShpNumRef+EntityId
    /// </summary>
    [Function("LookupCorrelation")]
    public async Task<HttpResponseData> LookupCorrelation(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "correlation/lookup")]
        HttpRequestData req)
    {
        var request = await JsonSerializer.DeserializeAsync<LookupCorrelationRequest>(
            req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (request == null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
        }

        using var operation = _telemetry.StartOperation<RequestTelemetry>("LookupCorrelation");

        try
        {
            // Strategy 1: Try CorrelationGuid (Primary - fastest)
            if (!string.IsNullOrEmpty(request.CorrelationGuid))
            {
                var guidPrefix = request.CorrelationGuid.Substring(0, 8);
                var guidResponse = await _guidLookupTable.GetEntityIfExistsAsync<GuidLookupEntity>(
                    guidPrefix,
                    request.CorrelationGuid);

                if (guidResponse.HasValue && guidResponse.Value != null)
                {
                    var guid = guidResponse.Value;

                    // Get full details from CorrelationMapping
                    var fullDetails = await GetCorrelationDetails(guid.CorrelationId);

                    _logger.LogInformation(
                        "Found correlation via GUID: {CorrelationGuid} → {CorrelationId}",
                        request.CorrelationGuid,
                        guid.CorrelationId);

                    operation.Telemetry.Properties["LookupMethod"] = "CorrelationGuid";
                    operation.Telemetry.Properties["CorrelationId"] = guid.CorrelationId;

                    return await CreateJsonResponse(req, HttpStatusCode.OK, new LookupCorrelationResponse(
                        true,
                        "CorrelationGuid",
                        guid.CorrelationId,
                        request.CorrelationGuid,
                        fullDetails?.TenderId,
                        fullDetails?.Status ?? "Active",
                        null));
                }
            }

            // Strategy 2: Try TenderId (Fallback 1)
            if (!string.IsNullOrEmpty(request.TenderId) && !string.IsNullOrEmpty(request.EntityId))
            {
                var tenderResponse = await _thirdPartyTable.GetEntityIfExistsAsync<ThirdPartyLookupEntity>(
                    request.EntityId,
                    request.TenderId);

                if (tenderResponse.HasValue && tenderResponse.Value != null)
                {
                    var tender = tenderResponse.Value;

                    _logger.LogInformation(
                        "Found correlation via TenderId: {TenderId} → {CorrelationId}",
                        request.TenderId,
                        tender.CorrelationId);

                    operation.Telemetry.Properties["LookupMethod"] = "TenderId";
                    operation.Telemetry.Properties["CorrelationId"] = tender.CorrelationId;

                    return await CreateJsonResponse(req, HttpStatusCode.OK, new LookupCorrelationResponse(
                        true,
                        "TenderId",
                        tender.CorrelationId,
                        tender.CorrelationGuid,
                        request.TenderId,
                        "Active",
                        null));
                }
            }

            // Strategy 3: Try ShpNumRef + EntityId (Fallback 2 - requires scan)
            if (!string.IsNullOrEmpty(request.ShpNumRef) && !string.IsNullOrEmpty(request.EntityId))
            {
                // Query by partition (EntityId) and filter by ShpNum
                var query = _correlationTable.QueryAsync<CorrelationMappingEntity>(
                    filter: $"PartitionKey eq '{request.EntityId}' and ShpNum eq '{request.ShpNumRef}'",
                    maxPerPage: 10);

                await foreach (var entity in query)
                {
                    if (entity.Status == "Active")
                    {
                        _logger.LogInformation(
                            "Found correlation via ShpNumRef: {ShpNumRef} → {CorrelationId}",
                            request.ShpNumRef,
                            entity.CorrelationId);

                        operation.Telemetry.Properties["LookupMethod"] = "ShpNumRef";
                        operation.Telemetry.Properties["CorrelationId"] = entity.CorrelationId;

                        return await CreateJsonResponse(req, HttpStatusCode.OK, new LookupCorrelationResponse(
                            true,
                            "ShpNumRef",
                            entity.CorrelationId,
                            entity.CorrelationGuid,
                            entity.TenderId,
                            entity.Status,
                            null));
                    }
                }
            }

            // Not found
            _logger.LogWarning(
                "Correlation not found for GUID: {CorrelationGuid}, TenderId: {TenderId}, ShpNumRef: {ShpNumRef}",
                request.CorrelationGuid,
                request.TenderId,
                request.ShpNumRef);

            operation.Telemetry.Properties["LookupMethod"] = "None";
            operation.Telemetry.Success = false;

            return await CreateJsonResponse(req, HttpStatusCode.NotFound, new LookupCorrelationResponse(
                false,
                null,
                null,
                null,
                null,
                null,
                "No correlation found for provided identifiers"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up correlation");
            operation.Telemetry.Success = false;
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    /// <summary>
    /// Links external TenderId to existing Correlation ID
    /// Called when first tender response is received from 3rd party
    /// </summary>
    [Function("LinkTender")]
    public async Task<HttpResponseData> LinkTender(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "correlation/link-tender")]
        HttpRequestData req)
    {
        var request = await JsonSerializer.DeserializeAsync<LinkTenderRequest>(
            req.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (request == null)
        {
            return await CreateErrorResponse(req, HttpStatusCode.BadRequest, "Invalid request body");
        }

        using var operation = _telemetry.StartOperation<RequestTelemetry>("LinkTender");
        operation.Telemetry.Properties["CorrelationGuid"] = request.CorrelationGuid;
        operation.Telemetry.Properties["TenderId"] = request.TenderId;

        try
        {
            // Step 1: Find correlation by GUID
            var guidPrefix = request.CorrelationGuid.Substring(0, 8);
            var guidResponse = await _guidLookupTable.GetEntityIfExistsAsync<GuidLookupEntity>(
                guidPrefix,
                request.CorrelationGuid);

            if (!guidResponse.HasValue || guidResponse.Value == null)
            {
                _logger.LogWarning(
                    "Cannot link tender - GUID not found: {CorrelationGuid}",
                    request.CorrelationGuid);

                return await CreateJsonResponse(req, HttpStatusCode.NotFound, new LinkTenderResponse(
                    false,
                    null,
                    request.CorrelationGuid,
                    request.TenderId,
                    null,
                    "Correlation GUID not found"));
            }

            var guidEntity = guidResponse.Value;
            var correlationId = guidEntity.CorrelationId;
            var now = DateTime.UtcNow;

            operation.Telemetry.Properties["CorrelationId"] = correlationId;

            // Step 2: Update CorrelationMapping with TenderId
            // First, get the entity to update
            var parts = correlationId.Split('-', 2);
            var entityId = parts[0];
            var rowKey = parts.Length > 1 ? parts[1] : "";

            // Find the actual entity (need full row key)
            var correlationQuery = _correlationTable.QueryAsync<CorrelationMappingEntity>(
                filter: $"PartitionKey eq '{entityId}' and CorrelationId eq '{correlationId}'",
                maxPerPage: 1);

            CorrelationMappingEntity? correlationEntity = null;
            await foreach (var entity in correlationQuery)
            {
                correlationEntity = entity;
                break;
            }

            if (correlationEntity != null)
            {
                correlationEntity.TenderId = request.TenderId;
                correlationEntity.LastUpdatedTimestamp = now;
                await _correlationTable.UpdateEntityAsync(correlationEntity, correlationEntity.ETag);
            }

            // Step 3: Create ThirdPartyLookup entry
            var thirdPartyEntity = new ThirdPartyLookupEntity
            {
                PartitionKey = request.EntityId,
                RowKey = request.TenderId,
                CorrelationId = correlationId,
                CorrelationGuid = request.CorrelationGuid,
                ShpNum = guidEntity.ShpNum,
                LinkedTimestamp = now
            };

            await _thirdPartyTable.UpsertEntityAsync(thirdPartyEntity);

            _logger.LogInformation(
                "Linked TenderId {TenderId} to CorrelationId {CorrelationId}",
                request.TenderId,
                correlationId);

            return await CreateJsonResponse(req, HttpStatusCode.OK, new LinkTenderResponse(
                true,
                correlationId,
                request.CorrelationGuid,
                request.TenderId,
                now,
                null));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error linking tender {TenderId}", request.TenderId);
            operation.Telemetry.Success = false;
            return await CreateErrorResponse(req, HttpStatusCode.InternalServerError, ex.Message);
        }
    }

    #region Helper Methods

    private async Task<CorrelationMappingEntity?> GetCorrelationDetails(string correlationId)
    {
        var parts = correlationId.Split('-', 2);
        if (parts.Length < 2) return null;

        var entityId = parts[0];

        var query = _correlationTable.QueryAsync<CorrelationMappingEntity>(
            filter: $"PartitionKey eq '{entityId}' and CorrelationId eq '{correlationId}'",
            maxPerPage: 1);

        await foreach (var entity in query)
        {
            return entity;
        }

        return null;
    }

    private static async Task<HttpResponseData> CreateJsonResponse<T>(
        HttpRequestData req,
        HttpStatusCode statusCode,
        T data)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
        return response;
    }

    private static async Task<HttpResponseData> CreateErrorResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string message)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        await response.WriteStringAsync(JsonSerializer.Serialize(new { error = message }));
        return response;
    }

    #endregion
}
