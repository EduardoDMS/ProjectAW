using System.Data;
using Microsoft.EntityFrameworkCore;

namespace ProjectAW.Infrastructure.Data;
/// <summary>
/// sealed se usa para que no se pueda heredar de esta clase, ya que es un servicio de infraestructura y no debería ser extendido.
/// </summary>
public sealed class EfTransactionManager : ITransactionManager
{
    private readonly AppDbContext _context;

    public EfTransactionManager(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Confirma al completar la operación. Si esta falla o se cancela, la transacción
    /// se descarta sin commit y EF Core revierte sus cambios.
    /// 
    /// El metodo func actua dentro de los parametros para que se pueda ejecutar dentro de la transaccion, y el cancellation token permite cancelar la operacion si es necesario.
    /// </summary>
    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        return ExecuteInTransactionAsync(operation, null, ct);
    }

    ///IsolationLevel.Serializable es el nivel de aislamiento más alto,
    ///lo que significa que las transacciones se ejecutan de manera completamente aislada unas de otras. 
    ///Esto evita problemas de concurrencia, como lecturas sucias, lecturas no repetibles y fantasmas, 
    ///pero puede afectar el rendimiento debido a la mayor contención de recursos.
    public Task ExecuteSerializableAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default)
    {
        return ExecuteInTransactionAsync(operation, IsolationLevel.Serializable, ct);
    }


    /// <summary>
    /// Ejecuta la operación dentro de una transacción con el nivel de aislamiento especificado. 
    /// </summary>

    private async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        IsolationLevel? isolationLevel,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(operation);

        // Sin nivel explícito conserva el aislamiento predeterminado del proveedor.
        await using var transaction = isolationLevel.HasValue
            ? await _context.Database.BeginTransactionAsync(isolationLevel.Value, ct)
            : await _context.Database.BeginTransactionAsync(ct);

        await operation(ct);
        await transaction.CommitAsync(ct);
    }
}
