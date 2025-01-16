Imports System
Imports System.ComponentModel
Imports System.Console
Imports System.Data
Imports System.IO
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.Marshalling
Imports System.Runtime.Versioning
Imports System.Security.AccessControl
Imports System.Threading
Imports System.Xml
Imports System.Xml.Serialization

Module dev_console

    Dim devList As New List(Of devs)

    Private max_Rows As Integer = 24  ' Default setup
    Private max_Cols As Integer = 80

    Private ErrMsg As String = ""

    Private configFile As String = "devices.cfg"
    Sub Main(args As String())
        If args.Count > 0 Then configFile = args(0)   ' If it's not specified, devices.cfg it is.
        AddHandler Console.CancelKeyPress, AddressOf Console_CancelKeyPress
        If OperatingSystem.IsLinux Then
            Console.WriteLine("Running under Linux.")
        Else
            Console.SetWindowSize(80, 25)
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
                End If
            Else
                Select Case sel
                    Case "*CC*"

                    Case "SAVE"
                        SaveDevices()
                    Case "ADD"
                        devList.Add(New devs)
                        EditItem(devList.Count)
                    Case "EXIT"
                        If OkToQuit() Then
                            Console.ResetColor()
                            Console.Clear()
                            End
                        Else
                            Exit Select
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
        Say("OS: (0) MVS38J (1) VMS  (2) MPE (3) RSTS/E (4) VM/370", 1, max_Rows - 3, ConsoleColor.White)
        Say("CONN TYPE: Always 0 (sockdev)", 1, max_Rows - 2, ConsoleColor.White)
        Say("[ENTER] Accept line", 1, max_Rows - 1, ConsoleColor.Green)
        Say("TAB/arrows DO NOT CHANGE FIELD", 22, max_Rows - 1, ConsoleColor.Red)
        Say("       DEVICE NAME:", 5, 5, ConsoleColor.Cyan)
        Say("DEVICE DESCRIPTION:", 5, 7, ConsoleColor.Cyan)
        Say("       DEVICE TYPE:", 5, 9, ConsoleColor.Cyan)
        Say("   CONNECTION TYPE:", 5, 11, ConsoleColor.Cyan)
        Say("  OPERATING SYSTEM:", 5, 13, ConsoleColor.Cyan)
        Say("DEVICE DESTINATION:", 5, 15, ConsoleColor.Cyan)
        Say("      AUTO CONNECT:", 5, 17, ConsoleColor.Cyan)
        Say("        OUTPUT PDF:", 5, 19, ConsoleColor.Cyan)
        Say(thisDev.DevName, 26, 5, ConsoleColor.Yellow)
        Say(thisDev.DevDescription, 26, 7, ConsoleColor.Yellow)
        Say(thisDev.DevType, 26, 9, ConsoleColor.Yellow)
        Say(thisDev.ConnType, 26, 11, ConsoleColor.Yellow)
        Say(thisDev.OS, 26, 13, ConsoleColor.Yellow)
        Say(thisDev.DevDest, 26, 15, ConsoleColor.Yellow)
        Say(thisDev.Auto, 26, 17, ConsoleColor.Yellow)
        Say(thisDev.PDF, 26, 19, ConsoleColor.Yellow)
        Dim myName As String = GetString(thisDev.DevName, 26, 5, 15, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myDesc As String = GetString(thisDev.DevDescription, 26, 7, 30, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myType As String = GetString(thisDev.DevType, 26, 9, 1, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myConn As String = GetString(thisDev.ConnType, 26, 11, 1, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myOS As String = GetString(thisDev.OS, 26, 13, 1, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myDest As String = GetString(thisDev.DevDest, 26, 15, 50, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myAuto As String = GetString(thisDev.Auto, 26, 17, 10, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
        Dim myPDF As String = GetString(thisDev.PDF, 26, 19, 10, ConsoleColor.Yellow, ConsoleColor.Black, ConsoleColor.White)
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
                    Stop
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
            devList(item) = thisDev
        End If
    End Sub

    Function CenterString(input As String, totalWidth As Integer) As String
        Return input.PadLeft((totalWidth + input.Length) \ 2).PadRight(totalWidth)
    End Function

    Private Function GetString(value As String, col As Integer, line As Integer, len As Integer, dColor As Integer, Optional fcolor As Integer = -1, Optional BColor As Integer = -1) As String
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
        ' Deserialize all devices.
        Dim newList As New List(Of devs)
        Dim serializer As New XmlSerializer(GetType(List(Of devs)))
        Dim xmlStream As New StreamReader(configFile)
        Try
            newList = serializer.Deserialize(xmlStream)
        Catch ex As Exception
            newList.Clear()
        Finally
            xmlStream.Close()
        End Try
        Return newList
    End Function

    Sub SaveDevices()
        ' Serialize the device list to XML
        Dim serializer As New XmlSerializer(GetType(List(Of devs)))
        Dim xmlStream As New StreamWriter(configFile)
        Using sw As New StringWriter()
            serializer.Serialize(sw, devList)
            xmlStream.Write(sw.ToString())
            xmlStream.Close()
        End Using
    End Sub
    Sub DisplayMenu()
        BackgroundColor = ConsoleColor.Black
        ForegroundColor = ConsoleColor.White
        Dim bannerLine As String = StrDup(max_Cols, "=")
        Dim devLine As String = ""
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
        For x = 0 To (devList.Count - 1)
            Dim thisDev As devs = devList(x)
            Console.SetCursorPosition(0, 7 + (x * 2))
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.Write($"{x + 1:00}")
            Console.SetCursorPosition(6, 7 + (x * 2))
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write(thisDev.DevName)
            Console.SetCursorPosition(22, 7 + (x * 2))
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write(thisDev.DevDescription)
            Console.SetCursorPosition(53, 7 + (x * 2))
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write(thisDev.OS)
            Console.SetCursorPosition(59, 7 + (x * 2))
            Console.ForegroundColor = ConsoleColor.Cyan
            Console.Write(thisDev.ConnType)
            Console.SetCursorPosition(62, 7 + (x * 2))
            Console.ForegroundColor = ConsoleColor.Yellow
            If thisDev.Auto Then
                Console.Write("YES")
            Else
                Console.Write("NO")
            End If
            Console.SetCursorPosition(68, 7 + (x * 2))
            Console.ForegroundColor = ConsoleColor.Yellow
            If thisDev.PDF Then
                Console.Write("YES")
            Else
                Console.Write("NO")
            End If
            If max_Cols > 90 Then
                Console.SetCursorPosition(73, 7 + (x * 2))
            Else
                Console.SetCursorPosition(5, 8 + (x * 2))
            End If
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write(thisDev.DevDest)
            ConsoleResetColor()
        Next
        Console.SetCursorPosition(1, max_Rows - 3)
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("OS: (0) MVS38J (1) VMS  (2) MPE (3) RSTS/E (4) VM/370")
        Console.WriteLine(" CONN: (0) SOCKDEV (others not implemented)")
        Console.Write(" Command: ADD, SAVE, EXIT or Item # to EDIT, or DELETE #")

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
        SetError("")
        Console.SetCursorPosition(13, 4)
        Dim selection As String = Console.ReadLine()
        Return selection
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