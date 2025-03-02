# ProjectPrinter

3/2/25 - There have been several small cosmetic updates, notably colorizing the output for ProjectPrinter.  Additionally, in previous versions a remote host that disconnected would not be reconnected unless you restarted the whole process.  This is mostly in how the .NET TCPClient handles events meaning [not at all].  So this required some creative error handling.  It seems to be working fine now.  If anyone runs into problems with it, let me know.  Currently the Google Drive links are outdated.  Hopefully I'll get to rebuild everything on the various platforms and get them uploaded this weekend.

1/22/25-Disabled writing of diagnostic and debugging text files with the exception of the .dst files.  The dst files are no longer parsed, but *exactly* as was 
received via the remote host system.  This facilitates the new "REPRINT" command via the telnet managegment interface. (port 16000).  dst filenames are semi-coded, and should not be changed.  If you do, the potential for an error during reprint is great.
Note reprints will generate the same output and job filenames as previous.  If you're looking for two copies, be sure to rename the first .pdf before you reprint or you'll still only have one copy, except it'll be dated differently.

<b>Changes to device configuration</b>

Due to an issue with AOT, the XML configuration file system has been scrapped.  Now it's a custom text file readable by the applications.  It's no longer devices.cfg (to avoid confusion) but devices.dat.  You will need to reconfigure all your devices.  Note there is now an Orientation and Output Directory option for device configuration.  Orientation is still in progress, Portrait (80 column) is working now but imperfectly.  It's recommended that you use the orientation of 4 to do portrait without the background as the green bar background is not accurately depicted.  Additionally it can now place the output PDF and TXT files in a specific folder or directory.  Each printer can have it's own output directory to allow you to separate jobs by printer.  When you configure this path, it can be both absolute or relative.  Leave off the final slash.  It'll strip it off if it sees it, but why bother if you don't have to.

The google drive now has AOT packages for x64 linux, arm 64 and Windows 64.  There is a natively built Debian 12 package there as well; however I noticed the generic linux-x64 worked just fine too.

A .NET 9.0 project written in Visual Basic .NET similar to Virtual1403.

This project attempts to reasonably emulate a line printer from mainframes and mini computers
of our great past.  From the big boy 1403's on IBM mainframes, to the workhorses on DEC's
VAX and ALPHA systems, the printers that cranked along on DEC's PDP-11's running RSTS/E,
and of course the printers servicing the venerable HP 3000 running MPE.  The reams of
fanfold greenbar paper are a distant memory that this project hopefully brings back with
a smile.  It was developed on a Windows 10 system, however we've gotten it to build on
linux with .NET 9, and even the Raspberry Pi 5.  It can handle a number of simultaneous 
printers, and seems to perform reasonably well when several printers are running concurrently.

The last manual AOT builds I've done are stored in the Google drive link below.  They may or may not be built from the current commit here.

[Click here to access the folder on my Google Drive](https://drive.google.com/drive/folders/1-aCWr1JMhf7zmtW9EJ3QdICv3WfYTBh0?usp=sharing)

While there are a bunch of other unrelated files on the google drive, you may play with anything there that interests you.  Anything you get from there is explicitly your own responsibility.  Chances are there will be no further development on any of that stuff, and you're essentially on your own.


There's three projects in this repo.  

ProjectPrinter is the server code.  Output pdf's and txt files are deposited in the
executable files directory.  This will be configurable in the future.

  Operating systems currently supported are:
                          
                          VMS (VAX and ALPHA on TTA0: serial connection via simH)
  
                          RSTS/E connected via serial (simH DZ line 0 -- KB6)

                          MPE via simH HP3000 via serial connection defined in HP3000

                          VM/370 Community Edition, hercules 1403 sockdev

                          MVS3.8J (TK4-, TK5- MVSCE etc)
                                MVS 3.8J now works, and collects job information
                                Thanks to Rudi for helping me out with testing the
                                overstrike capabilities.

device_config is a console device configuration utility (working).

ProjectPrinterManager is nowhere near working or complete, and not a part of any current build.  The code is still here however, so if you want to play with it feel free.

This project is not meant to replace virtual1403, which is a fantastic project.  If you want a highly 
polished virtual printer with a lot more capability than this project, check it out.  You'll love it.  I love it
so much I use it myself, and the greenbar paper background (currently in jpg) is from virtual1403.
racingmars definitely deserves that credit.  

A big thanks to the guys on the MPE Forever! Discord for checking things out and making some great suggestions!

If it weren't for them, I wouldn't have been motivated to research and struggle through the AOT modifications.  It was worth every nanosecond.

Rudi has suggested and started work on a Portrait orientation for output as well.  I'm looking forward to seeing how that works out.  Stay tuned!

