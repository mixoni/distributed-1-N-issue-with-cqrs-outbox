using System.Net.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Bff.Api;

public class CallMetrics
{
    public int Total, CustomersById, CustomersBatch, OrdersList, OrdersRead;
    public void Reset() => Total = CustomersById = CustomersBatch = OrdersList = OrdersRead = 0;
}

public class MetricsHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _hca;
    private static readonly Regex CustById = new(@"/api/customers/\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public MetricsHandler(IHttpContextAccessor hca) => _hca = hca;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        // Always take CallMetrics from the CURRENT request scope
        var metrics = _hca.HttpContext?.RequestServices.GetService<CallMetrics>();
        if (metrics != null)
        {
            var path = request.RequestUri!.AbsolutePath.ToLowerInvariant();
            metrics.Total++;
            if (path.Contains("/api/customers/batch")) metrics.CustomersBatch++;
            else if (CustById.IsMatch(path)) metrics.CustomersById++;
            else if (path.Contains("/api/orders/read")) metrics.OrdersRead++;
            else if (path.Contains("/api/orders")) metrics.OrdersList++;
        }

        return await base.SendAsync(request, ct);
    }
}
