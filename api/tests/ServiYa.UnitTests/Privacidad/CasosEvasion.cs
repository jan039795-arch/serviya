namespace ServiYa.UnitTests.Privacidad;

/// <summary>
/// Catálogo de intentos de compartir un contacto fuera de la plataforma (FR-033, research R8) y de
/// textos normales que no deben tocarse. Todos los datos son ficticios. Cuando aparezca una forma
/// nueva de evasión en el piloto, se agrega aquí primero.
/// </summary>
public static class CasosEvasion
{
    /// <summary>Texto con un contacto y el fragmento que no debe quedar visible.</summary>
    public static TheoryData<string, string> Ocultables => new()
    {
        // Teléfonos con separadores
        { "llámame al 55 1234 5678", "1234 5678" },
        { "mi cel 55-1234-5678 a cualquier hora", "1234-5678" },
        { "márcame 55.1234.5678", "1234.5678" },
        { "Tel (55) 1234 5678", "1234 5678" },
        { "+52 55 1234 5678", "1234 5678" },
        { "+52 1 55 12 34 56 78", "12 34 56 78" },
        { "5512345678", "5512345678" },
        { "5 5 1 2 3 4 5 6 7 8", "5 6 7 8" },
        { "55_12_34_56_78", "56_78" },
        { "4421234567 es mi número", "4421234567" },
        // Dígitos con palabras y mezclados
        { "cinco cinco uno dos tres cuatro cinco seis siete ocho", "cinco seis siete ocho" },
        { "Cinco Cinco, uno dos tres cuatro, cinco seis siete ocho", "cinco seis siete ocho" },
        { "55 doce 34 cincuenta y seis 78", "cincuenta y seis" },
        { "cinco 5 uno 2 tres 4 cinco 6 siete 8", "siete 8" },
        { "cero cuarenta y cuatro, treinta y tres, veintidós, once, noventa", "veintidós" },
        // Correos
        { "escríbeme a juan.perez@correo.test", "juan.perez@correo.test" },
        { "juanperez arroba correo punto com", "arroba correo punto com" },
        { "juan (arroba) correo (punto) com", "correo (punto) com" },
        { "juan [at] correo [dot] com", "correo [dot] com" },
        { "mi correo es JUAN_PEREZ@CORREO.TEST.MX", "CORREO.TEST.MX" },
        // Enlaces y cuentas de otras apps
        { "wa.me/5215512345678", "wa.me" },
        { "https://wa.me/5215512345678 ahí te atiendo", "wa.me" },
        { "https://api.whatsapp.com/send?phone=5215512345678", "whatsapp.com" },
        { "t.me/plomero_ficticio", "t.me/plomero_ficticio" },
        { "instagram.com/plomero.ficticio", "plomero.ficticio" },
        { "www.facebook.com/plomeroficticio", "plomeroficticio" },
        { "fb.com/plomeroficticio", "plomeroficticio" },
        { "m.me/plomeroficticio", "plomeroficticio" },
        { "mi insta es @plomero.ficticio", "@plomero.ficticio" },
        { "Telegram: @plomeroficticio", "@plomeroficticio" },
        { "búscame en insta: plomero.ficticio", "plomero.ficticio" },
        // "WhatsApp" seguido de números, aunque sean pocos
        { "WhatsApp 1234 5678", "1234 5678" },
        { "mándame whats al 1234 56", "1234 56" },
        { "Escríbeme por WhatsApp: cinco cinco uno dos", "cinco cinco uno dos" },
        { "mi wasap 33 12 34", "33 12 34" },
    };

    /// <summary>Textos normales de un oficio que deben quedar idénticos.</summary>
    public static TheoryData<string> NoSeOcultan => new()
    {
        "El trabajo cuesta $1,500",
        "Cobro $15,000.00 por la obra completa",
        "Presupuesto: $1,250,000.00 con materiales",
        "Puedo ir el 12/11/2026",
        "Puedo ir el 12-11-2026 en la mañana",
        "Termino en 3 horas",
        "Tengo 20 años de experiencia",
        "Son dos o tres días de trabajo",
        "Horario de 8:00 a 18:00",
        "Te mando WhatsApp en 2 horas",
        "Atiendo en calle 5 de Mayo 123",
        "Medidas 120 x 80 cm",
        "Tengo uno o dos ayudantes y cinco años en el oficio",
        "Hago trabajos de 2026, garantía de 6 meses",
        "",
        "Hola, ¿a qué hora puede venir?",
    };
}
