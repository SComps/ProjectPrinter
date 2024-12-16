Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.IO
Imports System.Net.Http
Imports System.Net.Sockets
Imports System.Runtime.CompilerServices
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Json
Imports System.Text
Imports System.Threading

Public Class devs
    Public DevName As String
    Public DevDescription As String
    Public DevType As Integer
    Public ConnType As Integer
    Public DevDest As String
    Public OS As Integer
    Public Auto As Boolean
    Public PDF As Boolean
    Private remoteHost As String
    Private remotePort As Integer
    Private client As New TcpClient
    Private clientStream As NetworkStream
    Private clientReader As StreamReader
    Private clientWriter As StreamWriter 'Should never be needed but what the hay.
    Private _cancellationTokenSource As CancellationTokenSource
    Private currentDocument As New List(Of String)

    Private Sub SplitDestination(dest As String)
        Dim thisHost As String
        Dim thisPort As Integer
        Dim splitDev As String()
        splitDev = dest.Split(":")
        thisHost = splitDev(0)
        thisPort = Val(splitDev(1))
        If thisHost.Trim <> "" Then
            remoteHost = thisHost
        Else
            Throw (New Exception("Destination does not contain a valid hostname"))
        End If
        If thisPort <> 0 Then
            remotePort = thisPort
        Else
            Throw (New Exception("Destination does not contain a valid port."))
        End If
    End Sub

    Public Async Sub Connect()
        SplitDestination(DevDest)
        Await StartAsync()
    End Sub
    ' Connects to the server and starts receiving data
    Public Async Function StartAsync() As Task
        client = New TcpClient()
        _cancellationTokenSource = New CancellationTokenSource()

        Try
            Program.Log("Connecting to server...")
            Await client.ConnectAsync(remoteHost, remotePort)
            clientStream = client.GetStream()
            Program.Log("Connected to server.")

            ' Start receiving data
            Await ReceiveDataAsync(_cancellationTokenSource.Token)
        Catch ex As Exception
            Program.Log($"Error: {ex.Message}")
            Throw
        Finally
            Disconnect()
        End Try
    End Function

    Private Sub TaskSleep(seconds As Integer)
        Dim delF As String = "[{0}] Data received, waiting {1} second for line to resume or settle."
        Log(String.Format(delF, DevName, seconds))
        Dim MyTime As DateTime = Now()
        Do Until Now > MyTime.AddSeconds(seconds)
            'Don't do anything
        Loop
    End Sub
    ' Continuously receives data from the server
    Private Async Function ReceiveDataAsync(cancellationToken As CancellationToken) As Task
        Dim lastLineReceived As DateTime = Now()
        Dim buffer(140) As Byte
        Dim dataBuilder As New StringBuilder()

        Try
            While Not cancellationToken.IsCancellationRequested
                While clientStream.DataAvailable = True
                    Dim recd As Integer = Await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                    'Log(recd & " Bytes Received.")
                    Dim receivedPart As String = Encoding.UTF8.GetString(buffer, 0, recd)
                    dataBuilder.Append(receivedPart)
                    ' Wait 3 seconds to see if more data arrives.
                    If clientStream.DataAvailable Then
                        'No need to wait
                    Else
                        TaskSleep(1)
                    End If
                End While

                ' Process complete lines (terminated by vbCr or FF)
                Dim fullData As String = dataBuilder.ToString()
                Dim lines As New List(Of String)()
                Dim currentLine As New StringBuilder()

                For Each c As Char In fullData
                    If c = vbCr Then
                        ' End the current line and start a new one
                        lines.Add(currentLine.ToString())
                        currentLine.Clear()
                    ElseIf c = Chr(12) Then ' Form Feed (FF)
                        ' End the current line, add FF on it'd own line and start a new one
                        lines.Add(currentLine.ToString() & vbCrLf)
                        currentLine.Clear()
                        currentLine.Append(c & vbCrLf)
                    Else
                        ' Add character to the current line
                        currentLine.Append(c)
                    End If
                Next

                ' Retain any partial line (not terminated by vbCr or FF)
                dataBuilder.Clear()
                dataBuilder.Append(currentLine.ToString())

                ' Output complete lines
                For Each line As String In lines
                    If Not String.IsNullOrEmpty(line) Then
                        currentDocument.Add(line)
                    End If
                Next
                If currentDocument.Count > 0 Then
                    Await ProcessDocument(currentDocument)
                    currentDocument.Clear()
                End If
            End While
        Catch ex As OperationCanceledException
            Log("Receiving canceled.")
        Catch ex As Exception
            Log($"Error receiving data: {ex.Message}")
        End Try
    End Function

    ' Disconnects the client
    Public Sub Disconnect()
        Try
            _cancellationTokenSource?.Cancel()
            clientStream?.Close()
            client?.Close()
            Program.Log("Disconnected from server.")
        Catch ex As Exception
            Program.Log($"Error during disconnection: {ex.Message}")
        End Try
    End Sub

    Private Async Function ProcessDocument(doc As List(Of String)) As Task
        If doc.Count > 4 Then
            ' For now we're just going to save it to a file.  Ultimately really process it
            Dim JobID, JobName, UserID As String
            Dim vals = VMS_ExtractJobInformation(doc)
            JobID = vals.JobId
            JobName = vals.JobName
            UserID = vals.User
            Dim fnFmt As String = "PRT-{0}-{1}-{2}.txt"
            Dim filename As String = String.Format(fnFmt, UserID, JobID, JobName)
            Dim oStream As New StreamWriter(filename)
            For Each l As String In doc
                oStream.Write(l)
            Next
            Await oStream.FlushAsync
            oStream.Close()
            Log(String.Format("[{2}] {0} lines of output written to {1}", currentDocument.Count, filename, DevName))
        Else
            Log(String.Format("[{1}] Ignoring document with {0} lines as line garbage or banners.", doc.Count, DevName))
        End If
    End Function

    Private Function IsTrailerPage(lines As String()) As Boolean
        ' Check if the trailer contains "COMPLETED ON" indicating job completion
        Return lines.Any(Function(line) line.Contains("COMPLETED ON"))
    End Function

    Private Function VMS_ExtractJobInformation(lines As List(Of String)) As (JobName As String, JobId As String, User As String)
        Dim GotInfo As Boolean = False
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log("Processing " & lines.Count & " lines for job information.")
        For Each line In lines
            line = line.ToUpper
            ' Toward the end of the Trailer page all of our information is available
            ' on a single line beginning with "Job"
            ' Indices 1,2 And 16 should hold this information.
            If line.Trim.StartsWith("JOB") Then
                'This might be it!
                Dim parts As String() = line.Split(" ")
                jobName = parts(1)
                jobId = parts(2)
                user = parts(16)
                jobId = jobId.Replace("(", "")
                jobId = jobId.Replace(")", "")
                GotInfo = True
            End If
        Next
        If Not GotInfo Then
            jobName = "UNKNOWN"
            jobId = Now.ToShortTimeString
            user = DevName
        End If
        Return (jobName, jobId, user)
    End Function


End Class
