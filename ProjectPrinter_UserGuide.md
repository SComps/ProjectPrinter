# ProjectPrinter Suite — User Guide

**Version:** github.2026.02.08.UD  
**Platform:** Windows, Linux, macOS (Raspberry Pi supported)  
**Runtime:** .NET 9.0

---

## Table of Contents

1. [Overview](#1-overview)
2. [Components at a Glance](#2-components-at-a-glance)
3. [Prerequisites & Installation](#3-prerequisites--installation)
4. [The Configuration File (`devices.dat`)](#4-the-configuration-file-devicesdat)
5. [device_config — Console Configuration Utility](#5-device_config--console-configuration-utility)
6. [Device_Config3270 — TN3270 Configuration Server](#6-device_config3270--tn3270-configuration-server)
7. [ProjectPrinter — The Print Engine](#7-projectprinter--the-print-engine)
8. [Connecting Your Emulator](#8-connecting-your-emulator)
9. [Output Files](#9-output-files)
10. [Running as a Background Service](#10-running-as-a-background-service)
11. [Testing the Installation](#11-testing-the-installation)
12. [Troubleshooting](#12-troubleshooting)
13. [Supported Operating Systems Reference](#13-supported-operating-systems-reference)
14. [Configuration Field Reference](#14-configuration-field-reference)

---

## 1. Overview

**ProjectPrinter** is a virtual line-printer emulator for mainframe and minicomputer enthusiasts. It receives raw print streams from emulators such as Hercules, SIMH, and DTCyber over a TCP/IP socket connection, processes the data (including carriage-control characters and EBCDIC-style overstrikes), and saves the results as:

- **Authentic Greenbar PDFs** — High-resolution files that recreate the look of classic fanfold greenbar paper, complete with tractor-feed holes, margin numbers, and alignment fiducials.
- **Plain-text files** — Standard ASCII text output of the job.

Output is automatically organized into subdirectories by user ID and tagged with the job name and job number extracted from the system's banner/header pages.

---

## 2. Components at a Glance

The solution contains four components:

| Component | Type | Purpose |
|-----------|------|---------|
| **ProjectPrinter** | Console service | Core engine. Listens for print jobs, processes data, writes PDFs and text. |
| **device_config** | Console TUI | Local utility for adding, editing, and deleting virtual printer configurations. |
| **Device_Config3270** | TN3270 server | Remote configuration utility — connect with any TN3270 terminal emulator. |
| **ProjectPrinterManager** | WinForms GUI | Windows desktop manager for viewing and editing device configurations. |

All components share a single configuration file: `devices.dat`.

---

## 3. Prerequisites & Installation

### Runtime Requirement
ProjectPrinter requires the **.NET 9.0 Runtime**. Download it from:
[https://dotnet.microsoft.com/en-us/download/dotnet/9.0](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

If you are building from source you will need the **.NET 9.0 SDK** instead.

### Building from Source

Open `ProjectPrinter.sln` in **Visual Studio 2022** or later, or use the .NET CLI:

```bash
# Publish the core printer service (self-contained AOT build)
dotnet publish ProjectPrinter/ProjectPrinter.vbproj --self-contained -c Release -o ./Publish/ProjectPrinter

# Publish the console config utility
dotnet publish device_config/device_config.vbproj --self-contained -c Release -o ./Publish/device_config

# Publish the TN3270 config server
dotnet publish Device_Config3270/Device_Config3270.vbproj --self-contained -c Release -o ./Publish/Device_Config3270
```

Pre-built binaries are available in the `BinPublish` directory for Windows and Linux.

> [!NOTE]
> The `Publish` and `BinPublish` directories contain self-contained executables — no separate .NET installation is required on the target machine when using these builds.

---

## 4. The Configuration File (`devices.dat`)

All components read and write a shared plain-text file named **`devices.dat`** (located in the same directory as the executable, unless overridden with the `config:` parameter).

Each line in the file represents one virtual printer device in pipe-delimited format:

```
DeviceName||Description||DeviceType||ConnType||Destination||OS||AutoConnect||OutputPDF||Orientation||OutputDir
```

**You should never need to edit this file by hand.** Use `device_config` or `Device_Config3270` instead.

> [!IMPORTANT]
> If `devices.dat` does not exist when ProjectPrinter starts, an empty file is created automatically. ProjectPrinter will then exit immediately with a message to run `device_config` first.

### Hot Reload

ProjectPrinter monitors `devices.dat` for changes every second. When the file is modified (by either config tool), it automatically:
1. Disconnects all currently connected devices.
2. Reloads the configuration.
3. Reconnects all devices.

There is a brief interruption during reload. This is normal.

---

## 5. device_config — Console Configuration Utility

`device_config` is a terminal-based (TUI) utility for managing your virtual printer devices. It requires a terminal window of at least **80 columns × 24 rows**.

### Starting device_config

```bash
# Use default config file (devices.dat in current directory)
./device_config

# Specify an alternate config file
./device_config /path/to/my_devices.dat
```

### Main Screen

```
================================================================================
 PROJECT PRINTER DEVICE CONFIGURATION [devices.dat]  3 devices.      Page: (1-3)
================================================================================
COMMAND ==>

      DEVICE          NAME                          OS  CONN  AUTO  PDF  DEST
01    MVS_PRT0        Job Output Printer             0     0   YES   YES
      127.0.0.1:1403
...
OS: (0) MVS38J (1) VMS  (2) MPE (3) RSTS/E (4) VM/370 (5) NOS 2.7.8
Paging: PgUp, PgDn or use Command UP and DOWN
Command: ADD, SAVE, EXIT or Item # to EDIT, or DELETE #
```

### Commands

| Command | Action |
|---------|--------|
| `ADD` | Adds a new blank device and opens the edit screen. |
| `SAVE` | Saves all changes to `devices.dat`. |
| `EXIT` | Exits (prompts if there are unsaved changes). |
| `1` (or any number) | Opens the edit screen for device number `1`. |
| `DELETE 2` | Deletes device number `2` (requires confirmation). |
| `UP` / `DOWN` or `PgUp` / `PgDn` | Scrolls through devices 4 at a time. |

> [!WARNING]
> Changes are **not** automatically saved. You must type `SAVE` before exiting, or your changes will be lost.

> [!NOTE]
> `Ctrl+C` is intentionally disabled in this tool. Use `EXIT` to quit.

### Edit Device Screen

When you type `ADD` or a device number, an edit screen is displayed with the following fields. Press **Enter** to accept each field and move to the next. Press **Escape** to cancel the current field and revert to its original value.

| Field | Description |
|-------|-------------|
| **DEVICE NAME** | Short identifier (up to 15 chars). Used in log messages and filenames. Example: `MVS_PRT0` |
| **DEVICE DESCRIPTION** | Friendly description (up to 30 chars). Example: `Job Output Printer` |
| **DEVICE TYPE** | `0` = Printer (standard). `1` = Reader (experimental). |
| **CONNECTION TYPE** | `0` = Socket (standard TCP connection). `1` = File. `2` = Physical. |
| **OPERATING SYSTEM** | See [OS Reference](#13-supported-operating-systems-reference). |
| **DEVICE DESTINATION** | IP address and port of the host emulator. Format: `ipaddress:port`. Example: `127.0.0.1:1403` |
| **AUTO CONNECT** | `True` / `False`. If True, ProjectPrinter connects to this device immediately on startup. |
| **OUTPUT PDF** | `True` / `False`. If True, generates a Greenbar PDF. A plain-text file is always generated. |
| **ORIENTATION** | See [Orientation Values](#orientation-values). |
| **OUTPUT DIR** | Directory where PDFs and text files will be saved. Can be absolute or relative. Example: `C:\PrintOutput` or `./output` |

After all fields are entered, type `Y` at the `Save? (Y/n)` prompt to apply your changes, or anything else to cancel.

---

## 6. Device_Config3270 — TN3270 Configuration Server

`Device_Config3270` launches a TN3270 server that lets you configure ProjectPrinter using any standard **TN3270 terminal emulator** (x3270, wc3270, Vista, etc.).

### Starting Device_Config3270

```bash
# Start on the default port (3270)
./Device_Config3270

# Start on a custom port
./Device_Config3270 -p 3275

# Specify an alternate config file
./Device_Config3270 -p 3270 /path/to/devices.dat
```

The server prints a confirmation message and waits. Press **Enter** in its console window to stop it.

### Connecting with a TN3270 Emulator

Configure your TN3270 emulator to connect to:
```
Host: localhost   (or the machine's IP if running remotely)
Port: 3270        (or the port you specified with -p)
```

### The Menu Screen

The interface presents a CICS-style transaction screen (Program: `PRTPRN01`, TransID: `CFG1`):

```
PROGRAM: PRTPRN01              PROJECT PRINTER CONFIGURATION   DATE: 03/22/26
TRANSID: CFG1                                                  TIME: 10:00:00
------------------------------------------------------------------------------
COMMAND ==> ________________________________________

ID   NAME            DESCRIPTION                    OS  CONN  AUTO  PDF
------------------------------------------------------------------------------
01   MVS_PRT0        Job Output Printer              0     0  YES   YES
     127.0.0.1:1403
...
------------------------------------------------------------------------------
ENTER:PROCESS   PF3:EXIT   PF7:UP   PF8:DOWN
OS:(0)MVS (1)VMS (2)MPE (3)RSTS (4)VM370 (5)NOS (6)VM/SP (7)TNDY (8)ZOS
```

### Commands

| Key / Command | Action |
|---------------|--------|
| Type `ADD` + **Enter** | Add a new device. |
| Type `SAVE` + **Enter** | Save all changes to `devices.dat`. |
| Type `EXIT` + **Enter** | Exit the session. If there are unsaved changes, they are **auto-saved** before disconnecting. |
| Type `1` (number) + **Enter** | Edit device number 1. |
| Type `DELETE 2` + **Enter** | Delete device 2 (a confirmation screen appears). |
| **PF7** | Page Up. |
| **PF8** | Page Down. |
| **PF3** | Exit / Cancel from any screen (auto-saves on exit). |

### Edit Screen

Tab between fields and type naturally. The interface accepts the same values as `device_config`. Press **Enter** to save the device, or **PF3** to cancel.

### Delete Confirmation Screen

When deleting a device, a confirmation screen is shown:
```
*** CONFIRM DELETION OF: MVS_PRT0        ***
TYPE 'Y' TO CONFIRM ==>
```
Type `Y` and press **Enter** to confirm, or press **PF3** to cancel.

> [!NOTE]
> Multiple TN3270 clients can connect simultaneously, but they all share the same device list. Be careful when multiple users configure devices at the same time.

---

## 7. ProjectPrinter — The Print Engine

`ProjectPrinter` is the core service that must be running to receive and process print jobs.

### Starting ProjectPrinter

```bash
./ProjectPrinter [options]
```

### Command-Line Options

| Option | Description |
|--------|-------------|
| *(no arguments)* | Starts with defaults: `config:devices.dat`, logging to `printers.log`. |
| `config:<filename>` | Load configuration from `<filename>` instead of `devices.dat`. |
| `logType:default` | Log all activity to the console (stdout) in real time with color coding. |
| `logType:none` | Disable all logging (silent operation). |
| `logType:<filename>` | Log to a specific file instead of `printers.log`. |
| `--imageproc` | Use the legacy image-based greenbar background processor (slower; use only if the default has issues). |
| `test` | Generate sample greenbar PDFs for visual verification and then exit. |
| `version` | Print the version string and exit. |
| `--daemon` *(Linux/macOS only)* | Detach from the terminal and run as a background process. On Windows, prints service installation instructions instead. |

### Examples

```bash
# Standard start (logging to printers.log)
./ProjectPrinter

# Start with console logging for debugging
./ProjectPrinter logType:default

# Use a custom config and log file
./ProjectPrinter config:my_printers.dat logType:my_printers.log

# Run silently
./ProjectPrinter logType:none

# Run as a daemon on Linux
./ProjectPrinter --daemon
```

### What Happens at Startup

1. If `devices.dat` does not exist, ProjectPrinter creates an empty one and exits.
2. All configured devices with `Auto Connect = True` are initialized.
3. ProjectPrinter attempts to connect to each device's destination.
4. The status timer starts. Every second, it:
   - Checks if any connected device has dropped its connection and attempts reconnection after 15 checks (~15 seconds).
   - Checks if `devices.dat` has been modified and hot-reloads if so.

### Log Output Colors (when `logType:default`)

| Color | Meaning |
|-------|---------|
| Yellow | Connecting / receiving data |
| Green | Connected / job processed successfully |
| Cyan | Informational (lines received, directories created) |
| Red | Errors, disconnections |
| White | General status |

### Reconnection Behavior

If a device loses its connection (e.g., the emulator restarts), ProjectPrinter will attempt to reconnect approximately every 15 seconds. You do not need to restart ProjectPrinter.

### Graceful Shutdown

- **Windows:** Press `Ctrl+C`.
- **Linux/macOS:** Send `SIGTERM` or `SIGINT`. Send `SIGHUP` to trigger an immediate configuration reload without restarting.
- **Windows Service:** Use `sc stop ProjectPrinter` or the Services control panel.

---

## 8. Connecting Your Emulator

See **ProjectPrinter_EmulatorGuide.md** for full step-by-step instructions for each emulator and OS combination.

### Hercules (IBM MVS 3.8J, VM/370, z/OS)

In your `hercules.cnf`, define a socket device pointing to ProjectPrinter's listening port:

```
# Device  Type  Address
000E      1403  127.0.0.1:1403 sockdev
```

The port number (`1403` in this example) must match the **Destination** port configured for the device in ProjectPrinter.

**MVS JCL/PROC to spool to the printer requires no changes** — the printer device simply needs to be online.

### SIMH (VAX/PDP-11 — VMS, RSTS/E)

SIMH uses a DZ11 serial multiplexer exposed as a Telnet port. In your SIMH `.ini` file:

```ini
set dz enable
set dz lines=8
att dz 1403
```

Configure ProjectPrinter with the OS set to `1` (VMS) or `3` (RSTS/E) as appropriate.

### SIMH (HP 3000 — MPE)

```ini
att lp 1403
```

Set the OS to `2` (MPE) in ProjectPrinter.

### DTCyber (CDC NOS 2.7.8)

Configure the NOS printer port in DTCyber's configuration to connect to ProjectPrinter's socket. Set the OS to `5` (NOS 2.7.8) in ProjectPrinter.

---

## 9. Output Files

### Directory Structure

ProjectPrinter organizes output files automatically:

```
OutputDir/
└── <UserID>/
    ├── <DevName>-<UserID>-<JobID>-<JobName>_<JobNum>.pdf
    └── <DevName>-<UserID>-<JobID>-<JobName>_<JobNum>.txt
```

For example:
```
C:\PrintOutput\
└── SYSPROG\
    ├── MVS_PRT0-SYSPROG-JOB00123-COMPILE_1.pdf
    └── MVS_PRT0-SYSPROG-JOB00123-COMPILE_1.txt
```

The **User ID**, **Job ID**, and **Job Name** are automatically extracted from the system's header/banner pages.

> [!NOTE]
> If a job is too small (fewer than 10 lines), it is treated as garbage or a stray banner and is discarded. No output file is created.

### PDF Output — Greenbar Format

Generated PDFs accurately replicate classic fanfold greenbar paper:

- **Page size:** 14.875" × 11" (standard wide-carriage format)
- **Orientation:** Landscape (default) or Portrait
- **Font:** Chainprinter (monospace, included)
- **Width:** 132 characters per line (landscape) or 80 characters (portrait)
- **Lines per page:** 66 lines at 6 LPI (lines per inch)
- **Background features:**
  - Alternating pale-green and white horizontal bands (0.5" each)
  - Tractor-feed holes on both edges (0.5" spacing)
  - Margin numbers 1–60 in the gutter area
  - Alignment fiducials (crosshair + circle + diamond) at the top

### Overstrike Support

For operating systems that use carriage return (`CR`) overstrikes (e.g., MVS 3.8J, VM/370, MPE), ProjectPrinter correctly renders overprinted lines by drawing the overlapping text segments at the same vertical position — replicating the effect of a physical printer head returning to the beginning of the line.

### Orientation Values

| Value | Description |
|-------|-------------|
| `0` | **Landscape** with full greenbar background (default, recommended) |
| `1` | **Portrait** with greenbar background (80 columns) |
| `2` | **Landscape** — no background (plain white paper look) |
| `3` | **Portrait** — no background |

---

## 10. Running as a Background Service

### Linux (systemd)

Create a service unit file at `/etc/systemd/system/projectprinter.service`:

```ini
[Unit]
Description=ProjectPrinter Virtual Line Printer
After=network.target

[Service]
Type=simple
User=youruser
WorkingDirectory=/opt/projectprinter
ExecStart=/opt/projectprinter/ProjectPrinter logType:/var/log/projectprinter.log
Restart=on-failure
RestartSec=5

[Install]
WantedBy=multi-user.target
```

Then enable and start it:

```bash
sudo systemctl daemon-reload
sudo systemctl enable projectprinter
sudo systemctl start projectprinter
sudo systemctl status projectprinter
```

To force a configuration reload without restarting:
```bash
kill -HUP $(cat /var/run/projectprinter.pid)
```

### Linux (daemon mode)

Alternatively, use the built-in daemon flag:

```bash
./ProjectPrinter --daemon
# Output: ProjectPrinter daemon started. PID: 12345
```

The PID is written to `/var/run/projectprinter.pid` (or the application directory if `/var/run` is not writable).

### Windows Service

Use the built-in `sc` command:

```powershell
sc create ProjectPrinter binPath= "C:\path\to\ProjectPrinter.exe config:devices.dat"
sc start ProjectPrinter
sc stop ProjectPrinter
```

Or use **NSSM** (Non-Sucking Service Manager) for easier management:
[https://nssm.cc](https://nssm.cc)

---

## 11. Testing the Installation

Run a self-test to verify that PDF generation is working correctly:

```bash
./ProjectPrinter test
```

This generates a file named `test_greenbar_output.pdf` in the current directory. Open it and verify:
- Alternating green/white horizontal bands are visible
- Tractor-feed holes appear on both left and right edges
- Diamond/crosshair alignment marks appear at the top
- Text lines are evenly spaced with a monospace font
- 132 characters fit the full width of the printable area

---

## 12. Troubleshooting

### ProjectPrinter exits immediately on startup

**Cause:** `devices.dat` is missing or contains no valid devices.  
**Fix:** Run `device_config` or `Device_Config3270` to configure at least one device, then restart ProjectPrinter.

### Device shows as disconnected / never connects

**Cause:** The emulator (Hercules, SIMH, etc.) is not running, or the destination IP/port is incorrect.  
**Fix:**
1. Verify the emulator is started and the printer device is online.
2. Confirm the `Destination` in the device config matches the emulator's socket address exactly.
3. Check for firewall rules blocking the port.
4. ProjectPrinter retries automatically every ~15 seconds — give it time.

### Output files are not being created

**Cause:** The job may be too small (fewer than 10 lines) and was discarded as garbage.  
**Fix:** Check `printers.log` for a message like:
```
[MVS_PRT0] Ignoring document with 5 lines as line garbage or banners.
```
If this occurs for real jobs, ensure the correct OS type is selected so carriage controls are parsed correctly.

### PDF file has no greenbar background (appears as plain white)

**Cause:** The Orientation is set to `2` or `3` (no background modes).  
**Fix:** Set Orientation to `0` (landscape with background) or `1` (portrait with background).

### PDF background doesn't look right (using `--imageproc`)

**Cause:** The `--imageproc` flag enables the legacy image-based background. `greenbar.jpg` must be present in the same directory as the executable.  
**Fix:** Remove `--imageproc` to use the default programmatic background renderer, which requires no image files.

### Characters appear garbled in the PDF

**Cause:** Non-printable or high-byte characters in the stream that are not being filtered.  
**Fix:** Ensure the correct OS type is set. Some OS types (like RSTS/E) use different character encoding and have special handling. If the issue persists, use `logType:default` to inspect what data is being received.

### device_config won't start — "Terminal too small"

**Fix:** Resize your terminal to at least 80 columns × 24 rows.

### Device_Config3270 — TN3270 emulator connects but screen is blank

**Cause:** Some TN3270 emulators require specific negotiation settings. Ensure your emulator is configured as a **3270 Model 2** (24×80) terminal.

---

## 13. Supported Operating Systems Reference

| OS Code | ID | Banner Header Format |
|---------|----|---------------------|
| **MVS 3.8J** / OS/VS2 | `0` | Lines beginning with `**** END JOB` or `**** END TSU` contain job name, job ID, and user ID. |
| **VMS** (VAX/ALPHA) | `1` | Lines beginning with `JOB` contain job name, job number, and user ID. |
| **MPE** (HP 3000) | `2` | The first non-blank line of the header page contains job name, number, and account. |
| **RSTS/E** (PDP-11) | `3` | Lines containing `ENTRY` at position 4 provide job queue and account information. Low-speed device — expect slow receipt. |
| **VM/370** (Community Ed.) | `4` | `LOCATION USERID` and `SPOOL FILE NAME` lines provide user and job info. Uses `CR`-based overstrikes. |
| **NOS 2.7.8** (CDC) | `5` | Lines beginning with `UJN` and `CREATING` provide job and user info. |
| **VM/SP** (incl. HPO) | `6` | `USERID ORIGIN`, `FILENAME FILETYPE`, and `SPOOLID` lines are used. |
| **Tandy XENIX** | `7` | No automatic job info extraction; job name is set to `XENIX`. Uses CR+LF line endings. |
| **IBM z/OS** | `8` | Lines beginning with `*` followed by `JOBID:`, `JOB NAME:`, and `USER ID:` keywords. Similar layout to MVS 3.8J with 5-line initial margin. |

---

## 14. Configuration Field Reference

### Quick Reference Table

| Field | Valid Values | Default | Notes |
|-------|-------------|---------|-------|
| Device Name | Any string, max 15 chars | — | Used in filenames; keep it short and alphanumeric. |
| Description | Any string, max 30 chars | — | Displayed in listings only. |
| Device Type | `0` or `1` | `0` | `0`=Printer. `1`=Reader (experimental). |
| Connection Type | `0`, `1`, or `2` | `0` | `0`=Socket (TCP). `1`=File. `2`=Physical. |
| Destination | `host:port` | — | Example: `127.0.0.1:1403`. Must match your emulator's print device socket. |
| OS | `0`–`8` | `0` | See [OS Reference](#13-supported-operating-systems-reference). Critical for correct output. |
| Auto Connect | `True` or `False` | `False` | Set to `True` for all devices you want active on startup. |
| Output PDF | `True` or `False` | `False` | Set to `True` to generate greenbar PDFs. Text output is always generated. |
| Orientation | `0`, `1`, `2`, or `3` | `0` | `0`=Landscape+BG, `1`=Portrait+BG, `2`=Landscape plain, `3`=Portrait plain. |
| Output Dir | Any valid path | — | Absolute or relative. No trailing slash required. Created automatically if it does not exist. |

---

*ProjectPrinter is open source. No warranty, express or implied. Thanks to the Mainframe Enthusiasts Discord, @racingmars for virtual1403 and the greenbar image, and @Rudi for everything.*
