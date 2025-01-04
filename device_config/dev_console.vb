Imports System
Imports System.Console
Imports System.IO
Imports System.Reflection.Metadata.Ecma335
Imports System.Xml.Serialization

Module dev_console

    Dim devList As New List(Of devs)

    Private max_Rows As Integer = 24  ' Default setup
    Private max_Cols As Integer = 80
    Sub Main(args As String())
        max_Rows = Console.WindowHeight
        max_Cols = Console.WindowWidth
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
        Dim bannerLine As String = StrDup(max_Cols, "=")
        Console.Clear()
        Console.Write(bannerLine)
        Console.WriteLine($" PROJECT PRINTER DEVICE CONFIGURATION   {devList.Count} devices loaded.")
        Console.Write(bannerLine)
        Console.WriteLine()
        Console.WriteLine(" 1. List configured devices")
        Console.WriteLine(" 2. Edit a configured device")
        Console.WriteLine(" 3. Add a new device")
        Console.WriteLine(" 4. Remove a device")
        Console.WriteLine()
        Console.WriteLine(" S. Save changes")
        Console.WriteLine(" X. Exit")
        Console.WriteLine()
        Console.Write(" Enter your selection:  ")
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
    OS_OTHER
End Enum