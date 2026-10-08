namespace HabitLogger.Logging;

/// <summary>
/// EXTRA 2: guarda las operaciones del usuario en ficheros .log.
/// Se crea un fichero por día dentro de la carpeta /logs (ej: logs/01_10_2026.log)
/// y cada línea lleva la hora de la operación.
/// </summary>
public class FileLogger
{
    private readonly string _carpeta;

    public FileLogger(string carpeta) => _carpeta = carpeta;

    public void Registrar(string operacion)
    {
        try
        {
            Directory.CreateDirectory(_carpeta); // no falla si ya existe

            DateTime ahora = DateTime.Now;
            string fichero = Path.Combine(_carpeta, $"{ahora:dd_MM_yyyy}.log");
            string linea = $"[{ahora:HH:mm:ss}] {operacion}{Environment.NewLine}";

            File.AppendAllText(fichero, linea); // crea el fichero del día si no existe
        }
        catch (Exception ex)
        {
            // Un fallo escribiendo el log nunca debe tumbar la aplicación.
            Console.Error.WriteLine($"[No se pudo escribir en el log: {ex.Message}]");
        }
    }
}
