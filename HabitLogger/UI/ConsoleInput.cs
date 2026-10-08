using System.Globalization;

namespace HabitLogger.UI;

/// <summary>
/// Toda la lectura de datos del usuario pasa por aquí.
/// Un único bucle genérico (Pedir) se repite hasta que la entrada es válida,
/// y cada tipo de dato solo aporta su "parser" (DRY).
/// </summary>
public static class ConsoleInput
{
    public const int LongitudMaximaTexto = 50;

    private static readonly string[] FormatosFecha = { "d/M/yyyy", "d-M-yyyy", "yyyy-MM-dd" };

    private delegate bool Parser<T>(string texto, out T valor);

    /* ------------------------------------------------------------------
       API pública
       ------------------------------------------------------------------ */

    /// <summary>Texto no vacío de hasta 50 caracteres.</summary>
    public static string PedirTexto(string mensaje, string? porDefecto = null) =>
        Pedir<string>(mensaje, ParseTexto,
            $"El texto no puede estar vacío ni superar {LongitudMaximaTexto} caracteres.", porDefecto);

    /// <summary>Número entero dentro del rango [min, max].</summary>
    public static int PedirEntero(string mensaje, int min, int max, string? porDefecto = null) =>
        Pedir<int>(mensaje,
            (string t, out int v) => int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)
                                     && v >= min && v <= max,
            $"Introduce un número entero entre {min} y {max}.", porDefecto);

    /// <summary>Fecha dd/MM/aaaa (no futura). Escribir "hoy" (o "h") devuelve la fecha actual.</summary>
    public static DateTime PedirFecha(string mensaje, string? porDefecto = null) =>
        Pedir<DateTime>(mensaje, ParseFecha,
            "Fecha no válida. Usa el formato dd/mm/aaaa (que no sea futura) o escribe 'hoy'.", porDefecto);

    /// <summary>Pregunta de sí/no. Devuelve true para "s" / "si" / "sí".</summary>
    public static bool Confirmar(string mensaje) =>
        Pedir<bool>(mensaje + " (s/n): ", ParseSiNo, "Responde 's' o 'n'.", null);

    /* ------------------------------------------------------------------
       Núcleo genérico
       ------------------------------------------------------------------ */

    /// <summary>
    /// Pregunta hasta obtener un valor válido. Si hay un valor por defecto,
    /// pulsar solo Enter lo acepta (útil al modificar registros).
    /// </summary>
    private static T Pedir<T>(string mensaje, Parser<T> parser, string error, string? porDefecto)
    {
        while (true)
        {
            Console.Write(mensaje);
            // ReadLine devuelve null si se cierra la entrada (Ctrl+D / Ctrl+Z): salimos con elegancia.
            string texto = (Console.ReadLine() ?? throw new EndOfStreamException()).Trim();

            if (texto.Length == 0 && porDefecto != null)
            {
                texto = porDefecto;
            }

            if (parser(texto, out T valor))
            {
                return valor;
            }

            Console.WriteLine(error);
        }
    }

    /* ------------------------------------------------------------------
       Parsers
       ------------------------------------------------------------------ */

    private static bool ParseTexto(string texto, out string valor)
    {
        valor = texto;
        return texto.Length > 0 && texto.Length <= LongitudMaximaTexto;
    }

    private static bool ParseFecha(string texto, out DateTime valor)
    {
        if (texto.Equals("hoy", StringComparison.OrdinalIgnoreCase) ||
            texto.Equals("h", StringComparison.OrdinalIgnoreCase))
        {
            valor = DateTime.Today;
            return true;
        }

        return DateTime.TryParseExact(texto, FormatosFecha, CultureInfo.InvariantCulture,
                                      DateTimeStyles.None, out valor)
               && valor.Date <= DateTime.Today; // un hábito no puede haber ocurrido en el futuro
    }

    private static bool ParseSiNo(string texto, out bool valor)
    {
        string t = texto.ToLowerInvariant();
        valor = t is "s" or "si" or "sí";
        return valor || t is "n" or "no";
    }
}