Imports System
Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Data
Imports System.Diagnostics.Tracing
Imports System.IO
Imports System.Net
Imports System.Net.Sockets
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports System.Runtime.Intrinsics
Imports System.Security.AccessControl
Imports System.Text
Imports System.Xml.Serialization
Public Class parmStruct
    Public arg As String
    Public value As String
End Class
Module Program

    Const parmDefined As String = "Defined parameter '{0}' as '{1}'"
    Const parmError As String = "Parameter error '{0]' is defined without a value"
    Const parmInvalid As String = "Invalid parameter {0}:{1}"

    Public GlobalParms As List(Of parmStruct)
    Public configFile As String = ""
    Public cmdPort As Integer = 0
    Public logType As String = "default"
    Public RemoteCommand As TcpListener

    Public Running As Boolean = True

    Sub Main(args As String())
        GlobalParms = CheckArgs(args)
        If GlobalParms.Count = 0 Then
            'This should absolutely never happen, but hey.
            Console.WriteLine("No parameters defined, terminating.")
            End
        Else
            'Process the parameters
            ProcessParms(GlobalParms)
        End If
        If cmdPort = "0" Then
            Log("Not Listening for a remote controller.")
        Else
            Dim listenerTask = StartTcpListenerAsync()
        End If

        While Running
            ' Just sit hanging around thanks.
        End While
        ShutDown()
    End Sub

    Function CheckArgs(args As String()) As List(Of parmStruct)
        ' checks arguements, sets values for operation.
        ' each arg is a string in the format of arg:value ie: config:appconfig.cfg
        Dim argList As New List(Of parmStruct)
        Dim ParmsProvided As Integer = 0
        If args.Length = 0 Then
            'No arguments were specified, set up the defaults
            args = {"config:ProjectPrinter.cfg", "cmdPort:16000", "logType:default"}
        End If

        For Each p As String In args
            Dim valuePair As String()
            Dim thisParm As String = ""
            Dim thisValue As String = ""
            valuePair = p.Split(":", StringSplitOptions.TrimEntries)
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
        Dim newCfg As String = ""
        Dim newPort As String = ""
        Dim newLogType As String = ""
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
                Case Else
                    Log(String.Format(parmError, p.arg, p.value), True)
            End Select
        Next
        configFile = newCfg
        cmdPort = Val(newPort)
        logType = newLogType
    End Sub

    Sub Log(errMsg As String, Optional term As Boolean = False)
        If logType = "" Then
            ' It's not defined, so assume default
            logType = "default"
        End If

        Select Case logType
            Case "default"
                Console.WriteLine(errMsg)
            Case "none"
                ' Requested silent operation
            Case Else
                ' Logging to the defined filename
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
        End
    End Sub




    ' ==============================================
    ' REMOTE CLIENT CODE FOLLOWS 
    ' ==============================================

    Async Function StartTcpListenerAsync() As Task
        Dim listener As New TcpListener(IPAddress.Any, cmdPort)

        Try
            listener.Start()
            Console.WriteLine($"Listening on port {cmdPort}...")

            While True
                ' Accept incoming client connections asynchronously
                Dim client As TcpClient = Await listener.AcceptTcpClientAsync()
                Console.WriteLine("Client connected.")
                Await HandleClientAsync(client) ' Process the client and wait for it to finish
            End While
        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
        Finally
            listener.Stop()
        End Try
    End Function

    Async Function HandleClientAsync(client As TcpClient) As Task
        Using client
            'Try
            Dim stream As NetworkStream = client.GetStream()
            Dim reader As New StreamReader(stream, Encoding.UTF8)
            Dim writer As New StreamWriter(stream, Encoding.UTF8) With {.AutoFlush = True}

            Log("Waiting for complete lines from the client...")
            While client.Connected
                ' Read a complete line of input
                Dim line As String = Await reader.ReadLineAsync()

                If line Is Nothing Then
                    Exit While ' Client disconnected
                End If
                ' Process the received line
                If line.Trim().Equals("STOP", StringComparison.OrdinalIgnoreCase) Then
                    Console.WriteLine("Stop command received. Closing connection.")
                    Exit While
                ElseIf line.Trim().Equals("SHUTDOWN", StringComparison.OrdinalIgnoreCase) Then
                    Console.WriteLine("Shutdown command received. Terminating application.")
                    Running = False
                    Exit While
                End If
                Dim response As String = ProcessLine(line)
                ' Send the response back to the client
                Await writer.WriteLineAsync(response)
            End While
            'Catch ex As Exception
            'Log($"Client error: {ex.Message}")
            'End Try
        End Using
    End Function

    Function ProcessLine(input As String) As String
        ' Process input from the remote client
        Dim NoCommand As String = "ERROR: '{0}' Invalid command. Please review the documentation."
        Select Case input.Trim
            Case "HELLO"
                Return "ProjectPrinter V0.1Alpha, Development. S Johnson 2024"
            Case "GETDEVICES"
                ' Serialize the device list to XML
                Dim serializer As New XmlSerializer(GetType(List(Of parmStruct)))
                Using sw As New StringWriter()
                    serializer.Serialize(sw, GlobalParms)
                    Return sw.ToString()
                End Using
        End Select
        Return String.Format(NoCommand, input)
    End Function
End Module
