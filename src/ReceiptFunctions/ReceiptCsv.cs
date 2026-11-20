using System.Globalization;

namespace ReceiptFunctions;

// Parses a receipts CSV (date,merchant,amount,currency) and sums the amounts per currency; no Azure involved.
public static class ReceiptCsv
{
    private const int ExpectedFieldCount = 4;

    // Sums the valid lines; a malformed line is counted and skipped so that one bad line never fails the file.
    public static ReceiptTotals Summarize(string content)
    {
        var totalsByCurrency = new SortedDictionary<string, decimal>(StringComparer.Ordinal);
        var lineCount = 0;
        var malformedLineCount = 0;
        var lines = content.TrimStart('\uFEFF').Split('\n');

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || IsHeader(line))
            {
                continue;
            }

            if (!TryParseLine(line, out var amount, out var currency))
            {
                malformedLineCount++;
                continue;
            }

            totalsByCurrency[currency] = totalsByCurrency.GetValueOrDefault(currency) + amount;
            lineCount++;
        }

        return new ReceiptTotals(lineCount, malformedLineCount, totalsByCurrency);
    }

    // The header is recognised by its first column name, wherever the file was exported from.
    private static bool IsHeader(string line) =>
        line.StartsWith("date,", StringComparison.OrdinalIgnoreCase);

    // Fields are plain (no quoting): the sample's merchants have no commas in their names.
    private static bool TryParseLine(string line, out decimal amount, out string currency)
    {
        amount = 0;
        currency = string.Empty;
        var fields = line.Split(',');
        if (fields.Length != ExpectedFieldCount)
        {
            return false;
        }

        var isDateValid = DateOnly.TryParseExact(fields[0].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        var isMerchantPresent = fields[1].Trim().Length > 0;
        var isAmountValid = decimal.TryParse(
            fields[2].Trim(),
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out amount);
        var currencyCode = fields[3].Trim();
        var isCurrencyValid = currencyCode.Length == 3 && currencyCode.All(char.IsAsciiLetter);

        if (!isDateValid || !isMerchantPresent || !isAmountValid || !isCurrencyValid)
        {
            return false;
        }

        currency = currencyCode.ToUpperInvariant();
        return true;
    }
}

// What one receipts file adds up to: the valid lines, the skipped ones, and the total of each currency.
public sealed record ReceiptTotals(
    int LineCount,
    int MalformedLineCount,
    IReadOnlyDictionary<string, decimal> TotalsByCurrency);
