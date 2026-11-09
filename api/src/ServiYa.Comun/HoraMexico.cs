namespace ServiYa.Comun;

/// <summary>Fechas en la hora del centro de México, para reglas por día como la mayoría de edad.</summary>
public static class HoraMexico
{
    /// <summary>Zona IANA; .NET la resuelve también en Windows.</summary>
    public static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/Mexico_City");

    public static DateOnly HoyEnMexico(this TimeProvider reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), Zona).DateTime);
    }
}
