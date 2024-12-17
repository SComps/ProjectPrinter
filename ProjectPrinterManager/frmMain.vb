Imports System.Threading
Imports ProjectPrinterManager.My
Imports Windows.Media.Protection.PlayReady

Imports System.Text
Imports System.Net.Sockets
Imports System.IO
Public Class frmMain

    Private enteredHost As String = ""
    Private hostname As String = ""
    Private hostPort As Integer

    Dim remote As New TcpClient()
    Dim netStream As NetworkStream
    Dim reader As StreamReader
    Dim writer As StreamWriter

    Private Sub frmMain_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        enteredHost = My.Settings.LastHost
        If enteredHost.Trim = "" Then
            Stop
            My.Settings.LastHost = "localhost:16000"
        Else
            txtHost.Text = enteredHost
        End If

    End Sub



    Private Sub txtHost_LostFocus(sender As Object, e As EventArgs) Handles txtHost.LostFocus
        If txtHost.Text <> enteredHost Then
            enteredHost = txtHost.Text.Trim
            My.Settings.LastHost = enteredHost
        End If
    End Sub

    Private Async Sub cmdConnect_Click(sender As Object, e As EventArgs) Handles cmdConnect.Click

        Dim vals = GetHostPort(enteredHost)
        hostname = vals.HostName
        hostPort = vals.portnumber
        If remote.Connected Then
            cmdConnect.Text = "Close"
        Else
            Await remote.ConnectAsync(hostname, hostPort)
            netStream = remote.GetStream()
            writer = New StreamWriter(netStream)
            reader = New StreamReader(netStream)
            writer.WriteLine("HELLO")
            Dim recd As String = GetData()
            If recd.Trim = "" Then Stop
        End If

    End Sub

    Public Function GetData()
        While netStream.DataAvailable
            Dim received As String = reader.ReadToEnd()
            MsgBox(received.ToString)
            Return received
        End While
        Return ""
    End Function

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


End Class
