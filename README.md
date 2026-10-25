<img src="https://raw.githubusercontent.com/fgilde/WinAnonymizer/main/icon.jpg" width="128" alt="WinAnonymizer" />

# WinAnonymizer

Small helpers to partially anonymize WPF applications.

## Packages

| Package | What | Antivirus |
|---|---|---|
| **WinAnonymizer** | Contracts (`IAnonymizer`), `Anonymizer.Current/Set`, `Window` extensions, inert `NoOpAnonymizer`. Managed-only. | Safe — no native code. |
| **WinAnonymizer.Native** | `DefaultAnonymizer`: window title, PEB image-path and parent-process spoofing via P/Invoke. | May be flagged. Opt-in. |

Most consumers install only **WinAnonymizer** and keep the inert default, or set their
own `IAnonymizer`. The native techniques live in a separate, opt-in package so nothing
breaks for users who just want the contracts.

```csharp
using WinAnonymizer;

window.SpoofIdentity();                       // inert by default — safe no-op
Anonymizer.Set(new DefaultAnonymizer());      // opt in to native behaviour
window.SpoofIdentity();                        // now spoofs the native window title
```

```
dotnet add package WinAnonymizer
dotnet add package WinAnonymizer.Native   # only if you want the native implementation
```

## Antivirus

`WinAnonymizer.Native` spoofs process/window identity through P/Invoke. Antivirus
heuristics (Windows Defender included) can flag or quarantine that DLL. The managed
**WinAnonymizer** package is unaffected. The assemblies are strong-name signed for a
stable identity; strong-naming is **not** an antivirus measure — reducing AV flags needs
an Authenticode code-signing certificate, which is not included here.

## Release

Push a tag `vX.Y.Z` → the GitHub Action packs both packages and publishes to NuGet via
trusted publishing.

```
git tag v0.1.0
git push origin v0.1.0
```
