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
            Console.WriteLine("Connecting to server...")
            Await client.ConnectAsync(remoteHost, remotePort)
            clientStream = client.GetStream()
            Console.WriteLine("Connected to server.")

            ' Start receiving data
            Await ReceiveDataAsync(_cancellationTokenSource.Token)
        Catch ex As Exception
            Console.WriteLine($"Error: {ex.Message}")
            Throw
        Finally
            Disconnect()
        End Try
    End Function

    ' Continuously receives data from the server
    Private Async Function ReceiveDataAsync(cancellationToken As CancellationToken) As Task
        Dim buffer(1024 - 1) As Byte
        Dim dataBuilder As New StringBuilder()

        Try
            While Not cancellationToken.IsCancellationRequested
                ' Check for incoming data
                If clientStream.DataAvailable Then
                    Dim bytesRead As Integer = Await clientStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                    If bytesRead = 0 Then
                        ' Server closed the connection
                        Console.WriteLine("Server closed the connection.")
                        Exit While
                    End If

                    ' Decode received bytes and append to the buffer
                    Dim receivedPart As String = Encoding.UTF8.GetString(buffer, 0, bytesRead)
                    dataBuilder.Append(receivedPart)

                    ' Process complete lines
                    Dim fullData As String = dataBuilder.ToString()
                    Dim lines As String() = fullData.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)

                    ' Output complete lines and retain any partial line
                    For i As Integer = 0 To lines.Length - 2
                        Console.WriteLine($"Received: {lines(i)}")
                    Next

                    ' Clear the builder, retaining any incomplete line
                    dataBuilder.Clear()
                    If Not fullData.EndsWith(vbCrLf) AndAlso Not fullData.EndsWith(vbLf) Then
                        dataBuilder.Append(lines.Last())
                    End If
                Else
                    ' Stream idle, process any remaining partial data
                    If dataBuilder.Length > 0 Then
                        Console.WriteLine($"Received (idle): {dataBuilder.ToString()}")
                        dataBuilder.Clear()
                    End If

                    ' Avoid busy-waiting; introduce a small delay
                    Await Task.Delay(50, cancellationToken)
                End If
            End While
        Catch ex As OperationCanceledException
            Console.WriteLine("Receiving canceled.")
        Catch ex As Exception
            Console.WriteLine($"Error receiving data: {ex.Message}")
        End Try
    End Function


    ' Disconnects the client
    Public Sub Disconnect()
        Try
            _cancellationTokenSource?.Cancel()
            clientStream?.Close()
            client?.Close()
            Console.WriteLine("Disconnected from server.")
        Catch ex As Exception
            Console.WriteLine($"Error during disconnection: {ex.Message}")
        End Try
    End Sub

End Class
