using System.Reflection;

// ---------------------------------------------------------------------------
// Single source of truth for the Codinex version number.
//
// Bump the three values below for every release, and keep them in sync with
// <Identity Version="..."> in src/Codinex.VSIX/source.extension.vsixmanifest.
//
//   AssemblyVersion               binding identity; keep the MAJOR.MINOR.0.0 form
//   AssemblyFileVersion           file version reported by Windows
//   AssemblyInformationalVersion  SemVer string; may carry a -beta.N suffix
// ---------------------------------------------------------------------------

[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]
[assembly: AssemblyInformationalVersion("0.1.0")]
