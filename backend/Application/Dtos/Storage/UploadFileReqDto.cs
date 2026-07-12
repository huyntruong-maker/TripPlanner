using Microsoft.AspNetCore.Http;

namespace Application.Dtos.Storage;

public class UploadFileReqDto
{
    public required IFormFile File { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}