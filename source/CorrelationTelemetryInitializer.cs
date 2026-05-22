using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace ShipmentIntegration.Telemetry;

/// <summary>
/// Custom telemetry initializer to enrich all telemetry with correlation context
/// </summary>
public class CorrelationTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        var context = _httpContextAccessor.HttpContext;
        
        if (context == null)
            return;

        // Extract correlation identifiers from request headers or context
        var correlationId = ExtractFromHeaders(context, "X-CorrelationId") 
                         ?? ExtractFromContext(context, "CorrelationId");
        
        var correlationGuid = ExtractFromHeaders(context, "X-CorrelationGuid") 
                           ?? ExtractFromContext(context, "CorrelationGuid");

        var tenderId = ExtractFromHeaders(context, "X-TenderId") 
                    ?? ExtractFromContext(context, "TenderId");

        // Add to telemetry properties
        if (telemetry is ISupportProperties propTelemetry)
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                propTelemetry.Properties["CorrelationId"] = correlationId;
            }

            if (!string.IsNullOrEmpty(correlationGuid))
            {
                propTelemetry.Properties["CorrelationGuid"] = correlationGuid;
            }

            if (!string.IsNullOrEmpty(tenderId))
            {
                propTelemetry.Properties["TenderId"] = tenderId;
            }

            // Add environment information
            propTelemetry.Properties["Environment"] = 
                Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") ?? "Unknown";
            
            propTelemetry.Properties["ServiceName"] = 
                Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "ShipmentIntegration";
        }

        // Set cloud role name for Application Map
        if (telemetry is { Context: not null })
        {
            telemetry.Context.Cloud.RoleName = "ShipmentIntegration";
            telemetry.Context.Cloud.RoleInstance = 
                Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? "local";
        }
    }

    private string? ExtractFromHeaders(HttpContext context, string headerName)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var values))
        {
            return values.FirstOrDefault();
        }
        return null;
    }

    private string? ExtractFromContext(HttpContext context, string key)
    {
        if (context.Items.TryGetValue(key, out var value))
        {
            return value?.ToString();
        }
        return null;
    }
}