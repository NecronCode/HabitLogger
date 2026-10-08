using System.Text;
using HabitLogger.Data;
using HabitLogger.Logging;
using HabitLogger.UI;

// Para que se vean bien las tildes y la «ñ» en la consola.
try
{
    Console.OutputEncoding = Encoding.UTF8;
    Console.InputEncoding = Encoding.UTF8;
}
catch (Exception)
{
    // Si la consola no lo permite, seguimos con la codificación por defecto.
}

// La BD y la carpeta de logs se crean junto al ejecutable (bin/Debug/net10.0/).
string carpetaBase = AppContext.BaseDirectory;
string rutaBd = Path.Combine(carpetaBase, "habitos.db");
string carpetaLogs = Path.Combine(carpetaBase, "logs");

var fileLogger = new FileLogger(carpetaLogs);

try
{
    using var db = new DataBaseManager(rutaBd);
    var repositorio = new HabitRepository(db);
    var errorLogger = new ErrorLogger(db);

    // Al arrancar: si la BD no existe SQLite la crea, y las tablas se crean si faltan.
    repositorio.CrearTabla();
    errorLogger.CrearTabla();

    new HabitApp(repositorio, errorLogger, fileLogger).Ejecutar();
}
catch (EndOfStreamException)
{
    // La entrada de consola se cerró (Ctrl+D / Ctrl+Z): salida normal, no es un fallo.
    fileLogger.Registrar("Entrada cerrada. Aplicación finalizada");
    Console.WriteLine();
    Console.WriteLine("Entrada cerrada. Hasta pronto.");
}
catch (Exception ex)
{
    // Último recurso: fallo al abrir la BD, disco lleno, permisos...
    Console.WriteLine($"No se pudo iniciar o continuar la aplicación: {ex.Message}");
    fileLogger.Registrar($"ERROR FATAL: {ex.GetType().Name} - {ex.Message}");
}
