using System.Text.RegularExpressions;
using ImageOcrMicroservice.Models;

namespace ImageOcrMicroservice.Services
{
    public class MetadataService
    {
        private readonly ILogger<MetadataService> _logger;

        // We define our search patterns (Regex) here. This makes them easy to manage.
        private static readonly Regex NiuRegex = new Regex(@"\b([MP]\d{12}[A-Z])\b", RegexOptions.Compiled);
        private static readonly Regex RccmRegex = new Regex(@"\b(RC/[A-Z]{3,}/\d{4}/[A-Z]/\d{4})\b", RegexOptions.Compiled);
        private static readonly Regex DateRegex = new Regex(@"\b(\d{2}[/.-]\d{2}[/.-]\d{4})\b", RegexOptions.Compiled);
        private static readonly Regex BusinessNameRegex = new Regex(@"\b(SUKA SARL|ALLIANCE INFINIMENT)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public MetadataService(ILogger<MetadataService> logger)
        {
            _logger = logger;
        }

        public DocumentMetadata ExtractMetadata(string rawText)
        {
            _logger.LogInformation("Starting metadata extraction from raw text.");

            var metadata = new DocumentMetadata
            {
                NiuNumbers = ExtractMatches(rawText, NiuRegex),
                RccmNumbers = ExtractMatches(rawText, RccmRegex),
                BusinessNames = ExtractMatches(rawText, BusinessNameRegex),
                Dates = ExtractMatches(rawText, DateRegex),
                RawText = rawText
            };

            _logger.LogInformation("Finished metadata extraction. Found {NiuCount} NIU(s), {DateCount} Date(s), {NameCount} Name(s).",
                metadata.NiuNumbers.Count, metadata.Dates.Count, metadata.BusinessNames.Count);

            return metadata;
        }

        /// <summary>
        /// A generic helper method to run a regex pattern against text and return all unique matches.
        /// </summary>
        private List<string> ExtractMatches(string text, Regex regex)
        {
            // Find all matches, select the matched value, remove duplicates, and return as a list.
            return regex.Matches(text)
                        .Cast<Match>()
                        .Select(m => m.Value.Trim())
                        .Distinct()
                        .ToList();
        }
    }
}
