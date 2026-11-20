using System.Globalization;

namespace ReceiptFunctions.Tests;

// Checks how a receipts CSV is summed: per currency, header, empty file, malformed lines, culture.
public class ReceiptCsvTests
{
    [Fact]
    public void Sums_amounts_per_currency()
    {
        var content = "2026-11-02,Northwind Bakery,12.50,EUR\n2026-11-03,Contoso Books,20.00,USD\n2026-11-04,Fabrikam Cafe,7.25,EUR\n";

        var totals = ReceiptCsv.Summarize(content);

        Assert.Equal(3, totals.LineCount);
        Assert.Equal(0, totals.MalformedLineCount);
        Assert.Equal(19.75m, totals.TotalsByCurrency["EUR"]);
        Assert.Equal(20.00m, totals.TotalsByCurrency["USD"]);
    }

    [Fact]
    public void Skips_the_header_line()
    {
        var content = "date,merchant,amount,currency\r\n2026-11-02,Northwind Bakery,12.50,EUR\r\n";

        var totals = ReceiptCsv.Summarize(content);

        Assert.Equal(1, totals.LineCount);
        Assert.Equal(0, totals.MalformedLineCount);
        Assert.Equal(12.50m, totals.TotalsByCurrency["EUR"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("date,merchant,amount,currency\n")]
    [InlineData("\n\n")]
    public void Empty_file_has_no_lines_and_no_totals(string content)
    {
        var totals = ReceiptCsv.Summarize(content);

        Assert.Equal(0, totals.LineCount);
        Assert.Equal(0, totals.MalformedLineCount);
        Assert.Empty(totals.TotalsByCurrency);
    }

    [Fact]
    public void Counts_and_skips_malformed_lines()
    {
        var content = string.Join('\n',
            "2026-11-02,Northwind Bakery,12.50,EUR",
            "2026-11-02,Missing currency,3.00",
            "2026-11-31,Impossible date,1.00,EUR",
            "2026-11-03,Bad amount,twelve,EUR",
            "2026-11-03,,4.00,EUR",
            "2026-11-03,Bad currency,4.00,EURO",
            "2026-11-04,Fabrikam Cafe,7.25,EUR");

        var totals = ReceiptCsv.Summarize(content);

        Assert.Equal(2, totals.LineCount);
        Assert.Equal(5, totals.MalformedLineCount);
        Assert.Equal(19.75m, totals.TotalsByCurrency["EUR"]);
    }

    [Fact]
    public void Parses_amounts_the_same_way_whatever_the_current_culture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            var totals = ReceiptCsv.Summarize("2026-11-02,Northwind Bakery,1234.56,EUR\n");

            Assert.Equal(1234.56m, totals.TotalsByCurrency["EUR"]);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void Normalizes_currency_codes_to_upper_case()
    {
        var content = "2026-11-02,Northwind Bakery,1.00,eur\n2026-11-03,Fabrikam Cafe,2.00,EUR\n";

        var totals = ReceiptCsv.Summarize(content);

        Assert.Equal(3.00m, Assert.Single(totals.TotalsByCurrency).Value);
    }

    [Fact]
    public void Ignores_a_byte_order_mark()
    {
        var totals = ReceiptCsv.Summarize("\uFEFFdate,merchant,amount,currency\n2026-11-02,Northwind Bakery,-2.00,EUR\n");

        Assert.Equal(0, totals.MalformedLineCount);
        Assert.Equal(-2.00m, totals.TotalsByCurrency["EUR"]);
    }
}
