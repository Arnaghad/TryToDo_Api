using TryToDo_Api.Classes;
using TryToDo_Api.Contexts;

namespace TryToDo_Api.Methods;

public static class DatabaseHelper
{
    public static void AddCategory(string categoryName, string categoryColor, string categoryUserId)
    {
        using (var context = new DatabaseContext())
        {
            Category category = new Category
            {
                Name = categoryName,
                Color = categoryColor,
                UserGuid = categoryUserId
            };

            context.Categories.Add(category);
            context.SaveChanges();
        }
    }

    public static void AddItem(string itemName, string itemUserId, string? itemDescription = null, int? categoryId = null, int? itemAprxHours = null, DateTime? itemEndedAt = null, bool? itemIsLooped = false, int? itemPriority = null)
    {
        using (var context = new DatabaseContext())
        {
            Item item = new Item
            {
                Name = itemName,
                Description = itemDescription,
                AprxHours = itemAprxHours,
                EndedAt = itemEndedAt,
                Priority = itemPriority,
                CategoryId = categoryId,
                IsLooped = itemIsLooped,
                UserGuid = itemUserId
            };

            context.Items.Add(item);
            context.SaveChanges();
        }
    }
    
    public static void DeleteItem(int itemId)
    {
        using (var context = new DatabaseContext())
        {
            var item = context.Items.Find(itemId);
            if (item != null)
            {
                context.Items.Remove(item);
                context.SaveChanges();
            }
            else
            {
                Console.WriteLine($"Item with ID {itemId} not found.");
            }
        }
    }
    
    public static void DeleteCategory(int categoryId)
    {
        using (var context = new DatabaseContext())
        {
            var category = context.Categories.Find(categoryId);
            if (category != null)
            {
                // First delete all items related to this category
                var relatedItems = context.Items.Where(i => i.CategoryId == categoryId).ToList();
                context.Items.RemoveRange(relatedItems);
            
                // Then delete the category
                context.Categories.Remove(category);
                context.SaveChanges();
            }
            else
            {
                Console.WriteLine($"Category with ID {categoryId} not found.");
            }
        }
    }
    
    public static void UpdateCategory(int categoryId, string newName, string newColor)
    {
        using (var context = new DatabaseContext())
        {
            var category = context.Categories.Find(categoryId);
            if (category != null)
            {
                category.Name = newName;
                category.Color = newColor;
                context.SaveChanges();
            }
            else
            {
                Console.WriteLine($"Category with ID {categoryId} not found.");
            }
        }
    }

    public static void UpdateItem(int itemId, string newName, string? newDescription = null, int? newAprxHours = null, DateTime? newEndedAt = null, bool? newIsLooped = null, int? newPriority = null, int? newCategoryId = null)
    {
        using (var context = new DatabaseContext())
        {
            var item = context.Items.Find(itemId);
            if (item != null)
            {
                item.Name = newName;
                item.Description = newDescription;
                item.AprxHours = newAprxHours;
                item.EndedAt = newEndedAt;
                item.IsLooped = newIsLooped;
                item.Priority = newPriority;
                item.CategoryId = newCategoryId;
                context.SaveChanges();
            }
            else
            {
                Console.WriteLine($"Item with ID {itemId} not found.");
            }
        }
    }
    
    public static List<Category> GetAllCategories()
    {
        using (var context = new DatabaseContext())
        {
            return context.Categories.ToList();
        }
    }

    public static List<Item> GetAllItems()
    {
        using (var context = new DatabaseContext())
        {
            return context.Items.ToList();
        }
    }

    public static List<Category> GetCategoriesByUserId(string userId)
    {
        using (var context = new DatabaseContext())
        {
            return context.Categories.Where(c => c.UserGuid == userId).ToList();
        }
    }

    public static List<Item> GetItemsByUserId(string userId)
    {
        using (var context = new DatabaseContext())
        {
            return context.Items.Where(i => i.UserGuid == userId).ToList();
        }
    }
    
    public static Item? GetItemById(int id)
    {
        using (var context = new DatabaseContext())
        {
            return context.Items.FirstOrDefault(i => i.Id == id);
        }
    }
    
    public static Category? GetCategorybyId(int id)
    {
        using (var context = new DatabaseContext())
        {
            return context.Categories.FirstOrDefault(c => c.Id == id);
        }
    }
    
    public static bool ExistsItem(int id)
    {
        using (var context = new DatabaseContext())
        {
            return context.Items.Any(c => c.Id == id);
        }
    }
    
    public static bool ExistsCategory(int id)
    {
        using (var context = new DatabaseContext())
        {
            return context.Categories.Any(c => c.Id == id);
        }
    }
}