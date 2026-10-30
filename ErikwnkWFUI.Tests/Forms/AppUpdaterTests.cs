using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using ErikwnkCore.Updater;
using ErikwnkWFUI.Forms;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Forms;

/// <summary>
/// The check for an update and the steps around it, against a made-up GitHub: a newer version is found, the same or an older one is not, a failure
/// (no answer, a refusal) is an answer and never an exception, a download that cannot start says so, and the version and the address are written as they should be.
/// </summary>
public class AppUpdaterTests
{
    private const string Newer = "{\"tag_name\":\"v99.0.0\",\"html_url\":\"https://example.com/r\",\"assets\":[{\"name\":\"app.exe\",\"browser_download_url\":\"https://example.com/app.exe\"}]}";

    private sealed class Fake : HttpMessageHandler
    {
        private readonly Func<HttpResponseMessage> _answer;

        public Fake(string? json, HttpStatusCode code = HttpStatusCode.OK, bool fail = false)
        {
            _answer = () =>
            {
                if (fail)
                    throw new HttpRequestException("no network");

                return new HttpResponseMessage(code) { Content = new StringContent(json ?? "", Encoding.UTF8, "application/json") };
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_answer());
        }
    }

    private static AppUpdater Updater(string? json, HttpStatusCode code = HttpStatusCode.OK, bool fail = false)
    {
        HttpClient check = new HttpClient(new Fake(json, code, fail));
        HttpClient download = new HttpClient(new Fake(null, HttpStatusCode.NotFound));

        return new AppUpdater("owner", "repo", check, download);
    }

    [Fact]
    public async Task ANewerVersion_IsFound_AndTheSameOrAnOlderOneIsNot()
    {
        AppUpdater updater = Updater(Newer);

        UpdateCheckResult? found = await updater.CheckForUpdateAsync(new Version(1, 0, 0), TimeSpan.FromSeconds(5));
        Assert.NotNull(found);
        Assert.Equal(new Version(99, 0, 0), found!.LatestVersion);

        Assert.Null(await updater.CheckForUpdateAsync(new Version(99, 0, 0), TimeSpan.FromSeconds(5)));
        Assert.Null(await updater.CheckForUpdateAsync(new Version(100, 0, 0), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ARefusalOrNoNetwork_IsAnAnswer_NeverAnException()
    {
        UpdateCheck refused = await Updater(null, HttpStatusCode.NotFound).CheckAsync(new Version(1, 0, 0), TimeSpan.FromSeconds(5));
        Assert.Null(refused.Result);

        UpdateCheck offline = await Updater(null, fail: true).CheckAsync(new Version(1, 0, 0), TimeSpan.FromSeconds(5));
        Assert.Null(offline.Result);

        Assert.Null(await Updater(null, fail: true).CheckForUpdateAsync(new Version(1, 0, 0), TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task TheSilentCheck_WithNothingToShow_OrNoNetwork_ReturnsQuietly()
    {
        await Updater(Newer).CheckForUpdateAsync(new Version(99, 0, 0), TimeSpan.FromSeconds(5), null!);
        await Updater(null, fail: true).CheckForUpdateAsync(new Version(1, 0, 0), TimeSpan.FromSeconds(5), null!);
    }

    [Fact]
    public async Task ADownloadThatCannotStart_SaysSo_AndACancelledOneThrowsTheCancel()
    {
        AppUpdater updater = Updater(Newer);
        MethodInfo apply = typeof(AppUpdater).GetMethod("ApplyUpdateAsync", BindingFlags.NonPublic | BindingFlags.Instance)!;

        UpdateCheckResult noAddress = new UpdateCheckResult(new Version(99, 0, 0), "", null);
        Assert.False(await (Task<bool>)apply.Invoke(updater, new object?[] { noAddress, new Progress<int>(), CancellationToken.None })!);

        UpdateCheckResult missing = new UpdateCheckResult(new Version(99, 0, 0), "", "https://example.com/app.exe");
        Assert.False(await (Task<bool>)apply.Invoke(updater, new object?[] { missing, new Progress<int>(), CancellationToken.None })!);

        using CancellationTokenSource cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => (Task<bool>)apply.Invoke(updater, new object?[] { missing, new Progress<int>(), cancelled.Token })!);
    }

    [Theory]
    [InlineData(1, 2, -1, "1.2.0")]
    [InlineData(1, 2, 3, "1.2.3")]
    [InlineData(10, 0, 7, "10.0.7")]
    public void TheVersion_IsShownWithThreeNumbers(int major, int minor, int build, string expected)
    {
        Version version = build < 0 ? new Version(major, minor) : new Version(major, minor, build);

        Assert.Equal(expected, AppUpdater.FormatVersion(version));
    }

    [Theory]
    [InlineData("https://example.com/release", true)]
    [InlineData("http://example.com/release", true)]
    [InlineData("file:///C:/Windows/System32/cmd.exe", false)]
    [InlineData("C:\\Windows\\System32\\cmd.exe", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void OnlyAWebAddress_IsOpenedInTheBrowser(string? address, bool expected)
    {
        Assert.Equal(expected, AppUpdater.IsWebAddress(address!));
    }
}
