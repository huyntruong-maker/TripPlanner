using Application.Dtos.Email;

namespace Application.Interfaces.Email;

public interface IEmailService
{
    Task<string> SendEmail(SendEmailReqDto request);
}