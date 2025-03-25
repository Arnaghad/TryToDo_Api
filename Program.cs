using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using TryToDo_Api.Classes;
using TryToDo_Api.Contexts;
using TryToDo_Api.Methods;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1.0.0",
        Title = "ToDo API",
        Description = "ToDo API for ToDo application",
        License = new OpenApiLicense
        {
            Name = "License",
            Url = new Uri("https://www.gnu.org/licenses/gpl-3.0.uk.html")
        }
    });
    
});
builder.Services.AddDbContext<DatabaseContext>();
builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<AuthUser>()
    .AddEntityFrameworkStores<DatabaseContext>();
builder.Services.AddScoped<UserManager<AuthUser>>();
builder.Services.AddSingleton<IEmailSender<AuthUser>, DummyEmailSender>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

DatabaseHelper.AddCategory("Work", "Blue", Guid.NewGuid().ToString());
DatabaseHelper.AddItem("Complete project", Guid.NewGuid().ToString(), "Finish the report", 1, 5, DateTime.Now.AddDays(2), false, 1);

app.MapGet("/items", async (HttpContext x) =>
{
    var listItem = DatabaseHelper.GetAllItems();
    var json = JsonSerializer.Serialize(listItem);
    return Results.Content(json, "application/json");
});

app.UseHttpsRedirection();
app.MapIdentityApi<AuthUser>();
app.UseAuthorization();
app.UseSwagger(options =>
{
    options.SerializeAsV2 = true;
});

// change swagger endpoint to /
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ToDo API");
    c.RoutePrefix = "";
});
app.Run();
