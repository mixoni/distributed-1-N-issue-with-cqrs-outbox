using System.Net.Http.Json;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Bff.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHttpContextAccessor();   // 👈 important
builder.Services.AddScoped<CallMetrics>();   // 👈 per-request metrics

builder.Services.AddHttpClient("orders", c =>
    c.BaseAddress = new Uri(builder.Configuration["Orders:BaseUrl"] ?? "http://localhost:5002/"))
    .AddHttpMessageHandler(sp => new MetricsHandler(sp.GetRequiredService<IHttpContextAccessor>())); 

builder.Services.AddHttpClient("customers", c =>
    c.BaseAddress = new Uri(builder.Configuration["Customers:BaseUrl"] ?? "http://localhost:5001/"))
    .AddHttpMessageHandler(sp => new MetricsHandler(sp.GetRequiredService<IHttpContextAccessor>())); 


var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors();
app.MapControllers();
app.Run();

namespace Bff.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("api/orders")]
    public class OrdersBffController : ControllerBase
    {
        private readonly IHttpClientFactory _http;
        private readonly CallMetrics _metrics;
        public OrdersBffController(IHttpClientFactory http, CallMetrics metrics)
        {
            _http = http;
            _metrics = metrics;
        }

        record OrderRow(int Id, int CustomerId, decimal Total);
        record OrdersReadRow(int OrderId, string CustomerName, decimal Total);
        record CustomerDto(int Id, string Name);
        record CustomersBatchRequest(int[] Ids);

        [HttpGet("v1/naive")]
        public async Task<ActionResult<IEnumerable<object>>> Naive()
        {
            _metrics.Reset();
            var sw = Stopwatch.StartNew();

            var orders = await _http.CreateClient("orders").GetFromJsonAsync<List<OrderRow>>("api/orders");
            var custClient = _http.CreateClient("customers");
            var result = new List<object>();
            foreach (var o in orders!)
            {
                var cust = await custClient.GetFromJsonAsync<CustomerDto>($"api/customers/{o.CustomerId}"); // 1+N!
                result.Add(new { o.Id, o.Total, CustomerName = cust!.Name });
            }
            sw.Stop();
            Response.Headers["X-Metrics-Total"] = _metrics.Total.ToString();
            Response.Headers["X-Metrics-CustomersById"] = _metrics.CustomersById.ToString();
            Response.Headers["X-Metrics-CustomersBatch"] = _metrics.CustomersBatch.ToString();
            Response.Headers["X-Metrics-OrdersList"] = _metrics.OrdersList.ToString();
            Response.Headers["X-Metrics-OrdersRead"] = _metrics.OrdersRead.ToString();

            return Ok(result);
        }

        [HttpGet("v1/batched")]
        public async Task<ActionResult<IEnumerable<object>>> Batched()
        {
            _metrics.Reset();
            var sw = Stopwatch.StartNew();

            var orders = await _http.CreateClient("orders").GetFromJsonAsync<List<OrderRow>>("api/orders");
            var ids = orders!.Select(o => o.CustomerId).Distinct().ToArray();
            var resp = await _http.CreateClient("customers").PostAsJsonAsync("api/customers/batch", new CustomersBatchRequest(ids));
            var map = await resp.Content.ReadFromJsonAsync<Dictionary<int, CustomerDto>>();
            
            sw.Stop();

            Response.Headers["X-Metrics-Total"] = _metrics.Total.ToString();
            Response.Headers["X-Metrics-CustomersById"] = _metrics.CustomersById.ToString();
            Response.Headers["X-Metrics-CustomersBatch"] = _metrics.CustomersBatch.ToString();
            Response.Headers["X-Metrics-OrdersList"] = _metrics.OrdersList.ToString();
            Response.Headers["X-Metrics-OrdersRead"] = _metrics.OrdersRead.ToString();

            return Ok(orders.Select(o => new { o.Id, o.Total, CustomerName = map![o.CustomerId].Name }));
        }

        [HttpGet("v2/read-model")]
        public async Task<ActionResult<IEnumerable<object>>> ReadModel()
        {
            _metrics.Reset();
            
            Response.Headers["X-Metrics-Total"] = _metrics.Total.ToString();
            Response.Headers["X-Metrics-CustomersById"] = _metrics.CustomersById.ToString();
            Response.Headers["X-Metrics-CustomersBatch"] = _metrics.CustomersBatch.ToString();
            Response.Headers["X-Metrics-OrdersList"] = _metrics.OrdersList.ToString();
            Response.Headers["X-Metrics-OrdersRead"] = _metrics.OrdersRead.ToString();
            
            return Ok(await _http.CreateClient("orders").GetFromJsonAsync<List<OrdersReadRow>>("api/orders/read"));
        }

    }
}
