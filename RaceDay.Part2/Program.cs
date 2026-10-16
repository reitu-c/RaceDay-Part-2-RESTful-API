using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Data;

var builder = WebApplication.CreateBuilder(args);

//Register the RaceDay database connection
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("RaceDayConnection")));

//Register controllers
builder.Services.AddControllers();

//Register services required for server-side sessions
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

//Register Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Register password hashing for dependency injection
builder.Services.AddScoped<RaceDay.Part2.Services.PasswordService>();

var app = builder.Build();

//Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//Enable session handling before controller endpoints execute
app.UseSession();

app.UseAuthorization();

app.MapControllers();

app.Run();

