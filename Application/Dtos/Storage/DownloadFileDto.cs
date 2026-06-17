namespace Application.Dtos.Storage;

public class DownloadFileDto
{
    public required Stream Stream { get; set; }

    public required string ContentType { get; set; }

    public required string FileName { get; set; }
}