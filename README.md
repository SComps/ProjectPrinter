# ProjectPrinter

Thanks to everyone on the Mainframe Enthusiasts Discord for putting up with my crap.  The MPE Forever discord for
encouraging me to get on with the AOT stuff.  

@racingmars for virtual1403 and the amazing green/blue bar pdf image.

@Rudi for like.. everything.  The support, ideas and just being around to test crazy stuff.  Great guy!

HFVMVS and PUBVM for running this code on their systems!  It's truly an honor to be allowed to run on 
your networks.

This project attempts to reasonably emulate a line printer from mainframes and mini computers
of our great past.  From the big boy 1403's on IBM mainframes, to the workhorses on DEC's
VAX and ALPHA systems, the printers that cranked along on DEC's PDP-11's running RSTS/E,
and of course the printers servicing the venerable HP 3000 running MPE.  The reams of
fanfold greenbar paper are a distant memory that this project hopefully brings back with
a smile.  It was developed on a Windows 10 system, however we've gotten it to build on
linux with .NET 9, and even the Raspberry Pi 5.  It can handle a number of simultaneous 
printers, and seems to perform reasonably well when several printers are running concurrently.

There are two projects in this repo.  

ProjectPrinter is the server code.  Output pdf's and txt files are deposited in the
configured directory (see device_config) 

This project is not meant to replace virtual1403, which is a fantastic project.  If you want a highly 
polished virtual printer with a lot more capability than this project, check it out.  You'll love it.  I love it
so much I use it myself, and the greenbar paper background (currently in jpg) is from virtual1403.
racingmars definitely deserves that credit.  


1/12/26
Significant enhancements to the Greenbar PDF generation:
- **Programmatic Greenbar Background:** Optimized PDF generation by replacing the static background image with a dynamic, programmatic drawer. This ensures high-fidelity rendering at any scale.
- **Standard Layout:** Strictly adheres to the standard 1403 wide-carriage format (14.875" x 11" nominal, dynamically scaled for content).
- **66 Lines Per Page:** Every page now accommodates exactly 66 lines of text content at 6 LPI (12pt spacing). The page height automatically adjusts to preserve this capacity even when top-margin offsets (like MVS's 5-line header skip) are used.
- **132 Characters Wide:** Font scaling has been refined to fit exactly 132 characters within the printable area, denoted by vertical dashed margin lines.
- **Comprehensive Detailing:** Includes tractor feed holes, gutter-aligned margin numbers (1-60), and alignment fiducials for an authentic look.
- **Legacy Fallback:** Use the `--imageproc` command-line flag to switch back to the original image-based background processing.
- **Testing:** Run with the `test` argument (e.g., `ProjectPrinter.exe test`) to generate a visual verification PDF.

1/12/36
Some fairly large changes.  Added Z/OS and Tandy's XENIX to the supported operating system list.

```
(0) OS_MVS38J           ' IBM MVS 3.8J or OSVS2
(1) OS_VMS              ' VAX and ALPHA VMS/OpenVMS
(2) OS_MPE              ' HP 3000 MPE
(3) OS_RSTS             ' DEC PDP-11 RSTS/E
(4) OS_VM370            ' IBM VM370 (VM370CE Community Edition) Special header pages
(5) OS_NOS278           ' CDC NOS 2.7.8
(6) OS_VMSP             ' IBM VM/SP (including HPO)
(7) OS_TANDYXENIX       ' Tandy XENIX
(8) OS_ZOS              ' IBM z/OS
```

The default logging is now 'printers.log' rather than the screen.  You can get to the original onscreen log by adding the parameter logType:default when starting the application.
I'm starting to get this to be a background process.  For now; it does need the terminal to remain open unless you've set it up as a systemd service on linux.  On Windows?  Yep.  Leave the terminal open.
You can view the log in printers.log; or as noted before; add the logType:default parameter.

Also in this update; the command listener is no longer a part of the application.  It used up resources; nobody really bothered with it, and to be honest it was flaky and most likely would have eventually been a security concern.  So it's gone.
Since this part of the code is gone; there's no need to support that funky reprint command that it offered; so no more .dst files. (again).

3/2/25 - There have been several small cosmetic updates, notably colorizing the output for ProjectPrinter.  Additionally, in previous versions a remote host that disconnected would not be reconnected unless you restarted the whole process.  This is mostly in how the .NET TCPClient handles events meaning [not at all].  So this required some creative error handling.  It seems to be working fine now.  If anyone runs into problems with it, let me know.  Currently the Google Drive links are outdated.  Hopefully I'll get to rebuild everything on the various platforms and get them uploaded this weekend.

1/22/25-Disabled writing of diagnostic and debugging text files with the exception of the .dst files.  The dst files are no longer parsed, but *exactly* as was 
received via the remote host system.  This facilitates the new "REPRINT" command via the telnet managegment interface. (port 16000).  dst filenames are semi-coded, and should not be changed.  If you do, the potential for an error during reprint is great.
Note reprints will generate the same output and job filenames as previous.  If you're looking for two copies, be sure to rename the first .pdf before you reprint or you'll still only have one copy, except it'll be dated differently.

<b>Changes to device configuration</b>

Due to an issue with AOT, the XML configuration file system has been scrapped.  Now it's a custom text file readable by the applications.  It's no longer devices.cfg (to avoid confusion) but devices.dat.  You will need to reconfigure all your devices.  Note there is now an Orientation and Output Directory option for device configuration.  Orientation is still in progress, Portrait (80 column) is working now but imperfectly.  It's recommended that you use the orientation of 4 to do portrait without the background as the green bar background is not accurately depicted.  Additionally it can now place the output PDF and TXT files in a specific folder or directory.  Each printer can have it's own output directory to allow you to separate jobs by printer.  When you configure this path, it can be both absolute or relative.  Leave off the final slash.  It'll strip it off if it sees it, but why bother if you don't have to.

  Operating systems currently supported are:
                          
                          VMS (VAX and ALPHA on TTA0: serial connection via simH)
  
                          RSTS/E connected via serial (simH DZ line 0 -- KB6)

                          MPE via simH HP3000 via serial connection defined in HP3000

                          VM/370 Community Edition, hercules 1403 sockdev

                          MVS3.8J (TK4-, TK5- MVSCE etc)
                                MVS 3.8J now works, and collects job information
                                Thanks to Rudi for helping me out with testing the
                                overstrike capabilities.
                                
                          NOS 2.7.8 (Control Data Corp.)

device_config is a console device configuration utility.

