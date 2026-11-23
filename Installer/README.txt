DoThingsBot 2.0.0.17 character recovery build

Run DoThingsBot-2.0.0.17-Setup.exe on the computer running the bot.
Close AC first. Setup suggests the existing folder using Decal registration.
Confirm the folder and complete installation, then relaunch AC through Decal.
Requires Decal, Virindi Views and .NET Framework 4.8.

Setup supports new installations, updates and repairs. It checks Decal's
32-bit registry entries, the old installer metadata, the Decal Agent location
and nearby registered plugin folders to find DoThingsBot. You can choose a
folder when its existing location cannot be identified automatically.
It installs missing files and corrects the selected plugin's path, assembly,
class, surrogate and Enabled values. An active existing Helix loader is
retained. Existing registration IDs are reused when repairing a setup.

Program files and registration snapshots are backed up under the program
folder's Backups directory. Existing resource XML files are preserved.
Each completed run writes install-receipt.json. The setup log is stored
under the Windows temporary directory as DoThingsBot-Setup.log.

This build recovers statistics and settings from usable saved copies and
preserves damaged originals when replacing them. A resumed buff job with a
null spell list proceeds through normal job cleanup.

The character debug log shows
LoginComplete DoThingsBot 2.0.0.17-character-recovery.1
when this build starts. With no recoverable configuration, review the
default settings and use /dtb start when ready.

Source.zip contains the corresponding modified Helix source and installer
scripts. This is a local build based on HelixNightbane 2.0.0.16.

Building setup

Run Installer\Build.ps1 with these inputs.
- ReferenceDirectory contains the Decal managed interop and Virindi Views DLLs.
- FrameworkReferences contains .NET Framework 4.8 reference assemblies.
- JsonAssembly points to a .NET Framework compatible Newtonsoft.Json 13 DLL.
- Compiler points to the Roslyn csc.dll run through dotnet.
- MakeNsis points to the NSIS 3 compiler executable.
- OutputDirectory receives the payload and setup executable.

Run Installer\Tests\Test-Installer.ps1 with Payload and MakeNsis pointing
to the generated payload directory and NSIS compiler. Tests use isolated
temporary folders and a unique CurrentUser registry subtree. Results are
written to the test directory. Production setup uses LocalMachine registry
entries in the 32-bit registry view.
