using System.Windows;

namespace WinAnonymizer;

/// <summary>
///     Fluent entry points on <see cref="Window"/>. Each call routes through the active
///     <see cref="Anonymizer.Current"/>, so swapping the implementation via
///     <see cref="Anonymizer.Set"/> changes the behaviour here too.
/// </summary>
public static class WindowExtensions
{
    /// <summary>Randomizes the native window title; keeps the window visible in the shell.</summary>
    public static Window SpoofIdentity(this Window window)
    {
        Anonymizer.Current.SpoofWindowIdentity(window);
        return window;
    }

    /// <summary>Removes the window from the taskbar and Alt-Tab.</summary>
    public static Window HideFromShell(this Window window)
    {
        Anonymizer.Current.HideFromShell(window);
        return window;
    }
}
