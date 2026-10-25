using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using static WinAnonymizer.NativeMethods;

namespace WinAnonymizer;

/// <summary>Default <see cref="IAnonymizer"/> implementation. Members are virtual for selective overrides.</summary>
public class DefaultAnonymizer : IAnonymizer
{
    // ─── Window / Process Identity ─────────────────────────────────────────────

    private static readonly string[] _windowTitlePool =
    [
        "Settings",
        "Microsoft Edge",
        "System",
        "Host Process for Windows Services",
        "Runtime Broker",
        "Windows Security",
        "Device Manager",
        "Windows Update",
        "Microsoft Store",
        "Task Manager",
    ];

    /// <inheritdoc />
    public virtual void SpoofWindowIdentity(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            // Title only — keep shell visibility so the user can still find the window.
            SetWindowText(hwnd, PickRandom(_windowTitlePool));

            // Defensive against prior style corruption: make sure it is actually shown.
            if (window.Visibility != Visibility.Visible)
                window.Visibility = Visibility.Visible;
            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            window.ShowInTaskbar = true;
            ShowWindow(hwnd, SW_SHOW);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Anon] SpoofWindowIdentity: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public virtual void HideFromShell(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;
            long style = GetWindowLongPtr(hwnd, GWL_EXSTYLE).ToInt64();
            style &= ~WS_EX_APPWINDOW;
            style |= WS_EX_TOOLWINDOW;
            SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(style));
            window.ShowInTaskbar = false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Anon] HideFromShell: {ex.Message}");
        }
    }

    // ─── PEB ImagePath Spoof ────────────────────────────────────────────────────

    private static readonly string[] _fakeExePaths =
    [
        @"C:\Windows\System32\svchost.exe",
        @"C:\Windows\System32\RuntimeBroker.exe",
        @"C:\Windows\System32\taskhostw.exe",
        @"C:\Program Files\WindowsApps\Microsoft.WindowsStore_0.0.0.0__8wekyb3d8bbwe\WinStore.App.exe",
        @"C:\Windows\System32\SearchHost.exe",
    ];

    /// <inheritdoc />
    public virtual void SpoofPebImagePath()
    {
        try
        {
            SpoofPebImagePathCore(PickRandom(_fakeExePaths));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Anon] SpoofPebImagePath: {ex.Message}");
        }
    }

    private static void SpoofPebImagePathCore(string fakePath)
    {
        var hProcess = Process.GetCurrentProcess().Handle;

        // Get PEB base address.
        NtQueryInformationProcess(hProcess, 0,
            out var pbi, Marshal.SizeOf<PROCESS_BASIC_INFORMATION>(), out _);

        IntPtr pebBase = pbi.PebBaseAddress;
        if (pebBase == IntPtr.Zero) return;

        // Read ProcessParameters pointer from PEB (offset 0x20 on x64).
        var ptrBuf = new byte[8];
        ReadProcessMemory(hProcess, pebBase + 0x20, ptrBuf, 8, out _);
        IntPtr procParams = (IntPtr)BitConverter.ToInt64(ptrBuf, 0);
        if (procParams == IntPtr.Zero) return;

        // Read ImagePathName UNICODE_STRING at ProcessParameters+0x60 (x64).
        var usBuf = new byte[16]; // sizeof UNICODE_STRING = 16 on x64 (2+2+4pad+8)
        ReadProcessMemory(hProcess, procParams + 0x60, usBuf, 16, out _);
        IntPtr strBuffer = (IntPtr)BitConverter.ToInt64(usBuf, 8);
        ushort maxLen = BitConverter.ToUInt16(usBuf, 2);

        // Write the fake path into the existing buffer (capped to MaximumLength).
        var fakeBytes = Encoding.Unicode.GetBytes(fakePath);
        int writeLen = Math.Min(fakeBytes.Length, maxLen - 2); // -2 for null terminator
        var padded = new byte[writeLen + 2];
        Array.Copy(fakeBytes, padded, writeLen);

        WriteProcessMemory(hProcess, strBuffer, padded, padded.Length, out _);

        // Update the Length field to match.
        var newLen = BitConverter.GetBytes((ushort)writeLen);
        WriteProcessMemory(hProcess, procParams + 0x60, newLen, 2, out _);
    }

    // ─── Human Timing Distributions ────────────────────────────────────────────

    /// <inheritdoc />
    public virtual TimeSpan SampleReactionTime()
    {
        const double mu = 5.393; // ln(220)
        const double sigma = 0.22;
        double z = BoxMullerZ();
        double ms = Math.Exp(mu + sigma * z);
        return TimeSpan.FromMilliseconds(Math.Clamp(ms, 60, 600));
    }

    /// <inheritdoc />
    public virtual int SampleTimingJitterMs()
    {
        double z = BoxMullerZ();
        return (int)Math.Clamp(z * 4, -12, 12);
    }

    /// <inheritdoc />
    public virtual double SampleNetworkIntervalFactor()
        => 0.78 + _rng.NextDouble() * 0.44;

    private static double BoxMullerZ()
    {
        double u1 = 1.0 - _rng.NextDouble();
        double u2 = 1.0 - _rng.NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    // ─── Mouse Path Humanization ────────────────────────────────────────────────

    /// <inheritdoc />
    public virtual IReadOnlyList<(int dx, int dy)> HumanizeMouseDelta(int dx, int dy)
    {
        int steps = _rng.Next(3, 7);
        double wobX = (_rng.NextDouble() - 0.5) * 10;
        double wobY = (_rng.NextDouble() - 0.5) * 10;

        // Control point halfway + perpendicular wobble.
        double cx = dx * 0.5 + wobX;
        double cy = dy * 0.5 + wobY;

        var result = new List<(int, int)>(steps);
        double prevX = 0, prevY = 0;

        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            double u = 1 - t;
            // Quadratic Bézier: P = (1-t)^2·P0 + 2(1-t)t·P1 + t^2·P2
            double bx = 2 * u * t * cx + t * t * dx;
            double by = 2 * u * t * cy + t * t * dy;

            result.Add(((int)Math.Round(bx - prevX), (int)Math.Round(by - prevY)));
            prevX = bx;
            prevY = by;
        }

        return result;
    }

    // ─── Network Header Anonymization ──────────────────────────────────────────

    private static readonly string[] _userAgentPool =
    [
        "Microsoft-CryptoAPI/10.0",
        "Windows-Update-Agent/10.0.19041.1",
        "WinHTTP/1.0",
        "Microsoft Office/16.0 (Windows NT 10.0; Microsoft Outlook 16.0.15000; Pro)",
        "Dalvik/2.1.0 (Linux; U; Android 11; SM-G998U Build/RP1A.200720.012)",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
        "Microsoft-Symbol-Server/6.13.0009.1140",
    ];

    /// <inheritdoc />
    public virtual string RandomUserAgent() => PickRandom(_userAgentPool);

    // ─── Parent Process Spoofing (Launcher) ────────────────────────────────────

    /// <inheritdoc />
    public virtual bool LaunchWithSpoofedParent(string exePath, string? workingDir = null)
    {
        int explorerPid = 0;
        try
        {
            explorerPid = Process.GetProcessesByName("explorer").FirstOrDefault()?.Id ?? 0;
        }
        catch { /* fall through — will use direct launch */ }

        if (explorerPid == 0)
            return DirectLaunch(exePath, workingDir);

        IntPtr explorerHandle = OpenProcess(PROCESS_CREATE_PROCESS, false, explorerPid);
        if (explorerHandle == IntPtr.Zero)
        {
            Debug.WriteLine("[Anon] OpenProcess explorer failed, falling back to direct launch.");
            return DirectLaunch(exePath, workingDir);
        }

        IntPtr attrList = IntPtr.Zero;
        try
        {
            IntPtr lpSize = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
            attrList = Marshal.AllocHGlobal(lpSize);
            if (!InitializeProcThreadAttributeList(attrList, 1, 0, ref lpSize))
                return DirectLaunch(exePath, workingDir);

            IntPtr parentHandle = explorerHandle;
            if (!UpdateProcThreadAttribute(attrList, 0,
                    (IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS,
                    ref parentHandle, (IntPtr)IntPtr.Size,
                    IntPtr.Zero, IntPtr.Zero))
                return DirectLaunch(exePath, workingDir);

            var si = new STARTUPINFOEX();
            si.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            si.lpAttributeList = attrList;

            bool ok = CreateProcess(
                null, $"\"{exePath}\"",
                IntPtr.Zero, IntPtr.Zero,
                false,
                EXTENDED_STARTUPINFO_PRESENT,
                IntPtr.Zero,
                workingDir ?? Path.GetDirectoryName(exePath),
                ref si, out var pi);

            if (ok)
            {
                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
            }
            return ok;
        }
        finally
        {
            if (attrList != IntPtr.Zero)
            {
                DeleteProcThreadAttributeList(attrList);
                Marshal.FreeHGlobal(attrList);
            }
            CloseHandle(explorerHandle);
        }
    }

    private static bool DirectLaunch(string exePath, string? workingDir)
    {
        try
        {
            Process.Start(new ProcessStartInfo(exePath)
            {
                UseShellExecute = true,
                WorkingDirectory = workingDir ?? Path.GetDirectoryName(exePath) ?? ""
            });
            return true;
        }
        catch { return false; }
    }

    // ─── Shared utilities ──────────────────────────────────────────────────────

    // Thread-local to avoid lock contention on the hot path.
    [ThreadStatic]
    private static Random? _rngLocal;
    private static Random _rng => _rngLocal ??= new Random(
        (int)(DateTime.UtcNow.Ticks ^ (long)RuntimeHelpers.GetHashCode(Thread.CurrentThread)));

    private static T PickRandom<T>(IReadOnlyList<T> pool)
        => pool[_rng.Next(pool.Count)];
}
