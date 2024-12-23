Imports System.Threading
Imports ProjectPrinterManager.My
Imports Windows.Media.Protection.PlayReady

Imports System.Text
Imports System.Net.Sockets
Imports System.IO
Imports System.Xml.Serialization
Imports System.Text.Json.Serialization
Imports System.Data.SqlTypes

Public Class frmMain

    Private enteredHost As String = ""
    Private hostname As String = ""
    Private hostPort As Integer

    Dim remote As New TcpClient()
    Dim netStream As NetworkStream
    Dim reader As StreamReader
    Dim writer As StreamWriter

    Dim myConfig As New parmStruct
    Dim myDevs As New List(Of devs)

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        enteredHost = My.Settings.LastHost
        If enteredHost.Trim = "" Then
            Stop
            My.Settings.LastHost = "localhost:16000"
        Else
            txtHost.Text = enteredHost
        End If

        Dim unused As Boolean = CheckHost()

    End Sub

    Private Function CheckHost() As Boolean
        Dim hostObject = GetHostPort(txtHost.Text)
        If hostObject.HostName <> "" Then
            enteredHost = txtHost.Text.Trim
            My.Settings.LastHost = enteredHost
            hostname = hostObject.HostName
            hostPort = hostObject.portnumber
            getButton.Enabled = True
            Return True
        Else
            getButton.Enabled = False
            Return False
        End If
    End Function
    Private Sub txtHost_LostFocus(sender As Object, e As EventArgs) Handles txtHost.LostFocus
        Dim unused As Boolean = CheckHost()
    End Sub

    Private Function GetHostPort(thisHost As String) As (HostName As String, portnumber As Integer)
        Dim MyHost As String = ""
        Dim MyPort As Integer
        Dim parts As String() = thisHost.Split(":")
        If parts.Count <> 2 Then
            Dim ret As DialogResult
            Dim msg As String = "Invalid hostname/port pair." & vbCrLf & vbCrLf &
                "It must be in the format 'hostname:port'" & vbCrLf &
                "Example: localhost:16000"
            Dim msgStyle As MsgBoxStyle = MsgBoxStyle.OkOnly
            ret = MsgBox(msg, msgStyle, "Invalid data entered")
        Else
            MyHost = parts(0)
            MyPort = Val(parts(1))
        End If
        Return (MyHost, MyPort)
    End Function

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles getButton.Click
        Dim oldColor As Color = DataLight.ForeColor
        DataLight.ForeColor = Color.Red
        Application.DoEvents()
        Me.Refresh()
        Try
            remote = New TcpClient(hostname, hostPort)
        Catch ex As Exception
            Dim resp As DialogResult
            Dim txtfmt As String = "Unable to connect to host {0} on " & vbCrLf &
                "port {1}.  The complete error is: " & vbCrLf & vbCrLf &
                "{2}."
            resp = MsgBox(String.Format(txtfmt, hostname, hostPort, ex.Message), vbOKOnly, "Connection Error")
            Return
        End Try
        Dim netStream As NetworkStream = remote.GetStream
        Dim b As Byte = 0
        Do While True
            Try
                netStream.ReadTimeout = 1000
                netStream.ReadByte()
            Catch ex As Exception
                Exit Do
            End Try
        Loop

        myDevs = GetConfig(remote)
        ListOfDevs.Items.Clear()
        For Each d As devs In myDevs
            ListOfDevs.Items.Add(d.DevName)
        Next
        DataLight.ForeColor = oldColor
        Application.DoEvents()
        Me.Refresh()
    End Sub

    Private Function GetConfig(remClient As TcpClient) As List(Of devs)

        Dim netStream As NetworkStream = remClient.GetStream()
        Dim readStream As New StreamReader(netStream)
        Dim writeStream As New StreamWriter(netStream)
        writeStream.AutoFlush = True
        writeStream.WriteLine("GUI_SDEV")
        writeStream.Flush()
        ' Wait for a reply
        Do While Not (netStream.DataAvailable)
            Application.DoEvents()
        Loop
        Dim reply As New List(Of String)
        Dim thisLine As String = ""
        Do Until thisLine.Trim = "[[EOD]]"
            thisLine = readStream.ReadLine
            If thisLine.Trim = "[[EOD]]" Then
                ' End of Data 
                Exit Do
            Else
                reply.Add(thisLine)
            End If
        Loop
        Dim dList As New List(Of devs)
        For Each l As String In reply
            Dim lSplit As String() = l.Split("|")
            Dim thisDev As New devs
            thisDev.DevName = lSplit(1)
            thisDev.DevDescription = lSplit(2)
            thisDev.DevType = lSplit(3)
            thisDev.ConnType = lSplit(4)
            thisDev.DevDest = lSplit(5)
            thisDev.OS = lSplit(6)
            thisDev.Auto = lSplit(7)
            thisDev.PDF = lSplit(8)
            dList.Add(thisDev)
        Next

        remote.Close()
        Return dList

    End Function

    Private Sub ListOfDevs_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListOfDevs.SelectedIndexChanged
        Dim editDev As New devs
        editDev = myDevs.Item(ListOfDevs.SelectedIndex)
        devName.Text = editDev.DevName
        devDescription.Text = editDev.DevDescription
        devType.SelectedIndex = (Val(editDev.DevType))
        devConn.SelectedIndex = (Val(editDev.ConnType))
        devOS.SelectedIndex = (Val(editDev.OS))
        devDest.Text = editDev.DevDest
        devAuto.Checked = editDev.Auto
        devPDF.Checked = editDev.PDF
    End Sub
End Class

Public Class parmStruct
    Public arg As String
    Public value As String
End Class

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