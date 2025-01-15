# ProjectPrinter

A .NET 9.0 project written in Visual Basic .NET similar to Virtual1403.

This project attempt to reasonably emulate a line printer from mainframes and mini computers
of our great past.  From the big boy 1403's on IBM mainframes, to the workhorses on DEC's
VAX and ALPHA systems, the printers that cranked along on DEC's PDP-11's running RSTS/E,
and of course the printers servicing the venerable HP 3000 running MPE.  The reams of
fanfold greenbar paper are a distant memory that this project hopefully brings back with
a smile.  It was developed on a Windows 10 system, however we've gotten it to build on
linux with .NET 9, and even the Raspberry Pi 5.  It can handle a number of simultaneous 
printers, and seems to perform reasonably well when several printers are running concurrently.

The last static linked (supposedly) linux versions for x64 and raspberry pi (arm)
can be found at this link.

https://drive.google.com/drive/folders/13juH_mvTz5u_BkZ0AqulOF6_oOGp93a6?usp=sharing


There's three projects in this repo.  

ProjectPrinter is the server code.  Output pdf's and txt files are deposited in the
executable files directory.  This will be configurable in the future.

  Operating systems currently supported are:
                          
                          VMS (VAX and ALPHA on TTA0: serial connection via simH)
  
                          RSTS/E connected via serial (simH DZ line 0 -- KB6)

                          MPE via simH HP3000 via serial connection defined in HP3000

                          VM/370 Community Edition, hercules 1403 sockdev

                          MVS3.8J (TK4-, TK5- MVSCE etc) are in progress.  Not yet completed
                             print jobs go through, but job information is not pulled.

device_config is a console device configuration utility (working).

ProjectPrinterManager is a WINDOWS ONLY GUI application (not working).

This project is not meant to replace virtual1403, which is a fantastic project.  If you want a highly 
polished virtual printer with a lot more capability than that, check it out.  You'll love it.  I love it
so much I use it myself, and the greenbar paper background (currently in jpg) is from virtual1403.
racingmars definitely deserves that credit.  
