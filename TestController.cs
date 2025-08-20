using Inscape.CosmosDb;
using Microsoft.AspNetCore.Mvc;
using Microsoft365.Common.Enums;
using Microsoft365.Common.Helpers;
using Microsoft365.Services.Email.TemplateEngine;
using Microsoft365.Services.M365Reports.Base;

namespace Microsoft365.Controllers;

public class TestController(
    IServiceProvider serviceProvider) : ControllerBase
{
    [HttpGet("test/send-report-now/{reportTag}")]
    public async Task<IActionResult> SendReportNowTest(
        string reportTag,
        [FromForm] string? select,
        [FromForm] ReportConfiguration reportConfiguration,
        CancellationToken cancellationToken)
    {
        var reportType = ReportTypeHelper.GetReportTypeByTag(reportTag);
        var m365Service = serviceProvider.GetRequiredKeyedService<IM365Report>(reportType);
        
        if (select is not null)
        {
            reportConfiguration.Select = select.Split(',')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }
        else
        {
            reportConfiguration.Select = m365Service.Metadata.AvailableColumns
                .Where(x => x.DefaultIncluded)
                .Select(x => x.Id)
                .ToArray();
        }
        
        var queryOptions = new QueryOptions
        {
            PriorityLevel = PriorityLevel.Low,
            DataFor = DataFor.Email,
            MaxItemCount = 15,
            ReportConfiguration = reportConfiguration
        };

        var externalTenantId = new Guid("ffeb59f1-35d1-41c3-b46e-db75b4ebcda1");
        const int internalTenantId = 37;
        
        var result = await m365Service.GetItemsAsync(externalTenantId, internalTenantId, null, queryOptions,
            cancellationToken);
        
        var dataObject = new Dictionary<string, object?>
        {
            { "items", result.Items },
            { "selectedColumns", new SortedSet<string>(queryOptions.ReportConfiguration.Select??[]) },
            { "reportConfiguration", queryOptions.ReportConfiguration },
            { "externalTenantId", externalTenantId },
            { "tenantName", "Test Tenant Name" },
            { "metadata", m365Service.Metadata },
            { "reportType", reportType },
            { "reportTag", reportTag },
            { "reportTitle", "Test Report Title" },
        };
        
        var extraItems = await m365Service.GetExtraItemsAsync(externalTenantId, internalTenantId, null,
            queryOptions, cancellationToken);

        foreach (var key in extraItems.Keys)
        {
            dataObject.TryAdd(key, extraItems[key]);
        }

        var razorTemplateEngine = new RazorTemplateEngineV2();
        var htmlContent = await razorTemplateEngine.RenderTemplateAsync(reportType.ToString(), dataObject);
        
        return Ok(htmlContent);
    }
}