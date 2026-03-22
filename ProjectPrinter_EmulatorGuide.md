# ProjectPrinter — Emulator Connection Guide

This guide covers how to configure **Hercules** and **SIMH** to send print output to ProjectPrinter for each supported operating system.

---

## Architecture Overview

There are two fundamentally different ways emulators connect to ProjectPrinter, and it's important to understand which applies to your system:

| Emulator | Connection Method | How It Works |
|----------|-------------------|-------------|
| **Hercules** | Direct TCP socket (`sockdev`) | Hercules itself opens a TCP connection directly to the IP/port you specify. ProjectPrinter is the client that connects *to Hercules*. |
| **SIMH** | Serial-over-TCP (DZ11 multiplexer) | SIMH exposes serial lines as a Telnet port. ProjectPrinter connects to SIMH as if it were a serial terminal. |

> [!IMPORTANT]
> In the Hercules `sockdev` model, **Hercules is the server** — it listens on the port and ProjectPrinter connects to it. Set your **Destination** in ProjectPrinter to point at Hercules's listening address and port. The flow is:
> `MVS prints → Hercules listens on :1403 → ProjectPrinter connects and receives data`
>
> In the SIMH model, **SIMH is also the server** — it listens for incoming Telnet connections on a specified port. ProjectPrinter connects as a serial terminal. The flow is:
> `VMS/RSTS prints to TTA0 → SIMH listens on :1403 → ProjectPrinter connects and receives data`

---

## Part 1 — Hercules

Hercules supports a `sockdev` keyword on printer device definitions. This tells Hercules to listen for an external TCP connection on the specified address and port. When ProjectPrinter connects, Hercules sends the raw print stream to it.

### 1.1 Hercules — MVS 3.8J (TK4-, TK5, MVSCE)

**Step 1: Configure `hercules.cnf`**

Locate your Hercules configuration file (commonly `tk4-.cnf`, `tk5.cnf`, `mvsce.cnf`, or `hercules.cnf`) and find the line printer device entries. They will look something like this in a default TK4-/TK5 setup:

```
# Default TK4-/TK5 printer definitions (may already exist)
00E  1403   hercules.prt
00F  1403   hercules.prt
```

Replace them with `sockdev` definitions:

```
# ProjectPrinter socket device definitions
000E  1403  127.0.0.1:1403  sockdev
000F  1403  127.0.0.1:1404  sockdev
```

> [!NOTE]
> The port number `1403` is a convention (matching the device model number) but any unused port above 1024 works.  
> If you run multiple systems on the same machine, give each its own port (e.g., `1403`, `1404`).

> [!WARNING]
> Do **not** use the `CRLF` or `NOCLEAR` options with `sockdev` printers — they are not valid for socket devices and will cause an error.

**Step 2: Configure ProjectPrinter**

Add a device in `device_config` or `Device_Config3270`:

| Field | Value |
|-------|-------|
| Device Name | `MVS_PRT0` |
| Description | `MVS 3.8J Job Printer` |
| Device Type | `0` (Printer) |
| Connection Type | `0` (Socket) |
| Destination | `127.0.0.1:1403` |
| OS | `0` (MVS 3.8J) |
| Auto Connect | `True` |
| Output PDF | `True` |
| Orientation | `0` (Landscape) |
| Output Dir | `C:\PrintOutput` (or any path) |

**Step 3: Bring the printer online in MVS**

If using the Hercules console or SDSF/MCS, vary the printer online:
```
/V 00E,ONLINE
```

MVS JES2 will automatically route spool output to the online printer. Jobs with `CLASS=A` (or whatever your default output class is) will print automatically.

**Step 4: Verify**

Submit a simple JCL job and watch the ProjectPrinter log for messages like:
```
[MVS_PRT0] Attempting to connect...
[MVS_PRT0] Connection successful.
[MVS_PRT0] receiving data from remote host.
[MVS_PRT0] received 250 lines from remote host.
```

---

### 1.2 Hercules — VM/370 Community Edition (VM370CE)

VM/370 CE uses the same Hercules `sockdev` mechanism, but the VM/SP spooling and printer management work differently from MVS.

**`hercules.cnf` entry:**
```
00E  1403  127.0.0.1:1403  sockdev
```

**ProjectPrinter device settings:**

| Field | Value |
|-------|-------|
| Destination | `127.0.0.1:1403` |
| OS | `4` (VM/370) |

**Printing from VM/370:**

In CMS, spool output to the virtual printer and then close it:
```
CP SPOOL PRT TO OPERATOR
PRINT filename
CP CLOSE PRT
```

Or, to print to the system printer for all output:
```
CP SPOOL PRT CONT
```

> [!NOTE]
> VM/370 uses CR-based overstrike characters for underlining and bold effects. ProjectPrinter specifically handles these when OS is set to `4` (VM370) — the CR characters are preserved in the data stream and rendered as overprints in the PDF.

---

### 1.3 Hercules — VM/SP (including HPO)

The setup is identical to VM/370 CE. Use the same `sockdev` line in `hercules.cnf` and configure ProjectPrinter with OS type `6` (VM/SP).

The VM/SP banner pages have a different format from VM/370 CE, so job identification will only work correctly when OS `6` is selected.

**ProjectPrinter device settings:**

| Field | Value |
|-------|-------|
| Destination | `127.0.0.1:1403` |
| OS | `6` (VM/SP) |

---

### 1.4 Hercules — IBM z/OS

z/OS on Hercules uses the same `sockdev` mechanism.

**`hercules.cnf` entry:**
```
000E  1403  127.0.0.1:1403  sockdev
```

**ProjectPrinter device settings:**

| Field | Value |
|-------|-------|
| Destination | `127.0.0.1:1403` |
| OS | `8` (z/OS) |

z/OS banner pages use a different format than MVS 3.8J (they contain `JOBID:`, `JOB NAME:`, and `USER ID:` keywords on lines starting with `*`). Ensure OS is set to `8` for correct job identification.

---

### 1.5 Multiple Printers on one Hercules System

You can define as many sockdev printers as you need. Use different device addresses and ports:

```
# hercules.cnf — Multiple printer configuration
000E  1403  127.0.0.1:1403  sockdev   # General output / JES2 CLASS A
000F  1403  127.0.0.1:1404  sockdev   # TSO User output / JES2 CLASS B
009  3211  127.0.0.1:1405  sockdev   # 3211 fast printer (if applicable)
```

Then configure a matching device in ProjectPrinter for each, with the appropriate destination port.

---

## Part 2 — SIMH

SIMH (the open-source computer simulator) handles printing differently from Hercules. SIMH exposes its serial multiplexer lines as Telnet ports. In most configurations relevant to ProjectPrinter, the line printer in the guest OS is routed through a **serial line** (TTA0 on VAX, KB6 on RSTS/E, LP on HP3000), and SIMH makes that line available as a Telnet-accessible socket.

ProjectPrinter connects to that socket and receives the print stream as if it were a terminal connected via serial port.

> [!NOTE]
> **SIMH expects ProjectPrinter to connect first, then the guest OS sends data.** If ProjectPrinter is not running and connected when the guest attempts to print, the print job may hang or fail on the guest side. Always start ProjectPrinter before starting or submitting print jobs.

---

### 2.1 SIMH — VAX / OpenVMS

VMS routes print jobs through a dedicated print queue. The queue output device is typically a serial terminal defined on the DZ11 (or DHQ11) multiplexer. In SIMH, `TTA0` is usually the first non-console DZ11 line.

**Step 1: SIMH `.ini` File**

Add/modify the following in your VAX SIMH `.ini` file:

```ini
; vax.ini — VAX SIMH configuration for ProjectPrinter

; Enable the DZ11 multiplexer (8 serial lines)
set dz enable
set dz lines=8

; Attach the DZ lines to a Telnet port.
; Line 0 of DZ11 = TTA0 in VMS, which we'll use as the printer line.
; ProjectPrinter will connect to port 1403 to receive print data.
att dz 1403
```

> [!NOTE]
> When you use `att dz 1403`, ALL DZ11 lines are exposed on port 1403 as a Telnet multiplexer. SIMH uses a rotary-style listener — connecting clients are assigned to the next available DZ line. To target a specific line, use the newer SIMH syntax:
> ```ini
> att dz line=0,1403
> ```
> This attaches only line 0 (TTA0) to port 1403.

**Step 2: VMS Configuration**

From the VMS system manager prompt, define a print queue that uses `TTA0:` as the output device:

```dcl
$ INITIALIZE /QUEUE /START /ON=TTA0: /PROCESSOR=LATSYM SYS$PRINT
```

Or for a simple passthrough queue:
```dcl
$ INITIALIZE /QUEUE /START /ON=TTA0: SYS$PRINT
$ START /QUEUE SYS$PRINT
```

To print a file:
```dcl
$ PRINT filename.LIS
$ PRINT /QUEUE=SYS$PRINT filename.LIS
```

**Step 3: Configure ProjectPrinter**

| Field | Value |
|-------|-------|
| Device Name | `VMS_PRT0` |
| Description | `VMS Line Printer` |
| Device Type | `0` (Printer) |
| Connection Type | `0` (Socket) |
| Destination | `127.0.0.1:1403` |
| OS | `1` (VMS) |
| Auto Connect | `True` |
| Output PDF | `True` |
| Orientation | `0` (Landscape) |
| Output Dir | `C:\PrintOutput\VMS` |

> [!NOTE]
> VMS job information is extracted from the **trailer page** of the print job. The trailer page contains a `JOB` line from which the job name, job number, and username are parsed. If VMS is not configured to generate banner/trailer pages, job identification will fall back to defaults.

---

### 2.2 SIMH — PDP-11 / RSTS/E

RSTS/E routes its line printer through the LP11 printer device, exposed on the PDP-11 as `LP:`. In SIMH this is typically designated **KB6** (the 7th KB device, zero-indexed). RSTS uses the DZ11 for serial line communication.

**Step 1: PDP-11 SIMH `.ini` File**

```ini
; pdp11.ini — PDP-11 SIMH configuration for RSTS/E + ProjectPrinter

; Enable the DZ11 multiplexer
set dz enable
set dz lines=8

; Attach all DZ lines to Telnet port 1403
; RSTS/E will use KB6 (DZ line 6) as the line printer
att dz 1403
```

Alternatively, if your SIMH version supports per-line attachment, target line 6 specifically:
```ini
att dz line=6,1403
```

> [!IMPORTANT]
> RSTS/E on a PDP-11 is a **low-speed device** in ProjectPrinter's classification. When printing over serial, data arrives slowly, character by character. ProjectPrinter will display the message:
> ```
> [RSTS_PRT0] receiving data from low speed device. Sit back and relax.
> ```
> This is normal. Do not assume the connection is broken if output is slow.

**Step 2: RSTS/E Configuration**

RSTS/E must be configured at sysgen time to enable the line printer on the appropriate DZ line. Verify with the RSTS/E monitor:
```
PRINT filename.LIS
```
or:
```
.PRINT filename.LIS
LPT:
```

**Step 3: Configure ProjectPrinter**

| Field | Value |
|-------|-------|
| Device Name | `RSTS_PRT0` |
| Description | `RSTS/E Line Printer` |
| Device Type | `0` (Printer) |
| Connection Type | `0` (Socket) |
| Destination | `127.0.0.1:1403` |
| OS | `3` (RSTS/E) |
| Auto Connect | `True` |
| Output PDF | `True` |
| Orientation | `0` (Landscape) |
| Output Dir | `C:\PrintOutput\RSTS` |

> [!NOTE]
> Job information in RSTS/E is extracted from lines where the word `ENTRY` appears at position 4 of the line. This provides the job queue name and user account (e.g., `[1,3]`). If extraction fails, the output file will be named under `UnknownUser`.

---

### 2.3 SIMH — HP 3000 / MPE

The HP3000 SIMH uses its own `LP` device to emulate the HP 2617A line printer. This device connects to ProjectPrinter through a serial port line.

**Step 1: HP3000 SIMH `.ini` File**

The HP3000 SIMH attaches the line printer (`lp`) to a serial connection. To route this to ProjectPrinter, attach the LP device's output to a socket listener:

```ini
; hp3000.ini — HP3000 SIMH configuration for MPE printing

; Attach the line printer to a Telnet port
att lp 1403
```

> [!NOTE]
> Some versions of the HP3000 SIMH use the `LP` device for direct file output and a separate **DL** (Data Link) or **PTR** device for serial connections. Consult your specific HP3000 SIMH documentation for the exact attachment method for your MPE version.

**Step 2: MPE Configuration**

From the MPE command prompt, submit a print job:
```mpe
:PRINT filename.PUB.SYS
```

MPE automatically routes output to `LDEV 6` (the standard line printer logical device). Ensure LDEV 6 is configured to the appropriate physical output in MPE's IO configuration.

**Step 3: Configure ProjectPrinter**

| Field | Value |
|-------|-------|
| Device Name | `MPE_PRT0` |
| Description | `HP 3000 MPE Printer` |
| Device Type | `0` (Printer) |
| Connection Type | `0` (Socket) |
| Destination | `127.0.0.1:1403` |
| OS | `2` (MPE) |
| Auto Connect | `True` |
| Output PDF | `True` |
| Orientation | `0` (Landscape) |
| Output Dir | `C:\PrintOutput\MPE` |

> [!NOTE]
> MPE jobs end with a `<FF><CR>` sequence. ProjectPrinter automatically detects and strips these extra control characters to prevent a blank trailing page in the output PDF.

---

## Part 3 — DTCyber (CDC NOS 2.7.8)

DTCyber is the emulator for Control Data Corporation mainframes running NOS 2.7.8.

**DTCyber Configuration**

In your DTCyber equipment configuration, define a line printer connected to a socket:

```
device,LP5106,005,lp,1403
```

This configures the LP5106 line printer controller at channel `005` to connect via socket on port `1403`. Exact syntax varies by DTCyber version — consult the DTCyber TIPS documentation.

**ProjectPrinter device settings:**

| Field | Value |
|-------|-------|
| Device Name | `NOS_PRT0` |
| Description | `CDC NOS 2.7.8 Printer` |
| Destination | `127.0.0.1:1403` |
| OS | `5` (NOS 2.7.8) |

**Printing from NOS 2.7.8:**
```nos
/PRINT,filename
```

NOS banner pages contain `UJN` (User Job Name) and `CREATING` lines that ProjectPrinter uses to identify the job and user.

---

## Part 4 — Tandy XENIX

Tandy XENIX is handled as a special case, as XENIX (an early UNIX derivative) does not produce banner/header pages in the same format as the IBM and DEC systems.

- **OS setting:** `7` (Tandy XENIX)
- **Line endings:** XENIX uses `CR+LF` pairs differently from other systems. ProjectPrinter has special handling to process these correctly.
- **Job naming:** Since XENIX doesn't produce parseable banner pages, the job name is set to `XENIX` and the ticks-based timestamp is used as the job ID.

---

## Part 5 — Putting It All Together (Quick Reference)

### Sample `devices.dat` for a Multi-System Setup

This is what a `devices.dat` file might look like for a machine running both an MVS system and a VAX simultaneously:

```
MVS_PRT0||MVS 3.8J Job Output||0||0||127.0.0.1:1403||0||True||True||0||C:\PrintOutput\MVS
MVS_PRT1||MVS 3.8J TSO Output||0||0||127.0.0.1:1404||0||True||True||0||C:\PrintOutput\MVS
VMS_PRT0||OpenVMS Line Printer||0||0||127.0.0.1:1500||1||True||True||0||C:\PrintOutput\VMS
```

### Port Assignment Convention

While any unused port above 1024 works, a common convention is:

| System | Suggested Port |
|--------|---------------|
| MVS 3.8J Printer 0 | `1403` |
| MVS 3.8J Printer 1 | `1404` |
| VM/370 Printer | `1405` |
| VMS Printer | `1500` |
| RSTS/E Printer | `1501` |
| MPE Printer | `1502` |
| NOS Printer | `1503` |
| z/OS Printer | `1404` |

---

## Part 6 — Troubleshooting Emulator Connections

### ProjectPrinter logs "unable to connect to remote host"

**For Hercules:**
- Verify Hercules is running and the device address is defined in `hercules.cnf` with `sockdev`.
- Check that Hercules has started the printer device: In the Hercules console, type `/$s 00E` (MVS) or verify the device status.
- Ensure nothing else is listening on port 1403 on your machine.

**For SIMH:**
- Verify the SIMH `.ini` file contains `att dz <port>` and that SIMH has started.
- Confirm SIMH printed something like `DZ: attached to port 1403` at startup.
- Test manually with a Telnet client: `telnet localhost 1403` — you should get a connection.

### Hercules printer is online but no output appears

- In MVS JES2, verify output is not held: `$DA` to display active jobs, `$TJ` to tip jobs to the printer.
- Ensure the JES2 output class routing matches your printer's class. In TK4-/TK5, the default class is `A`.
- Check `printers.log` — ProjectPrinter logs "Ignoring document with N lines" if the received data is too small (fewer than 10 lines). This usually means only a banner page printed without a job body.

### VMS jobs print but no file appears in the output directory

- VMS trailer pages (end-of-job) are required for ProjectPrinter to extract job info. Print a full job, not just individual files, to ensure the trailer is generated.
- Check the output directory in `devices.dat`. If the path doesn't exist, ProjectPrinter will attempt to create it, but check that the process has write permissions.

### RSTS/E output appears garbled or has extra characters

- RSTS/E uses some non-printable characters in its output. Set OS to `3` (RSTS/E) in ProjectPrinter — this enables special character handling that skips the standard non-printable character filter applied to other OS types.
- If output is still wrong, use `logType:default` in ProjectPrinter to see the raw line data as it's received.

### MPE jobs produce a blank final page

- This is caused by the `<FF><CR>` sequence MPE sends at end-of-job. ProjectPrinter automatically detects and removes this when OS is set to `2` (MPE). Verify you have OS set correctly in the device config.

### Device keeps disconnecting and reconnecting

- This is normal if the emulator is restarted or the guest OS powers down the printer device between jobs.
- ProjectPrinter will reconnect automatically in ~15 seconds. The log will show: `[DevName] Device disconnected. Attempting to reconnect...`
- If reconnection loops continuously, verify the emulator is actually running and hasn't crashed.
