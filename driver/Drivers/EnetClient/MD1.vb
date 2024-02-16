Module MD1

    Public Function HEADER_SUM(ByVal R_VALUE() As Byte) As Byte

        Dim V As Integer = 0
        Dim I As Integer

        For I = 0 To 17
            V += R_VALUE(I)
            If V >= 256 Then V = V - 256
        Next

        Return V

    End Function
    ' HEADER BYTE SUM

    Public Function ByteSumToASC(ByVal Data() As Byte, ByVal StartP As Integer, ByVal EndP As Integer) As Byte()

        Dim Result(1) As Byte
        Dim k As Integer = 0
        Dim s As String
        Dim i As Integer

        For i = StartP To EndP
            k += Data(i)
            If k >= 256 Then k = k - 256
        Next

        s = Hex(k)
        If s.Length = 1 Then s = "0" & s

        Result(0) = AscW(Mid(s, 1, 1))
        Result(1) = AscW(Mid(s, 2, 1))

        Return Result

    End Function
    ' 바이트 섬 아스키 컨버팅

End Module
