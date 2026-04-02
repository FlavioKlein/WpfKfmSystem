using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Interfaces;
using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Services;

public class ProductionOrderService : BaseService<ProductionOrderModel>, IProductionOrderService
{
    public List<ProductionOrderModel> GetByType(WeighingType type)
    {
        return Database.GetAll<ProductionOrderModel>().Where(po => po.Type == type).ToList();        
    }
}
