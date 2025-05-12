namespace FoodOrderBots.Models;

public class MenuItem
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Type { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsVegan { get; set; }
    public bool IsGlutenFree { get; set; }
}
