using WpfPorkProcessSystem.Enums;
using WpfPorkProcessSystem.Models;

namespace WpfPorkProcessSystem.Interfaces;

public interface IProductionOrderService : IBaseService<ProductionOrderModel>
{
    List<ProductionOrderModel> GetByType(WeighingType type);
}
