namespace ProjectAW.Infrastructure.Data;

/// <summary>Ejecuta operaciones de aplicación dentro de una transacción.</summary>
public interface ITransactionManager
{
    /// <summary>Usa el nivel de aislamiento predeterminado del proveedor.</summary>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);

    /// <summary>Usa aislamiento serializable para operaciones sensibles a concurrencia.</summary>
    Task ExecuteSerializableAsync(Func<CancellationToken, Task> operation, CancellationToken ct = default);
}
