namespace ServiYa.Comun.Correo.Plantillas;

/// <summary>Textos de los correos, en español de México y sin formato. Se agregan con cada historia.</summary>
public static class PlantillasCorreo
{
    public static CorreoSaliente CodigoVerificacion(string para, string codigo) => new(
        para,
        $"Tu código de ServiYa: {codigo}",
        $"""
        Hola:

        Tu código para verificar tu correo en ServiYa es:

            {codigo}

        Vence en 10 minutos. Si no creaste una cuenta, ignora este correo.

        Nadie de ServiYa te pedirá este código por teléfono ni por chat.
        """);

    public static CorreoSaliente RecuperarContrasena(string para, string codigo) => new(
        para,
        $"Código para cambiar tu contraseña: {codigo}",
        $"""
        Hola:

        Pediste cambiar tu contraseña de ServiYa. Tu código es:

            {codigo}

        Vence en 10 minutos. Si no lo pediste, ignora este correo: tu contraseña sigue igual.

        Nadie de ServiYa te pedirá este código por teléfono ni por chat.
        """);
}
