Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Runtime.Loader
Imports System.Text
Imports System.Threading

Public Class parmStruct
    Public arg As String
    Public value As String
End Class

Public Class LogEntry
    Public TimeStamp As String
    Public errMsg As String
    Public FColor As Integer
End Class

Public Enum DvType
    DT_PRINTER
    DT_READER
End Enum

Public Enum CNType
    CN_SOCKDEV
    CN_FILE
    CN_PHYSICAL
End Enum

Public Enum OSType
    OS_MVS38J           ' IBM MVS 3.8J or OSVS2
    OS_VMS              ' VAX and ALPHA VMS/OpenVMS
    OS_MPE              ' HP 3000 MPE
    OS_RSTS             ' DEC PDP-11 RSTS/E
    OS_VM370            ' IBM VM370 (VM370CE Community Edition) Special header pages
    OS_NOS278           ' CDC NOS 2.7.8
    OS_VMSP             ' IBM VM/SP (including HPO)
    OS_TANDYXENIX       ' Tandy XENIX
    OS_ZOS              ' IBM z/OS
End Enum

Module Program
    ' Updated for new install of VS2022 4-22-25
    Const parmDefined As String = "Defined parameter '{0}' as '{1}'"
    Const parmError As String = "Parameter error '{0]' is defined without a value"
    Const parmInvalid As String = "Invalid parameter {0}:{1}"

    Public DevList As New List(Of devs)
    Public DevRetries(255) As Integer
    Public StageList As New List(Of devs)
    Public GlobalParms As New List(Of parmStruct)
    Public configDate As Date
    Public configFile As String = "devices.dat"
    Public cmdPort As Integer = 0
    Public logType As String = "printers.log"
    Public logList As New List(Of LogEntry)     ' Holds the last 500 log messages

    Public Running As Boolean = True
    Public logOut As Boolean = False
    Public ShowPanel As Boolean = False
    Public LastScreen As Integer = 0            ' 0 = Log, 1 = Panel
    Public UseImageProc As Boolean = False
    Public WithEvents statTimer As New System.Timers.Timer
    Private ReadOnly cts As New CancellationTokenSource()

    ' POSIX signal registrations — stored as fields so the GC does not collect them.
    ' These are only assigned on Linux/macOS; they remain Nothing on Windows.
    Private _sigTermReg As IDisposable
    Private _sigIntReg As IDisposable
    Private _sigHupReg As IDisposable

    ' -----------------------------------------------------------------------
    ' RegisterSignals
    '   On Linux/macOS: hooks SIGTERM, SIGINT, and SIGHUP via
    '   PosixSignalRegistration (requires .NET 6+).
    '
    '   On Windows: hooks Console.CancelKeyPress for Ctrl+C / Ctrl+Break.
    '   A Windows Service stop command arrives through the SCM and is handled
    '   by the AssemblyLoadContext.Unloading event that is registered in Main.
    ' -----------------------------------------------------------------------
    Private Sub RegisterSignals()
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
            ' Windows — Ctrl+C / Ctrl+Break from a console window
            AddHandler Console.CancelKeyPress, Sub(sender, e)
                                                   e.Cancel = True    ' don't kill the process immediately
                                                   Log("Ctrl+C received – shutting down gracefully.", , ConsoleColor.Red)
                                                   ShutDown()
                                               End Sub
        Else
            ' Linux / macOS — proper POSIX signal handling
            ' We use a local helper to keep the lambda assignments tidy.
            _sigTermReg = PosixSignalRegistration.Create(PosixSignal.SIGTERM,
                Sub(ctx)
                    ctx.Cancel = True
                    Log("SIGTERM received – shutting down gracefully.", , ConsoleColor.Red)
                    ShutDown()
                End Sub)

            _sigIntReg = PosixSignalRegistration.Create(PosixSignal.SIGINT,
                Sub(ctx)
                    ctx.Cancel = True
                    Log("SIGINT received – shutting down gracefully.", , ConsoleColor.Red)
                    ShutDown()
                End Sub)

            ' SIGHUP: force an immediate config reload without restarting.
            ' Setting configDate to MinValue tricks statTimer into reloading
            ' on its very next tick — no extra code path required.
            _sigHupReg = PosixSignalRegistration.Create(PosixSignal.SIGHUP,
                Sub(ctx)
                    ctx.Cancel = True
                    Log("SIGHUP received – reloading device configuration.", , ConsoleColor.Yellow)
                    configDate = DateTime.MinValue
                End Sub)
        End If
    End Sub

    ' -----------------------------------------------------------------------
    ' Daemonize  (Linux/macOS only)
    '   Re-launches the process with stdio fully redirected so it detaches
    '   from the terminal.  The parent prints the child PID and exits; the
    '   child receives --daemonized in place of --daemon and skips this block.
    '
    '   On Windows this is a no-op — use "sc start" or NSSM to run as a
    '   Windows Service instead (see comments in Main).
    ' -----------------------------------------------------------------------
    Private Sub Daemonize(originalArgs As String())
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then
            Console.WriteLine("--daemon is not supported on Windows.")
            Console.WriteLine("Install as a Windows Service with: sc create ProjectPrinter binPath= ""{0}""", Environment.ProcessPath)
            Console.WriteLine("Or use NSSM (https://nssm.cc) for easier service management.")
            Return
        End If

        Dim childArgs As New List(Of String)
        For Each a In originalArgs
            If a.ToLower() <> "--daemon" Then childArgs.Add(a)
        Next
        childArgs.Add("--daemonized")

        Dim psi As New ProcessStartInfo(Environment.ProcessPath) With {
            .UseShellExecute = False,
            .CreateNoWindow = True,
            .RedirectStandardInput = True,
            .RedirectStandardOutput = True,
            .RedirectStandardError = True,
            .Arguments = String.Join(" ", childArgs.Select(Function(a) $"""{a}"""))
        }

        Dim child = Process.Start(psi)
        Console.WriteLine($"ProjectPrinter daemon started.  PID: {child.Id}")
    End Sub

    ' -----------------------------------------------------------------------
    ' WritePidFile / RemovePidFile  (Linux/macOS only)
    '   Windows services are tracked by the SCM; PID files are not needed
    '   and /var/run does not exist, so we skip this entirely on Windows.
    ' -----------------------------------------------------------------------
    Private Sub WritePidFile()
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then Return

        Dim pidPath As String = "/var/run/projectprinter.pid"
        Try
            File.WriteAllText(pidPath, Environment.ProcessId.ToString())
            Log($"PID file written to {pidPath}", , ConsoleColor.Gray)
        Catch
            ' /var/run is typically only writable by root; fall back gracefully.
            pidPath = Path.Combine(AppContext.BaseDirectory, "projectprinter.pid")
            Try
                File.WriteAllText(pidPath, Environment.ProcessId.ToString())
                Log($"PID file written to {pidPath} (fallback – /var/run not writable)", , ConsoleColor.Gray)
            Catch
                Log("Warning: could not write PID file.", , ConsoleColor.Yellow)
            End Try
        End Try
    End Sub

    Private Sub RemovePidFile()
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then Return

        For Each pidPath As String In {"/var/run/projectprinter.pid",
                                Path.Combine(AppContext.BaseDirectory, "projectprinter.pid")}
            Try
                If File.Exists(pidPath) Then File.Delete(pidPath)
            Catch
            End Try
        Next
    End Sub

    ' -----------------------------------------------------------------------
    ' OnUnloading
    '   Fires when the runtime is unloading the assembly — covers Windows
    '   Service stop via SCM, container SIGTERM on some runtimes, and acts
    '   as a safety net on Linux alongside the explicit POSIX handlers above.
    ' -----------------------------------------------------------------------
    Sub OnUnloading(ByVal context As AssemblyLoadContext)
        Debug.Print(context.ToString)
        ShutDown()
    End Sub

    Async Sub DoBackgroundWork(ByVal cancellationToken As CancellationToken)
        Do While Not cancellationToken.IsCancellationRequested
            Try
                Await Task.Delay(5000, cancellationToken)
            Catch ex As TaskCanceledException
                Exit Do
            End Try
        Loop
    End Sub

    Public Sub Main(args As String())
        For Each i As Integer In DevRetries
            i = 0
        Next

        ' ── Daemonize if requested ──────────────────────────────────────────
        ' Linux/macOS: pass --daemon to detach from the terminal.
        ' Windows:     --daemon prints service-install instructions and exits.
        If args.Contains("--daemon") Then
            Daemonize(args)
            Return
        End If

        Dim isDaemonized As Boolean = args.Contains("--daemonized")
        args = args.Where(Function(a) a.ToLower() <> "--daemonized").ToArray()

        ' ── Signal / shutdown handling ─────────────────────────────────────
        ' AssemblyLoadContext.Unloading covers Windows Service SCM stop and
        ' acts as a fallback on all platforms.
        AddHandler AssemblyLoadContext.Default.Unloading, AddressOf OnUnloading

        ' Platform-specific signal hooks layered on top.
        RegisterSignals()

        If isDaemonized Then WritePidFile()

        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        Dim version As String = "github.2026.02.08.UD"
        Dim isTest = False
        Dim isVersion = False

        If args.Count > 0 Then
            Dim filteredArgs As New List(Of String)
            For Each arg In args
                Dim lowerArg = arg.ToLower()
                If lowerArg = "--imageproc" Then
                    UseImageProc = True
                ElseIf lowerArg = "test" Then
                    isTest = True
                ElseIf lowerArg = "version" Then
                    isVersion = True
                Else
                    filteredArgs.Add(arg)
                End If
            Next
            args = filteredArgs.ToArray()
        End If

        Console.WriteLine($"ProjectPrinter version {version}. 2024,2025 As open source. No warranties, express or implied.")
        statTimer.Interval = 1000

        GlobalParms = CheckArgs(args)
        ProcessParms(GlobalParms)

        If isVersion Then
            Console.WriteLine("Project printer version: " & version & ". 2024,2025 As open source.")
            Console.WriteLine("This project has no warranty at all.  Nothing.  If it breaks, you own both pieces.")
            ShutDown()
            Return
        End If

        If isTest Then
            TestGreenbar.RunTest()
            Return
        End If

        If Not File.Exists(configFile) Then
            Dim fs As FileStream = File.Open(configFile, FileMode.Create)
            fs.Close()
        End If

        configDate = File.GetLastWriteTime(configFile)
        LoadDevices()
        statTimer.Enabled = True
        Running = True

        If logType <> "none" And logType <> "default" Then
            If isDaemonized Then
                Console.WriteLine($"Running as daemon (PID {Environment.ProcessId}).  Logging to {logType}")
            Else
                Console.WriteLine("Starting printers.  Please keep this terminal open.")
                Console.WriteLine("If you wish to view the log on this screen, use the parameter `logType:default`")
                Console.WriteLine($"Activity is being logged to {logType}")
            End If
        End If

        Task.Run(Sub() DoBackgroundWork(cts.Token))

        Try
            cts.Token.WaitHandle.WaitOne()
        Catch ex As OperationCanceledException
            Console.WriteLine($"Terminating signal: {ex.Message}")
        End Try

        Log("Terminating", , 12)
        RemoveHandler AssemblyLoadContext.Default.Unloading, AddressOf OnUnloading
    End Sub

    Async Sub DoLoop()
        While True
            Await Task.Delay(300)
        End While
    End Sub

    Function CheckArgs(args As String()) As List(Of parmStruct)
        Dim argList As New List(Of parmStruct)
        Dim ParmsProvided As Integer = 0

        ' Strip all -- flags before processing; they were already handled in Main
        args = args.Where(Function(a) Not a.StartsWith("--")).ToArray()

        If args.Length = 0 Then
            ' No arguments were specified, set up the defaults
            args = {"config:devices.dat", "logType:printers.log"}
        End If

        For Each p As String In args
            Dim valuePair As String()
            Dim thisParm As String = ""
            Dim thisValue As String = ""
            valuePair = p.Split(":", StringSplitOptions.TrimEntries)
            If valuePair.Count <> 2 Then
                Console.WriteLine($"*** Cannot parse option `{p}`. Terminating.")
                End
            End If
            If valuePair(0).Trim <> "" Then
                thisParm = valuePair(0)
            End If
            If valuePair(1).Trim = "" Then
                ' Do nothing.  It's not a defined parm
            Else
                thisValue = valuePair(1)
                Dim newParm As New parmStruct
                newParm.arg = thisParm
                newParm.value = thisValue
                argList.Add(newParm)
                ' See if the logtype is defined.  If it is, set it up here before
                ' we start getting chatty on the console.
                If newParm.arg = "logType" Then
                    logType = thisValue
                End If
                ParmsProvided += 1
            End If
        Next
        Return argList
    End Function

    Sub ProcessParms(parmList As List(Of parmStruct))
        Console.WriteLine(Environment.ProcessPath)
        Dim newCfg As String = "devices.dat"
        Dim newLogType As String = "printers.log"
        For Each p As parmStruct In parmList
            Select Case p.arg.ToLower
                Case "config"
                    newCfg = p.value
                    Log(String.Format(parmDefined, p.arg, p.value), False)
                Case "logtype"
                    newLogType = p.value
                    Log(String.Format(parmDefined, p.arg, p.value), False)
                    If p.value = "none" Then
                        Console.WriteLine("PROJECT PRINTER==>Logging disabled.")
                        Console.WriteLine("Keep this terminal open.  This application is not a daemon.")
                    End If
                Case Else
                    Log(String.Format(parmError, p.arg, p.value), True)
            End Select
        Next
        configFile = newCfg
        logType = newLogType
    End Sub

    Private Function RotateLog(TimeStamp As String, errMsg As String, Fcolor As Integer) As List(Of LogEntry)
        Dim tempLog As New List(Of LogEntry)
        Dim lastLog As Integer = logList.Count
        Dim starting As Integer = 0
        If lastLog > 499 Then
            starting = 1
        End If
        For i = starting To (logList.Count - 1)
            tempLog.Add(logList(i))
        Next
        Debug.Print(tempLog.Count)
        Dim newEntry As New LogEntry
        newEntry.TimeStamp = TimeStamp
        newEntry.errMsg = errMsg
        newEntry.FColor = Fcolor
        tempLog.Add(newEntry)
        Debug.Print("--->" & tempLog.Count)
        Return tempLog
    End Function

    Sub Log(errMsg As String, Optional term As Boolean = False, Optional FColor As Integer = ConsoleColor.White)
        Do While logOut = True
            Thread.Sleep(100)
        Loop
        logOut = True
        If logType = "" Then
            logType = "printers.log"
        End If

        Select Case logType
            Case "default"
                Dim timeStamp As String = DateTime.Now.ToString("yyyy-MM-dd (HH:mm.ss)")
                If Not ShowPanel Then
                    Console.Write(timeStamp & " ")
                    Console.ForegroundColor = FColor
                    Console.WriteLine(errMsg)
                    Console.ResetColor()
                End If
                logList = RotateLog(timeStamp, errMsg, FColor)
                Debug.Print("loglist-->" & logList.Count)
            Case "none"
                ' Requested silent operation
            Case Else
                Dim sw As New StreamWriter(logType, True)
                sw.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd (HH:mm.ss)"), errMsg))
                sw.Close()
        End Select
        logOut = False

        If term Then ShutDown()
    End Sub

    Sub ShutDown()
        Log("Shutdown requested", , ConsoleColor.Red)
        Running = False
        RemovePidFile()
        cts.Cancel()        ' unblocks WaitHandle.WaitOne() in Main so the process exits cleanly
    End Sub

    Private Function LoadDevs() As List(Of devs)
        Dim newList As New List(Of devs)
        Using rdr As New StreamReader(configFile)
            If rdr.EndOfStream Then
                Return newList
            End If
            Do
                Dim thisDev As String() = rdr.ReadLine().Split("||", StringSplitOptions.TrimEntries)
                If thisDev.Count <> 10 Then
                    ' Invalid record — skip silently
                Else
                    Dim nd As New devs
                    nd.DevName = thisDev(0)
                    nd.DevDescription = thisDev(1)
                    nd.DevType = Val(thisDev(2))
                    nd.ConnType = Val(thisDev(3))
                    nd.DevDest = thisDev(4)
                    nd.OS = Val(thisDev(5))
                    nd.Auto = (thisDev(6) = "True")
                    nd.PDF = (thisDev(7) = "True")
                    nd.Orientation = Val(thisDev(8))
                    nd.OutDest = thisDev(9)
                    newList.Add(nd)
                End If
            Loop Until rdr.EndOfStream
        End Using
        Return newList
    End Function

    Sub LoadDevices()
        For Each d As devs In DevList
            If d.Connected Then
                d.Disconnect()
                Log($"Disconnecting device {d.DevName} in preparation of a device load.")
            End If
        Next
        DevList.Clear()
        Log("Clearing device list.", , ConsoleColor.Yellow)
        DevList = LoadDevs()
        Log("Loading device list from configuration.", , ConsoleColor.Yellow)
        If DevList.Count > 0 Then
            Log(String.Format("Attempting to initialize {0} device(s)", DevList.Count), , ConsoleColor.Yellow)
            For Each d As devs In DevList
                Log("Initializing device: " & d.DevDescription, , ConsoleColor.Green)
                d.Connect()
            Next
        Else
            Program.Log($"No devices to initialize.  Run device_config.", , ConsoleColor.Red)
            Console.WriteLine("Shutting down because no devices are configured.")
            Environment.Exit(0)
        End If
    End Sub

    Private Sub statTimer_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles statTimer.Elapsed
        For Each d As devs In DevList
            If Not d.Connected Then
                Dim i As Integer = DevList.IndexOf(d)
                Dim retries As Integer = DevRetries(i)
                If retries > 14 Then
                    Log($"[{d.DevName}] Device disconnected. Attempting to reconnect...", , ConsoleColor.Yellow)
                    d.Connect()
                    DevRetries(i) = 0
                Else
                    DevRetries(i) += 1
                End If
            End If
        Next
        Dim currentDate As Date = File.GetLastWriteTime(configFile)
        If currentDate > configDate Then
            Log($"Configuration file has changed, reloading devices.")
            LoadDevices()
            configDate = currentDate
            Log($"Devices reloaded, configuration is now stamped {configDate}.")
        End If
    End Sub

End Module
