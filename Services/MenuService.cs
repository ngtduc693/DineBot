using FoodOrderBots.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoodOrderBots.Services;

public class MenuService
{
    private readonly Dictionary<string, MenuItem> _menuItems;

    public MenuService()
    {
        _menuItems = new Dictionary<string, MenuItem>
            {
                { "cheeseburger", new MenuItem { Name = "Cheeseburger", Price = 5.99m, Type = "FoodItem", IsAvailable = true } },
                { "veggie burger", new MenuItem { Name = "Veggie Burger", Price = 6.49m, Type = "FoodItem", IsAvailable = true, IsVegan = true } },
                { "chicken sandwich", new MenuItem { Name = "Chicken Sandwich", Price = 6.99m, Type = "FoodItem", IsAvailable = true } },
                { "coke", new MenuItem { Name = "Coke", Price = 1.99m, Type = "Drink", IsAvailable = true } },
                { "iced tea", new MenuItem { Name = "Iced Tea", Price = 1.99m, Type = "Drink", IsAvailable = true } },
                { "fries", new MenuItem { Name = "Fries", Price = 2.49m, Type = "Side", IsAvailable = true } },
                { "cheeseburger combo", new MenuItem { Name = "Cheeseburger Combo", Price = 8.99m, Type = "Combo", IsAvailable = true } },
                { "vegan combo", new MenuItem { Name = "Vegan Combo", Price = 9.49m, Type = "Combo", IsAvailable = true, IsVegan = true } }
            };
    }

    public MenuItem GetItem(string name)
    {
        return _menuItems.TryGetValue(name.ToLower(), out var item) ? item : null;
    }

    public IEnumerable<MenuItem> GetItemsByType(string type)
    {
        return _menuItems.Values.Where(item => item.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<MenuItem> GetItemsByFoodType(string foodType)
    {
        return foodType.ToLower() switch
        {
            "vegan" => _menuItems.Values.Where(item => item.IsVegan),
            "gluten-free" => _menuItems.Values.Where(item => item.IsGlutenFree),
            _ => Enumerable.Empty<MenuItem>()
        };
    }

    public bool IsItemAvailable(string name)
    {
        return GetItem(name)?.IsAvailable ?? false;
    }
}
