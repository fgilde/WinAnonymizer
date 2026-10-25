using System;

namespace WinAnonymizer;

/// <summary>
///     Ambient access point for the active <see cref="IAnonymizer"/>.
///     Defaults to the inert <see cref="NoOpAnonymizer"/>; install <c>WinAnonymizer.Native</c>
///     and call <see cref="Set"/> with <c>DefaultAnonymizer</c> to activate real behaviour.
/// </summary>
public static class Anonymizer
{
    private static IAnonymizer _current = new NoOpAnonymizer();

    /// <summary>The active implementation. Never null.</summary>
    public static IAnonymizer Current => _current;

    /// <summary>Replaces the active implementation and returns it.</summary>
    public static TAnonymizer Set<TAnonymizer>(TAnonymizer anonymizer) where TAnonymizer : IAnonymizer
    {
        _current = anonymizer;
        return anonymizer;
    }
}
