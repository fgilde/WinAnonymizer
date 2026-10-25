using System;
using System.Collections.Generic;
using System.Windows;

namespace WinAnonymizer;

/// <summary>
///     Inert default. Does nothing and returns neutral values, so code can depend on
///     <see cref="Anonymizer.Current"/> without pulling in native code. Install the
///     <c>WinAnonymizer.Native</c> package and call <see cref="Anonymizer.Set"/> with
///     <c>DefaultAnonymizer</c> to activate the real behaviour.
/// </summary>
public sealed class NoOpAnonymizer : IAnonymizer
{
    /// <inheritdoc />
    public void SpoofWindowIdentity(Window window) { }

    /// <inheritdoc />
    public void HideFromShell(Window window) { }

    /// <inheritdoc />
    public void SpoofPebImagePath() { }

    /// <inheritdoc />
    public TimeSpan SampleReactionTime() => TimeSpan.Zero;

    /// <inheritdoc />
    public int SampleTimingJitterMs() => 0;

    /// <inheritdoc />
    public double SampleNetworkIntervalFactor() => 1.0;

    /// <inheritdoc />
    public IReadOnlyList<(int dx, int dy)> HumanizeMouseDelta(int dx, int dy) => new[] { (dx, dy) };

    /// <inheritdoc />
    public string RandomUserAgent() => string.Empty;

    /// <inheritdoc />
    public bool LaunchWithSpoofedParent(string exePath, string? workingDir = null) => false;
}
