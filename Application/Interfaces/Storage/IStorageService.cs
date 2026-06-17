using Application.Dtos.Storage;

namespace Application.Interfaces.Storage;

public interface IStorageService
{
    Task<(bool, string)> UploadFile(UploadFileReqDto requestDto);

    Task<string> Remove(string fileName);

    Task<(string, DownloadFileDto? fileDto)> DownloadFileStream(string fileName);

    Task<(string, string?)> GetPresignedUrl(string fileName, int time = 5);

    Task<(bool, string)> AppendTextToFile(string fileName, string[] text);
}