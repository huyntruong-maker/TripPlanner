using System.IO.Compression;
using System.Text.Json;

namespace Domain.Helpers;

/// <summary>
/// Builds a ZIP archive in memory with various content types
/// </summary>
public class ZipArchiveBuilder : IDisposable
{
    private readonly MemoryStream _archiveStream;
    private readonly ZipArchive _archive;
    private bool _isFinalized;
    private readonly CompressionLevel _compressionLevel;

    public ZipArchiveBuilder(CompressionLevel compressionLevel = CompressionLevel.NoCompression)
    {
        _archiveStream = new MemoryStream();
        _archive = new ZipArchive(_archiveStream, ZipArchiveMode.Create, true);
        _compressionLevel = compressionLevel;
    }

    /// <summary>
    /// Adds JSON data to the archive
    /// </summary>
    public ZipArchiveBuilder AddJson<T>(T data, string entryName)
    {
        if (string.IsNullOrEmpty(entryName))
            throw new ArgumentException("Entry name cannot be null or empty", nameof(entryName));

        var entry = _archive.CreateEntry(entryName, _compressionLevel);
        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream);
        JsonSerializer.Serialize(writer, data);
        return this;
    }

    /// <summary>
    /// Adds JSON data to the archive asynchronously
    /// </summary>
    public async Task<ZipArchiveBuilder> AddJsonAsync<T>(T data, string entryName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(entryName))
        {
            entryName = Guid.NewGuid().ToString();
        }

        if (!entryName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            entryName += ".json";
        }

        var entry = _archive.CreateEntry(entryName, _compressionLevel);
        await using var entryStream = entry.Open();
        await JsonSerializer.SerializeAsync(entryStream, data, cancellationToken: cancellationToken);
        return this;
    }

    /// <summary>
    /// Adds a stream to the archive asynchronously
    /// </summary>
    public async Task<ZipArchiveBuilder> AddStreamAsync(Stream stream, string entryName, CancellationToken cancellationToken = default)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrEmpty(entryName))
            throw new ArgumentException("Entry name cannot be null or empty", nameof(entryName));
        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(stream));

        var entry = _archive.CreateEntry(entryName, _compressionLevel);
        await using var entryStream = entry.Open();
        await stream.CopyToAsync(entryStream, cancellationToken);
        return this;
    }

    /// <summary>
    /// Adds a stream to the archive synchronously
    /// </summary>
    public ZipArchiveBuilder AddStream(Stream stream, string entryName)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrEmpty(entryName))
            throw new ArgumentException("Entry name cannot be null or empty", nameof(entryName));
        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable", nameof(stream));

        var entry = _archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        stream.CopyTo(entryStream);
        return this;
    }

    /// <summary>
    /// Builds and returns the archive stream. The caller is responsible for disposing the returned stream.
    /// </summary>
    public Stream Build()
    {
        if (_isFinalized)
            throw new InvalidOperationException("Archive has already been built");

        _archive.Dispose();
        _archiveStream.Position = 0;
        _isFinalized = true;

        return _archiveStream;
    }

    public void Dispose()
    {
        if (!_isFinalized)
        {
            _archive.Dispose();
            _archiveStream.Dispose();
        }
    }
}