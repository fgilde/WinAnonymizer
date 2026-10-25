using System;
using System.Collections.Generic;
using System.Windows;

namespace WinAnonymizer;

/// <summary>
///     Window / process identity helpers plus human-timing utilities.
///     Swap the active implementation via <see cref="Anonymizer.Set"/>.
/// </summary>
public interface IAnonymizer
{
    /// <summary>Randomizes the native window title only; keeps shell visibility.</summary>
    void SpoofWindowIdentity(Window window);

    /// <summary>Stronger hide (taskbar + Alt-Tab).</summary>
    void HideFromShell(Window window);

    /// <summary>Overwrites this process's PEB ImagePath so tools see a spoofed path.</summary>
    void SpoofPebImagePath();

    /// <summary>Log-normal human reaction-time sample.</summary>
    TimeSpan SampleReactionTime();

    /// <summary>Gaussian timing jitter in milliseconds.</summary>
    int SampleTimingJitterMs();

    /// <summary>Jitter factor for network request intervals.</summary>
    double SampleNetworkIntervalFactor();

    /// <summary>Subdivides a raw delta into Bézier sub-steps with wobble.</summary>
    IReadOnlyList<(int dx, int dy)> HumanizeMouseDelta(int dx, int dy);

    /// <summary>Random plausible User-Agent string.</summary>
    string RandomUserAgent();

    /// <summary>Launches a process with explorer.exe as spoofed parent.</summary>
    bool LaunchWithSpoofedParent(string exePath, string? workingDir = null);
}
