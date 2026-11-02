namespace ErikwnkWFUI.Tests.Localization;

/// <summary>Amounts in the currency of the program: the symbol, its place and the number format of the currency, whatever the language of the program is.</summary>
public class AppCurrencyFormatterTests : IDisposable
{
    private readonly AppCurrency _before = AppCurrencyFormatter.Currency;

    public void Dispose()
    {
        AppCurrencyFormatter.Currency = _before;
    }

    [Fact]
    public void Dollar_IsWrittenWithTheSymbolBeforeTheAmount_InTheAmericanFormat()
    {
        AppCurrencyFormatter.Currency = AppCurrency.Dollar;

        Assert.Equal("$1,234.50", AppCurrencyFormatter.Format(1234.5m));
        Assert.Equal("$0.00", AppCurrencyFormatter.Format(0m));
        Assert.Equal("$", AppCurrencyFormatter.Symbol);
    }

    [Fact]
    public void Euro_IsWrittenWithTheSymbolAfterTheAmount_InTheGermanFormat()
    {
        AppCurrencyFormatter.Currency = AppCurrency.Euro;

        Assert.Equal("1.234,50€", AppCurrencyFormatter.Format(1234.5m));
        Assert.Equal("0,00€", AppCurrencyFormatter.Format(0m));
        Assert.Equal("€", AppCurrencyFormatter.Symbol);
    }

    [Fact]
    public void ANegativeAmount_HasItsSignInFrontOfTheWhole()
    {
        AppCurrencyFormatter.Currency = AppCurrency.Dollar;
        Assert.Equal("-$1,234.50", AppCurrencyFormatter.Format(-1234.5m));
        Assert.Equal("-$0.05", AppCurrencyFormatter.Format(-0.05m));

        AppCurrencyFormatter.Currency = AppCurrency.Euro;
        Assert.Equal("-1.234,50€", AppCurrencyFormatter.Format(-1234.5m));
    }

    [Fact]
    public void AFormatOfYourOwn_IsUsed_ForBothCurrencies()
    {
        AppCurrencyFormatter.Currency = AppCurrency.Dollar;
        Assert.Equal("$1234.6", AppCurrencyFormatter.Format(1234.56m, "0.0"));
        Assert.Equal("-$1235", AppCurrencyFormatter.Format(-1234.5m, "0"));

        AppCurrencyFormatter.Currency = AppCurrency.Euro;
        Assert.Equal("1234,6€", AppCurrencyFormatter.Format(1234.56m, "0.0"));
    }

    [Fact]
    public void ChangingTheCurrency_RaisesTheEventOnce_AndSettingTheSameOneDoesNot()
    {
        AppCurrencyFormatter.Currency = AppCurrency.Dollar;
        int raised = 0;
        EventHandler handler = (sender, e) => raised++;
        AppCurrencyFormatter.CurrencyChanged += handler;

        try
        {
            AppCurrencyFormatter.Currency = AppCurrency.Dollar;
            Assert.Equal(0, raised);

            AppCurrencyFormatter.Currency = AppCurrency.Euro;
            Assert.Equal(1, raised);

            AppCurrencyFormatter.Currency = AppCurrency.Euro;
            Assert.Equal(1, raised);
        }
        finally
        {
            AppCurrencyFormatter.CurrencyChanged -= handler;
        }
    }
}
