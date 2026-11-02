using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Helpers
{
    /// <summary>Something that moves on the beat of the <see cref="AnimationClock"/>.</summary>
    internal interface IAnimationClient
    {
        /// <summary>Called from the thread of the clock, once on each beat: ask the window for the next picture (post a message) and return.</summary>
        void RequestFrame();
    }

    /// <summary>
    /// The beat for everything that moves in the library (the text of a label, a spinner, the marquee of a progress bar, a moving text
    /// on a bar): one thread that wakes with the display (as often as the display shows a new picture, 60 times a second or more) and
    /// asks every client that moves for its next picture. A Windows Forms timer cannot do that: it ticks about every 31 milliseconds,
    /// late and unevenly, behind everything else in the message queue, and what moves then moves in jerks. The thread runs only while a
    /// client moves.
    /// </summary>
    internal static class AnimationClock
    {
        private static readonly object Gate = new object();
        private static readonly List<IAnimationClient> Clients = new List<IAnimationClient>();
        private static Thread _thread;

        // The clients that move now.
        internal static int Count
        {
            get
            {
                lock (Gate)
                    return Clients.Count;
            }
        }

        internal static void Add(IAnimationClient client)
        {
            lock (Gate)
            {
                if (!Clients.Contains(client))
                    Clients.Add(client);

                if (_thread == null)
                {
                    // It only waits for the display and posts a message: it must not be late.
                    _thread = new Thread(Run) { IsBackground = true, Name = "Animation", Priority = ThreadPriority.Highest };
                    _thread.Start();
                }
            }
        }

        internal static void Remove(IAnimationClient client)
        {
            lock (Gate)
                Clients.Remove(client);
        }

        private static void Run()
        {
            while (true)
            {
                IAnimationClient[] clients;

                lock (Gate)
                {
                    if (Clients.Count == 0)
                    {
                        _thread = null;

                        return;
                    }

                    clients = Clients.ToArray();
                }

                WaitForNextPicture();

                foreach (IAnimationClient client in clients)
                {
                    // One client that fails must not end the beat of all the others (and with it the thread, which
                    // would then never be started again).
                    try
                    {
                        client.RequestFrame();
                    }
                    catch (Exception)
                    {
                        // the client is gone or in a bad state: it asks for the next beat again or leaves the clock
                    }
                }
            }
        }

        /// <summary>Waits for the display and gives 0 when it did (the seam of the tests: a test can make it fail, throw or return at once).</summary>
        internal static Func<int> Flush = () => DwmFlush();

        // When the last beat was, in milliseconds.
        private static double _lastBeat;

        private static double Now()
        {
            return Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;
        }

        // Waits for the display. DwmFlush may return at once when a picture has just gone out; the next
        // call then waits for the following one, so it is called again until a beat has really passed. Where
        // it does not work (no desktop composition) or never waits, the wait is a short sleep: a long one
        // would make the text miss beats, because a sleep ends only when the system clock ticks (about every
        // 16 milliseconds).
        private static void WaitForNextPicture()
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                int result = 1;

                try
                {
                    result = Flush();
                }
                catch (Exception)
                {
                    // no dwmapi: the sleep below
                }

                if (result != 0)
                {
                    Thread.Sleep(1);
                    _lastBeat = Now();

                    return;
                }

                if (Now() - _lastBeat >= 3)
                {
                    _lastBeat = Now();

                    return;
                }
            }

            Thread.Sleep(2);
            _lastBeat = Now();
        }
    }
}
