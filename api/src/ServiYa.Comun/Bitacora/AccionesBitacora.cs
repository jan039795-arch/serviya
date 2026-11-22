namespace ServiYa.Comun.Bitacora;

/// <summary>Acciones que se registran en la bitácora (data-model.md, tasks.md). Se agregan con cada historia.</summary>
public static class AccionesBitacora
{
    public const string VerDocumento = nameof(VerDocumento);
    public const string AprobarDocumento = nameof(AprobarDocumento);
    public const string RechazarDocumento = nameof(RechazarDocumento);
    public const string AprobarPerfil = nameof(AprobarPerfil);
    public const string PedirCorrecciones = nameof(PedirCorrecciones);
    public const string RechazarPerfil = nameof(RechazarPerfil);
    public const string SuspenderCuenta = nameof(SuspenderCuenta);
    public const string ReactivarCuenta = nameof(ReactivarCuenta);
    public const string OtorgarRolAdmin = nameof(OtorgarRolAdmin);
    public const string RetirarRolAdmin = nameof(RetirarRolAdmin);
    public const string Reembolsar = nameof(Reembolsar);
}

/// <summary>Tipos de entidad a los que apunta una entrada de la bitácora.</summary>
public static class EntidadesBitacora
{
    public const string Usuario = nameof(Usuario);
    public const string Prestador = nameof(Prestador);
    public const string Documento = nameof(Documento);
    public const string Foto = nameof(Foto);
    public const string Pago = nameof(Pago);
    public const string Reporte = nameof(Reporte);
}
