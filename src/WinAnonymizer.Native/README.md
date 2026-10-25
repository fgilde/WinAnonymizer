# WinAnonymizer.Native

Native `IAnonymizer` implementation for [WinAnonymizer](https://www.nuget.org/packages/WinAnonymizer).

> ⚠️ **Antivirus heads-up.** This assembly uses P/Invoke to spoof the native window
> title, overwrite the PEB image path and spoof the parent process. Those are
> legitimate techniques here, but antivirus heuristics (including Windows Defender)
> may flag or quarantine the DLL. If you don't need the native behaviour, depend on
> the managed-only **WinAnonymizer** package instead and keep the inert default.

```csharp
using WinAnonymizer;

Anonymizer.Set(new DefaultAnonymizer());
window.SpoofIdentity();
```

Members are `virtual` — override selectively by subclassing `DefaultAnonymizer`.
