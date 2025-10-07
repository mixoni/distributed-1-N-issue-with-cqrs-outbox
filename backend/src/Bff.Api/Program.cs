using System.Net.Http.Json;
using System.Linq;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddHttpClient("orders", c => c.BaseAddress = new Uri(builder.Configuration["Orders:BaseUrl"] ?? "http://localhost:5002/"));
builder.Services.AddHttpClient("customers", c => c.BaseAddress = new Uri(builder.Configuration["Customers:BaseUrl"] ?? "http://localhost:5001/"));
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
        public OrdersBffController(IHttpClientFactory http) => _http = http;

        record OrderRow(int Id, int CustomerId, decimal Total);
        record OrdersReadRow(int OrderId, string CustomerName, decimal Total);
        record CustomerDto(int Id, string Name);
        record CustomersBatchRequest(int[] Ids);

        [HttpGet("v1/naive")]
        public async Task<ActionResult<IEnumerable<object>>> Naive()
        {
            var orders = await _http.CreateClient("orders").GetFromJsonAsync<List<OrderRow>>("api/orders");
            var custClient = _http.CreateClient("customers");
            var result = new List<object>();
            foreach (var o in orders!)
            {
                var cust = await custClient.GetFromJsonAsync<CustomerDto>($"api/customers/{o.CustomerId}"); // N+1!
                result.Add(new { o.Id, o.Total, CustomerName = cust!.Name });
            }
            return Ok(result);
        }

        [HttpGet("v1/batched")]
        public async Task<ActionResult<IEnumerable<object>>> Batched()
        {
            var orders = await _http.CreateClient("orders").GetFromJsonAsync<List<OrderRow>>("api/orders");
            var ids = orders!.Select(o => o.CustomerId).Distinct().ToArray();
            var resp = await _http.CreateClient("customers").PostAsJsonAsync("api/customers/batch", new CustomersBatchRequest(ids));
            var map = await resp.Content.ReadFromJsonAsync<Dictionary<int, CustomerDto>>();
            return Ok(orders.Select(o => new { o.Id, o.Total, CustomerName = map![o.CustomerId].Name }));
        }

        [HttpGet("v2/read-model")]
        public async Task<ActionResult<IEnumerable<object>>> ReadModel()
            => Ok(await _http.CreateClient("orders").GetFromJsonAsync<List<OrdersReadRow>>("api/orders/read"));
    }
}
