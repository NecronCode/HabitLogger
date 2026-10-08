namespace HabitLogger.Models;

/// <summary>
/// Una ocurrencia de un hábito: qué hábito, cuántas veces/unidades y en qué fecha.
/// Ejemplo: ("Vasos de agua", 8, 01/10/2026)
/// </summary>
public record Habito(long Id, string Nombre, int Cantidad, DateTime Fecha);