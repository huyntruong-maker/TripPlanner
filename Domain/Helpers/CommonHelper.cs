using Domain.Constants;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Collections.Concurrent;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Domain.Helpers;

public static class CommonHelper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = new();

    /// <summary>
    ///     Extract data between a symbol
    ///     Get first group
    /// </summary>
    /// <param name="text">Source</param>
    /// <param name="pattern">Pattern to search</param>
    /// <param name="idx">Index of matched data</param>
    /// <returns>Text result</returns>
    public static string RegexGroupValue(this string text, string pattern, int? idx = null)
    {
        var regex = new Regex(pattern);
        var match = regex.Match(text);
        return idx != null ? match.Groups[idx.Value].Value : match.Value;
    }

    /// <summary>
    ///     Extract data between a symbol
    ///     Get last group
    /// </summary>
    /// <param name="text">Source</param>
    /// <param name="pattern">Pattern to search</param>
    /// <param name="idx">Index of matched data</param>
    /// <returns>Text result</returns>
    public static string RegexGroupValueLast(this string text, string pattern, int? idx = null)
    {
        var regex = new Regex(pattern);
        var matches = regex.Matches(text);
        return idx != null ? matches.Last().Groups[idx.Value].Value : matches.Last().Value;
    }

    /// <summary>
    ///     Compute an object
    /// </summary>
    /// <param name="toCompute">Object to compute</param>
    /// <returns>Hash</returns>
    public static string ComputeHash(this object toCompute)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(toCompute.ToJson().ToByteArray());

        var sb = new StringBuilder();
        foreach (var hashByte in hashBytes) sb.Append(hashByte.ToString("x2"));

        return sb.ToString();
    }

    /// <summary>
    ///     Compute a string
    /// </summary>
    /// <param name="text">String to compute</param>
    /// <returns>Hash</returns>
    public static string ComputeHash(this string text)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(text.ToByteArray());

        var sb = new StringBuilder();
        foreach (var hashByte in hashBytes) sb.Append(hashByte.ToString("x2"));

        return sb.ToString();
    }

    /// <summary>
    ///     Compare 2 hash
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="toCompare">Hash to compare</param>
    /// <returns>Compare result</returns>
    public static bool CompareHash(string source, string toCompare)
    {
        return string.IsNullOrEmpty(source) ? string.IsNullOrEmpty(toCompare) : source.Equals(toCompare);
    }

    /// <summary>
    ///     Convert 0 to empty, usually use for data exportation
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="format">Format</param>
    /// <returns>Convert result</returns>
    public static string ConvertZeroToEmpty(this double? source, string format = "0.00")
    {
        if (source == null) return string.Empty;
        return source == 0 ? string.Empty : source.Value.ToString(format);
    }

    /// <summary>
    ///     Parse double
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="func">Function</param>
    /// <param name="defaultValue">Default value</param>
    /// <returns>Parse result, default value if parse error</returns>
    public static double? TryParseDoubleNullable(this string source, Func<string, string>? func,
        double? defaultValue = 0.0)
    {
        if (func != null) source = func(source);

        var canParse = double.TryParse(source, out var result);
        return canParse ? result : defaultValue;
    }

    /// <summary>
    ///     Convert to integer
    /// </summary>
    /// <param name="value">Value</param>
    /// <returns>Int</returns>
    public static int ToInt(this Enum value)
    {
        return Convert.ToInt32(value);
    }

    /// <summary>
    ///     Convert double to string
    /// </summary>
    /// <param name="source">Source</param>
    /// <param name="format">Format</param>
    /// <returns>Double as string</returns>
    public static string DoubleToString(this double? source, string format = "0.00")
    {
        if (source == null) return string.Empty;
        return source != 0 ? source.Value.ToString(format) : "0";
    }

    /// <summary>
    ///     Init array
    /// </summary>
    /// <param name="count">Array size</param>
    /// <param name="defaultValue">Init value</param>
    /// <returns>Array</returns>
    public static T[] InitArray<T>(int count, T defaultValue)
    {
        return Enumerable.Repeat(defaultValue, count).ToArray();
    }

    /// <summary>
    ///     Convert to byte array
    /// </summary>
    /// <param name="source">Source</param>
    /// <returns>Byte array</returns>
    public static byte[] ToByteArray(this string source)
    {
        return Encoding.UTF8.GetBytes(source);
    }

    /// <summary>
    ///     Convert to utf8
    /// </summary>
    /// <param name="data">Data</param>
    /// <returns>String</returns>
    public static string ToUtf8String(this byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    /// <summary>
    ///     Remove spaces
    /// </summary>
    /// <param name="source">Source</param>
    /// <returns>String with no space</returns>
    public static string RemoveSpaces(this string source)
    {
        return source.Replace(" ", "");
    }

    /// <summary>
    ///     Check file existence
    /// </summary>
    /// <param name="filePath">FilePath</param>
    /// <param name="cancelToken">Cancellation token</param>
    /// <returns>True if the file exists. Otherwise, False</returns>
    public static async Task<bool> CheckFileExistence(string filePath, CancellationToken? cancelToken = null)
    {
        var attempt = 5;
        while (attempt > 0)
        {
            if (File.Exists(filePath)) return true;

            if (cancelToken.HasValue)
                await Task.Delay(3000, cancelToken.Value);
            else
                await Task.Delay(3000);

            attempt--;
        }

        return false;
    }

    public static double MRound(this double number, double multiple)
    {
        if (multiple == 0) throw new ArgumentException("Multiple cannot be zero.");

        return Math.Round(number / multiple) * multiple;
    }

    public static string ConvertDashToDefaultValue(this string value)
    {
        return value == "-" ? "0" : value;
    }

    public static string ConvertToValidDoubleType(this string value)
    {
        return value == "-" || string.IsNullOrEmpty(value) ? "0" : value;
    }

    public static string SignFormat(this double value, bool plusZero = false)
    {
        return value.ToString(!plusZero ? "+#;-#;0" : "+#;-#;+0");
    }

    public static string GetSign(this double value, bool plusZero = false)
    {
        if (plusZero) return value >= 0 ? "+" : "-";

        if (value == 0) return string.Empty;

        return value > 0 ? "+" : "-";
    }

    public static bool IsEqual(this double value, double compareTo, double precision = double.Epsilon)
    {
        return Math.Abs(value - compareTo) < precision;
    }

    /// <summary>
    ///     Extract userId nullable from claims.
    /// </summary>
    /// <param name="claims">Logged in claims</param>
    /// <returns>UserId</returns>
    public static Guid GetUserIdNullable(this IEnumerable<Claim> claims)
    {
        if (claims.Count() == 0) return Guid.Empty;

        var claimValue = claims.FirstOrDefault(x => x.Type == "nameid" || x.Type == ClaimTypes.NameIdentifier)?.Value;
        return claimValue != null ? Guid.Parse(claimValue) : Guid.Empty;
    }

    /// <summary>
    ///     Extract userId from claims.
    /// </summary>
    /// <param name="claims">Logged in claims</param>
    /// <returns>UserId</returns>
    public static Guid GetUserId(this IEnumerable<Claim> claims)
    {
        var claimValue = claims.First(x => x.Type == "nameid" || x.Type == ClaimTypes.NameIdentifier).Value;
        return Guid.Parse(claimValue);
    }

    public static bool IsValidEmail(this string email)
    {
        var text = email.Trim();
        if (text.EndsWith(".")) return false;

        try
        {
            return new MailAddress(email).Address == text;
        }
        catch
        {
            return false;
        }
    }

    public static bool ValidatePasswordPolicy(this string password)
    {
        var regex = new Regex(UserConstants.Password.RegexPattern, RegexOptions.None, TimeSpan.FromSeconds(30));
        return regex.IsMatch(password);
    }

    public static string HashPassword(this string password)
    {
        PasswordHasher<string> passwordHasher = new(
            new OptionsWrapper<PasswordHasherOptions>(
                new PasswordHasherOptions()
                {
                    CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                }));

        string hashPassword = passwordHasher.HashPassword(string.Empty, password);
        return hashPassword;
    }

    /// <summary>
    /// Generate GUID Base64 Encoded
    /// </summary>
    /// <returns></returns>
    public static string GenerateBase64GuidToken()
    {
        var guidBytes = Guid.NewGuid().ToByteArray();

        return Convert.ToBase64String(guidBytes).TrimEnd('=');
    }
    public static string Base64Encode(this string plainText)
    {
        var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64Decode(this string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    /// <summary>
    /// Trims all string properties in an object and its nested objects
    /// </summary>
    /// <typeparam name="T">Type of the object</typeparam>
    /// <param name="obj">Object to process</param>
    /// <param name="logger">Logger for error reporting</param>
    public static void TrimData<T>(this T obj, ILogger? logger = null)
    {
        if (obj == null || obj is ValueType || obj is string) return;
        obj.TrimData(new HashSet<object>(ReferenceEqualityComparer.Instance), logger);
    }

    private static void TrimData<T>(this T obj, HashSet<object> visited, ILogger? logger)
    {
        if (obj == null || !visited.Add(obj)) return;

        var type = obj.GetType();
        if (type.IsPrimitive || type.IsValueType) return;

        if (obj is IEnumerable collection)
        {
            foreach (var item in collection)
            {
                item?.TrimData(visited, logger);
            }
            return;
        }

        // Get cached properties or add them to cache if not present
        var properties = _propertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0) // Skip indexers
             .ToArray());

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value == null) continue;

                // Trim string properties
                if (value is string strValue && !string.IsNullOrWhiteSpace(strValue))
                {
                    property.SetValue(obj, strValue.Trim());
                    continue;
                }

                value.TrimData(visited, logger);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "[TrimData] Error processing property '{PropertyName}' in type '{TypeName}'",
                    property.Name, obj.GetType().FullName);
            }
        }
    }

    public static bool IsInvalidGuid(params Guid[] guids)
    {
        return guids.Any(guid => guid == Guid.Empty);
    }

    /// <summary>
    /// Validates if a year is within a specified range.
    /// </summary>
    /// <param name="year">The year to validate.</param>
    /// <param name="minYear">The minimum valid year (default: 2000).</param>
    /// <param name="maxYear">The maximum valid year (default: 9999).</param>
    /// <returns>True if the year is valid, otherwise false.</returns>
    public static bool IsValidYearRange(int year, int minYear = 2000, int maxYear = 9999)
    {
        return year >= minYear && year <= maxYear;
    }

    /// <summary>
    /// Validates if a month is within the valid range (1-12).
    /// </summary>
    /// <param name="month">The month to validate.</param>
    /// <returns>True if the month is valid, otherwise false.</returns>
    public static bool IsValidMonth(int month)
    {
        return month >= 1 && month <= 12;
    }

    /// <summary>
    /// Gets the allowed MIME types for archive files
    /// </summary>
    /// <returns>Array of allowed MIME types for archives</returns>
    public static string[] GetAllowedArchiveMimeTypes()
    {
        return new string[]
        {
            GlobalConstants.MimeTypes.ApplicationZip,
            GlobalConstants.MimeTypes.ApplicationRar,
            GlobalConstants.MimeTypes.ApplicationXZip,
            GlobalConstants.MimeTypes.ApplicationVndRar,
            GlobalConstants.MimeTypes.ApplicationX,
            GlobalConstants.MimeTypes.ApplicationXRar,
            GlobalConstants.MimeTypes.ApplicationOctetStream
        };
    }

    /// <summary>
    /// Retrieves the allowed file extensions for workflow.
    /// </summary>
    /// <returns>Array of allowed file extensions.</returns>
    public static string[] GetWorkflowAllowedFileExtension()
    {
        return [
            GlobalConstants.FileExtension.Zip,
            GlobalConstants.FileExtension.Rar,
        ];
    }

    /// <summary>
    /// Validates if a file's MIME type is in the list of allowed MIME types
    /// </summary>
    /// <param name="file">The file to validate</param>
    /// <param name="allowedMimeTypes">Array of allowed MIME types</param>
    /// <returns>True if the MIME type is allowed, otherwise false</returns>
    public static bool IsValidMimeType(this IFormFile file, string[] allowedMimeTypes)
    {
        if (file == null || file.Length == 0 || string.IsNullOrWhiteSpace(file.ContentType))
        {
            return false;
        }

        return allowedMimeTypes.Contains(file.ContentType.ToLower());
    }

    /// <summary>
    /// Checks if the input string matches the specified regex pattern.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <param name="pattern">The regex pattern to match against.</param>
    /// <param name="options">Optional regex options (e.g., IgnoreCase, Compiled).</param>
    /// <returns>True if the input matches the pattern; otherwise, false.</returns>
    public static bool IsMatchRegexPattern(this string input, string pattern, RegexOptions options = RegexOptions.None)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(pattern))
            return false;

        return Regex.IsMatch(input, pattern, options);
    }

    public static async Task<string?> ConvertFileToBase64Async(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Checks if the uploaded file has a valid extension.
    /// </summary>
    /// <param name="file">The uploaded file (IFormFile).</param>
    /// <param name="allowedExtensions">A list of allowed file extensions (e.g., ".zip", ".rar").</param>
    /// <returns>True if the file has a valid extension, otherwise False.</returns>
    public static bool IsValidFileExtension(this IFormFile file, params string[] allowedExtensions)
    {
        if (file == null
            || file.Length == 0
            || string.IsNullOrWhiteSpace(file.FileName))
        {
            return false;
        }

        var extension = Path.GetExtension(file.FileName)?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return allowedExtensions.Any(ext => ext.ToLowerInvariant() == extension);
    }

}