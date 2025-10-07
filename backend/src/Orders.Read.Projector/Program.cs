using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Net.Http.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<OrdersReadDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddHttpClient("customers", c => c.BaseAddress = new Uri(builder.Configuration["Customers:BaseUrl"] ?? "http://localhost:5001/"));
builder.Services.AddHostedService<OrderCreatedConsumer>();
var app = builder.Build();
await app.RunAsync();

public class OrdersReadDbContext : DbContext
{
    public OrdersReadDbContext(DbContextOptions<OrdersReadDbContext> opt) : base(opt) { }
    public DbSet<OrdersRead> OrdersRead => Set<OrdersRead>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<OrdersRead>().ToTable("orders_read").HasKey(x => x.OrderId);
}
public class OrdersRead { public int OrderId { get; set; } public string CustomerName { get; set; } = default!; public decimal Total { get; set; } public DateTime CreatedAtUtc { get; set; } }

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IServiceProvider _sp; private readonly IHttpClientFactory _http; private readonly IConfiguration _cfg;
    public OrderCreatedConsumer(IServiceProvider sp, IHttpClientFactory http, IConfiguration cfg) { _sp = sp; _http = http; _cfg = cfg; }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = _cfg["Rabbit:Host"] ?? "localhost" };
        var conn = factory.CreateConnection();
        var ch = conn.CreateModel();
        ch.ExchangeDeclare("orders", ExchangeType.Fanout, durable: true);
        var q = ch.QueueDeclare().QueueName;
        ch.QueueBind(q, "orders", "");

        var consumer = new EventingBasicConsumer(ch);
        consumer.Received += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var ev = System.Text.Json.JsonSerializer.Deserialize<OrderCreated>(json)!;
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersReadDbContext>();

            if (await db.OrdersRead.AnyAsync(x => x.OrderId == ev.OrderId)) return;
            var client = _http.CreateClient("customers");
            var cust = await client.GetFromJsonAsync<CustomerDto>($"api/customers/{ev.CustomerId}");
            var name = cust?.Name ?? $"Customer#{ev.CustomerId}";

            db.OrdersRead.Add(new OrdersRead { OrderId = ev.OrderId, CustomerName = name, Total = ev.Total, CreatedAtUtc = ev.CreatedAtUtc });
            await db.SaveChangesAsync();
        };
        ch.BasicConsume(q, autoAck: true, consumer);
        return Task.CompletedTask;
    }

    private record OrderCreated(Guid EventId, int OrderId, int CustomerId, decimal Total, DateTime CreatedAtUtc);
    private record CustomerDto(int Id, string Name);
}
