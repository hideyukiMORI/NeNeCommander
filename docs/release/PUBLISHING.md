# Publish the Windows x64 beta

Issue #434 introduces one local packaging entry point:

```powershell
pwsh -NoProfile -File ./eng/publish-release.ps1 -OutputDirectory <absolute-new-output-directory>
```

Use a clean, committed checkout and the SDK pinned in `global.json`. The output
must be outside the checkout and must not already exist. The script never removes
or overwrites an earlier candidate. Task output and worktrees follow the project's
drive/location policy. The publishing command starts no application or CI, creates
no tag or GitHub release, and changes no display or security settings.

`Version` in `src/NeNeCommander.App/NeNeCommander.App.csproj` is the sole product
version. The .NET SDK derives `AssemblyVersion` and `FileVersion` from its numeric
part and adds the source commit to `ProductVersion`. The existing `app.manifest`
assembly identity is a stable Win32 side-by-side identity, separate from product
version metadata; publishing records both rather than adding a manifest generator.
The App project's copyright is retained in executable metadata and `RELEASE.txt`.
Internal library versions are not independently released.

The entry point performs one locked restore and one official `dotnet publish` of
the existing App project. Both evaluate `Release`, `win-x64`, `SelfContained=true`
and `ContinuousIntegrationBuild=true`. The existing project already sets
`WindowsAppSDKSelfContained=true`, unpackaged output, untrimmed publishing and
Release ReadyToRun. No installer, MSIX, single-file extraction, new package,
certificate or signature is added. Publish uses at most four MSBuild workers.

The package contains the full publish folder, the distribution README, build
identity, and original license/notice files from resolved NuGet packages and
framework packs. `eng/release-notices.ps1` is called only by this publishing entry;
it also retains package manifests and license URLs when a package has no local
license text. `docs/release/licenses/windows-sdk-license.json` fixes the Windows
SDK document's official resolved URL, length and expected SHA-256. Publishing
downloads that one original and refuses unavailable or changed content. An explicit
`-WindowsSdkLicensePath <cached-original-rtf>` avoids that download only when its
hash matches; a mismatched explicit cache fails instead of silently falling back.
The original stays outside the repository. No license is invented for the app.
NuGet restore and the uncached license retrieval require network access. Preserve
the publisher-provided .NET/Windows notices and recheck terms when inputs change.

Outputs are the named application folder, its ZIP, `publish.log`,
`release-manifest.json` and `SHA256SUMS.txt`. Manifest schema version 1 records:

- product/source/platform/toolchain identity and exact argument arrays;
- every tracked source input's SHA-256 and its unchanged comparison;
- copied license metadata, every payload file's size/SHA-256, and the ZIP's hash;
- the unsigned application status and explicit unperformed launch/release checks.

The checksum file covers the ZIP and manifest; the manifest does not hash itself.
The ZIP is a folder distribution and all its files must remain together. A failure
leaves its output/log for diagnosis and does not produce a success claim. Record
any candidate retry separately; do not remove evidence to hide the failed attempt.

Packaging is not release approval. Before creating the tag and GitHub prerelease,
the release owner verifies the final source and assets, the extracted application,
applicable deep-review evidence within 96 hours (QLT-013/SEC-009), successful CodeQL
analysis plus zero open owned alerts (SEC-008/ADR-0031), dependency audit and the
one-time hide-owned screen check in #94/ADR-0054. Record every unrun environmental
tier honestly. Reuse unchanged applicable evidence under ADR-0061 and do not run
a second full quality workflow when deep review already includes it.

The published ZIP must be downloaded again and checked against its recorded hash.
The shipping candidate is generated from the accepted source, never copied from
the ordinary development application's existing `bin` directory.

Official deployment references:

- [Unpackaged WinUI distribution](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/unpackage-winui-app)
- [Windows App SDK self-contained deployment](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps)
- [.NET runtime-specific publishing defaults](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/8.0/runtimespecific-app-default)
