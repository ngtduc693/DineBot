using System;
using System.Collections.Generic;
using System.Linq;

namespace FoodOrderBots.Models;

public class FoodOrderDetails
{
    public string? OrderId { get; set; } = Guid.NewGuid().ToString();
    public string? Intent { get; set; }
    public Dictionary<string, int> FoodItems { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> Drinks { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> Sides { get; set; } = new Dictionary<string, int>();
    public List<string> Customizations { get; set; } = new List<string>();
    public Dictionary<string, int> Combos { get; set; } = new Dictionary<string, int>();
    public List<string> FoodTypes { get; set; } = new List<string>();
    public List<string> Requests { get; set; } = new List<string>();

    public bool HasItems()
    {
        return FoodItems.Any() || Drinks.Any() || Sides.Any() || Combos.Any();
    }

    public override string ToString()
    {
        var details = new List<string>();

        if (Combos.Any())
            details.Add($"Combo(s): {string.Join(", ", Combos.Select(kv => $"{kv.Key} (x{kv.Value})"))}");

        if (FoodItems.Any())
            details.Add($"Food: {string.Join(", ", FoodItems.Select(kv => $"{kv.Key} (x{kv.Value})"))}");

        if (Drinks.Any())
            details.Add($"Drinks: {string.Join(", ", Drinks.Select(kv => $"{kv.Key} (x{kv.Value})"))}");

        if (Sides.Any())
            details.Add($"Sides: {string.Join(", ", Sides.Select(kv => $"{kv.Key} (x{kv.Value})"))}");

        if (Customizations.Any())
            details.Add($"Customizations: {string.Join(", ", Customizations)}");

        if (Requests.Any())
            details.Add($"Special requests: {string.Join(", ", Requests)}");

        return string.Join("\n", details);
    }
}