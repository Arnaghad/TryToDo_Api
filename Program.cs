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

app.MapGet("/items", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }

    var items = DatabaseHelper.GetItemsByUserId(user.Id);
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

    var categories = DatabaseHelper.GetCategoriesByUserId(user.Id);
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

    // Після item.UserGuid = user.Id;
    item.UserGuid = user.Id;
    try
    {
        DatabaseHelper.AddItem(
            item.Name,
            item.UserGuid,
            item.Description,
            item.CategoryId, // УВАГА: Це випадкове число з клієнта!
            item.AprxHours,
            item.EndedAt,
            item.IsLooped,
            item.Priority
        );
    }
    catch (Exception ex)
    {
         // Базова обробка помилок бази даних (особливо ForeignKey)
         Console.WriteLine($"Error adding item: {ex.Message}"); // Логування помилки на сервері
         // Повертаємо помилку клієнту
         return Results.Problem($"Internal server error while adding item: {ex.InnerException?.Message ?? ex.Message}", statusCode: 500);
    }
    // Приберіть рядок await userManager.UpdateAsync(user); якщо він там є

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
     // 1. Отримуємо авторизованого користувача
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        // Якщо користувач не знайдений (малоймовірно при [Authorize]), повертаємо помилку
        return Results.NotFound("User not found");
    }

    // 2. Читаємо тіло запиту (JSON з даними категорії)
    using var reader = new StreamReader(x.Request.Body);
    var body = await reader.ReadToEndAsync();
    Category? category = null; // Використовуємо Category? для можливості null
    try
    {
        category = JsonSerializer.Deserialize<Category>(body); // Десеріалізуємо JSON
    }
    catch (JsonException jsonEx)
    {
        Console.WriteLine($"Error deserializing category: {jsonEx.Message}");
        return Results.BadRequest($"Invalid JSON format for category: {jsonEx.Message}");
    }

    // Перевіряємо, чи вдалося розібрати JSON і чи є назва
    if (category == null || string.IsNullOrWhiteSpace(category.Name))
    {
        return Results.BadRequest("Category data is invalid or missing 'Name'.");
    }

    // 3. Встановлюємо ID користувача для категорії
    // UserGuid встановлюється АВТОМАТИЧНО з токена! Не потрібно його передавати в JSON.
    // Ми беремо його з об'єкта user.
    string userId = user.Id;

    // 4. ВИКЛИКАЄМО МЕТОД ЗБЕРЕЖЕННЯ В БАЗУ ДАНИХ
    try
    {
        // Переконуємось, що колір не null (можна встановити за замовчуванням)
        string color = string.IsNullOrWhiteSpace(category.Color) ? "black" : category.Color;

        DatabaseHelper.AddCategory(category.Name, color, userId); // Передаємо дані у ваш метод

        Console.WriteLine($"Category '{category.Name}' added for user '{userId}'."); // Логування на сервері

        // Успіх! Стандартна відповідь для POST - 201 Created
        // В ідеалі, тут можна повернути створений об'єкт або шлях до нього,
        // але для простоти повернемо Ok або Created.
        // return Results.Created($"/categories/{newly_created_id}", category); // Потрібно отримати ID
        return Results.Ok("Category added successfully."); // Повертаємо простий успіх 200 OK
    }
    catch (Exception ex) // Обробка можливих помилок бази даних
    {
        Console.WriteLine($"Error saving category '{category.Name}' to database: {ex.Message}");
        // Повертаємо загальну помилку сервера
        return Results.Problem($"An error occurred while saving the category.", statusCode: 500);
    }
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

app.MapGet("/user", [Authorize] async (HttpContext x, UserManager<AuthUser> userManager) =>
{
    var user = await userManager.GetUserAsync(x.User);
    if (user == null)
    {
        return Results.NotFound("User not found");
    }
    var json = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
    return Results.Content(json, "application/json");
})
.Produces<AuthUser>();

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
