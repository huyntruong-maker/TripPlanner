namespace Application.Dtos.Email;

public class SendEmailReqDto
{
    public required List<string> ToEmails { get; set; }

    public List<string>? CcEmails { get; set; }

    public List<string>? BccEmails { get; set; }

    public required string Subject { get; set; }

    public required string Body { get; set; }
}