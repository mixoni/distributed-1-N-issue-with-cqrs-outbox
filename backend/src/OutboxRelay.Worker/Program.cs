using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<OrdersDbContext>(opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddHostedService<OutboxRelayService>();
var app = builder.Build();
await app.RunAsync();

public class OutboxRelayService : BackgroundService
{
    private readonly IServiceProvider _sp; private readonly IConfiguration _cfg;
    public OutboxRelayService(IServiceProvider sp, IConfiguration cfg) { _sp = sp; _cfg = cfg; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = _cfg["Rabbit:Host"] ?? "localhost" };
        using var conn = factory.CreateConnection();
        using var ch = conn.CreateModel();
        ch.ExchangeDeclare("orders", ExchangeType.Fanout, durable: true);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
            var batch = await db.Set<OutboxMessage>().Where(m => m.ProcessedOnUtc == null).OrderBy(m => m.Id).Take(100).ToListAsync(stoppingToken);
            foreach (var msg in batch)
            {
                var body = Encoding.UTF8.GetBytes(msg.Payload);
                ch.BasicPublish(exchange: "orders", routingKey: "", basicProperties: null, body: body);
                msg.ProcessedOnUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(1000, stoppingToken);
        }
    }
}

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<OutboxMessage>().ToTable("outbox");
}
public class OutboxMessage { public long Id { get; set; } public DateTime OccurredOnUtc { get; set; } public string Type { get; set; } = default!; public string Payload { get; set; } = default!; public DateTime? ProcessedOnUtc { get; set; } }
