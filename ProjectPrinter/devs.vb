Imports System.Net.Sockets

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
    Private WithEvents client As New TcpClient()
    Public Sub Connect()
        Dim cE As String = "Attempting to establish a connection to {0}"
        Dim cS As String = "Connection to {0} established."
        Program.Log(String.Format(cE, DevDest))
        SplitDestination(DevDest)
        Try
            client.Connect(remoteHost, remotePort)
            Program.Log(String.Format(cS, DevDest))
        Catch ex As Exception
            Program.Log(ex.Message)
        End Try
    End Sub

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
End Class
