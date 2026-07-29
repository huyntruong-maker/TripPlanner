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

    public static string RegexGroupValue(this string text, string pattern, int? idx = null)
    {
        var regex = new Regex(pattern);
        var match = regex.Match(text);
        return idx != null ? match.Groups[idx.Value].Value : match.Value;
    }

    public static string RegexGroupValueLast(this string text, string pattern, int? idx = null)
    {
        var regex = new Regex(pattern);
        var matches = regex.Matches(text);
        return idx != null ? matches.Last().Groups[idx.Value].Value : matches.Last().Value;
    }

    public static string ComputeHash(this object toCompute)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(toCompute.ToJson().ToByteArray());

        var sb = new StringBuilder();
        foreach (var hashByte in hashBytes) sb.Append(hashByte.ToString("x2"));

        return sb.ToString();
    }

    public static string ComputeHash(this string text)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(text.ToByteArray());

        var sb = new StringBuilder();
        foreach (var hashByte in hashBytes) sb.Append(hashByte.ToString("x2"));

        return sb.ToString();
    }

    public static bool CompareHash(string source, string toCompare)
    {
        return string.IsNullOrEmpty(source) ? string.IsNullOrEmpty(toCompare) : source.Equals(toCompare);
    }

    public static string ConvertZeroToEmpty(this double? source, string format = "0.00")
    {
        if (source == null) return string.Empty;
        return source == 0 ? string.Empty : source.Value.ToString(format);
    }

    public static double? TryParseDoubleNullable(this string source, Func<string, string>? func,
        double? defaultValue = 0.0)
    {
        if (func != null) source = func(source);

        var canParse = double.TryParse(source, out var result);
        return canParse ? result : defaultValue;
    }

    public static int ToInt(this Enum value)
    {
        return Convert.ToInt32(value);
    }

    public static string DoubleToString(this double? source, string format = "0.00")
    {
        if (source == null) return string.Empty;
        return source != 0 ? source.Value.ToString(format) : "0";
    }

    public static T[] InitArray<T>(int count, T defaultValue)
    {
        return Enumerable.Repeat(defaultValue, count).ToArray();
    }

    public static byte[] ToByteArray(this string source)
    {
        return Encoding.UTF8.GetBytes(source);
    }

    public static string ToUtf8String(this byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    public static string RemoveSpaces(this string source)
    {
        return source.Replace(" ", "");
    }

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

    /// <summary>Returns Guid.Empty (not null) when no user-id claim is found.</summary>
    public static Guid GetUserIdNullable(this IEnumerable<Claim> claims)
    {
        if (claims.Count() == 0) return Guid.Empty;

        var claimValue = claims.FirstOrDefault(x => x.Type == "nameid" || x.Type == ClaimTypes.NameIdentifier)?.Value;
        return claimValue != null ? Guid.Parse(claimValue) : Guid.Empty;
    }

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

        var properties = _propertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
             .ToArray());

        foreach (var property in properties)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value == null) continue;

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

    public static bool IsValidYearRange(int year, int minYear = 2000, int maxYear = 9999)
    {
        return year >= minYear && year <= maxYear;
    }

    public static bool IsValidMonth(int month)
    {
        return month >= 1 && month <= 12;
    }

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

    public static string[] GetWorkflowAllowedFileExtension()
    {
        return [
            GlobalConstants.FileExtension.Zip,
            GlobalConstants.FileExtension.Rar,
        ];
    }

    public static bool IsValidMimeType(this IFormFile file, string[] allowedMimeTypes)
    {
        if (file == null || file.Length == 0 || string.IsNullOrWhiteSpace(file.ContentType))
        {
            return false;
        }

        return allowedMimeTypes.Contains(file.ContentType.ToLower());
    }

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