using HabitLogger.Data;

namespace HabitLogger.Logging;

/// <summary>
/// EXTRA 1: guarda los errores del sistema en la tabla Errores de la base de datos.
/// Importante: registrar un error nunca debe provocar otro error que tumbe la app,
/// por eso Registrar() atrapa cualquier fallo interno.
/// </summary>
public class ErrorLogger
{
    private const string SQL_CREAR_TABLA = @"
        CREATE TABLE IF NOT EXISTS Errores (
            Id         INTEGER PRIMARY KEY AUTOINCREMENT,
            FechaHora  TEXT NOT NULL,
            Origen     TEXT NOT NULL,
            Tipo       TEXT NOT NULL,
            Mensaje    TEXT NOT NULL,
            StackTrace TEXT
        );";

    private const string SQL_INSERTAR = @"
        INSERT INTO Errores (FechaHora, Origen, Tipo, Mensaje, StackTrace)
        VALUES (@fechaHora, @origen, @tipo, @mensaje, @stackTrace);";

    private readonly DataBaseManager _db;

    public ErrorLogger(DataBaseManager db) => _db = db;

    public void CrearTabla() => _db.EjecutarNonQuery(SQL_CREAR_TABLA);

    /// <param name="origen">Dónde ocurrió (por ejemplo, el nombre de la opción del menú).</param>
    public void Registrar(string origen, Exception ex)
    {
        try
        {
            _db.EjecutarNonQuery(SQL_INSERTAR, new()
            {
                ["@fechaHora"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ["@origen"] = origen,
                ["@tipo"] = ex.GetType().Name,
                ["@mensaje"] = ex.Message,
                ["@stackTrace"] = ex.StackTrace
            });
        }
        catch (Exception fallo)
        {
            // Último recurso: avisar por consola, pero sin lanzar nada más.
            Console.Error.WriteLine($"[No se pudo guardar el error en la BD: {fallo.Message}]");
        }
    }
}
