using Application.Dtos.Email;

namespace Application.Interfaces.Email;

/// <summary>
/// Transport strategy for delivering a single already-validated email batch.
/// Implementations own their own transport and configuration; <see cref="IEmailService"/>
/// stays responsible for validation, batching, and logging so those concerns are not
/// duplicated per provider. Select the active implementation with the
/// <c>Email:Provider</c> setting.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Delivers the message to exactly the recipients in <paramref name="request"/>.
    /// Returns an empty string on success, otherwise an <c>Email.Create.*</c> error code.
    /// </summary>
    Task<string> Send(SendEmailReqDto request, CancellationToken cancellationToken = default);
}
