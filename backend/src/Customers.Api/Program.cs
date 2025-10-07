using Microsoft.EntityFrameworkCore;
using BuildingBlocks.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<CustomersDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Db")));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseSwagger(); app.UseSwaggerUI();
app.UseCors();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
    db.Database.EnsureCreated();
    if (!db.Customers.Any())
    {
        db.Customers.AddRange(
            new Customer { Name = "Alice" },
            new Customer { Name = "Bob" },
            new Customer { Name = "Charlie" }
        );
        db.SaveChanges();
    }
}
app.MapControllers();
app.Run();

public class CustomersDbContext : DbContext
{
    public CustomersDbContext(DbContextOptions<CustomersDbContext> options) : base(options) {}
    public DbSet<Customer> Customers => Set<Customer>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>().ToTable("customers");
        base.OnModelCreating(modelBuilder);
    }
}
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
}

namespace Customers.Api.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using BuildingBlocks.Contracts;
    using Microsoft.EntityFrameworkCore;

    [ApiController]
    [Route("api/customers")]
    public class CustomersController : ControllerBase
    {
        private readonly CustomersDbContext _db;
        public CustomersController(CustomersDbContext db) => _db = db;

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerDto>> GetById(int id)
            => await _db.Customers.Where(c => c.Id == id)
                .Select(c => new CustomerDto(c.Id, c.Name))
                .SingleOrDefaultAsync() is { } dto ? Ok(dto) : NotFound();

        [HttpPost("batch")]
        public async Task<ActionResult<Dictionary<int, CustomerDto>>> Batch([FromBody] CustomersBatchRequest req)
            => Ok(await _db.Customers.Where(c => req.Ids.Contains(c.Id))
                 .Select(c => new CustomerDto(c.Id, c.Name))
                 .ToDictionaryAsync(c => c.Id, c => c));
    }
}
