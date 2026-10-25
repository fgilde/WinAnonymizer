# WinAnonymizer

Contracts and WPF window extensions to **partially anonymize WPF applications**.

Managed-only, no native code — this package will not trip antivirus. The default
`Anonymizer.Current` is inert (`NoOpAnonymizer`). Install **WinAnonymizer.Native**
and activate it to get real behaviour.

```csharp
using WinAnonymizer;

// Inert by default — safe no-op:
window.SpoofIdentity();

// Activate the native implementation (requires the WinAnonymizer.Native package):
Anonymizer.Set(new DefaultAnonymizer());
window.SpoofIdentity();   // now spoofs the native window title
```

Provide your own implementation anytime:

```csharp
Anonymizer.Set(new MyAnonymizer());   // your IAnonymizer
```
