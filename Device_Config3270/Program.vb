Imports System
Imports System.IO
Imports System.Collections.Generic
Imports TN3270Framework

Module Program
    Private devList As New List(Of Device)
    Private configFile As String = "devices.dat"

    Sub Main(args As String())
        Dim port As Integer = 3270

        Dim i As Integer = 0
        While i < args.Length
            If args(i).ToLower() = "-p" AndAlso i + 1 < args.Length Then
                port = Val(args(i + 1))
                i += 2
            Else
                configFile = args(i)
                i += 1
            End If
        End While

        LoadDevices()

        Console.WriteLine($"Starting TN3270 Device Configuration Server on port {port}...")
        Dim server As New TN3270Listener(port)
        AddHandler server.ConnectionReceived, AddressOf OnConnection

        server.Start()
        Console.WriteLine("Press ENTER to stop.")
        Console.ReadLine()
        server.StopListening()
    End Sub

    Sub OnConnection(sender As Object, e As TN3270ConnectionEventArgs)
        Console.WriteLine($"[ConfigServer] New connection from {e.RemoteEndPoint}")
        Dim session = e.Session
        Dim stateManager As New SessionStateManager(session, devList, configFile)
        AddHandler session.NegotiationComplete, AddressOf stateManager.InitSession
        AddHandler session.AidKeyReceived, AddressOf stateManager.HandleInput
        AddHandler session.Disconnected, Sub() Console.WriteLine($"[ConfigServer] Session {e.RemoteEndPoint} disconnected.")
        session.StartNegotiation()
    End Sub

    Sub LoadDevices()
        If Not File.Exists(configFile) Then Return

        devList.Clear()
        Try
            Using rdr As New StreamReader(configFile)
                While Not rdr.EndOfStream
                    Dim line = rdr.ReadLine()
                    If String.IsNullOrWhiteSpace(line) Then Continue While
                    Dim parts = line.Split("||", StringSplitOptions.TrimEntries)
                    If parts.Length = 10 Then
                        Dim d As New Device()
                        d.Name = parts(0)
                        d.Description = parts(1)
                        d.Type = Val(parts(2))
                        d.ConnType = Val(parts(3))
                        d.Destination = parts(4)
                        d.OS = Val(parts(5))
                        d.Auto = (parts(6) = "True")
                        d.PDF = (parts(7) = "True")
                        d.Orientation = Val(parts(8))
                        d.OutDest = parts(9)
                        devList.Add(d)
                    End If
                End While
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error loading devices: {ex.Message}")
        End Try
    End Sub
End Module

Public Class Device
    Public Property Name As String = ""
    Public Property Description As String = ""
    Public Property Type As Integer = 0
    Public Property ConnType As Integer = 0
    Public Property Destination As String = ""
    Public Property OS As Integer = 0
    Public Property Auto As Boolean = False
    Public Property PDF As Boolean = False
    Public Property Orientation As Integer = 0
    Public Property OutDest As String = ""
End Class

Public Enum ScreenMode
    Menu
    Edit
    ConfirmDelete
End Enum

Public Class SessionStateManager
    Private _session As TN3270Session
    Private _devList As List(Of Device)
    Private _configFile As String

    Private _mode As ScreenMode = ScreenMode.Menu
    Private _startIndex As Integer = 0
    Private _editingIndex As Integer = -1
    Private _statusMsg As String = ""
    Private _statusColor As Byte = TN3270Color.White
    Private _hasUnsavedChanges As Boolean = False

    Public Sub New(session As TN3270Session, devList As List(Of Device), configFile As String)
        _session = session
        _devList = devList
        _configFile = configFile
    End Sub

    Public Sub InitSession()
        ShowMenu()
    End Sub

    Public Sub HandleInput(sender As Object, e As AidKeyEventArgs)
        _statusMsg = ""
        _statusColor = TN3270Color.White

        Select Case _mode
            Case ScreenMode.Menu
                ProcessMenuInput(e)
            Case ScreenMode.Edit
                ProcessEditInput(e)
            Case ScreenMode.ConfirmDelete
                ProcessDeleteInput(e)
        End Select
    End Sub

    Private Sub ProcessMenuInput(e As AidKeyEventArgs)
        ' Handle Paging via PF Keys (PF7=Up, PF8=Down)
        If e.AidKey = &HF7 Then ' PF7
            _startIndex = Math.Max(0, _startIndex - 5)
            ShowMenu()
            Return
        ElseIf e.AidKey = &HF8 Then ' PF8
            If _startIndex + 5 < _devList.Count Then _startIndex += 5
            ShowMenu()
            Return
        ElseIf e.AidKey = &HF3 Then ' PF3 Exit
            If _hasUnsavedChanges Then
                SaveDevices()
                ' Optional: Add a log or notify the console that an auto-save occurred
                Console.WriteLine($"[ConfigServer] Auto-save triggered by PF3 exit from {_session.Client.Client.RemoteEndPoint}")
            End If
            _session.WriteText(23, 2, "SESSION TERMINATED. AUTO-SAVE COMPLETE.", TN3270Color.Red)
            _session.ShowScreen()
            _session.Close()
            Return
        End If

        ' Only process Enter for commands
        If e.AidKey <> &H7D Then
            ShowMenu()
            Return
        End If

        Dim cmd = _session.GetFieldValue("txtCmd")?.ToUpper().Trim()
        If String.IsNullOrEmpty(cmd) Then
            ShowMenu()
            Return
        End If

        If cmd = "ADD" Then
            Dim newDev As New Device()
            _devList.Add(newDev)
            _editingIndex = _devList.Count - 1
            _mode = ScreenMode.Edit
            _hasUnsavedChanges = True
            ShowEdit()
        ElseIf cmd = "SAVE" Then
            SaveDevices()
            _hasUnsavedChanges = False
            _statusMsg = "Devices saved successfully."
            _statusColor = TN3270Color.Green
            ShowMenu()
        ElseIf cmd = "EXIT" Then
            _session.WriteText(23, 1, "Connection closed by server.", TN3270Color.Red)
            _session.ShowScreen()
            _session.Close()
        ElseIf cmd.StartsWith("DELETE ") Then
            Dim idStr = cmd.Substring(7).Trim()
            Dim id As Integer
            If Integer.TryParse(idStr, id) AndAlso id > 0 AndAlso id <= _devList.Count Then
                _editingIndex = id - 1
                _mode = ScreenMode.ConfirmDelete
                ShowConfirmDelete()
            Else
                _statusMsg = "Invalid Device ID for Delete."
                _statusColor = TN3270Color.Red
                ShowMenu()
            End If
        Else
            ' Check if it's just a number to edit
            Dim id As Integer
            If Integer.TryParse(cmd, id) AndAlso id > 0 AndAlso id <= _devList.Count Then
                _editingIndex = id - 1
                _mode = ScreenMode.Edit
                ShowEdit()
            Else
                _statusMsg = $"Unknown command: {cmd}"
                _statusColor = TN3270Color.Red
                ShowMenu()
            End If
        End If
    End Sub

    Private Sub ProcessEditInput(e As AidKeyEventArgs)
        If e.AidKey = &HF3 Then ' PF3 Cancel
            _mode = ScreenMode.Menu
            ShowMenu()
            Return
        End If

        If e.AidKey <> &H7D Then ' Enter
            ShowEdit()
            Return
        End If

        ' Apply Changes
        Dim d = _devList(_editingIndex)
        d.Name = _session.GetFieldValue("txtName")?.Trim()
        d.Description = _session.GetFieldValue("txtDesc")?.Trim()
        d.Type = Val(_session.GetFieldValue("txtType"))
        d.ConnType = Val(_session.GetFieldValue("txtConn"))
        d.OS = Val(_session.GetFieldValue("txtOS"))
        d.Destination = _session.GetFieldValue("txtDest")?.Trim()
        Dim autoVal = _session.GetFieldValue("txtAuto")?.Trim().ToUpper()
        d.Auto = (autoVal = "TRUE" OrElse autoVal = "1" OrElse autoVal = "YES")

        Dim pdfVal = _session.GetFieldValue("txtPDF")?.Trim().ToUpper()
        d.PDF = (pdfVal = "TRUE" OrElse pdfVal = "1" OrElse pdfVal = "YES")
        d.Orientation = Val(_session.GetFieldValue("txtOrient"))
        d.OutDest = _session.GetFieldValue("txtOut")?.Trim()

        _statusMsg = $"Device '{d.Name}' updated."
        _statusColor = TN3270Color.Green
        _hasUnsavedChanges = True
        _mode = ScreenMode.Menu
        ShowMenu()
    End Sub

    Private Sub ProcessDeleteInput(e As AidKeyEventArgs)
        If e.AidKey = &H7D Then ' Enter on confirmation
            Dim cmd = _session.GetFieldValue("txtConfirm")?.ToUpper().Trim()
            If cmd = "Y" Then
                _devList.RemoveAt(_editingIndex)
                _statusMsg = "Device deleted."
                _statusColor = TN3270Color.Yellow
                _hasUnsavedChanges = True

                ' Ensure we aren't pointing at an empty page after deletion
                If _startIndex >= _devList.Count AndAlso _devList.Count > 0 Then
                    _startIndex = Math.Max(0, ((_devList.Count - 1) \ 5) * 5)
                End If
            Else
                _statusMsg = "Delete cancelled."
            End If
            _mode = ScreenMode.Menu
            ShowMenu()
        Else
            ShowConfirmDelete()
        End If
    End Sub

    ' --- UI Rendering ---

    Private Sub ShowMenu()
        _session.ClearFields()

        ' CICS Standard Header
        Dim programName = "PRTPRN01"
        Dim transId = "CFG1"
        Dim dateStr = DateTime.Now.ToString("MM/dd/yy")
        Dim timeStr = DateTime.Now.ToString("HH:mm:ss")

        _session.WriteText(1, 2, $"PROGRAM: {programName}", TN3270Color.Turquoise)
        _session.WriteText(1, 25, "PROJECT PRINTER CONFIGURATION", TN3270Color.White)
        _session.WriteText(1, 65, $"DATE: {dateStr}", TN3270Color.Turquoise)

        _session.WriteText(2, 2, $"TRANSID: {transId}", TN3270Color.Turquoise)
        _session.WriteText(2, 65, $"TIME: {timeStr}", TN3270Color.Turquoise)

        _session.WriteText(3, 1, StrDup(78, "-"), TN3270Color.Blue)

        ' Command Line Area
        _session.WriteText(4, 2, "COMMAND ==>", TN3270Color.Yellow)
        _session.AddField(4, 14, 40, "", False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtCmd")

        ' Status/Message Line
        If Not String.IsNullOrEmpty(_statusMsg) Then
            _session.WriteText(5, 2, "MSG:", TN3270Color.Turquoise)
            _session.WriteText(5, 7, _statusMsg, _statusColor)
        End If

        ' Device Table Header
        _session.WriteText(7, 2, "ID   NAME            DESCRIPTION                    OS  CONN  AUTO  PDF", TN3270Color.Turquoise)
        _session.WriteText(8, 1, StrDup(78, "-"), TN3270Color.Blue)

        ' Data Rows (4 devices per page to leave room for CICS styling)
        Dim rowPos = 9
        For i = _startIndex To Math.Min(_startIndex + 3, _devList.Count - 1)
            Dim d = _devList(i)
            _session.WriteText(rowPos, 2, (i + 1).ToString("00"), TN3270Color.Yellow)
            _session.WriteText(rowPos, 7, d.Name.PadRight(14).Substring(0, 14), TN3270Color.White)
            _session.WriteText(rowPos, 23, d.Description.PadRight(29).Substring(0, 29), TN3270Color.White)
            _session.WriteText(rowPos, 54, d.OS.ToString(), TN3270Color.White)
            _session.WriteText(rowPos, 58, d.ConnType.ToString(), TN3270Color.White)
            _session.WriteText(rowPos, 64, If(d.Auto, "YES ", "NO  "), TN3270Color.Pink)
            _session.WriteText(rowPos, 70, If(d.PDF, "YES ", "NO  "), TN3270Color.Pink)

            _session.WriteText(rowPos + 1, 7, d.Destination.PadRight(50).Substring(0, 50), TN3270Color.Green)
            rowPos += 3 ' Triple spacing for professional look
        Next

        ' Footer Area
        _session.WriteText(21, 1, StrDup(78, "-"), TN3270Color.Blue)
        _session.WriteText(22, 2, "ENTER:PROCESS   PF3:EXIT   PF7:UP   PF8:DOWN", TN3270Color.White)
        _session.WriteText(23, 2, "OS:(0)MVS (1)VMS (2)MPE (3)RSTS (4)VM370 (5)NOS (6)VM/SP (7)TNDY (8)ZOS", TN3270Color.Turquoise)

        _session.ShowScreen()
    End Sub

    Private Sub ShowEdit()
        _session.ClearFields()
        Dim d = _devList(_editingIndex)

        ' CICS Standard Header
        Dim programName = "PRTPRN01"
        Dim transId = "CFG1"
        Dim dateStr = DateTime.Now.ToString("MM/dd/yy")
        Dim timeStr = DateTime.Now.ToString("HH:mm:ss")

        _session.WriteText(1, 2, $"PROGRAM: {programName}", TN3270Color.Turquoise)
        _session.WriteText(1, 30, "EDIT DEVICE DETAILS", TN3270Color.White)
        _session.WriteText(1, 65, $"DATE: {dateStr}", TN3270Color.Turquoise)

        _session.WriteText(2, 2, $"TRANSID: {transId}", TN3270Color.Turquoise)
        _session.WriteText(2, 65, $"TIME: {timeStr}", TN3270Color.Turquoise)

        _session.WriteText(3, 1, StrDup(78, "-"), TN3270Color.Blue)

        ' Data Entry Section
        Dim labelCol = 5
        Dim fieldCol = 26

        ' --- Identification Block ---
        _session.WriteText(5, labelCol, "        DEVICE NAME:", TN3270Color.Turquoise)
        _session.AddField(5, fieldCol, 15, d.Name, False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtName").Modified = True

        _session.WriteText(6, labelCol, " DEVICE DESCRIPTION:", TN3270Color.Turquoise)
        _session.AddField(6, fieldCol, 30, d.Description, False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtDesc").Modified = True

        ' --- Connectivity Block ---
        _session.WriteText(8, labelCol, "        DEVICE TYPE:", TN3270Color.Turquoise)
        _session.AddField(8, fieldCol, 1, d.Type.ToString(), False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtType").Modified = True

        _session.WriteText(9, labelCol, "    CONNECTION TYPE:", TN3270Color.Turquoise)
        _session.AddField(9, fieldCol, 1, d.ConnType.ToString(), False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtConn").Modified = True

        _session.WriteText(10, labelCol, "   OPERATING SYSTEM:", TN3270Color.Turquoise)
        _session.AddField(10, fieldCol, 1, d.OS.ToString(), False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtOS").Modified = True

        _session.WriteText(12, labelCol, " DEVICE DESTINATION:", TN3270Color.Turquoise)
        _session.AddField(12, fieldCol, 50, d.Destination, False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtDest").Modified = True

        ' --- Options Block ---
        _session.WriteText(14, labelCol, "       AUTO CONNECT:", TN3270Color.Turquoise)
        _session.AddField(14, fieldCol, 10, d.Auto.ToString(), False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtAuto").Modified = True

        _session.WriteText(16, labelCol, "         OUTPUT PDF:", TN3270Color.Turquoise)
        _session.AddField(16, fieldCol, 10, d.PDF.ToString(), False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtPDF").Modified = True

        _session.WriteText(16, 42, "ORIENTATION:", TN3270Color.Turquoise)
        _session.AddField(16, 55, 1, d.Orientation.ToString(), False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtOrient").Modified = True

        _session.WriteText(18, labelCol, "   OUTPUT DIRECTORY:", TN3270Color.Turquoise)
        _session.AddField(18, fieldCol, 50, d.OutDest, False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtOut").Modified = True

        ' Footer Area
        _session.WriteText(21, 1, StrDup(78, "-"), TN3270Color.Blue)
        _session.WriteText(22, 2, "ENTER:SAVE   PF3:CANCEL", TN3270Color.White)
        _session.WriteText(23, 2, "OS:(0)MVS (1)VMS (2)MPE (3)RSTS (4)VM/370 (5)NOS (6)VM/SP (7)TNDY (8)ZOS", TN3270Color.Turquoise)

        _session.ShowScreen()
    End Sub

    Private Sub ShowConfirmDelete()
        _session.ClearFields()
        Dim d = _devList(_editingIndex)

        ' CICS Standard Header
        Dim programName = "PRTPRN01"
        Dim transId = "CFG1"
        Dim dateStr = DateTime.Now.ToString("MM/dd/yy")
        Dim timeStr = DateTime.Now.ToString("HH:mm:ss")

        _session.WriteText(1, 2, $"PROGRAM: {programName}", TN3270Color.Turquoise)
        _session.WriteText(1, 30, "CONFIRM DELETE", TN3270Color.White)
        _session.WriteText(1, 65, $"DATE: {dateStr}", TN3270Color.Turquoise)

        _session.WriteText(2, 2, $"TRANSID: {transId}", TN3270Color.Turquoise)
        _session.WriteText(2, 65, $"TIME: {timeStr}", TN3270Color.Turquoise)

        _session.WriteText(3, 1, StrDup(78, "-"), TN3270Color.Blue)

        ' Confirmation Block
        _session.WriteText(10, 20, "****************************************", TN3270Color.Red)
        _session.WriteText(11, 20, $"* CONFIRM DELETION OF: {d.Name.PadRight(15)} *", TN3270Color.Red)
        _session.WriteText(12, 20, "****************************************", TN3270Color.Red)

        _session.WriteText(14, 20, "TYPE 'Y' TO CONFIRM ==> ", TN3270Color.White)
        _session.AddField(14, 44, 1, "", False, TN3270Color.White, TN3270Color.Neutral, TN3270Highlight.Underline, "txtConfirm")

        _session.WriteText(16, 20, "PRESS ENTER TO PROCEED OR PF3 TO CANCEL", TN3270Color.Turquoise)

        ' Footer Area
        _session.WriteText(21, 1, StrDup(78, "-"), TN3270Color.Blue)
        _session.WriteText(22, 2, "ENTER:CONFIRM   PF3:CANCEL", TN3270Color.White)

        _session.ShowScreen()
    End Sub

    Private Sub SaveDevices()
        Try
            Using writer As New StreamWriter(_configFile, append:=False)
                For Each d In _devList
                    writer.WriteLine($"{d.Name}||{d.Description}||{d.Type}||{d.ConnType}||{d.Destination}||{d.OS}||{d.Auto}||" &
                        $"{d.PDF}||{d.Orientation}||{d.OutDest}")
                Next
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error saving devices: {ex.Message}")
        End Try
    End Sub
End Class

