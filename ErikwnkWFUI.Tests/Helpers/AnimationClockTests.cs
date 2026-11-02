using ErikwnkWFUI.Helpers;
using ErikwnkWFUI.Tests.Infrastructure;

namespace ErikwnkWFUI.Tests.Helpers;

/// <summary>
/// The beat of everything that moves: clients get a beat each, one that fails does not end the others, the thread goes when the last client leaves and
/// comes back with the next one, and the wait for the display has its fallbacks (it fails, it throws, it does not wait). [Collection]: the clock is shared
/// by every control that moves, and the wait is swapped here.
/// </summary>
[Collection(AccentColorTestCollection.Name)]
public class AnimationClockTests : IDisposable
{
    private readonly Func<int> _flush = AnimationClock.Flush;
    private readonly List<IAnimationClient> _clients = new List<IAnimationClient>();

    public void Dispose()
    {
        foreach (IAnimationClient client in _clients)
            AnimationClock.Remove(client);

        AnimationClock.Flush = _flush;
    }

    private sealed class CountingClient : IAnimationClient
    {
        private int _beats;

        public int Beats => Volatile.Read(ref _beats);

        public void RequestFrame()
        {
            Interlocked.Increment(ref _beats);
        }
    }

    private sealed class FailingClient : IAnimationClient
    {
        public void RequestFrame()
        {
            throw new InvalidOperationException("this client is in a bad state");
        }
    }

    private T Add<T>(T client) where T : IAnimationClient
    {
        _clients.Add(client);
        AnimationClock.Add(client);

        return client;
    }

    private static bool WaitFor(Func<bool> condition, int milliseconds = 3000)
    {
        DateTime end = DateTime.UtcNow.AddMilliseconds(milliseconds);

        while (DateTime.UtcNow < end)
        {
            if (condition())
                return true;

            Thread.Sleep(5);
        }

        return condition();
    }

    [Fact]
    public void AClientGetsBeatsWhileItIsOnTheClock_AndNoMoreAfterItLeaves()
    {
        CountingClient client = Add(new CountingClient());

        Assert.True(WaitFor(() => client.Beats >= 5), "only " + client.Beats + " beats");

        AnimationClock.Remove(client);
        Thread.Sleep(50);
        int settled = client.Beats;
        Thread.Sleep(150);

        Assert.Equal(settled, client.Beats);
    }

    [Fact]
    public void AClientAddedTwice_IsOnTheClockOnce()
    {
        CountingClient client = new CountingClient();
        int before = AnimationClock.Count;

        Add(client);
        Add(client);

        Assert.Equal(before + 1, AnimationClock.Count);
    }

    [Fact]
    public void AClientThatFails_DoesNotEndTheBeatOfTheOthers()
    {
        Add(new FailingClient());
        CountingClient other = Add(new CountingClient());

        Assert.True(WaitFor(() => other.Beats >= 5), "only " + other.Beats + " beats");
    }

    [Fact]
    public void WhenTheLastClientHasLeft_TheThreadEnds_AndTheNextClientStartsItAgain()
    {
        CountingClient first = Add(new CountingClient());
        Assert.True(WaitFor(() => first.Beats >= 2));
        AnimationClock.Remove(first);
        int before = AnimationClock.Count;
        Thread.Sleep(200);

        CountingClient second = Add(new CountingClient());

        Assert.True(WaitFor(() => second.Beats >= 3), "only " + second.Beats + " beats");
        Assert.True(AnimationClock.Count >= before);
    }

    public static IEnumerable<object[]> WaitsForTheDisplay()
    {
        yield return new object[] { "the wait fails", (Func<int>)(() => 1) };
        yield return new object[] { "the wait does not wait", (Func<int>)(() => 0) };
        yield return new object[] { "there is no display service", (Func<int>)(() => throw new DllNotFoundException("dwmapi")) };
    }

    [Theory]
    [MemberData(nameof(WaitsForTheDisplay))]
    public void WhereTheWaitForTheDisplayDoesNotWork_TheBeatGoesOnAtALimitedPace(string case_, Func<int> flush)
    {
        Assert.NotNull(case_);
        AnimationClock.Flush = flush;
        CountingClient client = Add(new CountingClient());
        int start = client.Beats;
        DateTime from = DateTime.UtcNow;

        Assert.True(WaitFor(() => client.Beats - start >= 5), "only " + (client.Beats - start) + " beats: " + case_);

        // (A wait that returns at once must not turn into a loop that eats a whole processor: a beat is at least about 3 ms apart.)
        Assert.True((DateTime.UtcNow - from).TotalMilliseconds >= 5 * 2, case_);
    }
}
