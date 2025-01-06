Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.IO
Imports System.Net.Http
Imports System.Net.Sockets
Imports System.Runtime
Imports System.Runtime.CompilerServices
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Json
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading
Imports PdfSharp.Drawing
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
        Await StartAsync()  ' Connects to the server and starts receiving data
    End Sub
    Public Async Function StartAsync() As Task
        client = New TcpClient()
        _cancellationTokenSource = New CancellationTokenSource()

        Try
            Program.Log("Attempting to connect...")
            Await client.ConnectAsync(remoteHost, remotePort)
            Program.Log("Connection successful.")
            clientStream = client.GetStream()
            IsConnected = True
            ' Start receiving data
            Await ReceiveDataAsync(_cancellationTokenSource.Token)
        Catch ex As Exception
            Program.Log($"[{DevName}] {ex.GetType().Name} - {ex.Message}")
            IsConnected = False
        Finally
            Try
                Disconnect()
            Catch disconnectEx As Exception
                Program.Log($"Error during disconnection: {disconnectEx.Message}")
            End Try
            IsConnected = False
        End Try
    End Function

    Private Async Function TaskSleepAsync(seconds As Integer) As Task
        Await Task.Delay(seconds * 1000)
    End Function

    Private Function DataIsAvailable() As Boolean
        'Program.Log($"{DevName} {clientStream.DataAvailable.ToString}")
        Return clientStream.DataAvailable
    End Function
    Private Async Function WaitForMoreDataAsync(dataAvailableCondition As Func(Of Boolean), timeoutMilliseconds As Integer, checkIntervalMilliseconds As Integer) As Task(Of Boolean)
        Dim elapsed As Integer = 0

        While elapsed < timeoutMilliseconds
            If dataAvailableCondition() Then
                Return True ' Data is available, exit early.
            End If

            Await Task.Delay(checkIntervalMilliseconds) ' Wait before checking again.
            elapsed += checkIntervalMilliseconds
        End While

        Return False ' Timed out waiting for data.
    End Function

    ' Continuously receives data from the server
    Private Async Function ReceiveDataAsync(cancellationToken As CancellationToken) As Task
        Dim buffer(4096) As Byte ' Larger buffer for fewer ReadAsync calls
        Dim dataBuilder As New StringBuilder()
        Dim lastReceivedTime As DateTime = DateTime.Now
        Dim inactivityTimeout As TimeSpan = TimeSpan.FromSeconds(2) ' Timeout period (2 seconds)

        Try
            While Not cancellationToken.IsCancellationRequested
                ' Check for data availability or cancellation
                If Not clientStream.DataAvailable Then
                    ' Wait for data to become available (with a small delay to avoid busy-waiting)
                    Await Task.Delay(100, cancellationToken) ' Block for 100ms and check again
                    ' If no data available and we are inactive for too long, process the current document
                    If DateTime.Now - lastReceivedTime > inactivityTimeout AndAlso dataBuilder.Length > 0 Then
                        ' Process the complete document if we have accumulated data and timeout has occurred
                        ProcessDocumentData(dataBuilder.ToString())
                        dataBuilder.Clear() ' Clear data for the next document
                        lastReceivedTime = DateTime.Now ' Reset the inactivity timer
                    End If
                Else
                    ' If data is available, read it
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
            Log($"Error receiving data: {ex.Message}")
        End Try
    End Function

    Private Sub ProcessDocumentData(documentData As String)
        ' Split the data into lines and process it
        Dim lines As New List(Of String)()
        Dim currentLine As New StringBuilder()

        ' Process each character in the full data
        For Each c As Char In documentData
            If c = vbCr Then
                lines.Add(currentLine.ToString())
                currentLine.Clear()
            ElseIf c = Chr(12) Then ' FF character (form feed)
                lines.Add(currentLine.ToString())
                lines.Add(c.ToString())
                currentLine.Clear()
            Else
                currentLine.Append(c)
            End If
        Next

        ' Add any remaining line data
        If currentLine.Length > 0 Then
            lines.Add(currentLine.ToString())
        End If

        ' Process the complete lines (document)
        If lines.Any() Then
            currentDocument.AddRange(lines)
            ProcessDocument(currentDocument)
            currentDocument.Clear() ' Clear for the next document
        End If
    End Sub




    ' THIS IS AN OLD VERSION OF RECEIVEDATAASYNC.  IT IS HERE FOR REFERENCE ONLY AND WILL BE REMOVED
    ' DO NOT USE THIS FUNCTION.

    ' If you use this function, CPU usage will spike dramatically.
    Private Async Function OldReceiveDataAsync(cancellationToken As CancellationToken) As Task
        Dim lastLineReceived As DateTime = Now()
        Dim buffer(140) As Byte
        Dim dataBuilder As New StringBuilder()

        Try
            While Not cancellationToken.IsCancellationRequested
                While clientStream.DataAvailable = True
                    If Not Receiving Then
                        Receiving = True
                        Program.Log($"[{DevName}] receiving data from remote host.")
                    End If
                    Dim recd As Integer = Await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                    'Log(recd & " Bytes Received.")
                    Dim receivedPart As String = Encoding.UTF8.GetString(buffer, 0, recd)
                    dataBuilder.Append(receivedPart)
                    ' Wait 3 seconds to see if more data arrives.
                    If clientStream.DataAvailable Then
                        'No need to wait
                    Else
                        Await TaskSleepAsync(1)
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
                    ProcessDocument(currentDocument)
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

    Private Sub ProcessDocument(doc As List(Of String))
        Dim vals As (JobName As String, JobID As String, User As String) = ("", "", "")
        If doc.Count > 5 Then

            Receiving = False
            Program.Log($"[{DevName}] received {doc.Count} lines from remote host.")
            ' Lets try to eat any blank lines or form feeds before any real data
            Dim idx As Integer = 0
            Do
                ' loop through until we hit real data
                If doc(idx).Trim = "" Then
                    doc(idx) = ""
                    Program.Log($"[{DevName}] Removing leading blank line from document.")
                End If
                If doc(idx).Trim = vbFormFeed Then
                    doc(idx) = ""
                    Program.Log($"[{DevName}] Removing unneeded FF from document.")
                End If

                If doc(idx).Trim <> "" Then Exit Do
                idx = idx + 1
            Loop
            Dim JobID, JobName, UserID As String
            Select Case OS
                Case 1
                    Program.Log($"[{DevName}] OS type is VMS")
                    vals = VMS_ExtractJobInformation(doc)
                Case 3
                    Program.Log($"[{DevName}] OS type is RSTS/E")
                    vals = RSTS_ExtractJobInformation(doc)
                Case Else
                    Program.Log($"[{DevName}] OS type is not known.")
                    vals = ("UNKNOWN", Now.Ticks.ToString, "OS UNKNOWN")
            End Select

            JobID = vals.JobID
            JobName = vals.JobName
            UserID = vals.User
            Dim fnFmt As String = "PRT-{0}-{1}-{2}.txt"
            Dim fnPdf As String = "PRT-{0}-{1}-{2}.pdf"
            Dim filename As String = String.Format(fnFmt, UserID, JobID, JobName)
            Dim pdfName As String = String.Format(fnPdf, UserID, JobID, JobName)
            Dim writer As New StreamWriter(filename)
            For Each l As String In doc
                writer.Write(l)
            Next
            writer.Flush()
            writer.Close()
            CreatePDF(JobName, doc, pdfName)

        Else
            Log(String.Format("[{1}] Ignoring document with {0} lines as line garbage or banners.", doc.Count, DevName))
            Receiving = False
        End If
    End Sub

    Private Function IsTrailerPage(lines As String()) As Boolean
        ' Check if the trailer contains "COMPLETED ON" indicating job completion
        Return lines.Any(Function(line) line.Contains("COMPLETED ON"))
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
                Program.Log("Found ENTRY")
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
                    ' Since RSTS doesn't give us the Job number, we'll put the queue name here
                    jobId = jobQueue
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

    Public Function CreatePDF(title As String, outList As List(Of String), filename As String) As String
        Dim firstline As Double = 0
        Dim linesPerPage As Integer = 66
        Dim StartLine = 0
        ' Initialize the PDF document
        Dim doc As New PdfSharp.Pdf.PdfDocument
        GlobalFontSettings.FontResolver = New ChainprinterFontResolver()
        doc.Info.Title = title

        ' Initialize background image (greenbar.jpg) to cover entire page
        Dim bkgrd As XImage = XImage.FromFile("greenbar.jpg")

        If OS = 3 Then
            firstline = 27
            linesPerPage = 66
            StartLine = 3
        End If

        If OS = 1 Then
            firstline = 27
            linesPerPage = 66
            StartLine = 3
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
                                    page.Orientation = PdfSharp.PageOrientation.Landscape

                                    ' Initialize graphics context for this page
                                    gfx = XGraphics.FromPdfPage(page)

                                    ' Draw background image to cover entire page
                                    gfx.DrawImage(bkgrd, 0, 0, page.Width.Point, page.Height.Point)

                                    ' Recalculate available width for text after margins
                                    availableWidth = page.Width.Point - leftMargin - rightMargin

                                    ' Initialize font with a temporary size
                                    font = New XFont("Chainprinter", 12)

                                    ' Calculate font size based on available width to fit 132 characters per line
                                    ' Measure the width of a single character (e.g., "W") at font size to estimate scaling
                                    Dim charWidth As Double = gfx.MeasureString("W", font).Width
                                    fontSize = availableWidth / (charWidth * 132) * 12 ' Scaling factor to fit 132 characters per line

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
        If outList(0).Trim = "" Then
            outList.RemoveAt(0)       ' In case it starts out with a LF/CR or otherwise empty line.
        End If
        ' Process each line from the output list
        For Each line As String In outList
            ' Remove Line Feed (LF) characters explicitly replace with vbCR
            line = line.Replace(vbLf, vbCr)
            line = regex.Replace(line, "") ' Remove non-printable characters

            ' Replace empty lines with a space
            line = If(String.IsNullOrEmpty(line), " ", line)

            ' Handle form feed and create new pages as needed
            If line(0) = vbFormFeed Then
                InitializeNewPage()
            End If

            ' Create a new page if current page is full (adjust according to page layout)
            If currentLine = (linesPerPage - 1) Then ' Max lines per page
                InitializeNewPage()
            End If

            ' Finally remove the FF character from the printable line.
            line = line.Replace(vbFormFeed, String.Empty)

            ' Draw the line of text
            gfx.DrawString(line, font, XBrushes.Black, New XRect(leftMargin, y, availableWidth, page.Height.Point), XStringFormats.TopLeft)

            ' Move down for next line
            y += lineHeight ' Adjust to maintain 66 lines per page
            currentLine += 1
        Next

        ' Save the document and return the output file
        Dim outputFile As String = filename
        Log($"Wrote {doc.PageCount} pages for {title} to {outputFile}.")
        doc.Save(outputFile)
        doc.Close()

        ' Ensure we properly end the function
        Return outputFile
    End Function



End Class
