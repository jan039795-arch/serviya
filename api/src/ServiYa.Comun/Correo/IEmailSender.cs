namespace ServiYa.Comun.Correo;

/// <summary>Correo listo para enviar, en texto simple.</summary>
public sealed record CorreoSaliente(string Para, string Asunto, string Texto);

/// <summary>Envío de correos (research R9). Quien lo usa nunca registra el destinatario ni el texto.</summary>
public interface IEmailSender
{
    Task EnviarAsync(CorreoSaliente correo, CancellationToken cancellationToken);
}

/// <summary>El proveedor rechazó el correo o no respondió. El mensaje no incluye datos personales.</summary>
public sealed class ErrorEnvioCorreoException : Exception
{
    public ErrorEnvioCorreoException()
    {
    }

    public ErrorEnvioCorreoException(string message)
        : base(message)
    {
    }

    public ErrorEnvioCorreoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
