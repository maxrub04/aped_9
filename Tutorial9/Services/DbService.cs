using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Tutorial9.Model;

namespace Tutorial9.Services;

public class DbService : IDbService
{
    private readonly IConfiguration _configuration;
    public DbService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task DoSomethingAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();

        DbTransaction transaction = await connection.BeginTransactionAsync();
        command.Transaction = transaction as SqlTransaction;

        // BEGIN TRANSACTION
        try
        {
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 1);
            command.Parameters.AddWithValue("@Name", "Animal1");
        
            await command.ExecuteNonQueryAsync();
        
            command.Parameters.Clear();
            command.CommandText = "INSERT INTO Animal VALUES (@IdAnimal, @Name);";
            command.Parameters.AddWithValue("@IdAnimal", 2);
            command.Parameters.AddWithValue("@Name", "Animal2");
        
            await command.ExecuteNonQueryAsync();
            
            await transaction.CommitAsync();
        }
        catch (Exception e)
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task ProcedureAsync()
    {
        await using SqlConnection connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using SqlCommand command = new SqlCommand();
        
        command.Connection = connection;
        await connection.OpenAsync();
        
        command.CommandText = "NameOfProcedure";
        command.CommandType = CommandType.StoredProcedure;
        
        command.Parameters.AddWithValue("@Id", 2);
        
        await command.ExecuteNonQueryAsync();
        
    }

        public async Task<int> AddProductToWarehouseAsync(Request request)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        await connection.OpenAsync();

        var transaction = await connection.BeginTransactionAsync();
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            // check whether the product exists
            command.CommandText = "SELECT 1 FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            
            var exists = await command.ExecuteScalarAsync();
            if (exists == null) throw new ArgumentException("The product doesn't exist");

            command.Parameters.Clear();
            command.CommandText = "SELECT 1 FROM Warehouse WHERE IdWarehouse = @IdWarehouse";
            command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            
            exists = await command.ExecuteScalarAsync();
            if (exists == null) throw new ArgumentException("The warehouse doesn't exist");

            if (request.Amount <= 0) throw new ArgumentException("Amount must be more than zerp");

            // order mathcing verification
            command.Parameters.Clear();
            command.CommandText = @"SELECT TOP 1 IdOrder 
                                    FROM [Order] 
                                    WHERE IdProduct = @IdProduct AND Amount = @Amount AND CreatedAt < @CreatedAt";
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);
            
            var idOrder = await command.ExecuteScalarAsync();
            if (idOrder == null) throw new ArgumentException("No matching orders found");

            // check if the order has already been fulfilled
            command.Parameters.Clear();
            command.CommandText = @"SELECT 1 FROM Product_Warehouse WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", (int)idOrder);
            
            exists = await command.ExecuteScalarAsync();
            if (exists != null) throw new ArgumentException("The order has already been fulfilled");

            // update
            command.Parameters.Clear();
            command.CommandText = @"UPDATE [Order] SET FulfilledAt = GETDATE() WHERE IdOrder = @IdOrder";
            command.Parameters.AddWithValue("@IdOrder", (int)idOrder);
            await command.ExecuteNonQueryAsync();

            //getting product price
            command.Parameters.Clear();
            command.CommandText = @"SELECT Price FROM Product WHERE IdProduct = @IdProduct";
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            var price = (decimal)(await command.ExecuteScalarAsync() ?? throw new ArgumentException("Product price not found"));

            // insert
            var totalPrice = price * request.Amount;
            command.Parameters.Clear();
            command.CommandText = @"INSERT INTO Product_Warehouse (IdWarehouse, IdProduct, IdOrder, Amount, Price, CreatedAt)
                                    OUTPUT INSERTED.IdProductWarehouse
                                    VALUES (@IdWarehouse, @IdProduct, @IdOrder, @Amount, @Price, GETDATE())";
            command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
            command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
            command.Parameters.AddWithValue("@IdOrder", (int)idOrder);
            command.Parameters.AddWithValue("@Amount", request.Amount);
            command.Parameters.AddWithValue("@Price", totalPrice);

            var insertedId = (int)await command.ExecuteScalarAsync();

            await transaction.CommitAsync();
            return insertedId;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
        // END TRANSACTION
    }

    public async Task<int> AddProductToWarehouseWithProcAsync(Request request)
    {
        await using var connection = new SqlConnection(_configuration.GetConnectionString("Default"));
        await using var command = new SqlCommand("AddProductToWarehouse", connection);
        command.CommandType = CommandType.StoredProcedure;

        command.Parameters.AddWithValue("@IdProduct", request.IdProduct);
        command.Parameters.AddWithValue("@IdWarehouse", request.IdWarehouse);
        command.Parameters.AddWithValue("@Amount", request.Amount);
        command.Parameters.AddWithValue("@CreatedAt", request.CreatedAt);

        await connection.OpenAsync();

        try
        {
            var result = await command.ExecuteScalarAsync();
            if (result == null || !int.TryParse(result.ToString(), out int id))
                throw new Exception(" Failed to return valid ID");

            return id;
        }
        catch (SqlException ex)
        {
            throw new ArgumentException($"Error: {ex.Message}");
        }
        // END TRANSACTION
    }

}