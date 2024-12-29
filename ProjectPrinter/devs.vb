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

    Private Sub TaskSleep(seconds As Integer)
        Dim delF As String = "[{0}] Data received, waiting {1} second for line to resume or settle."
        'Log(String.Format(delF, DevName, seconds))
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
        If doc.Count > 4 Then
            ' For now we're just going to save it to a file.  Ultimately really process it
            Dim JobID, JobName, UserID As String
            Dim vals = VMS_ExtractJobInformation(doc)
            JobID = vals.JobId
            JobName = vals.JobName
            UserID = vals.User
            Dim fnFmt As String = "PRT-{0}-{1}-{2}.txt"
            Dim fnPdf As String = "PRT-{0}-{1}-{2}.pdf"
            Dim filename As String = String.Format(fnFmt, UserID, JobID, JobName)
            Dim pdfName As String = String.Format(fnPdf, UserID, JobID, JobName)
            CreatePDF(JobName, doc, pdfName)
        Else
            Log(String.Format("[{1}] Ignoring document with {0} lines as line garbage or banners.", doc.Count, DevName))
        End If
    End Sub

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
            jobId = Now.ToShortTimeString.Replace(":", "-")
            jobId = jobId.Replace("/", "-")
            user = DevName
        End If
        Return (jobName, jobId, user)
    End Function

    Public Function CreatePDF(title As String, outList As List(Of String), filename As String) As String
        Dim firstline As Double = 55
        ' Initialize the PDF document
        Dim doc As New PdfSharp.Pdf.PdfDocument
        GlobalFontSettings.FontResolver = New ChainprinterFontResolver()
        doc.Info.Title = title

        ' Initialize background image (greenbar.jpg) to cover entire page
        Dim bkgrd As XImage = XImage.FromFile("greenbar.jpg")

        ' Define margins (1/2 inch for left and right margins)
        Dim leftMargin As Double = 30 ' 1/2 inch margin
        Dim rightMargin As Double = 30 ' 1/2 inch margin
        Dim availableWidth As Double ' Width for text after margins
        Dim fontSize As Double
        Dim font As XFont = Nothing ' Font will be initialized later
        Dim page As PdfPage = Nothing ' Page will be initialized later
        Dim gfx As XGraphics = Nothing ' gfx will be initialized later
        Dim y As Double = firstline ' Starting Y position for text
        Dim currentLine As Integer = 0

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
                                    currentLine = 0
                                End Sub

        ' Initialize the first page
        InitializeNewPage()

        ' Regex to remove any non-printable characters, and explicitly handle LF (line feed)
        Dim regex As New System.Text.RegularExpressions.Regex("[^\x20-\x7E\x0C\x0D\u00A0]", RegexOptions.Compiled)

        ' Process each line from the output list
        For Each line As String In outList
            ' Remove Line Feed (LF) characters explicitly
            line = line.Replace(vbLf, String.Empty)
            line = regex.Replace(line, "") ' Remove non-printable characters

            ' Replace empty lines with a space
            line = If(String.IsNullOrEmpty(line), " ", line)

            ' Handle form feed and create new pages as needed
            If line(0) = vbFormFeed Then
                InitializeNewPage()
            End If

            ' Create a new page if current page is full (adjust according to page layout)
            If currentLine > 0 AndAlso currentLine Mod 65 = 0 Then ' Max lines per page
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
        Log($"Wrote {doc.PageCount} pages for {title} to {outputFile}.{vbCrLf}")
        doc.Save(outputFile)
        doc.Close()

        ' Ensure we properly end the function
        Return outputFile
    End Function



End Class
