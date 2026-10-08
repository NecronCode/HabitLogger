using System.Globalization;
using HabitLogger.Data;
using HabitLogger.Logging;
using HabitLogger.Models;

namespace HabitLogger.UI;

/// <summary>
/// Menú principal y acciones del usuario. Cada acción se ejecuta a través de
/// EjecutarProtegido(), que es el único sitio donde se capturan y registran los errores.
/// </summary>
public class HabitApp
{
    private readonly HabitRepository _repo;
    private readonly ErrorLogger _errores;
    private readonly FileLogger _log;

    // Menú definido como datos: se imprime y se ejecuta con el mismo diccionario.
    private readonly Dictionary<int, (string Texto, Action Accion)> _menu;

    public HabitApp(HabitRepository repo, ErrorLogger errores, FileLogger log)
    {
        _repo = repo;
        _errores = errores;
        _log = log;

        _menu = new()
        {
            [1] = ("Ver hábitos registrados", VerHabitos),
            [2] = ("Añadir registro", Anadir),
            [3] = ("Actualizar registro", Actualizar),
            [4] = ("Eliminar registro", Eliminar),
        };
    }

    public void Ejecutar()
    {
        _log.Registrar("Aplicación iniciada");

        bool salir = false;
        while (!salir)
        {
            MostrarMenu();
            int opcion = ConsoleInput.PedirEntero("Opción: ", 0, _menu.Count);

            if (opcion == 0)
            {
                salir = true;
            }
            else
            {
                var (texto, accion) = _menu[opcion];
                EjecutarProtegido(texto, accion);
            }
        }

        _log.Registrar("Aplicación cerrada");
        Console.WriteLine("¡Hasta pronto!");
    }

    /* ------------------------------------------------------------------
       Acciones
       ------------------------------------------------------------------ */

    private void VerHabitos()
    {
        MostrarTabla(_repo.Listar());
        _log.Registrar("VER: listado de hábitos");
    }

    private void Anadir()
    {
        string nombre = ConsoleInput.PedirTexto("Hábito (ej. Vasos de agua): ");
        int cantidad = ConsoleInput.PedirEntero("Cantidad: ", 1, 1_000_000);
        DateTime fecha = ConsoleInput.PedirFecha("Fecha (dd/mm/aaaa o 'hoy'): ");

        long id = _repo.Insertar(nombre, cantidad, fecha);

        Console.WriteLine($"Registro añadido con Id {id}.");
        _log.Registrar($"INSERTAR: {Describir(new Habito(id, nombre, cantidad, fecha))}");
    }

    private void Actualizar()
    {
        if (!MostrarTabla(_repo.Listar())) return;

        Habito? actual = PedirHabitoExistente("Id del registro a actualizar: ");
        if (actual == null) return;

        Console.WriteLine("(Pulsa Enter para mantener el valor actual)");
        string nombre = ConsoleInput.PedirTexto($"Hábito [{actual.Nombre}]: ", actual.Nombre);
        int cantidad = ConsoleInput.PedirEntero($"Cantidad [{actual.Cantidad}]: ", 1, 1_000_000,
                                                actual.Cantidad.ToString());
        DateTime fecha = ConsoleInput.PedirFecha($"Fecha [{FormatoPantalla(actual.Fecha)}]: ",
                                                 actual.Fecha.ToString(HabitRepository.FormatoBd,
                                                                       CultureInfo.InvariantCulture));

        var nuevo = new Habito(actual.Id, nombre, cantidad, fecha);

        if (_repo.Actualizar(nuevo))
        {
            Console.WriteLine("Registro actualizado.");
            _log.Registrar($"ACTUALIZAR: {Describir(actual)}  ->  {Describir(nuevo)}");
        }
        else
        {
            Console.WriteLine("No se pudo actualizar: el registro ya no existe.");
        }
    }

    private void Eliminar()
    {
        if (!MostrarTabla(_repo.Listar())) return;

        Habito? habito = PedirHabitoExistente("Id del registro a eliminar: ");
        if (habito == null) return;

        if (!ConsoleInput.Confirmar($"¿Eliminar {Describir(habito)}?"))
        {
            Console.WriteLine("Operación cancelada.");
            return;
        }

        if (_repo.Eliminar(habito.Id))
        {
            Console.WriteLine("Registro eliminado.");
            _log.Registrar($"ELIMINAR: {Describir(habito)}");
        }
        else
        {
            Console.WriteLine("No se pudo eliminar: el registro ya no existe.");
        }
    }

    /* ------------------------------------------------------------------
       Gestión de errores (único punto de captura)
       ------------------------------------------------------------------ */

    private void EjecutarProtegido(string nombreAccion, Action accion)
    {
        try
        {
            accion();
        }
        catch (EndOfStreamException)
        {
            throw; // entrada cerrada: lo gestiona Program para salir limpiamente
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en «{nombreAccion}»: {ex.Message}");
            Console.WriteLine("El error se ha registrado. Puedes seguir usando la aplicación.");
            _errores.Registrar(nombreAccion, ex);
            _log.Registrar($"ERROR en {nombreAccion}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /* ------------------------------------------------------------------
       Auxiliares de pantalla
       ------------------------------------------------------------------ */

    private void MostrarMenu()
    {
        Console.WriteLine();
        Console.WriteLine("=== HABIT LOGGER - REGISTRO DE HÁBITOS ===");
        foreach (var (numero, (texto, _)) in _menu)
        {
            Console.WriteLine($"{numero}. {texto}");
        }
        Console.WriteLine("0. Salir");
    }

    /// <returns>false si no hay registros (para que el llamador pueda cortar).</returns>
    private static bool MostrarTabla(List<Habito> habitos)
    {
        if (habitos.Count == 0)
        {
            Console.WriteLine("No hay hábitos registrados todavía.");
            return false;
        }

        // El ancho de la columna "Hábito" se ajusta al nombre más largo.
        const string cabecera = "Hábito";
        int ancho = Math.Max(cabecera.Length, habitos.Max(h => h.Nombre.Length)) + 2;

        Console.WriteLine();
        Console.WriteLine("Id".PadRight(6) + cabecera.PadRight(ancho) + "Cantidad".PadRight(10) + "Fecha");
        foreach (Habito h in habitos)
        {
            Console.WriteLine(h.Id.ToString().PadRight(6) + h.Nombre.PadRight(ancho) +
                              h.Cantidad.ToString().PadRight(10) + FormatoPantalla(h.Fecha));
        }
        return true;
    }

    private Habito? PedirHabitoExistente(string mensaje)
    {
        long id = ConsoleInput.PedirEntero(mensaje, 1, int.MaxValue);
        Habito? habito = _repo.ObtenerPorId(id);

        if (habito == null)
        {
            Console.WriteLine($"No existe ningún registro con Id {id}.");
        }

        return habito;
    }

    private static string FormatoPantalla(DateTime fecha) =>
        fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    private static string Describir(Habito h) =>
        $"[{h.Id}] {h.Nombre} x{h.Cantidad} el {FormatoPantalla(h.Fecha)}";
}
