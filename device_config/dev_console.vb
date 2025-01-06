Imports System
Imports System.Console
Imports System.Data
Imports System.IO
Imports System.Reflection.Metadata.Ecma335
Imports System.Runtime.InteropServices
Imports System.Runtime.InteropServices.Marshalling
Imports System.Runtime.Versioning
Imports System.Xml.Serialization

Module dev_console

    Dim devList As New List(Of devs)

    Private max_Rows As Integer = 24  ' Default setup
    Private max_Cols As Integer = 80
    Sub Main(args As String())
        If OperatingSystem.IsLinux Then
            Console.WriteLine("Running under Linux.")
        Else
            Console.SetWindowSize(132, 32)
        End If

        max_Rows = Console.WindowHeight
        max_Cols = Console.WindowWidth
        If max_Rows < 24 Or max_Cols < 80 Then
            Console.WriteLine("CONFIGURATION:  Your terminal window must be at least 80 columns wide,")
            Console.WriteLine($"and 24 columns high.  It is currently {max_Cols}x{max_Rows}")
            Console.WriteLine("Please correct this problem and try again.")
            End
        End If
        Console.Clear()
        If Not File.Exists("devices.cfg") Then
            Dim fs As FileStream = File.Open("devices.cfg", FileMode.Create)
            fs.Close()
        End If
        devList.Clear()
        devList = LoadDevs()
        DisplayMenu()
    End Sub

    Private Function LoadDevs() As List(Of devs)
        ' Unload all devices.
        Dim newList As New List(Of devs)
        Dim serializer As New XmlSerializer(GetType(List(Of devs)))
        Dim xmlStream As New StreamReader("devices.cfg")
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
        Dim xmlStream As New StreamWriter("devices.cfg")
        Using sw As New StringWriter()
            serializer.Serialize(sw, devList)
            xmlStream.Write(sw.ToString())
            xmlStream.Close()
        End Using
    End Sub
    Sub DisplayMenu()
        Console.ResetColor()
        Dim bannerLine As String = StrDup(max_Cols, "=")
        Dim devLine As String = ""
        If max_Cols > 90 Then
            devLine = "      DEVICE          NAME                          OS  CONN  AUTO  PDF  DEST"
        Else
            devLine = "      DEVICE          NAME                          OS  CONN  AUTO  PDF"
        End If
        Console.Clear()
        Console.Write(bannerLine)
        Console.WriteLine($" PROJECT PRINTER DEVICE CONFIGURATION   {devList.Count} devices loaded.")
        Console.Write(bannerLine)
        Console.SetCursorPosition(1, 4)
        Console.WriteLine("COMMAND ==> ")
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
            Console.ResetColor()
        Next
        Console.SetCursorPosition(1, max_Rows - 3)
        Console.ForegroundColor = ConsoleColor.White
        Console.WriteLine("OS: (0) MVS (1) VMS  (2) MPE (3) RSTS/E")
        Console.WriteLine(" CONN: (0) SOCKDEV (1) NOT IMPL.  (3) NOT IMPL.")
        Console.Write(" Command: ADD, SAVE, EXIT or Item # to EDIT")
        Console.SetCursorPosition(13, 4)
        Dim selection As String = Console.ReadLine()

    End Sub
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
End Enum