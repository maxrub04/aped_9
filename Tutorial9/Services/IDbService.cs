using Tutorial9.Model;

namespace Tutorial9.Services;

public interface IDbService
{
    Task DoSomethingAsync();
    Task ProcedureAsync();
    Task<int> AddProductToWarehouseAsync(Request request);
    Task<int> AddProductToWarehouseWithProcAsync(Request request);

}