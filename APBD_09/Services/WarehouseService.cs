using APBD_09.Models;
using Microsoft.Data.SqlClient;

namespace APBD_09.Services;

public class WarehouseService : IWarehouseService
{
    private readonly string _connectionString = 
        "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";

    public async Task<int> AddProduct(AddProdRequest request)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than 0");
        }

        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction =  connection.BeginTransaction();

        try
        {
            await EnsureExistance(connection, transaction,"Product", "IdProduct", request.ProductId);
            await EnsureExistance(connection, transaction,"Warehouse", "IdWarehouse", request.WarehouseId);
            
            int orderId = await ValidateAndGetOrder(connection, transaction, request);

            await UpdateFulfilledOrder(connection, transaction, orderId);
            
            var newId = await InsertProduct(connection, transaction, request, orderId);
            await transaction.CommitAsync();
            return newId;
        }catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task EnsureExistance(SqlConnection connection, SqlTransaction transaction, string table, string key,
        int id)
    {
        var sql = @"SELECT COUNT(1) FROM {table} WHERE {key} = @id";
        using var cmd = new SqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("@id", id);
        if ((int)await cmd.ExecuteScalarAsync() == 0)
            throw new InvalidOperationException($"{table} not found.");
    }

    private async Task<int> ValidateAndGetOrder(SqlConnection connection, SqlTransaction transaction,
        AddProdRequest request)
    {
        var sqlCount = @"SELECT COUNT(1) FROM [Order]
                             WHERE IdProduct = @pid
                               AND Amount = @amt
                               AND CreatedAt < @ocr";
        using var cmd = new SqlCommand(sqlCount, connection, transaction);
        cmd.Parameters.AddWithValue("@pid", request.ProductId);
        cmd.Parameters.AddWithValue("@amt", request.Amount);
        cmd.Parameters.AddWithValue("@ocr", request.OrderCreatedAt);

        if ((int)await cmd.ExecuteScalarAsync() == 0)
            throw new InvalidOperationException("Matching purchase order not found or date invalid.");
        
        var sqlFul = @"SELECT COUNT(1) FROM Product_Warehouse pw
                            JOIN [Order] o ON pw.IdOrder = o.IdOrder
                            WHERE o.IdProduct = @pid
                              AND pw.Amount = @amt
                              AND o.CreatedAt < @ocr";
        cmd.CommandText = sqlFul;
        if ((int)await cmd.ExecuteScalarAsync() > 0)
            throw new InvalidOperationException("Order already fulfilled.");
        
        var sqlId = @"SELECT TOP 1 IdOrder
                          FROM [Order]
                          WHERE IdProduct = @pid
                            AND Amount = @amt
                            AND CreatedAt < @ocr
                          ORDER BY CreatedAt ASC";
        cmd.CommandText = sqlId;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
    
    private async Task UpdateFulfilledOrder(SqlConnection connection, SqlTransaction transaction, int orderId)
    {
        const string sql = "UPDATE [Order] SET FullfilledAt = @now WHERE IdOrder = @oid";
        using var cmd = new SqlCommand(sql, connection, transaction);
        cmd.Parameters.AddWithValue("@now", DateTime.UtcNow);
        cmd.Parameters.AddWithValue("@oid", orderId);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertProduct(SqlConnection connection, SqlTransaction transaction, AddProdRequest request,
        int orderId)
    {
        using var priceCmd = new SqlCommand("SELECT Price FROM Product WHERE IdProduct = @pid", connection, transaction);
        priceCmd.Parameters.AddWithValue("@pid", request.ProductId);
        var unitPrice = (decimal)await priceCmd.ExecuteScalarAsync();
        var totalPrice = unitPrice * request.Amount;
        
        const string sqlInsert = @"INSERT INTO Product_Warehouse
                                 (IdProduct, IdWarehouse, IdOrder, Amount, Price, CreatedAt)
                                 VALUES (@pid, @wid, @oid, @amt, @price, @now)";
        using var cmd = new SqlCommand(sqlInsert, connection, transaction);
        cmd.Parameters.AddWithValue("@pid", request.ProductId);
        cmd.Parameters.AddWithValue("@wid", request.WarehouseId);
        cmd.Parameters.AddWithValue("@oid", orderId);
        cmd.Parameters.AddWithValue("@amt", request.Amount);
        cmd.Parameters.AddWithValue("@price", totalPrice);
        cmd.Parameters.AddWithValue("@now", DateTime.Now);
        
        var res = await cmd.ExecuteNonQueryAsync();
        return Convert.ToInt32(res);
    }

}