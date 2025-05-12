namespace APBD_09.Models;

public class AddProdRequest
{
    public int ProductId { get; set; }
    public int WarehouseId { get; set; }
    public int Amount { get; set; }
    public DateTime OrderCreatedAt { get; set; }
}