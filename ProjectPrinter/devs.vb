Public Class devs
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


    Public DevName As String
    Public DevDescription As String
    Public DevType As Integer
    Public ConnType As Integer
    Public DevDest As String
    Public OS As Integer
    Public Auto As Boolean
    Public PDF As Boolean


End Class
