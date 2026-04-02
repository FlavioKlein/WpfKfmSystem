using WpfPorkProcessSystem.Data;
using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Utilities;

/// <summary>
/// Utility class for debugging InMemoryDatabase operations
/// </summary>
public static class DatabaseDebugger
{
    public static void PrintChamberStocks(InMemoryDatabase db, string moment)
    {
        Console.WriteLine($"\n=== Chamber Stocks - {moment} ===");
        var chambers = db.GetAll<SprayChamberModel>();
        
        foreach (var chamber in chambers.OrderBy(c => c.Id))
        {
            Console.WriteLine($"Chamber {chamber.Id}: Stock = {chamber.Stock}, Capacity = {chamber.Capacity}");
        }
        Console.WriteLine("=====================================\n");
    }

    public static void PrintProductionOrderSummary(InMemoryDatabase db, int orderId)
    {
        var order = db.GetById<ProductionOrderModel>(orderId);
        
        if (order == null)
        {
            Console.WriteLine($"Order {orderId} not found!");
            return;
        }

        Console.WriteLine($"\n=== Production Order {orderId} - {order.Type} ===");
        Console.WriteLine($"Qty Carcasses: {order.QuantityCarcasses}");
        Console.WriteLine($"Total Weight: {order.TotalWeighing}");
        Console.WriteLine($"Items Count: {order.Items?.Count ?? 0}");
        Console.WriteLine($"Notes Count: {order.Notes?.Count ?? 0}");
        
        if (order.Items != null)
        {
            foreach (var item in order.Items.OrderBy(i => i.Sequential))
            {
                Console.WriteLine($"  Item {item.Sequential}: Chamber {item.SprayChamberId}, " +
                                  $"Initial Stock: {item.SprayChamberInitialStock}, " +
                                  $"Current Stock: {item.SprayChamberStock}");
            }
        }
        Console.WriteLine("=====================================\n");
    }
}
