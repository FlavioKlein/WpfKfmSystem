using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Data;

public static class DataSeeder
{
    public static void SeedData()
    {
        var db = InMemoryDatabase.Instance;

        db.ClearAllData();

        SeedProducts(db);
        SeedClassificationWeighing(db);
        SeedSprayChamber(db);
        SeedWeighingScale(db);
        SeedProductionOrder(db);
    }

    private static void SeedWeighingScale(InMemoryDatabase db)
    {
        _ = db.GetCollection<WeighingScaleModel>();

        db.Add<WeighingScaleModel>(new() { Id = 1, Name = "Tendal de Entrada Aspersão", Type = WeighingScaleType.Tent });
        db.Add<WeighingScaleModel>(new() { Id = 2, Name = "Tendal de Saída Aspersão", Type = WeighingScaleType.Tent });
    }

    private static void SeedProductionOrder(InMemoryDatabase db)
    {
        _ = db.GetCollection<ProductionOrderModel>();

        var executionDate = new DateTime(2026, 3, 15);

        var product = db.GetById<ProductModel>(1);

        #region Order Entrance / Leaving
        // Order 1 = Entrance
        var order = new ProductionOrderModel();
        order.Id = 1;
        order.Status = ProductionOrderStatusType.Finalized;
        order.Type = WeighingType.SprayChamberEntrance;        
        order.ProductId = 1;
        order.Product = product;
        order.WeighingScaleId = 1;
        order.ExecutionDate = executionDate;
        order.FacturingDate = order.ExecutionDate;
        order.ExpirationDate = order.ExecutionDate.AddDays(15);
        order.Shift = "Morning";
        order.Batch = "Batch 001";
        order.Hammer = "Hammer 001";
        order.Description = "Description Order 001";

        SeedProductionOrderItems(db, order);

        db.Add<ProductionOrderModel>(order);

        //Order 2 = Exit
        order = new ProductionOrderModel();
        order.Id = 2;
        order.Status = ProductionOrderStatusType.Finalized;
        order.Type = WeighingType.SprayChamberExit;
        order.ProductId = 1;
        order.Product = product;        
        order.EntranceOrderNumber = 1;
        order.WeighingScaleId = 1;
        order.ExecutionDate = executionDate.AddDays(1);
        order.FacturingDate = order.ExecutionDate;
        order.ExpirationDate = order.ExecutionDate.AddDays(15);
        order.Shift = "Morning";
        order.Batch = "Batch 001";
        order.Hammer = "Hammer 001";
        order.Description = "Description Order 002";

        SeedExitChamberProductionNotes(db, order, 1);

        db.Add<ProductionOrderModel>(order);
        #endregion
    }

    private static void SeedProductionOrderItems(InMemoryDatabase db, ProductionOrderModel order)
    {
        var listChamber = db.GetCollection<SprayChamberModel>().OrderBy(c => c.Id);

        order.Items = [];
        order.Notes = [];

        foreach (var chamber in listChamber)
        {
            var item = new ProductionOrderItemModel();
            
            item.Id = order.Items.Count + 1;
            item.Sequential = item.Id; // In this case will be the same as Id, but in a real scenario, it could be different
            item.ProductionOrderId = order.Id;
            item.SprayChamberId = chamber.Id;
            item.SprayChamberCapacity = chamber.Capacity;
            item.SprayChamberInitialStock = 0;
            item.SprayChamberStock = 0;
            item.AcceptClassificationIds = GenerateAcceptClass(db);
            item.AcceptClassifications = string.Join(", ", item.AcceptClassificationIds.Select(id => db.GetById<ClassificationWeighingModel>(id)?.Name ?? "Unknown"));

            SeedEntranceChamberProductionNotes(db, order, item, chamber);

            order.Items.Add(item);
        }
    }

    private static void SeedEntranceChamberProductionNotes(InMemoryDatabase db, ProductionOrderModel order, 
        ProductionOrderItemModel item, SprayChamberModel chamber)
    {
        _ = db.GetCollection<ProductionNotesModel>();

        var listClass = db.GetCollection<ClassificationWeighingModel>().OrderBy(c => c.Id);
        var roundQuantity = new Random().Next(1, chamber.Capacity);
        for (int i = 0; i < roundQuantity; i++)
        {
            // Randomly select a classification from the accepted classifications for this item
            var roundIndex = new Random().Next(0, item.AcceptClassificationIds.Length - 1);
            var classificationId = item.AcceptClassificationIds[roundIndex];
            var classification = listClass.FirstOrDefault(c => c.Id == classificationId);
            // Generate a random weight within the limits of the selected classification
            int lowerLimit = (int)Math.Round(classification?.LowerLimit ?? 0);
            int upperLimit = (int)Math.Round(classification?.UpperLimit ?? 0);
            var roundWeight = new Random().Next(lowerLimit, upperLimit);

            // Create a production note for this item
            var note = new ProductionNotesModel
            {
                Id = i + 1,
                ProductionOrderId = order.Id,
                ProductId = order.ProductId,
                Product = order.Product,
                ExecutionDate = order.ExecutionDate,
                FacturingDate = order.FacturingDate,
                ExpirationDate = order.ExpirationDate,
                WeighingScaleId = order.WeighingScaleId,                
                Shift = order.Shift,
                Batch = order.Batch,
                Hammer = order.Hammer,
                SprayChamberId = chamber.Id,
                ClassificationId = classificationId,
                Weight = roundWeight
            };

            item.SprayChamberStock++;
            chamber.Stock++;

            order.QuantityCarcasses++;
            order.TotalWeighing += roundWeight;

            order.Notes.Add(note);
        }
    }

    private static void SeedExitChamberProductionNotes(InMemoryDatabase db, ProductionOrderModel order, int entranceOrderId)
    {
        var entranceOrder = db.GetById<ProductionOrderModel>(entranceOrderId);

        if (entranceOrder == null) return;

        order.Notes = [];

        // Start with zero values for exit order (will be decremented as notes are processed)
        order.QuantityCarcasses = 0;
        order.TotalWeighing = 0;
        order.Items = [];

        // Create NEW copies of items from entrance order (deep copy)
        foreach (var entranceItem in entranceOrder.Items)
        {
            var exitItem = new ProductionOrderItemModel
            {
                Id = entranceItem.Id,
                Sequential = entranceItem.Sequential,
                ProductionOrderId = order.Id,
                SprayChamberId = entranceItem.SprayChamberId,
                SprayChamberCapacity = entranceItem.SprayChamberCapacity,
                SprayChamberInitialStock = entranceItem.SprayChamberStock, // Initial stock for exit = final stock from entrance
                SprayChamberStock = 0, // Will start at 0 and remain 0 for exit orders
                AcceptClassificationIds = entranceItem.AcceptClassificationIds,
                AcceptClassifications = entranceItem.AcceptClassifications
            };

            order.Items.Add(exitItem);

            var listNotesByChamber = entranceOrder.Notes.Where(n => n.SprayChamberId == exitItem.SprayChamberId).ToList();

            foreach (var entranceNote in listNotesByChamber)
            {
                // Create a production note for this exit item
                var exitNote = new ProductionNotesModel
                {
                    Id = entranceNote.Id,
                    ProductionOrderId = order.Id,
                    ProductId = order.ProductId,
                    Product = order.Product,
                    ExecutionDate = order.ExecutionDate,
                    FacturingDate = order.FacturingDate,
                    ExpirationDate = order.ExpirationDate,
                    WeighingScaleId = order.WeighingScaleId,
                    Shift = order.Shift,
                    Batch = order.Batch,
                    Hammer = order.Hammer,
                    SprayChamberId = entranceNote.SprayChamberId,
                    ClassificationId = entranceNote.ClassificationId,
                    Weight = entranceNote.Weight
                };

                // Decrement chamber stock
                var chamber = db.GetById<SprayChamberModel>(exitNote.SprayChamberId);
                if (chamber != null)
                {
                    chamber.Stock--;
                }

                // Increment counters for exit order (counting what's leaving)
                order.QuantityCarcasses++;
                order.TotalWeighing += exitNote.Weight;

                order.Notes.Add(exitNote);
            }
        }
    }

    private static int[] GenerateAcceptClass(InMemoryDatabase db)
    {
        var listClass = db.GetCollection<ClassificationWeighingModel>().OrderBy(c => c.Id);
        var acceptClassIds = new List<int>();

        var roundQuantity = new Random().Next(1, listClass.Count() + 1);

        for (int i = 0; i < roundQuantity; i++)
        {
            var randomIndex = new Random().Next(0, listClass.Count());
            var selectedClass = listClass.ElementAt(randomIndex);
            if (!acceptClassIds.Contains(selectedClass.Id))
            {
                acceptClassIds.Add(selectedClass.Id);
            }
        }

        return [.. acceptClassIds];
    }

    private static void SeedProducts(InMemoryDatabase db)
    {
        _ = db.GetCollection<ProductModel>();

        db.Add<ProductModel>(new() { Id = 1, Name = "Carcaça Suína" });
        db.Add<ProductModel>(new() { Id = 2, Name = "Carcaça Bovina" });
    }

    private static void SeedClassificationWeighing(InMemoryDatabase db)
    {
        _ = db.GetCollection<ClassificationWeighingModel>();

        var product = db.GetById<ProductModel>(1);

        db.Add<ClassificationWeighingModel>(new()
        {
            Id = 1,
            Name = "I",
            ProductId = 1,
            Product = product,
            LowerLimit = 0.0m,
            UpperLimit = 85.0m            
        });

        db.Add<ClassificationWeighingModel>(new()
        {
            Id = 2,
            Name = "H",
            ProductId = 1,
            Product = product,
            LowerLimit = 85.01m,
            UpperLimit = 107.0m,
        });

        db.Add<ClassificationWeighingModel>(new()
        {
            Id = 3,
            Name = "G",
            ProductId = 1,
            Product = product,
            LowerLimit = 107.01m,
            UpperLimit = 300.0m
        });
    }

    private static void SeedSprayChamber(InMemoryDatabase db)
    {
        _ = db.GetCollection<SprayChamberModel>();

        db.Add<SprayChamberModel>(new()
        {
            Id = 1,
            Name = "Chamber 01",
            Description = "Spray chamber principal",
            Capacity = 300,
            Stock = 0
        });

        db.Add<SprayChamberModel>(new()
        {
            Id = 2,
            Name = "Chamber 02",
            Description = "Spray chamber secundária",
            Capacity = 300,
            Stock = 0
        });

        db.Add<SprayChamberModel>(new()
        {
            Id = 3,
            Name = "Chamber 03",
            Description = "Chamber 03",
            Capacity = 300,
            Stock = 0
        });

        db.Add<SprayChamberModel>(new()
        {
            Id = 4,
            Name = "Chamber 04",
            Description = "Chamber 04",
            Capacity = 300,
            Stock = 0
        });

        db.Add<SprayChamberModel>(new()
        {
            Id = 5,
            Name = "Chamber 05",
            Description = "Chamber 05",
            Capacity = 300,
            Stock = 0
        });


        db.Add<SprayChamberModel>(new()
        {
            Id = 6,
            Name = "Chamber 06",
            Description = "Chamber 06",
            Capacity = 300,
            Stock = 0
        });


        db.Add<SprayChamberModel>(new()
        {
            Id = 7,
            Name = "Chamber 07",
            Description = "Chamber 07",
            Capacity = 300,
            Stock = 0
        });


        db.Add<SprayChamberModel>(new()
        {
            Id = 8,
            Name = "Chamber 08",
            Description = "Chamber 08",
            Capacity = 300,
            Stock = 0
        });


        db.Add<SprayChamberModel>(new()
        {
            Id = 9,
            Name = "Chamber 09",
            Description = "Chamber 09",
            Capacity = 300,
            Stock = 0
        });

        db.Add<SprayChamberModel>(new()
        {
            Id = 10,
            Name = "Chamber 10",
            Description = "Chamber 10",
            Capacity = 300,
            Stock = 0
        });

        db.Add<SprayChamberModel>(new()
        {
            Id = 11,
            Name = "Chamber 11",
            Description = "Chamber 11",
            Capacity = 300,
            Stock = 0
        });

        db.Add<SprayChamberModel>(new()
        {
            Id = 12,
            Name = "Chamber 12",
            Description = "Chamber 12",
            Capacity = 300,
            Stock = 0
        });
    }
}