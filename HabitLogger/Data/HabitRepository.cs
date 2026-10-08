using System.Data;
using System.Globalization;
using HabitLogger.Models;

namespace HabitLogger.Data;

/// <summary>
/// Todo el SQL de la tabla Habitos vive aquí. El resto del programa
/// trabaja con objetos Habito y no sabe nada de SQL.
/// </summary>
public class HabitRepository
{
    // La fecha se guarda como texto ISO (yyyy-MM-dd): así ordena bien en SQL.
    public const string FormatoBd = "yyyy-MM-dd";

    private const string SQL_CREAR_TABLA = @"
        CREATE TABLE IF NOT EXISTS Habitos (
            Id       INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre   TEXT    NOT NULL,
            Cantidad INTEGER NOT NULL CHECK (Cantidad > 0),
            Fecha    TEXT    NOT NULL
        );";

    private const string SQL_INSERTAR = @"
        INSERT INTO Habitos (Nombre, Cantidad, Fecha)
        VALUES (@nombre, @cantidad, @fecha);
        SELECT last_insert_rowid();";

    private const string SQL_LISTAR = "SELECT * FROM Habitos ORDER BY Fecha DESC, Id DESC;";

    private const string SQL_POR_ID = "SELECT * FROM Habitos WHERE Id = @id;";

    private const string SQL_ACTUALIZAR = @"
        UPDATE Habitos
        SET Nombre = @nombre, Cantidad = @cantidad, Fecha = @fecha
        WHERE Id = @id;";

    private const string SQL_ELIMINAR = "DELETE FROM Habitos WHERE Id = @id;";

    private readonly DataBaseManager _db;

    public HabitRepository(DataBaseManager db) => _db = db;

    public void CrearTabla() => _db.EjecutarNonQuery(SQL_CREAR_TABLA);

    public long Insertar(string nombre, int cantidad, DateTime fecha)
    {
        object? id = _db.EjecutarEscalar(SQL_INSERTAR, Parametros(nombre, cantidad, fecha));
        return Convert.ToInt64(id);
    }

    public List<Habito> Listar() =>
        _db.EjecutarConsulta(SQL_LISTAR).Rows.Cast<DataRow>().Select(AHabito).ToList();

    public Habito? ObtenerPorId(long id)
    {
        DataTable tabla = _db.EjecutarConsulta(SQL_POR_ID, new() { ["@id"] = id });
        return tabla.Rows.Count == 0 ? null : AHabito(tabla.Rows[0]);
    }

    /// <returns>true si se actualizó alguna fila (es decir, el Id existía).</returns>
    public bool Actualizar(Habito h)
    {
        var parametros = Parametros(h.Nombre, h.Cantidad, h.Fecha);
        parametros["@id"] = h.Id;
        return _db.EjecutarNonQuery(SQL_ACTUALIZAR, parametros) > 0;
    }

    /// <returns>true si se eliminó alguna fila (es decir, el Id existía).</returns>
    public bool Eliminar(long id) =>
        _db.EjecutarNonQuery(SQL_ELIMINAR, new() { ["@id"] = id }) > 0;

    /* ------------------------------------------------------------------
       Auxiliares (DRY: insertar y actualizar comparten los mismos parámetros)
       ------------------------------------------------------------------ */
    private static Dictionary<string, object?> Parametros(string nombre, int cantidad, DateTime fecha) => new()
    {
        ["@nombre"] = nombre,
        ["@cantidad"] = cantidad,
        ["@fecha"] = fecha.ToString(FormatoBd, CultureInfo.InvariantCulture)
    };

    private static Habito AHabito(DataRow fila) => new(
        Convert.ToInt64(fila["Id"]),
        (string)fila["Nombre"],
        Convert.ToInt32(fila["Cantidad"]),
        DateTime.ParseExact((string)fila["Fecha"], FormatoBd, CultureInfo.InvariantCulture));
}
