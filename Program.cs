using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
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
DatabaseHelper.AddItem("Complete project", Guid.NewGuid().ToString(), "Finish the report");

app.MapGet("/items", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    if (user.UserId == Guid.Empty)
    {
        return Results.NotFound("User doesnt have items");
    }

    var items = DatabaseHelper.GetItemsByUserId(user.UserId.ToString());
    var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
    return Results.Ok(json);
}).WithOpenApi(genOp =>
{
    genOp.Description = "Повертає всі завдання користувача, котрі були ним раніше створені";
    genOp.Summary = "Повертає всі завдання користувача, котрі були ним раніше створені";
    return genOp;
})
.Produces<IList<Item>>();

app.MapGet("/categories", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    if (user.UserId == Guid.Empty)
    {
        return Results.NotFound("User doesnt have items");
    }

    var categories = DatabaseHelper.GetCategoriesByUserId(user.UserId.ToString());
    var json = JsonSerializer.Serialize(categories, new JsonSerializerOptions { WriteIndented = true });
    return Results.Ok(json);
}).WithOpenApi(genOp =>
{
    genOp.Description = "Повертає всі категорії користувача, котрі були ним раніше створені";
    genOp.Summary = "Повертає всі категорії користувача, котрі були ним раніше створені";
    return genOp;
})
.Produces<IList<Category>>();

app.MapPost("/items/add", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    var item = JsonSerializer.Deserialize<Item>(body);

    if (user.UserId == null)
    {
        user.UserId = Guid.NewGuid();
    }

    item.UserGuid = user.UserId.ToString();
    await userManager.UpdateAsync(user);
    return Results.Ok("Item added");
}).WithOpenApi(genOp =>
{
    genOp.Description = "Для того аби створити нове завдання для авторизованого користувача \n" +
                        "потрібно в Body запиту, вставити наступний Json: \n" +
                        "{ \n" +
                        "\t Name: назва завдання, обов'язково в лапках} \n" +
                        "\t Description: опис, теж в лапках, всі тексти вставляти в лапках, \n" +
                        "\t AprxHours: приблизний строк завдання в годинах, ПИСАТИ БЕЗ ЛАПОК, БО ЦЕ ЧИСЛО \n" +
                        "\t EndedAt: дата і час дедлайну у форматі YYYY:MM:DD HH:MM:SS, В ЛАПКАХ \n" +
                        "\t Priority: приорітет від 0 до 10, без лапок \n" +
                        "\t CategoryId: число, без лапок \n" +
                        "\t IsLooped: true чи false, в лапках \n" +
                        "}";
    genOp.Summary = "Додавання нового завдання до користувача";
    return genOp;
})
.Produces<string>();;

app.MapPost("/categories/add", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    var category = JsonSerializer.Deserialize<Category>(body);

    if (user.UserId == null)
    {
        user.UserId = Guid.NewGuid();
    }

    category.UserGuid = user.UserId.ToString();
    await userManager.UpdateAsync(user);
    return Results.Ok("Category added");
});

app.MapPut("/categories/update", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    var updatedCategory = JsonSerializer.Deserialize<Category>(body);

    if (!DatabaseHelper.ExistsCategory(updatedCategory.Id))
    {
        return Results.NotFound("Category not found");
    } else {
        DatabaseHelper.UpdateCategory(updatedCategory.Id, updatedCategory.Name, updatedCategory.Color);
    }
    
    return Results.Ok("Category updated");
});

app.MapPut("/items/update", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    var updatedItem = JsonSerializer.Deserialize<Item>(body);

    if (!DatabaseHelper.ExistsItem(updatedItem.Id))
    {
        return Results.NotFound("Item not found");
    } else {
        DatabaseHelper.UpdateItem(updatedItem.Id, updatedItem.Name, updatedItem.Description, updatedItem.AprxHours,
            updatedItem.EndedAt, updatedItem.IsLooped, updatedItem.Priority, updatedItem.CategoryId);
    }
    
    return Results.Ok("Item updated");
});

app.MapDelete("/items/delete/{id}", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }
    
    var id = int.Parse(x.Request.Query["id"]);

    if (!DatabaseHelper.ExistsItem(id))
    {
        return Results.NotFound("Item not found");
    } else {
        DatabaseHelper.DeleteItem(id);
    }
    
    return Results.Ok("Item deleted");
});

app.MapDelete("/categories/delete/{id}", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }
    
    var id = int.Parse(x.Request.Query["id"]);

    if (!DatabaseHelper.ExistsCategory(id))
    {
        return Results.NotFound("Item not found");
    } else {
        DatabaseHelper.DeleteCategory(id);
    }
    
    return Results.Ok("Category deleted");
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
