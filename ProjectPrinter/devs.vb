Imports System.ComponentModel
Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports System.Net.Sockets
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Threading.Tasks.Dataflow
Imports PdfSharp.Drawing
Imports PdfSharp.Events
Imports PdfSharp.Fonts
Imports PdfSharp.Pdf

Public Class devs
    Public DevName As String
    Public DevDescription As String
    Public DevType As Integer
    Public ConnType As Integer
    Public DevDest As String
    Public OS As Integer
    Public Auto As Boolean
    Public PDF As Boolean
    Public Orientation As Integer
    Public OutDest As String
    Private remoteHost As String
    Private remotePort As Integer
    Private client As New TcpClient
    Private clientStream As NetworkStream
    Private clientReader As StreamReader
    Private clientWriter As StreamWriter 'Should never be needed but what the hay.
    Private _cancellationTokenSource As CancellationTokenSource
    Private currentDocument As New List(Of String)
    Private IsConnected As Boolean = False
    Private Receiving = False
    Private JobNumber As Integer = 0


    Public ReadOnly Property Connected As Boolean
        Get
            Return IsConnected
        End Get
    End Property

    Private Sub SplitDestination(dest As String)
        Dim thisHost As String
        Dim thisPort As Integer
        Dim splitDev As String()
        splitDev = dest.Split(":")
        thisHost = splitDev(0)
        thisPort = Val(splitDev(1))
        If thisHost.Trim <> "" Then
            remoteHost = thisHost.Trim
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
        Await StartAsync()  ' Connects to the server and starts receiving data
    End Sub
    Public Async Function StartAsync() As Task
        client = New TcpClient()
        _cancellationTokenSource = New CancellationTokenSource()

        Try
            Program.Log($"[{DevName}] Attempting to connect...",, ConsoleColor.Yellow)
            Await client.ConnectAsync(remoteHost, remotePort)
            Program.Log($"[{DevName}] Connection successful.",, ConsoleColor.Green)
            If OutDest.EndsWith("/") Or OutDest.EndsWith("\") Then
                OutDest = OutDest.Substring(0, OutDest.Length - 1)
            End If
            If Not FileIO.FileSystem.DirectoryExists(OutDest) Then
                Program.Log($"[{DevName}] Created output directory {OutDest}",, ConsoleColor.Cyan)
                FileIO.FileSystem.CreateDirectory(OutDest)
            End If
            clientStream = client.GetStream()
            IsConnected = True
            ' Start receiving data
            Await ReceiveDataAsync(_cancellationTokenSource.Token)

        Catch ex As Exception
            Program.Log($"[{DevName}] unable to connect to remote host.",, ConsoleColor.Red)
            IsConnected = False
        Finally
            Try
                Disconnect()
            Catch disconnectEx As Exception
                Program.Log($"[{DevName}] Error during disconnection: {disconnectEx.Message}",, ConsoleColor.Red)
                IsConnected = False
            End Try
            IsConnected = False
        End Try
    End Function

    ' Continuously receives data from the server
    Private Async Function ReceiveDataAsync(cancellationToken As CancellationToken) As Task
        Dim buffer(8192) As Byte ' Larger buffer for fewer ReadAsync calls, a bit over a full page.
        Dim dataBuilder As New StringBuilder()
        Dim lastReceivedTime As DateTime = DateTime.Now
        Dim inactivityTimeout As TimeSpan = TimeSpan.FromSeconds(5) ' Timeout period (5 seconds)

        Try
            While Not cancellationToken.IsCancellationRequested
                ' Check for data availability or cancellation
                If Not clientStream.DataAvailable Then
                    ' Wait for data to become available (with a small delay to avoid busy-waiting)
                    Await Task.Delay(100, cancellationToken) ' Block for 100ms and check again
                    clientStream.WriteByte(0)
                    ' If no data available and we are inactive for too long, process the current document
                    If DateTime.Now - lastReceivedTime > inactivityTimeout AndAlso dataBuilder.Length > 0 Then
                        ' Process the complete document if we have accumulated data and timeout has occurred
                        ProcessDocumentData(dataBuilder.ToString())
                        dataBuilder.Clear() ' Clear data for the next document
                        lastReceivedTime = DateTime.Now ' Reset the inactivity timer
                    End If
                Else
                    ' If data is available, read it
                    If Not Receiving Then
                        Receiving = True
                        If ((OS <> OSType.OS_RSTS) And (OS <> OSType.OS_NOS278)) Then
                            Program.Log($"[{DevName}] receiving data from remote host.",, ConsoleColor.Yellow)

                        Else
                            Program.Log($"[{DevName}] receiving data from low speed device. Sit back and relax.",, ConsoleColor.Yellow)
                        End If
                    End If
                    Dim recd As Integer = Await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                    If recd > 0 Then
                        ' Append received data to the data builder
                        Dim receivedPart As String = Encoding.UTF8.GetString(buffer, 0, recd)
                        dataBuilder.Append(receivedPart)
                        ' Update last received time to now
                        lastReceivedTime = DateTime.Now
                    End If
                End If

            End While
        Catch ex As OperationCanceledException
            Log("Receiving canceled.")
        Catch ex As Exception
            If ex.HResult = -2146232800 Then
                Log($"[{DevDest}] Disconnected from remote host.",, ConsoleColor.Red)
            Else
                Log($"Error receiving data: [{ex.HResult}] {ex.ToString}",, ConsoleColor.Red)
            End If
        End Try
    End Function

    Private Sub ProcessDocumentData(documentData As String)
        'This really sucks but Tandy XENIX needs 3 line feeds for the first page.  I hate that.
        'If OS = OSType.OS_TANDYXENIX Then
        'documentData = vbCrLf & vbCrLf & vbCrLf & documentData
        'End If
        ' Split the data into lines and process it
        JobNumber = JobNumber + 1
        Dim lines As New List(Of String)()
        Dim currentLine As New StringBuilder()
        Dim dataStream As String = $"{OutDest}/{DevName}--{Now.Ticks}--{JobNumber}.dst"
        Dim swriter As New StreamWriter(dataStream)
        ' Process each character in the full data
        ' CR = MOVE HEAD TO HOME POSITION (Useless in our situation)
        ' LF = ADVANCE ONE LINE. (we'll assume a CR is paired with it)
        ' FF = Advance to TOP  OF FORM (That'll be preserved but on it's own line)
        Dim ignoreChars As Integer = 0
        'Stop
        For Each c As Char In documentData
            ' Output to the dst file
            Dim o As String = c
            o = o.Replace(vbCr, "<CR>")
            o = o.Replace(vbLf, "<LF>")
            o = o.Replace(vbFormFeed, "<FF>")
            swriter.Write(o)
            Debug.Write(o)
            If ignoreChars = 0 Then
                Select Case c
                    Case vbCr
                        'Pass it into the string as printable data if it's VM370
                        'Go figure, MVS apparently uses overstrikes too.  Crazy!
                        If OS = OSType.OS_VM370 Then currentLine.Append(c)
                        If OS = OSType.OS_MVS38J Then currentLine.Append(c)
                        If OS = OSType.OS_MPE Then currentLine.Append(c)
                        If OS = OSType.OS_TANDYXENIX Then
                            currentLine.Append(vbCrLf)
                            lines.Add(currentLine.ToString())
                            currentLine.Clear()
                        End If
                    Case vbLf
                        ' New Line, return to 'home' position implied.
                        If currentLine.ToString.Trim.Length > 0 Then
                            lines.Add(currentLine.ToString())
                            currentLine.Clear()
                        Else
                            lines.Add(" ")
                            currentLine.Clear()
                        End If
                    Case vbFormFeed
                        'New Line, Form Feed, New Line
                        lines.Add(currentLine.ToString())
                        lines.Add(c.ToString())
                        currentLine.Clear()
                    Case ChrW(27)
                        ' Ignore this and the next two characters.
                        ignoreChars = 2
                    Case Else
                        ' Process anything as as data
                        currentLine.Append(c)
                End Select
            Else
                ignoreChars = ignoreChars - 1
                ' Ignoring esc sequences <esc> <byte> <byte>
            End If


        Next
        swriter.Flush()
        swriter.Close()

        ' Add any remaining line data
        If currentLine.Length > 0 Then
            lines.Add(currentLine.ToString().Replace(vbCrLf, vbLf))
        End If

        ' For some reason MPE ends jobs with <FF> then <CR>
        If (lines(lines.Count - 2) = vbFormFeed) And (lines(lines.Count - 1) = vbCr) Then
            Program.Log($"[{DevName}] Removing extra control characters after job completion for MPE")
            'This is end of job data for MPE, we don't need it or we'll
            'wind up with a blank page
            lines.RemoveAt(lines.Count - 1)
            lines.RemoveAt(lines.Count - 1) ' Yes we want to do it twice.
        End If

        ' Check to see if it completes with a FF.  (We do this anyway)
        If lines(lines.Count - 1) = vbFormFeed Then
            lines.RemoveAt(lines.Count - 1)   ' Just get rid of it.
        End If
        If lines.Count <= 10 Then
            File.Delete(dataStream)
            Program.Log($"[{DevName}] removed garbage datastream file.")
        End If
        ' Process the complete lines (document)
        If (lines.Any()) And (lines.Count > 9) Then
            currentDocument.AddRange(lines)
            ProcessDocument(currentDocument)
            Program.Log($"[{DevName}] Waiting for new document.")
            currentDocument.Clear() ' Clear for the next document
            Receiving = False
        End If
    End Sub


    ' Disconnects the client
    Public Sub Disconnect()
        Try
            _cancellationTokenSource?.Cancel()
            clientStream?.Close()
            client?.Close()
        Catch ex As Exception
            Program.Log($"[{DevName}] Error during disconnection: {ex.Message}",, ConsoleColor.Red)
        End Try
    End Sub

    Private Sub ProcessDocument(doc As List(Of String))
        ' Before we do anything to this, lets dump a diagnostics file
        ' This is the document BEFORE we do anything to clean it up.
        'Check if there's a trailing slash or backslash
        If OutDest.EndsWith("/") Or OutDest.EndsWith("\") Then
            OutDest = OutDest.Substring(0, OutDest.Length - 1)
        End If
        If Not FileIO.FileSystem.DirectoryExists(OutDest) Then
            Program.Log($"[{DevName}] Created output directory {OutDest}",, ConsoleColor.Yellow)
            FileIO.FileSystem.CreateDirectory(OutDest)
        End If
        If Not FileIO.FileSystem.DirectoryExists($"{OutDest}/data") Then
            Program.Log($"[{DevName}] Created data data directory {OutDest}/data",, ConsoleColor.Yellow)
            FileIO.FileSystem.CreateDirectory($"{OutDest}/data")
        End If
        Dim vals As (JobName As String, JobID As String, User As String) = ("", "", "")
        Receiving = False
        Program.Log($"[{DevName}] received {doc.Count} lines from remote host.",, ConsoleColor.Cyan)
        If doc.Count > 10 Then
            If (OS <> OSType.OS_RSTS) And ((OS > OSType.OS_MVS38J) And (OS <> OSType.OS_ZOS)) Then
                If OS <> OSType.OS_TANDYXENIX Then        'This is getting insanely stupid folks.
                    ' Lets try to eat any blank lines or form feeds before any real data
                    ' Don't do it for RSTS/E or MVS38J
                    Program.Log($"[{DevName}] Examining document information.")
                    Dim idx As Integer = 0
                    Do
                        doc(idx) = doc(idx).Trim
                        'Program.Log($"[{DevName}] '{doc(idx)}'")
                        ' loop through until we hit real data
                        If doc(idx) = vbFormFeed Then
                            doc(idx) = " " & vbLf
                            Exit Do ' The first form feed is probably junk, but it does start data.
                        End If
                        If doc(idx).Trim = "" Then
                            doc(idx) = " "
                        End If
                        If doc(idx).Trim <> "" Then Exit Do
                        idx = idx + 1
                    Loop
                End If
            End If
            Dim JobID, JobName, UserID As String
            Select Case OS
                Case OSType.OS_MVS38J
                    Program.Log($"[{DevName}] OS type os MVS 3.8J OS/VS2",, ConsoleColor.Green)
                    vals = MVS38J_ExtractJobInformation(doc)
                Case OSType.OS_VMS
                    Program.Log($"[{DevName}] OS type is VMS",, ConsoleColor.Green)
                    vals = VMS_ExtractJobInformation(doc)
                Case OSType.OS_MPE
                    Program.Log($"[{DevName}] OS type is MPE",, ConsoleColor.Green)
                    vals = MPE_ExtractJobInformation(doc)
                Case OSType.OS_RSTS
                    Program.Log($"[{DevName}] OS type is RSTS/E",, ConsoleColor.Green)
                    vals = RSTS_ExtractJobInformation(doc)
                Case OSType.OS_VM370
                    Program.Log($"[{DevName}] OS type is VM/370",, ConsoleColor.Green)
                    vals = VM370_ExtractJobInformation(doc)
                Case OSType.OS_NOS278
                    Program.Log($"[{DevName}] OS type is NOS 2.7.8 DTcyber",, ConsoleColor.Green)
                    vals = NOS278_ExtractJobInformation(doc)
                Case OSType.OS_VMSP
                    Program.Log($"[{DevName}] OS type is VM/SP",, ConsoleColor.Green)
                    vals = VMSP_ExtractJobInformation(doc)
                Case OSType.OS_TANDYXENIX
                    Program.Log($"[{DevName}] OS Type is TANDY XENIX",, ConsoleColor.Green)
                    vals = ("XENIX", Now.Ticks, "XENIX")
                Case OSType.OS_ZOS
                    Program.Log($"[{DevName}] OS Type is IBM Z/OS",, ConsoleColor.Green)
                    vals = ZOS_ExtractJobInformation(doc)
                Case Else
                    Program.Log($"[{DevName}] OS type is not known. [{OS}]",, ConsoleColor.Yellow)
                    vals = ("UNKNOWN", Now.Ticks.ToString, "OS UNKNOWN")
            End Select

            JobID = vals.JobID
            JobName = vals.JobName
            UserID = vals.User
            'Check if there's a trailing slash or backslash
            If OutDest.EndsWith("/") Or OutDest.EndsWith("\") Then
                OutDest = OutDest.Substring(0, OutDest.Length - 1)
            End If
            If Not FileIO.FileSystem.DirectoryExists(OutDest) Then
                Program.Log($"[{DevName}] Created output directory {OutDest}",, ConsoleColor.Yellow)
                FileIO.FileSystem.CreateDirectory(OutDest)
            End If
            Dim filename As String = $"{OutDest}/{DevName}-{UserID}-{JobID}-{JobName}_{JobNumber}.txt"
            Dim pdfName As String = $"{OutDest}/{DevName}-{UserID}-{JobID}-{JobName}_{JobNumber}.pdf"


            'Dim writer As New StreamWriter(filename)
            'For Each l As String In doc
            'l = l.Replace(vbCr, "<CR>")
            'l = l.Replace(vbLf, "<LF>")
            'l = l.Replace(vbFormFeed, "<FF>")
            'writer.WriteLine(l)
            'Next
            'writer.Flush()
            'writer.Close()

            CreatePDF(JobName, doc, pdfName)

        Else
            Log(String.Format("[{1}] Ignoring document with {0} lines as line garbage or banners.", doc.Count, DevName))
            Receiving = False
        End If
    End Sub

    Private Function ZOS_ExtractJobInformation(lines As List(Of String)) As (Jobname As String, JobID As String, User As String)
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log($"[{DevName}] resolving Z/OS job information.")
        For Each line As String In lines
            line = line.ToUpper.Trim
            If line.Trim <> "" Then
                Try
                    Dim parts As String() = line.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    If parts(0).StartsWith("*") Then

                        If parts(1) = "JOBID:" Then
                            jobId = parts(2)
                        End If
                        If parts(1) = "JOB" And parts(2) = "NAME:" Then
                            jobName = parts(3)
                        End If
                        If parts(1) = "USER" And parts(2) = "ID:" Then
                            user = parts(3)
                        End If
                    End If
                Catch ex As Exception

                End Try
            End If
        Next
        Return (jobName, jobId, user)
    End Function

    Private Function MVS38J_ExtractJobInformation(lines As List(Of String)) As (Jobname As String, JobID As String, User As String)
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log($"[{DevName}] resolving MVS 3.8J (OS/VS2) job information.")
        For Each line As String In lines
            line = line.ToUpper.Trim
            If line.Trim <> "" Then
                Try
                    Dim parts As String() = line.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                    If parts(0).StartsWith("****") Then
                        ' Usually a pretty good job information line for MVS.
                        ' But lets make sure we have END and JOB following it.
                        If (parts(1) = "END") And
                            ((parts(2) = "JOB") Or
                            (parts(2) = "TSU")) Then
                            jobId = parts(3)
                            jobName = parts(4)
                            user = parts((parts.Count - 7))
                        End If
                    End If
                Catch ex As Exception
                    'Log($"MVS38J EXTRACT: {ex.Message}")
                End Try
            End If
        Next
        Return (jobName, jobId, user)
    End Function

    Private Function VM370_ExtractJobInformation(lines As List(Of String)) As (Jobname As String, JobID As String, User As String)
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log($"[{DevName}] resolving VM/370 job information.")
        For Each line As String In lines
            line = line.ToUpper.Trim
            Dim parts As String() = line.Split(" ", StringSplitOptions.RemoveEmptyEntries)
            If parts.Count > 2 Then
                If ((parts(0) = "LOCATION") And (parts(1) = "USERID")) Then
                    Program.Log($"[{DevName}] Determined VM370 UserId: {parts(3)}")
                    user = parts(3)
                End If
                If ((parts(0) = "SPOOL") And (parts(1) = "FILE") And (parts(2) = "NAME")) Then
                    jobName = $"{parts(4)}.{parts(5)}"
                    Program.Log($"[{DevName}] Setting VM370 Jobname to {jobName}")
                End If
                If ((parts(0) = "SPOOL") And (parts(1) = "FILE") And (parts(2) = "ID")) Then
                    jobId = parts(3)
                    Program.Log($"[{DevName}] VM370 Spool ID {parts(3)}")
                End If
            End If
        Next
        Return (jobName, jobId, user)
    End Function

    Private Function VMSP_ExtractJobInformation(lines As List(Of String)) As (Jobname As String, JobID As String, User As String)
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log($"[{DevName}] resolving VM/SP job information.")
        For Each line As String In lines
            line = line.ToUpper.Trim
            Dim parts As String() = line.Split(" ", StringSplitOptions.RemoveEmptyEntries)
            If parts.Count > 4 Then
                If ((parts(2) = "USERID") And (parts(3) = "ORIGIN")) Then
                    Program.Log($"[{DevName}] Determined VM370 UserId: {parts(0)}")
                    user = parts(0)
                End If
                If (parts(2) = "FILENAME" And parts(3) = "FILETYPE") Then
                    jobName = $"{parts(0)}.{parts(1)}"
                    Program.Log($"[{DevName}] Setting Jobname to {jobName}")
                End If
                If (parts(2) = "SPOOLID") Then
                    jobId = parts(0)
                    Program.Log($"[{DevName}] Spool ID {jobId}")
                End If
            End If
        Next
        Return (jobName, jobId, user)
    End Function
    Private Function RSTS_ExtractJobInformation(lines As List(Of String)) As (Jobname As String, JobID As String, User As String)
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log($"[{DevName}] resolving RSTS/E job information.")
        For Each line As String In lines
            line = line.ToUpper
            ' First see if the word "ENTRY" appears
            If line.Contains("ENTRY") Then
                'Ok, ENTRY is here, is it in the 4th position?
                Dim parts As String() = line.Split(" ")
                If parts(4) = "ENTRY" Then
                    'Yessir it is! We may have a winner.
                    'or at least a very coincidental screw up where
                    'the word ENTRY appears in exactly the right spot.
                    'which face it, is absolutely going to happen sometime 
                    'but I'm not magic, so...
                    Dim jobParts As String() = parts(5).Split(":")
                    ' after entry is something like SYS$PRINT:[1,3]START.COM
                    ' so separate those.
                    Dim jobQueue As String = jobParts(0)
                    Dim jobData As String = jobParts(1)
                    'Now lets get the account number out for the user ID [proj,prog]
                    Dim EOU As Integer = jobData.IndexOf("]") + 1
                    ' The userID is going to be the left EOU characters
                    user = Left(jobData, EOU)
                    ' The job data is from EOU+1 to the end of the string
                    jobName = Right(jobData, (Len(jobData) - (EOU)))
                    ' Since RSTS doesn't give us the Job number, we'll put the rolling JobNumber here
                    ' with the current short date
                    jobId = $"{Now.ToShortDateString}({JobNumber})".Replace("/", "-")
                End If
            End If
        Next
        Return (jobName, jobId, user)
    End Function

    Private Function VMS_ExtractJobInformation(lines As List(Of String)) As (JobName As String, JobId As String, User As String)
        Dim GotInfo As Boolean = False
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log($"[{DevName}] resolving VMS job information.")
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
            jobId = Now.ToShortTimeString.Replace(":", "-")
            jobId = jobId.Replace("/", "-")
            user = DevName
        End If
        Return (jobName, jobId, user)
    End Function

    Private Function MPE_ExtractJobInformation(lines As List(Of String)) As (JobName As String, JobId As String, User As String)
        Dim GotInfo As Boolean = False
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log($"[{DevName}] resolving MPE job information.")
        ' As near as I can tell, the first (data) line of an MPE header page has all the
        ' job information we'll need.  If I'm wrong, somebody correct me.

        For Each line In lines
            line = line.ToUpper
            If line.Trim.Length > 0 Then
                ' If we hit a line that has data, the first one should be our payload
                Dim parts As String() = line.Split(" ")
                jobName = parts(0).Trim.Replace("#", "")
                jobName = jobName.Replace(vbNullChar, "")
                jobName = jobName.Replace(";", "")
                jobId = parts(1).Trim.Replace("#", "")
                jobId = jobId.Replace(";", "")
                user = parts(5).Replace(";", "")
                jobId = jobId.Replace("(", "")
                jobId = jobId.Replace(")", "")
                GotInfo = True
                Exit For
            End If
        Next
        If Not GotInfo Then
            jobName = "UNKNOWN"
            jobId = Now.ToShortTimeString.Replace(":", "-")
            jobId = jobId.Replace("/", "-")
            user = DevName
        End If
        Return (jobName, jobId, user)
    End Function
    Private Function NOS278_ExtractJobInformation(lines As List(Of String)) As (JobName As String, JobId As String, User As String)
        Dim GotInfo As Boolean = False
        Dim jobName As String = "UnknownJob"
        Dim jobId As String = "0000"
        Dim user As String = "UnknownUser"
        Log($"[{DevName}] resolving NOS 2.7.8 job information.")
        ' As near as I can tell, the first (data) line of an MPE header page has all the
        ' job information we'll need.  If I'm wrong, somebody correct me.

        For Each line In lines
            line = line.ToUpper
            If line.Trim.Length > 0 Then
                ' If we hit a line that has data, the first one should be our payload
                Dim parts As String() = line.Split(" ", StringSplitOptions.RemoveEmptyEntries)
                If parts(0) = "UJN" Then
                    ' We have the banner page with the job data
                    jobId = parts(2)
                    jobName = parts(5)
                    GotInfo = True
                End If
                If parts(0) = "CREATING" Then
                    user = parts(7)
                    If user.Trim = "" Then user = "CONSOLE" ' Jobs sent from the console don't have a user
                    GotInfo = True
                    Exit For  ' No need to keep looking
                End If
            End If

        Next

        If Not GotInfo Then
            jobName = "UNKNOWN"
            jobId = Now.ToShortTimeString.Replace(":", "-")
            jobId = jobId.Replace("/", "-")
            user = DevName
        End If
        Return (jobName, jobId, user)
    End Function
    Public Function CreatePDF(title As String, outList As List(Of String), filename As String) As String
        Try
            Program.Log($"{DevName} beginning PDF generation.")
            Dim firstline As Double = 0
            Dim linesPerPage As Integer = 66
            Dim StartLine = 0
            ' Initialize the PDF document
            Dim doc As New PdfSharp.Pdf.PdfDocument
            GlobalFontSettings.FontResolver = New ChainprinterFontResolver()
            doc.Info.Title = title
            Dim bkgrd As XImage
            If Orientation <= 1 Then
                ' Initialize background image (greenbar.jpg) to cover entire page
                bkgrd = XImage.FromFile("greenbar.jpg")
            Else
                bkgrd = XImage.FromFile("greenbar.jpg")
            End If

            If OS = OSType.OS_MVS38J Then
                Program.Log($"Setting page for MVS 3.8J")
                firstline = 45
                linesPerPage = 66
                StartLine = 5
            End If

            If OS = OSType.OS_RSTS Then
                Program.Log($"Setting page for RSTS/E")
                firstline = 27
                linesPerPage = 66
                StartLine = 0
            End If

            If OS = OSType.OS_MPE Then
                Program.Log($"Setting page for MPE")
                firstline = 25
                linesPerPage = 66
                StartLine = 3
            End If

            If OS = OSType.OS_NOS278 Then
                Program.Log($"Setting page for NOS 2.7.8")
                firstline = 25
                linesPerPage = 66
                StartLine = 3
            End If

            If OS = OSType.OS_VMS Then
                Program.Log($"Setting page for VMS")
                firstline = 25
                linesPerPage = 66
                StartLine = 3
            End If

            If OS = OSType.OS_VM370 Then
                Program.Log($"Setting page for VM/370CE")
                firstline = 7
                linesPerPage = 66
                StartLine = 2
            End If
            If OS = OSType.OS_VMSP Then
                Program.Log($"Setting page for VM/SP")
                firstline = 7
                linesPerPage = 66
                StartLine = 0
            End If

            If OS = OSType.OS_TANDYXENIX Then
                Program.Log($"Setting page for TANDY XENIX")
                firstline = 25
                linesPerPage = 66
                StartLine = 0
            End If

            If OS = OSType.OS_ZOS Then
                Program.Log($"Setting page for Z/OS")
                firstline = 25
                linesPerPage = 66
                StartLine = 5
            End If

            ' Define margins (1/2 inch for left and right margins)
            Dim leftMargin As Double = 30 ' 1/2 inch margin
            Dim rightMargin As Double = 30 ' 1/2 inch margin
            Dim availableWidth As Double ' Width for text after margins
            Dim fontSize As Double
            Dim font As XFont = Nothing ' Font will be initialized later
            Dim page As PdfPage = Nothing ' Page will be initialized later
            Dim gfx As XGraphics = Nothing ' gfx will be initialized later
            Dim y As Double = firstline ' Starting Y position for text
            Dim currentLine As Integer = StartLine

            ' Declare lineHeight for later use
            Dim lineHeight As Double

            ' Function to initialize a new page and reset layout
            Dim InitializeNewPage = Sub()
                                        ' Initialize the page
                                        page = doc.AddPage()
                                        If Orientation <= 1 Then
                                            page.Orientation = PdfSharp.PageOrientation.Landscape
                                        Else
                                            page.Orientation = PdfSharp.PageOrientation.Portrait
                                        End If

                                        ' Initialize graphics context for this page
                                        gfx = XGraphics.FromPdfPage(page)

                                        ' Draw background image to cover entire page
                                        If (Orientation = 0) Or (Orientation = 2) Then
                                            gfx.DrawImage(bkgrd, 0, 0, page.Width.Point, page.Height.Point)
                                        End If
                                        'DrawGreenBarBackground(gfx, availableWidth, page.Height.Point)
                                        ' Recalculate available width for text after margins
                                        availableWidth = page.Width.Point - leftMargin - rightMargin

                                        ' Initialize font with a temporary size
                                        font = New XFont("Chainprinter", 12)

                                        ' Calculate font size based on available width to fit 132 characters per line
                                        ' Measure the width of a single character (e.g., "W") at font size to estimate scaling
                                        Dim charWidth As Double = gfx.MeasureString("W", font).Width
                                        If Orientation <= 1 Then
                                            fontSize = availableWidth / (charWidth * 132) * 12 ' Scaling factor to fit 132 characters per line
                                        Else
                                            fontSize = availableWidth / (charWidth * 80) * 12
                                        End If

                                        ' Update font with the correct size
                                        font = New XFont("Chainprinter", fontSize)

                                        ' Calculate line height based on 66 lines per page
                                        Dim newHeight As Double = page.Height.Point / 66
                                        lineHeight = (newHeight)

                                        ' Reset text starting position
                                        y = firstline
                                        currentLine = StartLine
                                    End Sub

            ' Initialize the first page
            InitializeNewPage()

            ' Regex to remove any non-printable characters, and explicitly handle LF (line feed)
            Dim regex As New System.Text.RegularExpressions.Regex("[^\x20-\x7E\x0C\x0D\u00A0]", RegexOptions.Compiled)
            Dim mperegex As New System.Text.RegularExpressions.Regex("[^\x20-\x7E\x0C]", RegexOptions.Compiled)
            If outList(0).Trim = "" Then
                outList.RemoveAt(0)       ' In case it starts out with a LF/CR or otherwise empty line.
            End If
            ' Process each line from the output list
            For Each line As String In outList
                Try
                    ' Remove non-printable characters (same as before)
                    If ((OS <> OSType.OS_RSTS) And (OS <> OSType.OS_MPE)) Then
                        line = regex.Replace(line, String.Empty) ' Remove non-printable characters
                        ' Replace empty lines with a space
                        line = If(String.IsNullOrEmpty(line), " ", line)
                    End If

                    ' Handle Form Feed (FF) and create a new page if necessary
                    If line.StartsWith(vbFormFeed) Then
                        InitializeNewPage()
                    End If

                    ' Create a new page if current page is full
                    If currentLine >= linesPerPage Then
                        InitializeNewPage()
                    End If

                    ' Remove FF characters before drawing
                    If line <> vbFormFeed Then
                        'If the line is just a FF then don't print it, we just created a new page
                        ' Check if the line contains embedded CRs (not at the end)
                        If Orientation > 1 Then
                            If line.Length > 80 Then line = line.Substring(0, 80)    ' Cut off the line
                        Else
                            If line.Length > 132 Then line = line.Substring(0, 132)   ' Cut off the line
                        End If

                        If line.Contains(Chr(13)) Then
                                ' Split the line by CR characters (we will handle the overstrike behavior here)
                                Dim segments As List(Of String) = line.Split(Chr(13)).ToList()

                                ' Track the current starting position for drawing
                                Dim currentX As Double = leftMargin
                                If (segments.Count > 1) And (segments(1).Trim <> "") Then
                                    'Program.Log($"Segment count is {segments.Count}")
                                    ' Iterate through the segments
                                    Dim segIdx As Integer = 0
                                    Dim myOffset As Double = 0      ' Set if we want to offset overstrikes.
                                    For Each segment As String In segments
                                        'Program.Log($"{segment}")
                                        If Not String.IsNullOrEmpty(segment) Then
                                            ' Draw the base text segment at the starting position
                                            gfx.DrawString(segment, font, XBrushes.Black, New XRect(currentX, y, availableWidth, page.Height.Point), XStringFormats.TopLeft)
                                            If segIdx > 0 Then
                                                ' Overprint the segment (with a 'myoffset' horizontal offset) by printing it again
                                                gfx.DrawString(segment, font, XBrushes.Black, New XRect(currentX, y, availableWidth, page.Height.Point), XStringFormats.TopLeft)
                                            End If
                                            segIdx += 1
                                        End If
                                    Next
                                Else
                                    'Program.Log($"{segments.Count} segment 2 is [{segments(1)}]")
                                    ' The second segment is blank, so just write it as usual.
                                    gfx.DrawString(line, font, XBrushes.Black, New XRect(leftMargin, y, availableWidth, page.Height.Point), XStringFormats.TopLeft)
                                End If
                            Else
                                ' If there are no CRs, just print the line normally
                                gfx.DrawString(line, font, XBrushes.Black, New XRect(leftMargin, y, availableWidth, page.Height.Point), XStringFormats.TopLeft)
                            End If

                        ' Move down for the next line
                        y += lineHeight
                            currentLine += 1
                        End If
                Catch ex As Exception
                    ' Handle errors (e.g., invalid characters or drawing issues)
                End Try
            Next

            ' Save the document and return the output file
            'Program.Log($"[{DevName}] skipping output file to save space on device.",, ConsoleColor.White)
            Dim outputFile As String = filename
            Log($"Wrote {doc.PageCount} pages for {title} to {outputFile}.",, ConsoleColor.Green)
            doc.Save(outputFile)
            doc.Close()

            ' Ensure we properly end the function
            Return outputFile
        Catch ex As Exception
            Program.Log($"Error: {ex.Message}",, ConsoleColor.Red)
        End Try
        Return "" ' Just to quiet down the IDE

    End Function

    Public Sub Reprint(jobname As String)

        Program.Log($"Reprinting job {jobname} for device {DevName}",, ConsoleColor.Green)
        'Dim dstNameFormat = $"{OutDest}/{DevName}-{Now.DayOfYear}-{JobNumber}.dst"
        If Not jobname.EndsWith(".dst") Then
            jobname = jobname & ".dst"
        End If
        Dim fname As String = OutDest & "/" & jobname
        If Not FileIO.FileSystem.FileExists(fname) Then
            Program.Log($"[{DevName}] Job {fname} does not exist in {OutDest}.",, ConsoleColor.Red)
            Return
        End If
        Dim dataBuilder As New StringBuilder()
        Dim inFile As New StreamReader(fname)
        dataBuilder.Append(inFile.ReadToEnd)
        ProcessDocumentData(dataBuilder.ToString)
        dataBuilder.Clear()
        inFile.Close()

    End Sub

    ' EXPERIMENTAL

    Public Sub DrawGreenBarBackground(ByVal gfx As XGraphics, ByVal pageWidth As Double, ByVal pageHeight As Double)
        ' Define colors for the stripes
        Dim whiteColor As XColor = XColors.White
        Dim greenColor As XColor = XColor.FromArgb(232, 255, 232) ' Light green

        ' Define stripe heights (in points, approximated from image analysis)
        Dim whiteStripeHeight As Double = 49 * 72 / 96 ' Convert 49 pixels to points (assuming 96 DPI)
        Dim greenStripeHeight As Double = 16 * 72 / 96 ' Convert 16 pixels to points (assuming 96 DPI)

        ' Start drawing from the top of the page
        Dim currentY As Double = 0

        ' Loop to fill the page with alternating stripes
        While currentY < pageHeight
            ' Draw white stripe
            gfx.DrawRectangle(New XSolidBrush(whiteColor), 0, currentY, pageWidth, whiteStripeHeight)
            currentY += whiteStripeHeight

            ' Check if we're still within the page height
            If currentY >= pageHeight Then Exit While

            ' Draw green stripe
            gfx.DrawRectangle(New XSolidBrush(greenColor), 0, currentY, pageWidth, greenStripeHeight)
            currentY += greenStripeHeight
        End While

        ' Optional: Draw vertical perforation marks on the left and right edges
        Dim dotSpacing As Double = 10 * 72 / 96 ' Approximate spacing between dots in points
        Dim dotDiameter As Double = 2 * 72 / 96 ' Approximate dot size in points
        Dim dotColor As XColor = XColors.Gray

        ' Left edge perforations
        Dim currentDotY As Double = 0
        While currentDotY < pageHeight
            gfx.DrawEllipse(New XSolidBrush(dotColor), 0, currentDotY, dotDiameter, dotDiameter)
            currentDotY += dotSpacing
        End While

        ' Right edge perforations
        currentDotY = 0
        Dim rightEdgeX As Double = pageWidth - dotDiameter
        While currentDotY < pageHeight
            gfx.DrawEllipse(New XSolidBrush(dotColor), rightEdgeX, currentDotY, dotDiameter, dotDiameter)
            currentDotY += dotSpacing
        End While
    End Sub

    Public Sub OldDrawBackgroundTemplate(gfx As XGraphics, drawBG As Boolean, dark As XColor, light As XColor)
        Const feedHoleRadius As Double = 5.5
        Dim pageWidth As Double = gfx.PageSize.Width
        Dim pageHeight As Double = gfx.PageSize.Height

        ' Alignment fiducial - Draw this first to avoid being overwritten
        If drawBG Then
            Dim darkPen As New XPen(dark, 0.7)
            gfx.DrawLine(darkPen, 20, 54 - feedHoleRadius * 2, 20, 54 + feedHoleRadius * 2) ' Vertical line
            gfx.DrawLine(darkPen, 20 - feedHoleRadius * 2, 54, 20 + feedHoleRadius * 2, 54) ' Horizontal line
            darkPen.Width = 1.5
            gfx.DrawEllipse(darkPen, 20 - (feedHoleRadius + 0.6), 54 - (feedHoleRadius + 0.6), (feedHoleRadius + 0.6) * 2, (feedHoleRadius + 0.6) * 2) ' Circle
        End If

        ' Tractor feed holes - Draw circles at specified positions
        Dim grayPen As New XPen(XColors.LightGray, 0.75)
        Dim lightGrayBrush As New XSolidBrush(XColor.FromArgb(230, 230, 230))
        ' Top holes
        gfx.DrawEllipse(grayPen, lightGrayBrush, 20 - (feedHoleRadius + 1), 18 - (feedHoleRadius + 1), (feedHoleRadius + 1) * 2, (feedHoleRadius + 1) * 2)
        gfx.DrawEllipse(grayPen, lightGrayBrush, pageWidth - 20 - (feedHoleRadius + 1), 18 - (feedHoleRadius + 1), (feedHoleRadius + 1) * 2, (feedHoleRadius + 1) * 2)
        ' Bottom holes
        Dim y As Double
        For i As Integer = 1 To 21
            y = 18 + 18 * 2 * i
            gfx.DrawEllipse(grayPen, lightGrayBrush, 20 - feedHoleRadius, y - feedHoleRadius, feedHoleRadius * 2, feedHoleRadius * 2)
            gfx.DrawEllipse(grayPen, lightGrayBrush, pageWidth - 20 - feedHoleRadius, y - feedHoleRadius, feedHoleRadius * 2, feedHoleRadius * 2)
        Next

        If Not drawBG Then Exit Sub

        ' Print area alignment arrows (left and right sides)
        gfx.DrawPolygon(New XPen(XColors.Transparent), New XSolidBrush(light), New XPoint() {
            New XPoint(40 + 2, 72 - 11),
            New XPoint(40 + 2 + 5, 72),
            New XPoint(40 + 2 + 10, 72 - 11)
        }, XFillMode.Winding)

        gfx.DrawPolygon(New XPen(XColors.Transparent), New XSolidBrush(light), New XPoint() {
            New XPoint(pageWidth - 40 - 2, 72 - 11),
            New XPoint(pageWidth - 40 - 2 - 5, 72),
            New XPoint(pageWidth - 40 - 2 - 10, 72 - 11)
        }, XFillMode.Winding)

        ' Green bars (faint) - Draw faint green bars between vertical rules, without overwriting other elements
        Dim barHeight As Double = 36
        Dim barCount As Integer = CInt(pageHeight / barHeight) + 1
        For i As Integer = 0 To barCount - 1
            Dim yPos As Double = 72 + (i * barHeight)

            ' Alternate faint green with white for the bars
            Dim brush As XSolidBrush
            If i Mod 2 = 0 Then
                brush = New XSolidBrush(XColor.FromArgb(220, 255, 220)) ' Faint pale green
            Else
                brush = New XSolidBrush(XColor.FromArgb(255, 255, 255)) ' White
            End If

            ' Draw a faint green bar only between vertical rules (adjusted width)
            gfx.DrawRectangle(brush, 40, yPos, pageWidth - 80, barHeight) ' Adjusted to leave space for vertical lines
        Next

        ' Draw vertical lines - Left and right side
        Dim darkPenVertical As New XPen(dark, 0.5)
        gfx.DrawLine(darkPenVertical, 30, 72, 30, pageHeight - 1) ' Left vertical line
        gfx.DrawLine(darkPenVertical, 40, 72, 40, pageHeight - 1) ' Left vertical line
        gfx.DrawLine(darkPenVertical, pageWidth - 30, 72, pageWidth - 30, pageHeight - 1) ' Right vertical line
        gfx.DrawLine(darkPenVertical, pageWidth - 40, 72, pageWidth - 40, pageHeight - 1) ' Right vertical line

        ' Left margin numbers
        Dim font As New XFont("C:\Windows\Fonts\segoeui.ttf", 7)
        gfx.DrawString("1", font, New XSolidBrush(dark), New XPoint(30, 72))
        For i As Integer = 1 To 60
            gfx.DrawString((i + 1).ToString(), font, New XSolidBrush(dark), New XPoint(30, 72 + (i * 12)))
        Next

        ' Right margin numbers
        gfx.DrawString("1", font, New XSolidBrush(dark), New XPoint(pageWidth - 40, 72))
        For i As Integer = 1 To 80
            gfx.DrawString((i + 1).ToString(), font, New XSolidBrush(dark), New XPoint(pageWidth - 40, 72 + (i * 9)))
        Next
    End Sub
End Class
