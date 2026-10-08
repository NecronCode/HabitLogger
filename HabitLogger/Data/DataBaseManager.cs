using System.Data;
using Microsoft.Data.Sqlite;

namespace HabitLogger.Data;

/// <summary>
/// Clase GENÉRICA de acceso a SQLite.
/// </summary>
public class DataBaseManager : IDisposable
{
    private readonly string _cadenaConexion;
    private SqliteConnection? _conexion;

    /* ------------------------------------------------------------------
       CONSTRUCTOR
       ------------------------------------------------------------------ */
    public DataBaseManager(string rutaArchivo)
    {
        // Si el archivo .db no existe, SQLite lo crea automáticamente.
        _cadenaConexion = new SqliteConnectionStringBuilder
        {
            DataSource = rutaArchivo,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    /* ------------------------------------------------------------------
       CONEXIÓN: abrir y cerrar
       ------------------------------------------------------------------ */
    public void AbrirConexion()
    {
        _conexion ??= new SqliteConnection(_cadenaConexion);

        if (_conexion.State != ConnectionState.Open)
        {
            _conexion.Open();
        }
    }

    public void CerrarConexion()
    {
        if (_conexion != null && _conexion.State != ConnectionState.Closed)
        {
            _conexion.Close();
        }
    }

    /* ------------------------------------------------------------------
       EJECUCIÓN DE SQL (todo llega por parámetro)
       ------------------------------------------------------------------ */

    /// <summary>
    /// Para CREATE, INSERT, UPDATE, DELETE...
    /// Devuelve el número de filas afectadas.
    /// </summary>
    public int EjecutarNonQuery(string sql, Dictionary<string, object?>? parametros = null)
    {
        try
        {
            AbrirConexion();
            using var cmd = CrearComando(sql, parametros);
            return cmd.ExecuteNonQuery();
        }
        finally
        {
            CerrarConexion();
        }
    }

    /// <summary>
    /// Para consultas que devuelven un único valor (COUNT, last_insert_rowid...).
    /// Devuelve la primera columna de la primera fila, o null si no hay resultado.
    /// </summary>
    public object? EjecutarEscalar(string sql, Dictionary<string, object?>? parametros = null)
    {
        try
        {
            AbrirConexion();
            using var cmd = CrearComando(sql, parametros);
            return cmd.ExecuteScalar();
        }
        finally
        {
            CerrarConexion();
        }
    }

    /// <summary>
    /// Para SELECT. Devuelve el resultado completo en un DataTable.
    /// </summary>
    public DataTable EjecutarConsulta(string sql, Dictionary<string, object?>? parametros = null)
    {
        var tabla = new DataTable();

        try
        {
            AbrirConexion();
            using var cmd = CrearComando(sql, parametros);
            using var reader = cmd.ExecuteReader();
            tabla.Load(reader);
        }
        finally
        {
            CerrarConexion();
        }

        return tabla;
    }

    /* ------------------------------------------------------------------
       AUXILIAR PRIVADO
       ------------------------------------------------------------------ */

    /// <summary>
    /// Crea el comando y le añade los parámetros (@nombre, @id...).
    /// Usar parámetros evita la inyección SQL: nunca concatenar texto
    /// del usuario dentro de la consulta.
    /// </summary>
    private SqliteCommand CrearComando(string sql, Dictionary<string, object?>? parametros)
    {
        var cmd = new SqliteCommand(sql, _conexion);

        if (parametros != null)
        {
            foreach (var p in parametros)
            {
                // Un null debe convertirse en DBNull para guardarse como NULL
                cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
            }
        }

        return cmd;
    }

    /* ------------------------------------------------------------------
       IDisposable: liberar la conexión al terminar
       ------------------------------------------------------------------ */
    public void Dispose()
    {
        CerrarConexion();
        _conexion?.Dispose();
        _conexion = null;
        GC.SuppressFinalize(this);
    }
}
