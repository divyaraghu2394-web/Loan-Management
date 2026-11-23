using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace LoanManagement.Core.Services;

public sealed class AzureDocumentIntelligenceOptions
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public bool AllowUnverifiedDocuments { get; set; }
}

public interface IDocumentValidationService
{
    Task<DocumentValidationResult> ValidateAadhaarAsync(
        IFormFile document,
        string aadhaarNumber,
        CancellationToken cancellationToken = default);

    Task<DocumentValidationResult> ValidatePanAsync(
        IFormFile document,
        string panNumber,
        CancellationToken cancellationToken = default);
}

public sealed record DocumentValidationResult(bool IsValid, string Message)
{
    public static DocumentValidationResult Success() =>
        new(true, string.Empty);
}

public sealed class AzureDocumentValidationService : IDocumentValidationService
{
    private static readonly Regex AadhaarPattern =
        new(@"(?<!\d)\d{4}[\s-]?\d{4}[\s-]?\d{4}(?!\d)", RegexOptions.Compiled);

    private static readonly Regex PanPattern =
        new(@"(?<![A-Z])[A-Z]{5}[0-9]{4}[A-Z](?![A-Z])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly AzureDocumentIntelligenceOptions _options;
    private readonly ILogger<AzureDocumentValidationService> _logger;

    public AzureDocumentValidationService(
        IOptions<AzureDocumentIntelligenceOptions> options,
        ILogger<AzureDocumentValidationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<DocumentValidationResult> ValidateAadhaarAsync(
        IFormFile document,
        string aadhaarNumber,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(
            document,
            aadhaarNumber,
            "Aadhaar",
            AadhaarPattern,
            new[] { "aadhaar", "uidai", "unique identification" },
            cancellationToken);

    public Task<DocumentValidationResult> ValidatePanAsync(
        IFormFile document,
        string panNumber,
        CancellationToken cancellationToken = default) =>
        ValidateAsync(
            document,
            panNumber,
            "PAN",
            PanPattern,
            new[] { "income tax", "permanent account number", "pan card" },
            cancellationToken);

    private async Task<DocumentValidationResult> ValidateAsync(
        IFormFile document,
        string expectedNumber,
        string documentName,
        Regex numberPattern,
        IReadOnlyCollection<string> keywords,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            if (_options.AllowUnverifiedDocuments)
            {
                return new(true,
                    "Document uploaded. OCR verification is skipped in development mode.");
            }

            return new(false,
                "Document OCR validation is not enabled. Configure Azure Document Intelligence before submitting.");
        }

        if (string.IsNullOrWhiteSpace(_options.Endpoint) ||
            string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            if (_options.AllowUnverifiedDocuments)
            {
                return new(true,
                    "Document uploaded. OCR verification is skipped because Azure Document Intelligence is not configured.");
            }

            return new(false,
                "Azure Document Intelligence is not configured. Contact the administrator.");
        }

        try
        {
            var client = new DocumentAnalysisClient(
                new Uri(_options.Endpoint),
                new AzureKeyCredential(_options.ApiKey));

            await using Stream stream = document.OpenReadStream();
            AnalyzeDocumentOperation operation =
                await client.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    "prebuilt-read",
                    stream,
                    cancellationToken: cancellationToken);

            string text = string.Join(
                "\n",
                operation.Value.Pages
                    .SelectMany(page => page.Lines)
                    .Select(line => line.Content));

            string normalizedText = Regex.Replace(text.ToUpperInvariant(), @"\s+", " ");
            string normalizedExpected = Regex.Replace(expectedNumber.ToUpperInvariant(), @"\D", "");
            string normalizedExtracted = Regex.Replace(text.ToUpperInvariant(), @"\D", "");

            bool hasKeyword = keywords.Any(keyword =>
                normalizedText.Contains(keyword.ToUpperInvariant(), StringComparison.Ordinal));

            bool hasExpectedNumber = documentName == "Aadhaar"
                ? normalizedExtracted.Contains(normalizedExpected, StringComparison.Ordinal)
                : PanPattern.IsMatch(text) && normalizedText.Contains(expectedNumber.ToUpperInvariant(), StringComparison.Ordinal);

            if (!hasKeyword || !hasExpectedNumber)
            {
                return new(false,
                    $"The uploaded document could not be verified as the provided {documentName} document.");
            }

            return DocumentValidationResult.Success();
        }
        catch (Exception ex) when (ex is RequestFailedException or IOException)
        {
            _logger.LogError(ex, "Azure OCR validation failed for {DocumentName}.", documentName);
            return new(false,
                $"Unable to verify the {documentName} document. Please upload a clearer image or PDF.");
        }
    }
}
