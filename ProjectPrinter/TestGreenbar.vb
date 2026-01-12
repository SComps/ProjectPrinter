Imports PdfSharp.Fonts

Module TestGreenbar
    ''' <summary>
    ''' Quick test to generate a sample PDF with the programmatic greenbar background.
    ''' Run with: dotnet run -- test
    ''' </summary>
    Public Sub RunTest()
        Console.WriteLine("=== Greenbar Background Test ===")
        Console.WriteLine()

        ' Set up the font resolver (required for PDF generation)
        GlobalFontSettings.FontResolver = New ChainprinterFontResolver()

        ' Create a test device
        Dim testDev As New devs()
        testDev.DevName = "TEST_PRINTER"
        testDev.DevDescription = "Test Printer for Greenbar"
        testDev.OS = OSType.OS_MVS38J
        testDev.Orientation = 0  ' Landscape with background
        testDev.OutDest = "."

        ' Generate sample print lines (simulating a job)
        Dim testLines As New List(Of String)

        ' Add a header
        testLines.Add("**** TEST JOB OUTPUT - GREENBAR BACKGROUND TEST ****")
        testLines.Add("")
        testLines.Add("  This is a test of the programmatic greenbar paper background.")
        testLines.Add("  The background should show:")
        testLines.Add("    - Alternating green and white bands")
        testLines.Add("    - Tractor feed holes on left and right edges")
        testLines.Add("    - Diamond alignment fiducials in top corners")
        testLines.Add("    - Dotted vertical margin lines")
        testLines.Add("")

        ' Add 66 lines to fill exactly one standard page
        Dim longLinePrefix As String = "Line "
        For i As Integer = 1 To 66
            Dim baseText As String = $"{i:D2}: ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789!@#$%^&*() "
            Dim lineOutput As String = (longLinePrefix & baseText & baseText).Substring(0, 132)
            testLines.Add(lineOutput)
        Next

        ' Add some more content
        testLines.Add("")
        testLines.Add("**** END OF TEST JOB ****")

        ' Generate the PDF
        Dim outputPath As String = "test_greenbar_output.pdf"
        Console.WriteLine($"Generating test PDF: {outputPath}")
        Console.WriteLine($"Total lines: {testLines.Count}")
        Console.WriteLine()

        Try
            Dim result As String = testDev.CreatePDF("GREENBAR_TEST", testLines, outputPath)
            Console.WriteLine()
            Console.WriteLine($"SUCCESS! PDF generated: {result}")
            Console.WriteLine()
            Console.WriteLine("Please open the PDF and verify the greenbar background matches the original JPG.")
        Catch ex As Exception
            Console.WriteLine($"ERROR: {ex.Message}")
            Console.WriteLine(ex.StackTrace)
        End Try
    End Sub
End Module
