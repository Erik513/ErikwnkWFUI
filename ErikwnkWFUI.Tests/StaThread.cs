using System.Runtime.ExceptionServices;

namespace ErikwnkWFUI.Tests;

/// <summary>
/// Runs an action on a dedicated STA thread and rethrows whatever it threw -
/// xUnit itself runs tests on an MTA thread, but Clipboard access (used by
/// DataGridView's paste/cut) throws unless the calling thread is STA. Rather
/// than a custom xUnit test framework just for this, tests that touch the
/// clipboard call <see cref="Run"/> around the part of the test that needs it.
/// </summary>
internal static class StaThread
{
    public static void Run(Action action)
    {
        Exception? caught = null;

        Thread thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (caught != null)
        {
            ExceptionDispatchInfo.Capture(caught).Throw();
        }
    }
}
