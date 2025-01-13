Imports PdfSharp.Fonts
Imports System.IO

Public Class ChainprinterFontResolver
    Implements IFontResolver

    Private ReadOnly _fontPath As String

    ' Constructor to initialize the path of the Chainprinter font
    Public Sub New()
        '_fontPath = Path.Combine(Directory.GetCurrentDirectory(), "chainprinter.ttf")
        _fontPath = Path.Combine(Directory.GetCurrentDirectory(), "ibmplexmono.ttf")
    End Sub

    Public Function GetFont(faceName As String) As Byte() Implements IFontResolver.GetFont
        ' Load the font file as a byte array
        If File.Exists(_fontPath) Then
            Return File.ReadAllBytes(_fontPath)
        Else
            Throw New FileNotFoundException($"The font file '{_fontPath}' was not found.")
        End If
    End Function

    Public Function ResolveTypeface(familyName As String, isBold As Boolean, isItalic As Boolean) As FontResolverInfo Implements IFontResolver.ResolveTypeface
        ' Ensure the requested font is "Chainprinter"
        If familyName.Equals("Chainprinter", StringComparison.CurrentCultureIgnoreCase) Then
            ' Return the same font regardless of bold/italic attributes
            Return New FontResolverInfo("Chainprinter")
        End If

        ' Return Nothing if the font family is not supported
        Return Nothing
    End Function
End Class
