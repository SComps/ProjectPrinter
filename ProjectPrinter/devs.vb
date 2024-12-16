Imports System.IO
Imports System.Net.Http
Imports System.Net.Sockets
Imports System.Runtime.CompilerServices
Imports System.Text
Imports System.Threading

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
        Await StartAsync()
    End Sub
    ' Connects to the server and starts receiving data
    Public Async Function StartAsync() As Task
        client = New TcpClient()
        _cancellationTokenSource = New CancellationTokenSource()

        Try
            Program.Log("Connecting to server...")
            Await client.ConnectAsync(remoteHost, remotePort)
            clientStream = client.GetStream()
            Program.Log("Connected to server.")

            ' Start receiving data
            Await ReceiveDataAsync(_cancellationTokenSource.Token)
        Catch ex As Exception
            Program.Log($"Error: {ex.Message}")
            Throw
        Finally
            Disconnect()
        End Try
    End Function

    ' Continuously receives data from the server
    Private Async Function ReceiveDataAsync(cancellationToken As CancellationToken) As Task
        Dim buffer((4096) As Byte   ' Set up a 4K buffer which should be large enough to handle even the fastest connection.
        Log(String.Format("Initializing receive buffer with {0:N0} bytes.", buffer.Length))
        Dim dataBuilder As New StringBuilder()

        Try
            While Not cancellationToken.IsCancellationRequested
                ' Read incoming data
                Dim bytesRead As Integer = Await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                If bytesRead = 0 Then
                    ' Server closed the connection
                    Program.Log("Server closed the connection.")

                    ' Process any remaining data in the buffer as the last line
                    If dataBuilder.Length > 0 Then
                        Program.Log($"Received (last line): {dataBuilder.ToString()}")
                    End If

                    Exit While
                End If

                ' Decode received bytes and append to the buffer
                Dim receivedPart As String = Encoding.UTF8.GetString(buffer, 0, bytesRead)
                dataBuilder.Append(receivedPart)

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
                        ' End the current line, add FF to start a new one
                        lines.Add(currentLine.ToString())
                        currentLine.Clear()
                        currentLine.Append(c)
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
                        Program.Log($"Received: {line}")
                    End If
                Next
            End While
        Catch ex As OperationCanceledException
            Program.Log("Receiving canceled.")
        Catch ex As Exception
            Program.Log($"Error receiving data: {ex.Message}")
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

End Class
