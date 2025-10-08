using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Contracts;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<OrdersDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseSwagger(); 
app.UseSwaggerUI();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.EnsureCreated();
}

app.MapControllers();
app.Run();

public class OrdersDbContext : DbContext
{
    public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options) { }
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<OrdersRead> OrdersRead => Set<OrdersRead>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().ToTable("orders");
        modelBuilder.Entity<OutboxMessage>().ToTable("outbox");
        modelBuilder.Entity<OrdersRead>().ToTable("orders_read").HasKey(x => x.OrderId);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        AddOutbox();
        return base.SaveChanges();
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddOutbox();
        return base.SaveChangesAsync(cancellationToken);
    }
    private void AddOutbox()
    {
        var newOrders = ChangeTracker.Entries<Order>().Where(e => e.State == EntityState.Added).Select(e => e.Entity).ToList();
        foreach (var o in newOrders)
        {
            var ev = new OrderCreatedEvent(Guid.NewGuid(), o.Id, o.CustomerId, o.Total, o.CreatedAtUtc);
            Outbox.Add(new OutboxMessage { OccurredOnUtc = DateTime.UtcNow, Type = nameof(OrderCreatedEvent), Payload = JsonSerializer.Serialize(ev) });
        }
    }
}

public class Order { public int Id { get; set; } public int CustomerId { get; set; } public decimal Total { get; set; } public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow; }
public class OutboxMessage { public long Id { get; set; } public DateTime OccurredOnUtc { get; set; } public string Type { get; set; } = default!; public string Payload { get; set; } = default!; public DateTime? ProcessedOnUtc { get; set; } }
public class OrdersRead { public int OrderId { get; set; } public string CustomerName { get; set; } = default!; public decimal Total { get; set; } public DateTime CreatedAtUtc { get; set; } }

namespace Orders.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly OrdersDbContext _db;
        public OrdersController(OrdersDbContext db) => _db = db;

        public record CreateOrderRequest(int CustomerId, decimal Total);

        [HttpPost]
        public async Task<ActionResult> Create([FromBody] CreateOrderRequest req)
        {
            _db.Orders.Add(new Order { CustomerId = req.CustomerId, Total = req.Total });
            await _db.SaveChangesAsync();
            return Created("", null);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> List()
            => Ok(await _db.Orders.OrderByDescending(o => o.CreatedAtUtc)
                  .Select(o => new { o.Id, o.CustomerId, o.Total, o.CreatedAtUtc })
                //   .Take(50)
                  .ToListAsync());

        [HttpGet("read")]
        public async Task<ActionResult<IEnumerable<object>>> Read()
            => Ok(await _db.OrdersRead.OrderByDescending(x => x.CreatedAtUtc)
                  .Select(x => new { x.OrderId, x.CustomerName, x.Total, x.CreatedAtUtc })
                //   .Take(50)
                  .ToListAsync());
    }
}
