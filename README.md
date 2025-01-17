# ProjectPrinter

# If you have run an older version of this application and are getting an error while generating output, re-run "device_config" to set up the additional orientation and output destination for each device.  Thos parameters have been added, but are NOT in your older device.cfg files.

A .NET 9.0 project written in Visual Basic .NET similar to Virtual1403.

This project attempts to reasonably emulate a line printer from mainframes and mini computers
of our great past.  From the big boy 1403's on IBM mainframes, to the workhorses on DEC's
VAX and ALPHA systems, the printers that cranked along on DEC's PDP-11's running RSTS/E,
and of course the printers servicing the venerable HP 3000 running MPE.  The reams of
fanfold greenbar paper are a distant memory that this project hopefully brings back with
a smile.  It was developed on a Windows 10 system, however we've gotten it to build on
linux with .NET 9, and even the Raspberry Pi 5.  It can handle a number of simultaneous 
printers, and seems to perform reasonably well when several printers are running concurrently.

The last static linked (supposedly) linux versions for x64 and raspberry pi (arm)
can be found at this link.

[Click here to access the folder on my Google Drive](https://drive.google.com/drive/folders/1-aCWr1JMhf7zmtW9EJ3QdICv3WfYTBh0?usp=sharing)


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

ProjectPrinterManager is a WINDOWS ONLY GUI application (not working).

This project is not meant to replace virtual1403, which is a fantastic project.  If you want a highly 
polished virtual printer with a lot more capability than this project, check it out.  You'll love it.  I love it
so much I use it myself, and the greenbar paper background (currently in jpg) is from virtual1403.
racingmars definitely deserves that credit.  

A big thanks to the guys on the MPE Forever! Discord for checking things out and making some great suggestions!
Devices are now configured to save the output in a user specified directory.

Rudi has suggested and started work on a Portrait orientation for output as well.  I'm looking forward to seeing how that works out.  Stay tuned!

