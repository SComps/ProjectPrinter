Imports System.Console
Imports System.Drawing
Imports System.IO
Imports System.Text
Module dev_console

    Dim devList As New List(Of devs)

    Private max_Rows As Integer = 24  ' Default setup
    Private max_Cols As Integer = 80

    Private ErrMsg As String = ""

    Private configFile As String = "devices.dat"

    Private StartShow As Integer = 0
    Private StopShow As Integer = 0


    Structure filedev
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
    End Structure
    Sub Main(args As String())
        If args.Count > 0 Then configFile = args(0)   ' If it's not specified, devices.cfg it is.
        AddHandler Console.CancelKeyPress, AddressOf Console_CancelKeyPress
        If OperatingSystem.IsLinux Then
            Console.WriteLine("Running under Linux.")
        Else
            Console.SetWindowSize(80, 24)
        End If

        max_Rows = Console.WindowHeight
        max_Cols = Console.WindowWidth
        If max_Rows < 24 Or max_Cols < 80 Then
            Console.WriteLine("CONFIGURATION:  Your terminal window must be at least 80 columns wide,")
            Console.WriteLine($"and 24 columns high.  It is currently {max_Cols}x{max_Rows}")
            Console.WriteLine("Please correct this problem and try again.")
            End
        Else
            Console.WriteLine($"Your screen is {max_Cols}x{max_Rows}")
        End If
        BackgroundColor = ConsoleColor.Black
        ForegroundColor = ConsoleColor.White
        Console.Clear()
        If Not File.Exists(configFile) Then
            Dim fs As FileStream = File.Open(configFile, FileMode.Create)
            fs.Close()
        End If
        devList.Clear()
        devList = LoadDevs()
        StartShow = 0
        StopShow = StartShow + 4
        If StopShow > devList.Count - 1 Then StopShow = devList.Count - 1
        Do While True
            DisplayMenu()
            Dim sel As String = ""
            sel = GetCmd()
            If sel Is Nothing Then sel = "*CC*"
            sel = sel.ToUpper.Trim
            ' DELETE is a multi part command and can't be used in SELECT
            If sel.StartsWith("DELETE") Then
                Dim parts As String() = sel.Split(" ")
                Dim cancelDelete As Boolean = False
                If parts.Count <> 2 Then
                    SetError("Invalid command structure.")
                Else
                    If Val(parts(1)) = 0 Then
                        SetError("Invalid entry ID number")
                        cancelDelete = True
                    End If
                    If Val(parts(1)) > devList.Count Then
                        SetError($"Item {parts(1)} does not exist.")
                        cancelDelete = True
                    End If
                    If Not cancelDelete Then
                        Console.SetCursorPosition(1, 5)
                        Console.ForegroundColor = ConsoleColor.Red
                        Console.Write($"Press Y to confirm deletion of item {parts(1)} ===> ")
                        ConsoleResetColor()
                        Dim resp As String = Console.ReadKey.KeyChar
                        If resp.ToUpper <> "Y" Then
                            SetError($"Cancelled deletion of item {parts(1)}.")
                            cancelDelete = True
                        Else
                            devList.RemoveAt(Val(parts(1) - 1)) ' remember 0 based
                        End If
                    End If
                    ' Adjust Start/Stop
                    Dim NewStop = devList.Count - 1
                    If StartShow + 4 > devList.Count - 1 Then
                        StopShow = devList.Count + 1
                    End If
                End If
            Else
                Select Case sel
                    Case "*CC*"

                    Case "SAVE"
                        SaveDevices()
                    Case "ADD"
                        devList.Add(New devs)
                        EditItem(devList.Count)
                        ' Adjust Start/Stop
                        Dim NewStop = devList.Count - 1
                        If StartShow + 4 > devList.Count - 1 Then
                            StopShow = devList.Count + 1
                        End If
                        If StartShow < 0 Then StartShow = 0
                        If StopShow > devList.Count - 1 Then StopShow = devList.Count - 1
                    Case "EXIT"
                        If OkToQuit() Then
                            Console.ResetColor()
                            Console.Clear()
                            Environment.Exit(0)
                        Else
                            Exit Select
                        End If
                    Case "D", "DOWN"
                        StartShow = StopShow
                        StopShow = StartShow + 4
                        If StopShow > (devList.Count - 1) Then
                            StopShow = (devList.Count - 1)
                            StartShow = StopShow - 4
                        End If

                    Case "U", "UP"
                        StartShow = StartShow - 4
                        If StartShow > (devList.Count - 1) - 4 Then StartShow = (devList.Count - 1) - 4
                        If StartShow < 0 Then StartShow = 0
                        StopShow = StartShow + 4
                        If StopShow > (devList.Count - 1) Then
                            StopShow = (devList.Count - 1)
                            StartShow = StopShow - 4
                        End If
                    Case Else
                        Dim itemID As Integer = Val(sel)
                        Select Case itemID
                            Case 0
                                SetError($"ERR: Invalid Command {sel}")
                            Case Else
                                If itemID > devList.Count Then
                                    SetError($"ERR: Device {itemID} not listed.")
                                Else
                                    ' Send the itemID to editItem
                                    EditItem(itemID)
                                End If
                        End Select
                End Select
            End If
        Loop
    End Sub

    Private Function OkToQuit() As Boolean
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White
        Console.Clear()
        Console.WriteLine("Hey Rudi!!!  Baldurs Gate forever!")
        Console.Write("Unsaved changes may be lost.  Are you sure? [Y/n] ==> ")
        Dim opt As String = Console.ReadLine
        If opt.ToUpper.StartsWith("Y") Then
            Return True
        Else
            Return False
        End If
    End Function

    Private Sub EditItem(item As Integer)
        ' First reduce item by 1 (the list is 0 based)
        item = item - 1
        Dim thisDev As devs = devList(item)
        Console.Clear()
        Console.BackgroundColor = ConsoleColor.Black
        Console.ForegroundColor = ConsoleColor.White
        Dim bannerLine As String = StrDup(max_Cols, "=")
        Say(bannerLine, 0, 0, ConsoleColor.White)
        Say(CenterString($"E D I T   D E V I C E", max_Cols), 0, 1, ConsoleColor.White)
        Say(bannerLine, 0, 2, ConsoleColor.White)
        Say("OS: (0) MVS38J (1) VMS  (2) MPE (3) RSTS/E (4) VM/370 (5) NOS 2.7.8", 1, max_Rows - 2, ConsoleColor.White)
        Say("[ENTER] Accept line", 1, max_Rows - 1, ConsoleColor.Green)
        Say("TAB/arrows DO NOT CHANGE FIELD", 22, max_Rows - 1, ConsoleColor.Red)
        Say("       DEVICE NAME:", 5, 3, ConsoleColor.Cyan)
        Say("DEVICE DESCRIPTION:", 5, 5, ConsoleColor.Cyan)
        Say("       DEVICE TYPE:", 5, 7, ConsoleColor.Cyan)
        Say("   CONNECTION TYPE:", 5, 9, ConsoleColor.Cyan)
        Say("  OPERATING SYSTEM:", 5, 11, ConsoleColor.Cyan)
        Say("DEVICE DESTINATION:", 5, 13, ConsoleColor.Cyan)
        Say("      AUTO CONNECT:", 5, 15, ConsoleColor.Cyan)
        Say("        OUTPUT PDF:", 5, 17, ConsoleColor.Cyan)
        Say("ORIENTATION:", 40, 17, ConsoleColor.Cyan)
        Say("        OUTPUT DIR:", 5, 19, ConsoleColor.Cyan)
        Say(thisDev.DevName, 26, 3, ConsoleColor.Yellow)
        Say(thisDev.DevDescription, 26, 5, ConsoleColor.Yellow)
        Say(thisDev.DevType, 26, 7, ConsoleColor.Yellow)
        Say(thisDev.ConnType, 26, 9, ConsoleColor.Yellow)
        Say(thisDev.OS, 26, 11, ConsoleColor.Yellow)
        Say(thisDev.DevDest, 26, 13, ConsoleColor.Yellow)
        Say(thisDev.Auto, 26, 15, ConsoleColor.Yellow)
        Say(thisDev.PDF, 26, 17, ConsoleColor.Yellow)
        Say(thisDev.Orientation, 54, 17, ConsoleColor.Yellow)
        Say(thisDev.OutDest, 26, 19, ConsoleColor.Yellow)
        Dim myName As String = GetString(thisDev.DevName, 26, 3, 15, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myDesc As String = GetString(thisDev.DevDescription, 26, 5, 30, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myType As String = GetString(thisDev.DevType, 26, 7, 1, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myConn As String = GetString(thisDev.ConnType, 26, 9, 1, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myOS As String = GetString(thisDev.OS, 26, 11, 1, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myDest As String = GetString(thisDev.DevDest, 26, 13, 50, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myAuto As String = GetString(thisDev.Auto, 26, 15, 10, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myPDF As String = GetString(thisDev.PDF, 26, 17, 10, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myOrient As String = GetString(thisDev.Orientation, 54, 17, 1, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myOutDest As String = GetString(thisDev.OutDest, 26, 19, 50, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Say("Save? (Y/n) ==> ", 1, max_Rows - 4, ConsoleColor.Green)
        Dim opt As String = Console.ReadLine
        If opt.ToUpper.StartsWith("Y") Then
            ' move what's been entered into the temporary object
            thisDev.DevName = myName.Trim
            thisDev.DevDescription = myDesc.Trim
            thisDev.DevType = Val(myType)
            thisDev.ConnType = Val(myConn)
            thisDev.OS = Val(myOS)
            thisDev.DevDest = myDest
            Select Case myAuto.ToUpper.Trim
                Case "TRUE", "YES", "1"

                    thisDev.Auto = True
                Case "FALSE", "NO", "0", "-1"
                    thisDev.Auto = False
                Case Else
                    thisDev.Auto = False    ' Default to false.
            End Select
            Select Case myPDF.ToUpper
                Case "TRUE", "YES", "1"
                    thisDev.PDF = True
                Case "FALSE", "NO", "0", "-1"
                    thisDev.PDF = False
                Case Else
                    thisDev.PDF = False    ' Default to false
            End Select
            thisDev.OutDest = myOutDest
            thisDev.Orientation = Val(myOrient)
            devList(item) = thisDev
        End If
    End Sub

    Function CenterString(input As String, totalWidth As Integer) As String
        Return input.PadLeft((totalWidth + input.Length) \ 2).PadRight(totalWidth)
    End Function

    Private Function GetString(value As String, col As Integer, line As Integer, len As Integer, dColor As Integer, Optional fcolor As Integer = -1, Optional BColor As Integer = -1) As String
        If value Is Nothing Then value = ""
        Dim editedString As String = value
        Dim cursorPosition As Integer = value.Length
        Dim keyInfo As ConsoleKeyInfo
        Dim outputLength As Integer = Math.Min(len, editedString.Length)

        Console.SetCursorPosition(col, line)
        Console.Write(editedString.PadRight(len)) ' Display the string
        Console.SetCursorPosition(col + cursorPosition, line)

        Do
            keyInfo = Console.ReadKey(True) ' Capture key press without displaying it

            Select Case keyInfo.Key
                Case ConsoleKey.LeftArrow
                    ' Move cursor left
                    If cursorPosition > 0 Then
                        cursorPosition -= 1
                    End If

                Case ConsoleKey.RightArrow
                    ' Move cursor right
                    If cursorPosition < outputLength Then
                        cursorPosition += 1
                    End If

                Case ConsoleKey.Backspace
                    ' Delete character before cursor
                    If cursorPosition > 0 Then
                        editedString = editedString.Remove(cursorPosition - 1, 1)
                        cursorPosition -= 1
                    End If

                Case ConsoleKey.Delete
                    ' Delete character at cursor
                    If cursorPosition < editedString.Length Then
                        editedString = editedString.Remove(cursorPosition, 1)
                    End If

                Case ConsoleKey.Escape
                    ' Cancel editing, return original value
                    Return value

                Case ConsoleKey.Enter
                    ' Finish editing
                    Exit Do

                Case Else
                    ' Handle character input
                    If editedString.Length < len AndAlso Not Char.IsControl(keyInfo.KeyChar) Then
                        editedString = editedString.Insert(cursorPosition, keyInfo.KeyChar.ToString())
                        cursorPosition += 1
                    End If
            End Select

            ' Update display
            Console.SetCursorPosition(col, line)
            Console.Write(editedString.PadRight(len)) ' Display edited string
            Console.SetCursorPosition(col + cursorPosition, line)
        Loop

        Return editedString
    End Function

    Private Function OGetString(value As String, col As Integer, line As Integer, len As Integer, dColor As Integer, Optional fcolor As Integer = -1, Optional BColor As Integer = -1) As String
        Dim curfColor As ConsoleColor = Console.ForegroundColor
        Dim curbColor As ConsoleColor = Console.BackgroundColor
        If value Is Nothing Then value = ""
        If fcolor > -1 Then
            ForegroundColor = fcolor
        End If
        If BColor > -1 Then
            BackgroundColor = BColor
        End If
        SetCursorPosition(col, line)
        Console.Write(value & StrDup((len - value.Length), " "))
        SetCursorPosition(col, line)
        Dim repl As String = Console.ReadLine()
        If repl.Trim = "" Then repl = value
        SetCursorPosition(col, line)
        ForegroundColor = dColor
        BackgroundColor = curbColor
        Console.Write(repl & StrDup(len - repl.Length, " "))
        ForegroundColor = curfColor
        Return repl
    End Function

    Private Sub Say(txt As String, col As Integer, line As Integer, Optional fcolor As Integer = -1)
        Dim curColor As ConsoleColor = Console.ForegroundColor
        If fcolor > -1 Then
            ForegroundColor = fcolor
        End If
        SetCursorPosition(col, line)
        Write(txt)
        ForegroundColor = curColor
    End Sub

    Private Sub Console_CancelKeyPress(sender As Object, e As ConsoleCancelEventArgs)
        ' Set Cancel to True to suppress the default Ctrl+C action (application termination)
        e.Cancel = True
        SetError("Control-C not allowed. Use EXIT instead.")
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
    Sub SaveDevices()
        Using writer As New StreamWriter(configFile, append:=False)
            For Each d As devs In devList
                writer.WriteLine($"{d.DevName}||{d.DevDescription}||{d.DevType}||{d.ConnType}||{d.DevDest}||{d.OS}||{d.Auto}||" &
                    $"{d.PDF}||{d.Orientation}||{d.OutDest}")
            Next
        End Using

    End Sub
    Sub DisplayMenu()
        BackgroundColor = ConsoleColor.Black
        ForegroundColor = ConsoleColor.White
        Dim bannerLine As String = StrDup(max_Cols, "=")
        Dim devLine As String = ""
        If StartShow < 0 Then StartShow = 0
        If StopShow > StartShow + 4 Then StopShow = StartShow + 4
        If StopShow > (devList.Count - 1) Then
            StopShow = devList.Count - 1
            StartShow = StopShow - 4
        End If
        If max_Cols > 90 Then
            devLine = "      DEVICE          NAME                          OS  CONN  AUTO  PDF  DEST"
        Else
            devLine = "      DEVICE          NAME                          OS  CONN  AUTO  PDF"
        End If
        Console.Clear()
        Console.SetCursorPosition(0, 0)
        Console.Write(bannerLine)
        Console.SetCursorPosition(0, 1)
        Console.WriteLine($" PROJECT PRINTER DEVICE CONFIGURATION [{configFile}] {devList.Count} devices.")
        Dim PageDisp As String = $"Page: ({StartShow + 1}-{StopShow + 1})"
        Dim stPos As Integer = (max_Cols - 1) - (PageDisp.Length - 1)
        Console.SetCursorPosition(stPos, 1)
        Console.ForegroundColor = ConsoleColor.Green
        Console.Write(PageDisp)
        ConsoleResetColor()
        Console.SetCursorPosition(0, 2)
        Console.Write(bannerLine)
        Console.SetCursorPosition(1, 4)
        Console.WriteLine("COMMAND ==> ")
        Console.SetCursorPosition(1, 5)
        Console.ForegroundColor = ConsoleColor.Red
        Console.Write(ErrMsg)
        ConsoleResetColor()
        Console.SetCursorPosition(0, 6)
        Console.WriteLine(devLine)
        Dim currLine As Integer = 7
        If StopShow > devList.Count - 1 Then StopShow = devList.Count
        If StartShow < 0 Then StartShow = 0
        For x = StartShow To StopShow
            Dim thisDev As devs = devList(x)
            Console.SetCursorPosition(0, currLine)
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.Write($"{x + 1:00}")
            Console.SetCursorPosition(6, currLine)
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write(thisDev.DevName)
            Console.SetCursorPosition(22, currLine)
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write(thisDev.DevDescription)
            Console.SetCursorPosition(53, currLine)
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write(thisDev.OS)
            Console.SetCursorPosition(59, currLine)
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write(thisDev.ConnType)
            Console.SetCursorPosition(62, currLine)
            Console.ForegroundColor = ConsoleColor.Yellow
            If thisDev.Auto Then
                Console.Write("YES")
            Else
                Console.Write("NO")
            End If
            Console.SetCursorPosition(68, currLine)
            Console.ForegroundColor = ConsoleColor.Yellow
            If thisDev.PDF Then
                Console.Write("YES")
            Else
                Console.Write("NO")
            End If
            If max_Cols > 90 Then
                Console.SetCursorPosition(73, currLine)
            Else
                Console.SetCursorPosition(5, currLine + 1)
            End If
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write(thisDev.DevDest)
            currLine = currLine + 2
            ConsoleResetColor()
        Next
        Console.SetCursorPosition(0, max_Rows - 3)
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("OS: (0) MVS38J (1) VMS  (2) MPE (3) RSTS/E (4) VM/370 (5) NOS 2.7.8")
        Console.WriteLine("Paging: PgUp, PgDn or use Command UP and DOWN")
        Console.Write("Command: ADD, SAVE, EXIT or Item # to EDIT, or DELETE #")

    End Sub

    Public Sub ConsoleResetColor()
        Console.ForegroundColor = ConsoleColor.White
        Console.BackgroundColor = ConsoleColor.Black
    End Sub
    Public Sub SetError(txt As String)
        ErrMsg = txt
        If txt.Trim <> "" Then
            If OperatingSystem.IsWindows Then
                Console.Beep(800, 200)  ' Play a tone, not the windows chime.
            Else
                Console.Beep()  ' Linux cannot handle the frequency/duration call.
            End If
        End If
    End Sub
    Public Function GetCmd() As String
        SetError("")  ' Clear any previous errors
        Console.SetCursorPosition(13, 4)

        Dim accumulatedInput As String = ""  ' To store the accumulated input
        Dim insertMode As Boolean = False    ' Track Insert/Overwrite mode
        Dim cursorPosition As Integer = 0    ' Track the position of the cursor

        While True
            Dim key As ConsoleKeyInfo = Console.ReadKey(True)  ' Capture the key press without displaying it

            ' Handle PageUp and PageDown to return "UP" or "DOWN"
            If key.Key = ConsoleKey.PageUp Then
                ' Discard accumulated input and return "UP"
                Return "UP"
            ElseIf key.Key = ConsoleKey.PageDown Then
                ' Discard accumulated input and return "DOWN"
                Return "DOWN"
            End If

            ' Handle Backspace: Remove the last character
            If key.Key = ConsoleKey.Backspace Then
                If cursorPosition > 0 Then
                    accumulatedInput = accumulatedInput.Remove(cursorPosition - 1, 1) ' Remove char
                    cursorPosition -= 1
                    Console.SetCursorPosition(13 + cursorPosition, 4) ' Move cursor back and redraw
                    Console.Write(" "c) ' Erase the character
                    Console.SetCursorPosition(13 + cursorPosition, 4) ' Reset cursor position
                End If
                ' Handle Delete: Remove the character at the current cursor position
            ElseIf key.Key = ConsoleKey.Delete Then
                If cursorPosition < accumulatedInput.Length Then
                    accumulatedInput = accumulatedInput.Remove(cursorPosition, 1) ' Remove char
                    Console.SetCursorPosition(13 + cursorPosition, 4) ' Move cursor to the current position
                    Console.Write(" "c) ' Erase the character
                    Console.SetCursorPosition(13 + cursorPosition, 4) ' Reset cursor position
                End If
                ' Handle Insert: Toggle between insert and overwrite modes
            ElseIf key.Key = ConsoleKey.Insert Then
                insertMode = Not insertMode  ' Toggle insert mode
                ' Optionally, display a message to indicate the mode (this is up to you)
                ' Handle normal typing: Add a character to the input string
            ElseIf Char.IsLetterOrDigit(key.KeyChar) OrElse Char.IsPunctuation(key.KeyChar) OrElse Char.IsWhiteSpace(key.KeyChar) Then
                If insertMode AndAlso cursorPosition < accumulatedInput.Length Then
                    ' In insert mode, insert the character at the current cursor position
                    accumulatedInput = accumulatedInput.Insert(cursorPosition, key.KeyChar)
                Else
                    ' In overwrite mode (or if no insert mode), just append the character
                    If cursorPosition < accumulatedInput.Length Then
                        accumulatedInput = accumulatedInput.Remove(cursorPosition, 1).Insert(cursorPosition, key.KeyChar)
                    Else
                        accumulatedInput &= key.KeyChar
                    End If
                End If
                cursorPosition += 1
            End If

            ' Redraw the accumulated input after each key press
            Console.SetCursorPosition(13, 4)
            Console.Write(accumulatedInput.PadRight(accumulatedInput.Length)) ' Clear and reprint the string
            Console.SetCursorPosition(13 + cursorPosition, 4) ' Reset cursor position after typing

            ' If Enter is pressed, return the accumulated string
            If key.Key = ConsoleKey.Enter Then
                Return accumulatedInput
            End If
        End While
        Return accumulatedInput
    End Function

End Module


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
End Class

Public Enum ORIENT
    LANDSCAPE
    PORTRAIT
    LANDSCAPE_NOBACK
    PORTRAIT_NOBACK
End Enum

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