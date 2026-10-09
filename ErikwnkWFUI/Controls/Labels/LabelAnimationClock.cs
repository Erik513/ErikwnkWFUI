using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using static ErikwnkWFUI.Native.NativeMethods;

namespace ErikwnkWFUI.Controls
{
    /// <summary>
    /// The beat for the moving text of all labels: one thread that wakes with the display (about 60
    /// times a second) and asks every label that moves for its next picture. A Windows Forms timer
    /// cannot do that: it ticks about every 31 milliseconds, late and unevenly, behind everything else
    /// in the message queue, and the text then moves in jerks. The thread runs only while a label moves.
    /// </summary>
    internal static class LabelAnimationClock
    {
        private static readonly object Gate = new object();
        private static readonly List<ThemedLabel> Labels = new List<ThemedLabel>();
        private static Thread _thread;

        // The labels that move now.
        internal static int Count
        {
            get
            {
                lock (Gate)
                    return Labels.Count;
            }
        }

        internal static void Add(ThemedLabel label)
        {
            lock (Gate)
            {
                if (!Labels.Contains(label))
                    Labels.Add(label);

                if (_thread == null)
                {
                    // It only waits for the display and posts a message: it must not be late.
                    _thread = new Thread(Run) { IsBackground = true, Name = "Label animation", Priority = ThreadPriority.Highest };
                    _thread.Start();
                }
            }
        }

        internal static void Remove(ThemedLabel label)
        {
            lock (Gate)
                Labels.Remove(label);
        }

        private static void Run()
        {
            while (true)
            {
                ThemedLabel[] labels;

                lock (Gate)
                {
                    if (Labels.Count == 0)
                    {
                        _thread = null;

                        return;
                    }

                    labels = Labels.ToArray();
                }

                WaitForNextPicture();

                foreach (ThemedLabel label in labels)
                    label.RequestFrame();
            }
        }

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
                    result = DwmFlush();
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
