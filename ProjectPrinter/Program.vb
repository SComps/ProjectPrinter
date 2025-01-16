Imports System.IO
Imports System.Threading
Imports System.Net
Imports System.Net.Mime.MediaTypeNames
Imports System.Net.Sockets
Imports System.Reflection
Imports System.Runtime
Imports System.Text
Imports System.Text.Json
Imports System.Xml.Serialization
Imports System.ComponentModel.Design

Public Class parmStruct
    Public arg As String
    Public value As String
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
    OS_MVS38J
    OS_VMS
    OS_MPE
    OS_RSTS
    OS_VM370
End Enum
Module Program

    Const parmDefined As String = "Defined parameter '{0}' as '{1}'"
    Const parmError As String = "Parameter error '{0]' is defined without a value"
    Const parmInvalid As String = "Invalid parameter {0}:{1}"

    Public DevList As New List(Of devs)
    Public StageList As New List(Of devs)
    Public GlobalParms As New List(Of parmStruct)
    Public configFile As String = "devices.cfg"
    Public cmdPort As Integer = 0
    Public logType As String = "default"
    Public RemoteCommand As TcpListener

    Public Running As Boolean = True

    Public WithEvents statTimer As New System.Timers.Timer

    Public Function Main(args As String()) As Integer
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


        If cmdPort = "0" Then
            Log("Not Listening for a remote controller.")
        Else

            Dim listenerTask = StartTcpListenerAsync()
        End If

        LoadDevices()
        statTimer.Enabled = True
        While Running
            Thread.Sleep(300)
        End While
        ShutDown()
        Return 0
    End Function

    Sub CheckTimer(source As Object, args As EventArgs) Handles statTimer.Elapsed
        For Each d As devs In DevList
            If Not d.Connected Then
                Program.Log($"[{d.DevName}] Remote not connected.  Retrying...")
                d.Connect()
            End If
        Next
        statTimer.Enabled = True
    End Sub

    Function CheckArgs(args As String()) As List(Of parmStruct)
        ' checks arguements, sets values for operation.
        ' each arg is a string in the format of arg:value ie: config:appconfig.cfg
        If args.Count > 0 Then
            If args(0).ToUpper = "HELP" Then
                DoHelp()
            End If
        End If
        Dim argList As New List(Of parmStruct)
        Dim ParmsProvided As Integer = 0
        If args.Length = 0 Then
            'No arguments were specified, set up the defaults
            args = {"config:devices.cfg", "cmdPort:16000", "logType:default"}
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
        Dim newPort As String = "16000"
        Dim newLogType As String = "default"
        For Each p As parmStruct In parmList

            Select Case p.arg
                Case "config"
                    newCfg = p.value
                    Log(String.Format(parmDefined, p.arg, p.value), False)
                Case "cmdPort"
                    newPort = p.value
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
        cmdPort = Val(newPort)
        logType = newLogType
        'Stop
    End Sub

    Sub Log(errMsg As String, Optional term As Boolean = False)
        If logType = "" Then
            ' It's not defined, so assume default
            logType = "default"
        End If

        Select Case logType
            Case "default"
                Console.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd (HH:mm.ss)"), errMsg))
            Case "none"
                ' Requested silent operation
            Case Else
                ' Logging to the defined filename
                Console.WriteLine($"Logging to file {logType}")
                Dim logExists As Boolean = File.Exists(logType)
                Dim sw As New StreamWriter(logType, True)
                sw.WriteLine(String.Format("{0} {1}", DateTime.Now.ToString("yyyy-MM-dd (HH:mm.ss)"), errMsg))
                sw.Close()
        End Select

        ' If we're instructed to terminate, do so here.
        If term Then ShutDown()
    End Sub

    Sub ShutDown()
        ' Do everything needed to terminate the process here
        Log("Shutdown requested")
        Running = False
    End Sub

    Public Function SerializeToXml(Of T)(obj As T) As String
        Dim serializer As New XmlSerializer(GetType(T))
        Using stringWriter As New StringWriter()
            serializer.Serialize(stringWriter, obj)
            Return stringWriter.ToString()
        End Using
    End Function
    Sub LoadDevices()
        ' Unload all devices.
        DevList.Clear()
        Log("Remote management clearing device list.")
        ' Reload from the file
        Dim serializer As New XmlSerializer(GetType(List(Of devs)))
        Dim xmlStream As New StreamReader(configFile)
        Try
            DevList = serializer.Deserialize(xmlStream)
        Catch ex As Exception
            Console.WriteLine($"ERR: {ex.Message}")
            Console.WriteLine("ERR: You have no devices defined or the config file specified is corrupt.")
            Console.WriteLine("INF: Use the device_config tool to establish your devices.")
            Console.WriteLine($"USAGE: device_config {configFile}")
            xmlStream.Close()
            End
        End Try
        xmlStream.Close()
        Log("Remote management loading device list from configuration.")
        Log(String.Format("Attempting to initialize {0} device(s)", DevList.Count))
        For Each d As devs In DevList
            Log("Initializing device: " & d.DevDescription)
            d.Connect()
        Next
    End Sub

    Sub SaveDevices()
        ' Serialize the device list to XML
        Dim serializer As New XmlSerializer(GetType(List(Of devs)))
        Dim xmlStream As New StreamWriter(configFile)
        Using sw As New StringWriter()
            serializer.Serialize(sw, DevList)
            xmlStream.Write(sw.ToString())
            xmlStream.Close()
        End Using
        Log("Remote management updated the device list configuration.")
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

    Private Sub DoHelp()
        Console.WriteLine("Command line options:")
        Console.WriteLine("")
        Console.WriteLine("arguments take the form key:value")
        Console.WriteLine("CASE IS SENSITIVE IN BOTH KEY AND VALUE!")
        Console.WriteLine("")
        Console.WriteLine("config defines the configuration file:  config:devices.cfg [DEFAULT]")
        Console.WriteLine("cmdPort defines the listening port for management: cmdPort:16000 [DEFAULT]")
        Console.WriteLine("logType defines logging options as follows")
        Console.WriteLine("      default: Logs to the screen or stdout [DEFAULT]")
        Console.WriteLine("      none: No logging once application is initialized.")
        Console.WriteLine("   any valid filename: example logType:logfile.log")
        Console.WriteLine("")
        Console.WriteLine("if no options are present, defaults are selected automatically.")
        Console.WriteLine("")
        End
    End Sub


    ' ==============================================
    ' REMOTE CLIENT CODE FOLLOWS 
    ' ==============================================

    Async Function StartTcpListenerAsync() As Task
        Dim listener As New TcpListener(IPAddress.Any, cmdPort)

        Try
            listener.Start()
            Log($"Remote management async server started on port {cmdPort}")

            While True
                ' Accept incoming client connections asynchronously
                Dim client As TcpClient = Await listener.AcceptTcpClientAsync()
                Log("Accepted connection on remote management port.")
                Await HandleClientAsync(client) ' Process the client and wait for it to finish
            End While
        Catch ex As Exception
            Log($"Error: {ex.Message}")
            Running = False
        Finally
            listener.Stop()
        End Try
    End Function

    Async Function HandleClientAsync(client As TcpClient) As Task
        Using client
            Try
                Dim stream As NetworkStream = client.GetStream()
                Dim reader As New StreamReader(stream, Encoding.UTF8)
                Dim writer As New StreamWriter(stream, Encoding.UTF8) With {.AutoFlush = True}
                Await writer.WriteLineAsync(vbCrLf & "ProjectPrinter Remote Management.")
                Log("Waiting for complete lines from the client...")
                While client.Connected
                    Await writer.WriteAsync(">>>")
                    ' Read a complete line of input
                    Dim line As String = Await reader.ReadLineAsync()

                    If line Is Nothing Then
                        Exit While ' Client disconnected
                    End If
                    ' Process the received line
                    If line.Trim().Equals("STOP", StringComparison.OrdinalIgnoreCase) Then
                        Log("Stop command received. Closing connection.")
                        Exit While
                    ElseIf line.Trim().Equals("SHUTDOWN", StringComparison.OrdinalIgnoreCase) Then
                        Log("Shutdown command received. Terminating.")
                        Running = False
                        Exit While
                    End If
                    Dim response As String = ProcessLine(line)
                    ' Send the response back to the client
                    Await writer.WriteLineAsync(response)
                End While
            Catch ex As Exception
                Log($"Client error: {ex.Message}")
            End Try
        End Using
    End Function

    Function ProcessLine(input As String) As String
        ' Process input from the remote client
        Dim NoCommand As String = "ERROR: '{0}' Invalid command. Please review the documentation."
        ' Break input into words separated by a space or comma.
        Dim parsed As String() = input.Split({" ", ","})
        Dim cmd As String = parsed(0).ToUpper
        Log("Remote-->" & cmd)
        Select Case cmd
            Case "HELLO"
                Return "ProjectPrinter V0.1Alpha, Development. 2024"
            Case "SHOW_DEVS"
                Return DeviceList()
            Case "UPDATE_DEVS"
                SaveDevices()
                Return "Device list updated with current values."
            Case "LOAD_DEVS"
                LoadDevices()
                Return "Device list loaded from stored configuration."
            Case "CLR_STAGE"
                StageList.Clear()
                Program.Log("Cleared device staging area.")
                Return "Staging list cleared."
            Case "STAGE_DEV"
                ' Adds a new device to the staging area
                Return AddADevice(input)
            Case "COMMIT_STAGE"
                ' Replaces all current devices with the staging devices
                ' waits until everything is idle, then restarts.
            Case "HELP"
                Return ShowHelp()
            Case "GUI_SEND"
                ' Sends configuration data to GUI client
                Return GUI_SendConfig()
            Case "GUI_SDEV"
                ' Sends device data for the GUI client.
                Return GUI_SendDev()
            Case "GUI_RECV"
                ' Receives configuration from GUI client.
            Case "GUI_RDEV"
                ' Receives Device configuration for all devices.
            Case "RESTART"
                'Restart
                Dim asm As Assembly = Assembly.GetExecutingAssembly()
                Dim asmName As String = asm.GetName().Name() & ".exe"
                Process.Start(System.Environment.CurrentDirectory & "\" & asmName)
                Environment.Exit(0)
        End Select
        Return String.Format(NoCommand, input)
    End Function

    Private Function AddADevice(inputline As String) As String
        Dim newDevice As New devs
        Dim inputParms As String() = inputline.Split("|")
        If inputParms.Count <> 8 Then
            Return $"Invalid device data. {inputParms.Count} elements received."
        End If
        newDevice.DevName = inputParms(1)
        newDevice.DevDescription = inputParms(2)
        newDevice.DevType = inputParms(3)
        newDevice.ConnType = inputParms(4)
        newDevice.DevDest = inputParms(5)
        newDevice.OS = inputParms(6)
        newDevice.Auto = inputParms(7)
        newDevice.PDF = inputParms(8)
        StageList.Add(newDevice)
        Program.Log($"[STAGING] Added new device {newDevice.DevName} to the staging list.")
        Return "Staging device accepted."
    End Function
    Function ShowHelp() As String
        Dim hlp As New StringBuilder
        hlp.AppendLine("ProjectPrinter - 2024,2025 provided as true open source.  As in here's")
        hlp.AppendLine("the source, do what you want with it.  Be respectful and honorable.  If you")
        hlp.AppendLine("use this commercially, please at least try to help the authors out a little.")
        hlp.AppendLine("The world is an expensive, dark place.  Thanks Biden!  Your a freakin' pip.")
        hlp.AppendLine("")
        hlp.AppendLine("COMMAND HELP")
        hlp.AppendLine("------- ----")
        hlp.AppendLine("")
        hlp.AppendLine("HELLO       - Returns version information")
        hlp.AppendLine("SHOW_DEVS   - Displays a list of configured devices.")
        hlp.AppendLine("UPDATE_DEVS - Writes the current device configuration to a file.")
        hlp.AppendLine("LOAD_DEVS   - Loads the device list from the configuration file, and activates")
        hlp.AppendLine("              the devices if necessary.")
        hlp.AppendLine("STOP        - Disconnects the remote management client (this screen)")
        hlp.AppendLine("SHUTDOWN    - Terminates the ProjectPrinter process.")
        hlp.AppendLine("HELP        -  Really?  Display this message.")
        hlp.AppendLine("")
        hlp.AppendLine("This management connection is rudimentary at best.  There is no editting")
        hlp.AppendLine("features written into the code.  That would be a waste of coding effort")
        hlp.AppendLine("when there are much more important things to be done, like making it do what")
        hlp.AppendLine("it's intended to do as fast as we can make it happen.  Type your commands")
        hlp.AppendLine("carefully--or type them again... correctly.")
        hlp.AppendLine("")
        hlp.AppendLine("As more commands become available, they will be documented here.")
        Return hlp.ToString()
    End Function

    Private Function oldGUI_SendDev()
        ' Serialize the device list to XML
        Dim serializer As New XmlSerializer(GetType(List(Of devs)))
        Dim outString As String = ""
        'Dim xmlStream As New StreamWriter()
        Using sw As New StringWriter()
            serializer.Serialize(sw, DevList)
            outString = sw.ToString()
        End Using
        outString = outString & vbCrLf & "[[EOD]]"
        Return outString
    End Function

    Private Function GUI_SendDev()
        Dim outFmt As String = "DEV|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}" & vbCrLf
        Dim sb As New StringBuilder
        For Each d In DevList
            sb.Append(String.Format(outFmt, d.DevName, d.DevDescription, d.DevType, d.ConnType, d.DevDest, d.OS, d.Auto, d.PDF))
        Next
        sb.Append("[[EOD]]")
        Return sb.ToString
    End Function

    Private Function GUI_SendConfig()
        Dim outFmt As String = "CFG|{0}|{1}|{2}" & vbCrLf
        Dim sb As New StringBuilder
        sb.Append(String.Format(outFmt, configFile, cmdPort, logType))
        sb.Append("[[EOD]]")
        Return sb.ToString
    End Function

End Module
