
' // SINGLE INSTANCE BRANCH MODIFIED

Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Reflection
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
    Public StageList As New List(Of devs)
    Public GlobalParms As New List(Of parmStruct)
    Public configFile As String = "devices.dat"
    Public cmdPort As Integer = 0
    Public logType As String = "printers.log"
    Public logList As New List(Of LogEntry) 'Holds the last 500 log messages
    Public RemoteCommand As TcpListener

    Public Running As Boolean = True
    Public logOut As Boolean = False
    Public ShowPanel As Boolean = False
    Public LastScreen As Integer = 0   '0 = Log, 1 = Panel
    Public WithEvents statTimer As New System.Timers.Timer
    Private ReadOnly cts As New CancellationTokenSource()

    Sub OnSignalReceived(ByVal context As AssemblyLoadContext)
        Log("Termination signal received...",, 12)
        cts.Cancel() ' Request cancellation
    End Sub

    Async Sub DoBackgroundWork(ByVal cancellationToken As CancellationToken)
        Do While Not cancellationToken.IsCancellationRequested
            Console.WriteLine($"Background work running at {DateTime.Now}...")
            Try
                ' Wait asynchronously, respecting the cancellation token
                Await Task.Delay(5000, cancellationToken)
            Catch ex As TaskCanceledException
                ' Expected exception on cancellation
                Exit Do
            End Try
        Loop
        Console.WriteLine("Background task stopped.")
    End Sub

    Public Sub Main(args As String())
        AddHandler AssemblyLoadContext.Default.Unloading, AddressOf OnSignalReceived
        Dim assembly As Assembly = Assembly.GetExecutingAssembly()
        Dim version As String = "github.0.1.0-SI"
        If args.Count > 0 Then
            If args.Count = 1 And args(0).ToUpper = "VERSION" Then
                Console.WriteLine("Project printer version: " & version.ToString & ". 2024,2025 As open source.")
                Console.WriteLine("This project has no warranty at all.  Nothing.  If it breaks, you own both pieces.")
                ShutDown()
            End If
        End If
        Console.WriteLine($"ProjectPrinter version {version.ToString}. 2024,2025 As open source. No warranties, express or implied.")
        statTimer.Interval = 30000 '30 seconds
        GlobalParms = CheckArgs(args)
        If GlobalParms.Count = 0 Then
            'This should absolutely never happen, but hey.
            Console.WriteLine("No parameters defined, terminating.")
            End
        Else
            'Process the parameters
            ProcessParms(GlobalParms)

        End If
        If Not File.Exists(configFile) Then
            Dim fs As FileStream = File.Open(configFile, FileMode.Create)
            fs.Close()
        End If
        LoadDevices()
        statTimer.Enabled = True
        Running = True
        Console.WriteLine("Calling DoLoop")
        Task.Run(Sub() DoBackgroundWork(cts.Token))
        Try
            cts.Token.WaitHandle.WaitOne()
        Catch ex As OperationCanceledException
            Console.WriteLine(ex.Message)
        End Try
        Log("Terminating",, 12)
        RemoveHandler AssemblyLoadContext.Default.Unloading, AddressOf OnSignalReceived
    End Sub

    Async Sub DoLoop()
        Console.WriteLine("Starting DoLoop")
        While True
            Await Task.Delay(300)
        End While

    End Sub
    Function CheckArgs(args As String()) As List(Of parmStruct)
        ' checks arguements, sets values for operation.
        ' each arg is a string in the format of arg:value ie: config:appconfig.cfg
        If args.Count > 0 Then

        End If
        Dim argList As New List(Of parmStruct)
        Dim ParmsProvided As Integer = 0
        If args.Length = 0 Then
            'No arguments were specified, set up the defaults
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
                ' Do nothing.  It's not defined parm
            Else
                thisValue = valuePair(1)
                Dim newParm As New parmStruct
                newParm.arg = thisParm
                newParm.value = thisValue
                argList.Add(newParm)
                ' See if the logtype is defined.  If it is set it up here before 
                ' start getting chatty on the console.
                If newParm.arg = "logType" Then
                    logType = thisValue
                End If
                ParmsProvided += 1
            End If
        Next
        Return argList
    End Function

    Sub ProcessParms(parmList As List(Of parmStruct))
        Dim newCfg As String = "devices.cfg"        ' Set up some sane defaults.
        Dim newLogType As String = "printers.log"
        For Each p As parmStruct In parmList

            Select Case p.arg
                Case "config"
                    newCfg = p.value
                    Log(String.Format(parmDefined, p.arg, p.value), False)
                Case "logType"
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
        'Stop
    End Sub

    Private Function RotateLog(TimeStamp As String, errMsg As String, Fcolor As Integer) As List(Of LogEntry)
        Dim tempLog As New List(Of LogEntry)
        'We'll keep up to 500 lines of log, just in case.
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
            ' It's not defined, so assume default
            logType = "default"
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
                Dim logExists As Boolean = File.Exists(logType)
                Dim sw As New StreamWriter(logType, True)
                sw.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd (HH:mm.ss)"), errMsg))
                sw.Close()
        End Select
        logOut = False

        ' If we're instructed to terminate, do so here.
        If term Then ShutDown()
    End Sub

    Sub ShutDown()
        ' Do everything needed to terminate the process here
        Log("Shutdown requested",, ConsoleColor.Red)
        Running = False
    End Sub

    Private Function LoadDevs() As List(Of devs)
        Dim newList As New List(Of devs)
        ' No  serializer stuff here
        Using rdr As New StreamReader(configFile)
            'Is the file empty
            If rdr.EndOfStream Then
                Return newList
            End If
            Do
                Dim thisDev As String() = rdr.ReadLine().Split("||", StringSplitOptions.TrimEntries)
                If thisDev.Count <> 10 Then
                    'Do nothing with the device, it's invalid... somebody mess with the file?
                Else
                    Dim nd As New devs
                    nd.DevName = thisDev(0)
                    nd.DevDescription = thisDev(1)
                    nd.DevType = Val(thisDev(2))
                    nd.ConnType = Val(thisDev(3))
                    nd.DevDest = thisDev(4)
                    nd.OS = Val(thisDev(5))
                    If thisDev(6) = "True" Then
                        nd.Auto = True
                    Else
                        nd.Auto = False
                    End If
                    If thisDev(7) = "True" Then
                        nd.PDF = True
                    Else
                        nd.PDF = False
                    End If
                    nd.Orientation = Val(thisDev(8))
                    nd.OutDest = thisDev(9)
                    newList.Add(nd)
                End If
            Loop Until rdr.EndOfStream
        End Using
        Return newList
    End Function


    Sub LoadDevices()
        ' Unload all devices.
        DevList.Clear()
        Log("Clearing device list.",, ConsoleColor.Yellow)
        ' Reload from the file
        DevList = LoadDevs()
        Log("Loading device list from configuration.",, ConsoleColor.Yellow)
        If DevList.Count > 0 Then
            Log(String.Format("Attempting to initialize {0} device(s)", DevList.Count),, ConsoleColor.Yellow)
            For Each d As devs In DevList
                Log("Initializing device: " & d.DevDescription,, ConsoleColor.Green)
                d.Connect()
            Next
        Else
            Program.Log($"No devices to initialize.  Run device_config.",, ConsoleColor.Red)
            Console.WriteLine("Shutting down because no devices are configured.")
            Environment.Exit(0)
        End If
    End Sub

    Sub SaveDevices()
        Using writer As New StreamWriter(configFile, append:=False)
            For Each d As devs In DevList
                writer.WriteLine($"{d.DevName}||{d.DevDescription}||{d.DevType}||{d.ConnType}||{d.DevDest}||{d.OS}||{d.Auto}||" &
                    $"{d.PDF}||{d.Orientation}||{d.OutDest}")
            Next
        End Using

    End Sub


    Function DeviceList() As String
        Dim out As String = ""
        Dim outF As String = "{0,15}{1,20}{2,20}:{3,20}" & vbCrLf
        out = out & "DEVICE LIST" & vbCrLf & vbCrLf
        out = out & StrDup(79, "-") & vbCrLf
        out = out & String.Format(outF, "DEVICE", "DEV_TYPE", "CONN_TYPE", "DESTINATION")
        out = out & StrDup(79, "-") & vbCrLf
        For Each d As devs In DevList
            out = out & String.Format(outF, d.DevName, d.DevType, d.ConnType, d.DevDest)
        Next
        out = out & StrDup(79, "-") & vbCrLf & vbCrLf
        out = out & "END OF DEVICE LIST" & vbCrLf & vbCrLf
        Return out
    End Function




End Module
