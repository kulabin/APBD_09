using APBD_09.Models;

namespace APBD_09.Services;

public interface IWarehouseService
{
    Task<int> AddProduct(AddProdRequest request);
    Task<int> AddProductWithProcedure(AddProdRequest request);
}