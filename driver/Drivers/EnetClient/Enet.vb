Imports System.Windows.Forms.Application
Imports System.Runtime.InteropServices

Public Class Enet

#Region " 변수 선언 "

    Public Event ENET_EVENT(ByVal E_CODE As NetEvent)
    Friend WithEvents Timer_SEND As New System.Timers.Timer

    Private _LSEnet As New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
    Private _PlcIP As String = "192.168.1.80"
    Private _PlcPort As Integer = 2004
    Private _SendBufferSize As Integer = 2048
    Private _SendTimeout As Integer = 2000
    Private _ReceiveBufferSize As Integer = 2048
    Private _ReceiveTimeout As Integer = 2000
    Private _RunCommand As Boolean = False
    Private _NetMode As Net_Mode = Net_Mode.DisConnection
    Private _NetErrCount As Integer = 0


    Private _READ_BLOCK(31) As NET_BLOCK               '읽기 데이타 저장 변수 선언


    Private _READ_MES As New Queue
    Private _WRITE_MES As New Queue
    Private _WriteID As Integer = 1                    'write invoke Id

    Private _ReadWriteCount As Integer                 '읽고 쓰기 BfCount

    Private _GcCount As Integer                        '가비지 카운터

    ' 포인터 선언
    'Private _pMemory As IntPtr                         ' 데이타를 저장 하는 포인터
    Private _memDic As Dictionary(Of String, IntPtr)

    Private _NetRunCount As Long                       '통신 시도 카운터
    Private _NetOkCount As Long                        '통신 성공 카운터
    Private _NetNgCount As Long                        '통신 실패 카운터


#End Region

#Region " 구조체 와 열거형 멤버 "

    Private Enum Net_Mode
        DisConnection = 0
        ConnectionRun = 1
        NetOK = 2
        ReadWrite = 3

    End Enum

    Public Structure NET_BLOCK
        Dim USE As Boolean
        Dim DEVICE As String
        Dim INTERVAL As Integer
        Dim REAL_INTERVAL As Integer
        Dim SEND_DATA() As Byte
        Dim START_ADD As Integer
        Dim DATA_COUNT As Integer
        Dim SAVE_ADD As Integer
        Dim INVOKEID As Byte
    End Structure
    ' 읽기 데이타를 저장하는 구조

    Public Structure Net_Count
        Dim NetCount As Long
        Dim NetOk As Long
        Dim NetNg As Long
    End Structure

    Public Enum NetEvent
        DisConnection = 0
        Connection = 1
        SendErr = 2
        RecErr = 3
        NetStart = 4
        NetEnd = 5
    End Enum
    ' 이벤트를 정의하는 열거형 멤버

#End Region

#Region " 생성과 소멸 "

    Public Sub New()
        MyBase.New()

        Me._memDic = New Dictionary(Of String, IntPtr)
        Me.Timer_SEND.Interval = 500
        Me.Timer_SEND.Enabled = False
        Me.Timer_SEND.AutoReset = True
        Me.LogWrite("--------------------------------------------")
        Me.LogWrite("LS-ENET 엔진을 생성 하였습니다.")

    End Sub
    ' 메모리 사이즈를 설정 하지 않은 상태에서 드라이브를 생성

    Protected Overrides Sub Finalize()
        MyBase.Finalize()

        Me.LogWrite("LS-ENET 엔진을 해제 하였습니다.")

    End Sub

#End Region

#Region " 속성 "

    Public Property SET_IP() As String
        Get
            Return Me._PlcIP
        End Get
        Set(ByVal Value As String)
            Me._PlcIP = Value
        End Set
    End Property
    ' Plc ip 속성

    Public Property SET_PORT() As Integer
        Get
            Return Me._PlcPort
        End Get
        Set(ByVal Value As Integer)
            Me._PlcPort = Value
        End Set
    End Property
    ' Plc port 속성

    Public Property SendBufferSize() As Integer
        Get
            Return Me._SendBufferSize
        End Get
        Set(ByVal Value As Integer)
            Me._SendBufferSize = Value
        End Set
    End Property
    ' 버퍼 사이즈 속성

    Public Property ReceiveBufferSize() As Integer
        Get
            Return Me._ReceiveBufferSize
        End Get
        Set(ByVal Value As Integer)
            Me._ReceiveBufferSize = Value
        End Set
    End Property
    ' 버퍼 사이즈 속성

    Public Property SendTimeout() As Integer
        Get
            Return Me._SendTimeout
        End Get
        Set(ByVal Value As Integer)
            Me._SendTimeout = Value
        End Set
    End Property
    ' 타임아웃 속성

    Public Property ReceiveTimeout() As Integer
        Get
            Return Me._ReceiveTimeout
        End Get
        Set(ByVal Value As Integer)
            Me._ReceiveTimeout = Value
        End Set
    End Property
    ' 타임아웃 속성

    Public Property Blocks As NET_BLOCK()
        Get
            Return Me._READ_BLOCK
        End Get
        Set(value As NET_BLOCK())

        End Set
    End Property

#End Region

#Region " 외부 메서드 "

    Public Sub NetRun()

        If Me._RunCommand = True Then
            Exit Sub
        End If

        Me._RunCommand = True

        Dim _THReadWrite As New System.Threading.Thread(AddressOf TH_ReadWrite)
        _THReadWrite.Name = "NetRun"
        _THReadWrite.IsBackground = True
        _THReadWrite.Start()

        Me.Timer_SEND.Enabled = True

        Me.LogWrite("PLC IP 를 " & Me._PlcIP & " 로 셋팅하였습니다.")
        Me.LogWrite("PLC PORT 를 " & Me._PlcPort.ToString & " 로 셋팅하였습니다.")
        Me.LogWrite("SEND BUFFER SIZE 를 " & Me._SendBufferSize.ToString & " 로 셋팅하였습니다.")
        Me.LogWrite("RECEIVE BUFFER SIZE 를 " & Me._ReceiveBufferSize.ToString & " 로 셋팅하였습니다.")
        Me.LogWrite("SEND TIME OUT 를 " & Me._SendTimeout.ToString & " 로 셋팅하였습니다.")
        Me.LogWrite("RECEIVE TIME OUT 를 " & Me._ReceiveTimeout.ToString & " 로 셋팅하였습니다.")

        Me.LogWrite("통신을 시작 하였습니다.")

        RaiseEvent ENET_EVENT(NetEvent.NetStart)

    End Sub
    ' plc 와 연결하는 메서드

    'Public Sub NetEnd()

    '    Dim I As Integer

    '    Me._THEnd = True

    '    'If Me._RunCommand = True Then
    '    '    While Me._THEnd
    '    '        I += 1
    '    '        If I > 50 Then
    '    '            Exit While
    '    '        End If
    '    '        System.Threading.Thread.Sleep(100)
    '    '    End While
    '    'End If

    '    RaiseEvent ENET_EVENT(NetEvent.NetEnd)

    'End Sub
    '' 진행중인 쓰래드를 종료한다.

    Public Sub NetEnd()

        Me._RunCommand = False


    End Sub
    ' PLC와의 통신 중단

    Public Function SET_NetBlock(ByVal Device As String, ByVal MemAddress As Integer, ByVal SaveAddress As Integer, ByVal ReadCount As Integer, ByVal ReadTime As Integer, ByVal FrameNum As Integer) As Integer
        ' "통신 블럭 저장 %*B100,100,10 어드레스,읽을갯수,통신주기100ms"
        ' BLOCK 타입은 "%*B100,읽을갯수,통신주기"

        If Me._memDic.ContainsKey(Device) = False Then
            Me._memDic.Add(Device, System.Runtime.InteropServices.Marshal.AllocHGlobal(1000000))
        End If

        If FrameNum < 0 Or FrameNum > 31 Then
            Return -1
        End If

        If ReadCount < 0 Or ReadCount > 1460 Then
            Return -1
        End If

        On Error GoTo BlockErr

        Dim SEND_DATA(40) As Byte

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = FrameNum + 100      'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H15                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H54                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H14                       ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &H9                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("B")
        SEND_DATA(33) = AscW(Mid(Format(MemAddress, "000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format(MemAddress, "000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format(MemAddress, "000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format(MemAddress, "000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format(MemAddress, "000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format(MemAddress, "000000"), 6, 1))

        SEND_DATA(39) = ReadCount Mod 265
        SEND_DATA(40) = ReadCount \ 256


        Me._READ_BLOCK(FrameNum).DEVICE = Device
        Me._READ_BLOCK(FrameNum).START_ADD = MemAddress
        Me._READ_BLOCK(FrameNum).DATA_COUNT = ReadCount
        Me._READ_BLOCK(FrameNum).INVOKEID = FrameNum + 100
        Me._READ_BLOCK(FrameNum).SEND_DATA = SEND_DATA
        Me._READ_BLOCK(FrameNum).INTERVAL = ReadTime
        Me._READ_BLOCK(FrameNum).SAVE_ADD = SaveAddress
        Me._READ_BLOCK(FrameNum).USE = True

        Return FrameNum

BlockErr:

        Return -1

    End Function
    ' read block 를 등록한다.

    Public Function NetCount() As Net_Count

        Dim ReturnD As New Net_Count
        ReturnD.NetCount = Me._NetRunCount
        ReturnD.NetOk = Me._NetOkCount
        ReturnD.NetNg = Me._NetNgCount
        Return ReturnD

    End Function
    ' 통신 횟수를 리턴 한다.

#End Region

#Region " 외부 WRITE FUNCTION "


    Public Function WriteW_Bool(ByVal Device As String, ByVal MemAddress As Integer, ByVal BitOffset As Integer, ByVal value As Boolean) As Integer

        ' 비트 쓰기
        ' 비트 쓰기
        ' 비트 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        ' "어드레스,값/단위:DEC"
        ' 비트쓰기 와 워드 쓰기를 분리한다. 

        Dim SEND_DATA(42) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H17                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H0                        ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &HA                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("X")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress * 16 + BitOffset), "0000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress * 16 + BitOffset), "0000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress * 16 + BitOffset), "0000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress * 16 + BitOffset), "0000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress * 16 + BitOffset), "0000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress * 16 + BitOffset), "0000000"), 6, 1))
        SEND_DATA(39) = AscW(Mid(Format((MemAddress * 16 + BitOffset), "0000000"), 7, 1))
        SEND_DATA(40) = &H1
        SEND_DATA(41) = &H0
        If value = True Then
            SEND_DATA(42) = &H1
        Else
            SEND_DATA(42) = &H0
        End If


        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*W100.0 의 형식의 bool 데이타를 쓴다.

    Public Function WriteB_Bool(ByVal Device As String, ByVal MemAddress As Integer, ByVal BitOffset As Integer, ByVal value As Boolean) As Integer

        ' 비트 쓰기
        ' 비트 쓰기
        ' 비트 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        ' "어드레스,값/단위:DEC"
        ' 비트쓰기 와 워드 쓰기를 분리한다. 

        Dim SEND_DATA(42) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H17                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H0                        ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &HA                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("X")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress * 8 + BitOffset), "0000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress * 8 + BitOffset), "0000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress * 8 + BitOffset), "0000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress * 8 + BitOffset), "0000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress * 8 + BitOffset), "0000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress * 8 + BitOffset), "0000000"), 6, 1))
        SEND_DATA(39) = AscW(Mid(Format((MemAddress * 8 + BitOffset), "0000000"), 7, 1))
        SEND_DATA(40) = &H1
        SEND_DATA(41) = &H0
        If value = True Then
            SEND_DATA(42) = &H1
        Else
            SEND_DATA(42) = &H0
        End If


        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100.0 의 형식의 bool 데이타를 쓴다.

    Public Function WriteX_Bool(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Boolean) As Integer

        ' 비트 쓰기
        ' 비트 쓰기
        ' 비트 쓰기

        On Error GoTo BlockErr

        If MemAddress > 9999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        ' "어드레스,값/단위:DEC"
        ' 비트쓰기 와 워드 쓰기를 분리한다. 

        Dim SEND_DATA(42) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H17                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H0                        ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &HA                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("X")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "0000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "0000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "0000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "0000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "0000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "0000000"), 6, 1))
        SEND_DATA(39) = AscW(Mid(Format((MemAddress), "0000000"), 7, 1))
        SEND_DATA(40) = &H1
        SEND_DATA(41) = &H0
        If value = True Then
            SEND_DATA(42) = &H1
        Else
            SEND_DATA(42) = &H0
        End If


        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*X100 의 형식의 bool 데이타를 쓴다.

    Public Function Write_UI1(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Byte) As Integer

        ' 바이트 쓰기
        ' 바이트 쓰기
        ' 바이트 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(41) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H16                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H14                       ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &H9                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("B")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
        SEND_DATA(39) = &H1
        SEND_DATA(40) = &H0
        SEND_DATA(41) = value


        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 의 형식의 BYTE 데이타를 쓴다.

    Public Function Write_UI2(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Integer) As Integer

        ' 워드 쓰기
        ' 워드 쓰기
        ' 워드 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(42) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H17                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H14                       ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &H9                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("B")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
        SEND_DATA(39) = &H2
        SEND_DATA(40) = &H0
        SEND_DATA(41) = value Mod 256
        SEND_DATA(42) = value \ 256

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 의 형식의 워드 데이타를 쓴다.

    '    Public Function WriteW_UI2(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Integer) As Integer

    '        ' 워드 쓰기
    '        ' 워드 쓰기
    '        ' 워드 쓰기

    '        On Error GoTo BlockErr

    '        If MemAddress > 499999 Then
    '            Return -1
    '        End If

    '        ' LSIS-GLOFA
    '        Dim SEND_DATA(42) As Byte

    '        ' invoke Id 증가
    '        If Me._WriteID = 99 Then
    '            Me._WriteID = 1
    '        Else
    '            Me._WriteID += 1
    '        End If

    '        ' HEADER
    '        SEND_DATA(0) = AscW("L")
    '        SEND_DATA(1) = AscW("G")
    '        SEND_DATA(2) = AscW("I")
    '        SEND_DATA(3) = AscW("S")
    '        SEND_DATA(4) = AscW("-")
    '        SEND_DATA(5) = AscW("G")
    '        SEND_DATA(6) = AscW("L")
    '        SEND_DATA(7) = AscW("O")
    '        SEND_DATA(8) = AscW("F")
    '        SEND_DATA(9) = AscW("A")
    '        SEND_DATA(10) = &H0                 'plc info
    '        SEND_DATA(11) = &H0                 'plc info
    '        SEND_DATA(12) = &H0                 'reserved
    '        SEND_DATA(13) = &H33                'h33   mmi->plc
    '        SEND_DATA(14) = Me._WriteID         'invoke Id
    '        SEND_DATA(15) = &H0                 'invoke Id
    '        SEND_DATA(16) = &H17                'Application Instruction
    '        SEND_DATA(17) = &H0                 'Application Instruction
    '        SEND_DATA(18) = &H0                 'reserved
    '        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
    '        ' BODY
    '        SEND_DATA(20) = &H58                       ' COMMAND
    '        SEND_DATA(21) = &H0                        ' COMMAND
    '        SEND_DATA(22) = &H2                        ' 데이타 타입
    '        SEND_DATA(23) = &H0                        ' 데이타 타입
    '        SEND_DATA(24) = &H0                        ' Don’t Care
    '        SEND_DATA(25) = &H0                        ' Don’t Care
    '        SEND_DATA(26) = &H1                        ' 블럭 넘버
    '        SEND_DATA(27) = &H0                        ' 블럭 넘버
    '        SEND_DATA(28) = &H9                        ' 어드레스 길이 
    '        SEND_DATA(29) = &H0                        ' 어드레스 길이
    '        SEND_DATA(30) = AscW("%")
    '        SEND_DATA(31) = AscW(Device)
    '        SEND_DATA(32) = AscW("W")
    '        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
    '        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
    '        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
    '        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
    '        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
    '        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
    '        SEND_DATA(39) = &H2
    '        SEND_DATA(40) = &H0
    '        SEND_DATA(41) = value Mod 256
    '        SEND_DATA(42) = value \ 256


    '        Me._WRITE_MES.Enqueue(SEND_DATA)

    '        Return Me._WriteID

    'BlockErr:

    '        Return -1

    '    End Function
    '    ' %*W100 의 형식의 워드 데이타를 쓴다.

    Public Function Write_UI4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Long) As Integer

        ' 더블워드 쓰기
        ' 더블워드 쓰기
        ' 더블워드 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(44) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H19                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H14                      ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &H9                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("B")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
        SEND_DATA(39) = &H4
        SEND_DATA(40) = &H0
        ' 포인터 선언 및 8 바이트 할당
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Long
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 4)

        ' 메모리 반환
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 의 형식의 UI4 데이타를 쓴다.

    '    Public Function WriteD_UI4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Long) As Integer

    '        ' 더블워드 쓰기
    '        ' 더블워드 쓰기
    '        ' 더블워드 쓰기

    '        On Error GoTo BlockErr

    '        If MemAddress > 999999 Then
    '            Return -1
    '        End If

    '        ' LSIS-GLOFA
    '        Dim SEND_DATA(44) As Byte

    '        ' invoke Id 증가
    '        If Me._WriteID = 99 Then
    '            Me._WriteID = 1
    '        Else
    '            Me._WriteID += 1
    '        End If

    '        ' HEADER
    '        SEND_DATA(0) = AscW("L")
    '        SEND_DATA(1) = AscW("G")
    '        SEND_DATA(2) = AscW("I")
    '        SEND_DATA(3) = AscW("S")
    '        SEND_DATA(4) = AscW("-")
    '        SEND_DATA(5) = AscW("G")
    '        SEND_DATA(6) = AscW("L")
    '        SEND_DATA(7) = AscW("O")
    '        SEND_DATA(8) = AscW("F")
    '        SEND_DATA(9) = AscW("A")
    '        SEND_DATA(10) = &H0                 'plc info
    '        SEND_DATA(11) = &H0                 'plc info
    '        SEND_DATA(12) = &H0                 'reserved
    '        SEND_DATA(13) = &H33                'h33   mmi->plc
    '        SEND_DATA(14) = Me._WriteID         'invoke Id
    '        SEND_DATA(15) = &H0                 'invoke Id
    '        SEND_DATA(16) = &H19                'Application Instruction
    '        SEND_DATA(17) = &H0                 'Application Instruction
    '        SEND_DATA(18) = &H0                 'reserved
    '        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
    '        ' BODY
    '        SEND_DATA(20) = &H58                       ' COMMAND
    '        SEND_DATA(21) = &H0                        ' COMMAND
    '        SEND_DATA(22) = &H3                        ' 데이타 타입
    '        SEND_DATA(23) = &H0                        ' 데이타 타입
    '        SEND_DATA(24) = &H0                        ' Don’t Care
    '        SEND_DATA(25) = &H0                        ' Don’t Care
    '        SEND_DATA(26) = &H1                        ' 블럭 넘버
    '        SEND_DATA(27) = &H0                        ' 블럭 넘버
    '        SEND_DATA(28) = &H9                        ' 어드레스 길이 
    '        SEND_DATA(29) = &H0                        ' 어드레스 길이
    '        SEND_DATA(30) = AscW("%")
    '        SEND_DATA(31) = AscW(Device)
    '        SEND_DATA(32) = AscW("D")
    '        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
    '        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
    '        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
    '        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
    '        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
    '        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
    '        SEND_DATA(39) = &H4
    '        SEND_DATA(40) = &H0
    '        ' 포인터 선언 및 8 바이트 할당
    '        Dim Pt As IntPtr
    '        Pt = Marshal.AllocHGlobal(8)

    '        Dim RValue(0) As Long
    '        RValue(0) = value

    '        Marshal.Copy(RValue, 0, Pt, 1)
    '        Marshal.Copy(Pt, SEND_DATA, 41, 4)

    '        ' 메모리 반환
    '        Marshal.FreeHGlobal(Pt)

    '        Me._WRITE_MES.Enqueue(SEND_DATA)

    '        Return Me._WriteID

    'BlockErr:

    '        Return -1

    '    End Function
    '    ' %*D100 의 형식의 UI4 데이타를 쓴다.

    Public Function Write_SI2(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Short) As Integer

        ' 워드 쓰기
        ' 워드 쓰기
        ' 워드 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(42) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H17                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H14                       ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &H9                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("B")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
        SEND_DATA(39) = &H2
        SEND_DATA(40) = &H0
        ' 포인터 선언 및 8 바이트 할당
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Short
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 2)

        ' 메모리 반환
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 의 형식의 SI2 데이타를 쓴다.

    '    Public Function WriteW_SI2(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Short) As Integer

    '        ' 워드 쓰기
    '        ' 워드 쓰기
    '        ' 워드 쓰기

    '        On Error GoTo BlockErr

    '        If MemAddress > 499999 Then
    '            Return -1
    '        End If

    '        ' LSIS-GLOFA
    '        Dim SEND_DATA(42) As Byte

    '        ' invoke Id 증가
    '        If Me._WriteID = 99 Then
    '            Me._WriteID = 1
    '        Else
    '            Me._WriteID += 1
    '        End If

    '        ' HEADER
    '        SEND_DATA(0) = AscW("L")
    '        SEND_DATA(1) = AscW("G")
    '        SEND_DATA(2) = AscW("I")
    '        SEND_DATA(3) = AscW("S")
    '        SEND_DATA(4) = AscW("-")
    '        SEND_DATA(5) = AscW("G")
    '        SEND_DATA(6) = AscW("L")
    '        SEND_DATA(7) = AscW("O")
    '        SEND_DATA(8) = AscW("F")
    '        SEND_DATA(9) = AscW("A")
    '        SEND_DATA(10) = &H0                 'plc info
    '        SEND_DATA(11) = &H0                 'plc info
    '        SEND_DATA(12) = &H0                 'reserved
    '        SEND_DATA(13) = &H33                'h33   mmi->plc
    '        SEND_DATA(14) = Me._WriteID         'invoke Id
    '        SEND_DATA(15) = &H0                 'invoke Id
    '        SEND_DATA(16) = &H17                'Application Instruction
    '        SEND_DATA(17) = &H0                 'Application Instruction
    '        SEND_DATA(18) = &H0                 'reserved
    '        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
    '        ' BODY
    '        SEND_DATA(20) = &H58                       ' COMMAND
    '        SEND_DATA(21) = &H0                        ' COMMAND
    '        SEND_DATA(22) = &H2                        ' 데이타 타입
    '        SEND_DATA(23) = &H0                        ' 데이타 타입
    '        SEND_DATA(24) = &H0                        ' Don’t Care
    '        SEND_DATA(25) = &H0                        ' Don’t Care
    '        SEND_DATA(26) = &H1                        ' 블럭 넘버
    '        SEND_DATA(27) = &H0                        ' 블럭 넘버
    '        SEND_DATA(28) = &H9                        ' 어드레스 길이 
    '        SEND_DATA(29) = &H0                        ' 어드레스 길이
    '        SEND_DATA(30) = AscW("%")
    '        SEND_DATA(31) = AscW(Device)
    '        SEND_DATA(32) = AscW("W")
    '        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
    '        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
    '        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
    '        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
    '        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
    '        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
    '        SEND_DATA(39) = &H2
    '        SEND_DATA(40) = &H0
    '        ' 포인터 선언 및 8 바이트 할당
    '        Dim Pt As IntPtr
    '        Pt = Marshal.AllocHGlobal(8)

    '        Dim RValue(0) As Short
    '        RValue(0) = value

    '        Marshal.Copy(RValue, 0, Pt, 1)
    '        Marshal.Copy(Pt, SEND_DATA, 41, 2)

    '        ' 메모리 반환
    '        Marshal.FreeHGlobal(Pt)

    '        Me._WRITE_MES.Enqueue(SEND_DATA)

    '        Return Me._WriteID

    'BlockErr:

    '        Return -1

    '    End Function
    '    ' %*W100 의 형식의 SI2 데이타를 쓴다.

    Public Function Write_SI4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Integer) As Integer

        ' 더블워드 쓰기
        ' 더블워드 쓰기
        ' 더블워드 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(44) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H19                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H14                       ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &H9                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("B")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
        SEND_DATA(39) = &H4
        SEND_DATA(40) = &H0
        ' 포인터 선언 및 8 바이트 할당
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Integer
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 4)

        ' 메모리 반환
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 의 형식의 SI4 데이타를 쓴다.

    '    Public Function WriteD_SI4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Integer) As Integer

    '        ' 더블워드 쓰기
    '        ' 더블워드 쓰기
    '        ' 더블워드 쓰기

    '        On Error GoTo BlockErr

    '        If MemAddress > 249999 Then
    '            Return -1
    '        End If

    '        ' LSIS-GLOFA
    '        Dim SEND_DATA(44) As Byte

    '        ' invoke Id 증가
    '        If Me._WriteID = 99 Then
    '            Me._WriteID = 1
    '        Else
    '            Me._WriteID += 1
    '        End If

    '        ' HEADER
    '        SEND_DATA(0) = AscW("L")
    '        SEND_DATA(1) = AscW("G")
    '        SEND_DATA(2) = AscW("I")
    '        SEND_DATA(3) = AscW("S")
    '        SEND_DATA(4) = AscW("-")
    '        SEND_DATA(5) = AscW("G")
    '        SEND_DATA(6) = AscW("L")
    '        SEND_DATA(7) = AscW("O")
    '        SEND_DATA(8) = AscW("F")
    '        SEND_DATA(9) = AscW("A")
    '        SEND_DATA(10) = &H0                 'plc info
    '        SEND_DATA(11) = &H0                 'plc info
    '        SEND_DATA(12) = &H0                 'reserved
    '        SEND_DATA(13) = &H33                'h33   mmi->plc
    '        SEND_DATA(14) = Me._WriteID         'invoke Id
    '        SEND_DATA(15) = &H0                 'invoke Id
    '        SEND_DATA(16) = &H19                'Application Instruction
    '        SEND_DATA(17) = &H0                 'Application Instruction
    '        SEND_DATA(18) = &H0                 'reserved
    '        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
    '        ' BODY
    '        SEND_DATA(20) = &H58                       ' COMMAND
    '        SEND_DATA(21) = &H0                        ' COMMAND
    '        SEND_DATA(22) = &H3                        ' 데이타 타입
    '        SEND_DATA(23) = &H0                        ' 데이타 타입
    '        SEND_DATA(24) = &H0                        ' Don’t Care
    '        SEND_DATA(25) = &H0                        ' Don’t Care
    '        SEND_DATA(26) = &H1                        ' 블럭 넘버
    '        SEND_DATA(27) = &H0                        ' 블럭 넘버
    '        SEND_DATA(28) = &H9                        ' 어드레스 길이 
    '        SEND_DATA(29) = &H0                        ' 어드레스 길이
    '        SEND_DATA(30) = AscW("%")
    '        SEND_DATA(31) = AscW(Device)
    '        SEND_DATA(32) = AscW("D")
    '        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
    '        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
    '        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
    '        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
    '        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
    '        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
    '        SEND_DATA(39) = &H4
    '        SEND_DATA(40) = &H0
    '        ' 포인터 선언 및 8 바이트 할당
    '        Dim Pt As IntPtr
    '        Pt = Marshal.AllocHGlobal(8)

    '        Dim RValue(0) As Integer
    '        RValue(0) = value

    '        Marshal.Copy(RValue, 0, Pt, 1)
    '        Marshal.Copy(Pt, SEND_DATA, 41, 4)

    '        ' 메모리 반환
    '        Marshal.FreeHGlobal(Pt)

    '        Me._WRITE_MES.Enqueue(SEND_DATA)

    '        Return Me._WriteID

    'BlockErr:

    '        Return -1

    '    End Function
    '    ' %*D100 의 형식의 SI4 데이타를 쓴다.

    Public Function Write_FP4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Single) As Integer

        ' 더블워드 쓰기
        ' 더블워드 쓰기
        ' 더블워드 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(44) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H19                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H14                       ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &H9                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("B")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
        SEND_DATA(39) = &H4
        SEND_DATA(40) = &H0
        ' 포인터 선언 및 8 바이트 할당
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Single
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 4)

        ' 메모리 반환
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 의 형식의 FP4 데이타를 쓴다.

    Public Function Write_FP8(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Double) As Integer

        ' 더블워드 쓰기
        ' 더블워드 쓰기
        ' 더블워드 쓰기

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(48) As Byte

        ' invoke Id 증가
        If Me._WriteID = 99 Then
            Me._WriteID = 1
        Else
            Me._WriteID += 1
        End If

        ' HEADER
        SEND_DATA(0) = AscW("L")
        SEND_DATA(1) = AscW("G")
        SEND_DATA(2) = AscW("I")
        SEND_DATA(3) = AscW("S")
        SEND_DATA(4) = AscW("-")
        SEND_DATA(5) = AscW("G")
        SEND_DATA(6) = AscW("L")
        SEND_DATA(7) = AscW("O")
        SEND_DATA(8) = AscW("F")
        SEND_DATA(9) = AscW("A")
        SEND_DATA(10) = &H0                 'plc info
        SEND_DATA(11) = &H0                 'plc info
        SEND_DATA(12) = &H0                 'reserved
        SEND_DATA(13) = &H33                'h33   mmi->plc
        SEND_DATA(14) = Me._WriteID         'invoke Id
        SEND_DATA(15) = &H0                 'invoke Id
        SEND_DATA(16) = &H1D                'Application Instruction
        SEND_DATA(17) = &H0                 'Application Instruction
        SEND_DATA(18) = &H0                 'reserved
        SEND_DATA(19) = HEADER_SUM(SEND_DATA)
        ' BODY
        SEND_DATA(20) = &H58                       ' COMMAND
        SEND_DATA(21) = &H0                        ' COMMAND
        SEND_DATA(22) = &H14                       ' 데이타 타입
        SEND_DATA(23) = &H0                        ' 데이타 타입
        SEND_DATA(24) = &H0                        ' Don’t Care
        SEND_DATA(25) = &H0                        ' Don’t Care
        SEND_DATA(26) = &H1                        ' 블럭 넘버
        SEND_DATA(27) = &H0                        ' 블럭 넘버
        SEND_DATA(28) = &H9                        ' 어드레스 길이 
        SEND_DATA(29) = &H0                        ' 어드레스 길이
        SEND_DATA(30) = AscW("%")
        SEND_DATA(31) = AscW(Device)
        SEND_DATA(32) = AscW("B")
        SEND_DATA(33) = AscW(Mid(Format((MemAddress), "000000"), 1, 1))
        SEND_DATA(34) = AscW(Mid(Format((MemAddress), "000000"), 2, 1))
        SEND_DATA(35) = AscW(Mid(Format((MemAddress), "000000"), 3, 1))
        SEND_DATA(36) = AscW(Mid(Format((MemAddress), "000000"), 4, 1))
        SEND_DATA(37) = AscW(Mid(Format((MemAddress), "000000"), 5, 1))
        SEND_DATA(38) = AscW(Mid(Format((MemAddress), "000000"), 6, 1))
        SEND_DATA(39) = &H8
        SEND_DATA(40) = &H0
        ' 포인터 선언 및 8 바이트 할당
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Double
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 8)

        ' 메모리 반환
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 의 형식의 FP8 데이타를 쓴다.

#End Region

#Region " 외부 READ FUNCTION "

    Public Function ReadBool(ByVal Device As String, ByVal MemAddress As Integer, ByVal BitOffset As Integer) As Boolean

        Dim pReturn(1) As Byte
        Dim ReturnD As Boolean
        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress
        Dim pTH As New IntPtr(i)

        System.Runtime.InteropServices.Marshal.Copy(pTH, pReturn, 0, 2)

        Select Case BitOffset
            Case 0
                If (pReturn(0) And &H1) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 1
                If (pReturn(0) And &H2) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 2
                If (pReturn(0) And &H4) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 3
                If (pReturn(0) And &H8) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 4
                If (pReturn(0) And &H10) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 5
                If (pReturn(0) And &H20) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 6
                If (pReturn(0) And &H40) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 7
                If (pReturn(0) And &H80) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 8
                If (pReturn(1) And &H1) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 9
                If (pReturn(1) And &H2) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 10
                If (pReturn(1) And &H4) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 11
                If (pReturn(1) And &H8) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 12
                If (pReturn(1) And &H10) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 13
                If (pReturn(1) And &H20) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 14
                If (pReturn(1) And &H40) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
            Case 15
                If (pReturn(1) And &H80) > 0 Then
                    ReturnD = True
                Else
                    ReturnD = False
                End If
        End Select

        pTH = Nothing

        Return ReturnD

    End Function
    ' UI1 를 리턴하는 펑션

    Public Function ReadUI1(ByVal Device As String, ByVal MemAddress As Integer) As Byte

        Dim pReturn(0) As Byte
        Dim ReturnD As Byte
        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress
        Dim pTH As New IntPtr(i)

        System.Runtime.InteropServices.Marshal.Copy(pTH, pReturn, 0, 1)
        ReturnD = pReturn(0)

        pTH = Nothing

        Return ReturnD

    End Function
    ' UI1 를 리턴하는 펑션

    Public Function ReadUI2(ByVal Device As String, ByVal MemAddress As Integer) As Integer

        Dim pReturn(1) As Byte
        Dim ReturnD As Integer
        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress
        Dim pTH As New IntPtr(i)

        System.Runtime.InteropServices.Marshal.Copy(pTH, pReturn, 0, 2)
        ReturnD = pReturn(0) + pReturn(1) * 256

        pTH = Nothing

        Return ReturnD

    End Function
    ' UI2 를 리턴하는 펑션

    Public Function ReadUI4(ByVal Device As String, ByVal MemAddress As Integer) As Long

        Dim pReturn(3) As Byte
        Dim ReturnD As Long
        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress
        Dim pTH As New IntPtr(i)

        System.Runtime.InteropServices.Marshal.Copy(pTH, pReturn, 0, 4)
        ReturnD = pReturn(0) + pReturn(1) * 256 ^ 1 + pReturn(2) * 256 ^ 2 + pReturn(3) * 256 ^ 3

        pTH = Nothing

        Return ReturnD

    End Function
    ' UI4 를 리턴하는 펑션

    Public Function ReadSI2(ByVal Device As String, ByVal MemAddress As Integer) As Integer

        Dim pReturn(0) As Short
        Dim ReturnD As Integer
        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress
        Dim pTH As New IntPtr(i)

        System.Runtime.InteropServices.Marshal.Copy(pTH, pReturn, 0, 1)
        ReturnD = pReturn(0)

        pTH = Nothing

        Return ReturnD

    End Function
    ' SI2 를 리턴하는 펑션

    Public Function ReadSI4(ByVal Device As String, ByVal MemAddress As Integer) As Integer

        Dim pReturn(0) As Integer
        Dim ReturnD As Integer
        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress
        Dim pTH As New IntPtr(i)

        System.Runtime.InteropServices.Marshal.Copy(pTH, pReturn, 0, 1)
        ReturnD = pReturn(0)

        pTH = Nothing

        Return ReturnD

    End Function
    ' SI4 를 리턴하는 펑션

    Public Function ReadFP4(ByVal Device As String, ByVal MemAddress As Integer) As Single

        Dim pReturn(0) As Single
        Dim ReturnD As Single
        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress
        Dim pTH As New IntPtr(i)

        System.Runtime.InteropServices.Marshal.Copy(pTH, pReturn, 0, 1)
        ReturnD = pReturn(0)

        pTH = Nothing

        Return ReturnD

    End Function
    ' FP4 를 리턴하는 펑션

    Public Function ReadFP8(ByVal Device As String, ByVal MemAddress As Integer) As Double

        Dim pReturn(0) As Double
        Dim ReturnD As Double
        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress
        Dim pTH As New IntPtr(i)

        System.Runtime.InteropServices.Marshal.Copy(pTH, pReturn, 0, 1)
        ReturnD = pReturn(0)

        pTH = Nothing

        Return ReturnD

    End Function
    ' FP8 를 리턴하는 펑션

    Public Function ReadAddressPoint(ByVal Device As String, ByVal MemAddress As Integer) As Integer


        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress

        Return i

    End Function
    ' 어드레스의 메모리 주소를 리턴 한다.

    Public Function ReadMemory(ByVal Device As String, ByVal MemAddress As Integer) As IntPtr

        Return Me._memDic(Device).ToInt32 + MemAddress

    End Function

#End Region

#Region "내부 FUNCTION "

    Public Sub LogWrite(ByVal Value As String)

        On Error GoTo Errpoint

        Dim Folder As System.IO.Directory
        Dim i As Integer


        If Folder.Exists(StartupPath & "\Net_Log") = False Then
            Folder.CreateDirectory(StartupPath & "\Net_Log")
        End If

        Dim FS As System.IO.File
        Dim SW As System.IO.StreamWriter

        SW = System.IO.File.AppendText(StartupPath & "\Net_Log\" & Format(Now, "yyyyMMdd") & ".TXT")
        SW.Write(Format(Date.Now, "yyyy-MM-dd HH:mm:ss:fff") & "   :   " & Value & vbCrLf)

        SW.Close()
        SW = Nothing

        FS = Nothing
        Folder = Nothing

Errpoint:

    End Sub
    ' Log 를 찍는 메서드

    Private Sub NetOk()

        If Me._NetRunCount < 1000000 Then
            Me._NetRunCount += 1
            Me._NetOkCount += 1
        Else
            Me._NetRunCount = 1
            Me._NetOkCount = 1
            Me._NetNgCount = 0
        End If

    End Sub
    ' 통신 성공 카운터

    Private Sub NetNg()

        If Me._NetRunCount < 1000000 Then
            Me._NetRunCount += 1
            Me._NetNgCount += 1
        Else
            Me._NetRunCount = 1
            Me._NetOkCount = 0
            Me._NetNgCount = 1
        End If

    End Sub
    ' 통신 실패 카운터

#End Region

#Region " 내부 이벤트 "

    Private Sub Timer_SEND_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles Timer_SEND.Elapsed


        Me.Timer_SEND.Enabled = False

        '가비지 모음
        Me._GcCount += 1
        If Me._GcCount > 200 Then
            GC.Collect()
            Me._GcCount = 0
        End If

        Me.Timer_SEND.Enabled = True

    End Sub
    ' 통신관련 메인 컨트롤

    Private Sub TH_ReadWrite()

        Dim i As Integer
        Dim s As String

        '--------------------------------------------------
        ' 쓰래드 안에서 무한루프를 돈다.
        '--------------------------------------------------
        While True


            '--------------------------------------------------
            ' 현재 통신 run 컨멘드가 아닌상태는 프로세서를 실행하지않고
            ' 루프 대기를 한다.
            '--------------------------------------------------
            If Me._RunCommand = False Then
                System.Threading.Thread.Sleep(10)
                GoTo Deleay
            End If


            '--------------------------------------------------
            ' mode = disconnection
            ' 현 모드가 disconnection 이면 connection 을 실행한다.
            '--------------------------------------------------
            If Me._NetMode = Net_Mode.DisConnection Then
                Me._NetMode = Net_Mode.ConnectionRun

                Dim _IpAddress As System.NET.IPAddress = System.Net.IPAddress.Parse(Me._PlcIP)

                Try

                    Me._LSEnet.SendBufferSize = Me._SendBufferSize
                    Me._LSEnet.SendTimeout = Me._SendTimeout
                    Me._LSEnet.ReceiveBufferSize = Me._ReceiveBufferSize
                    Me._LSEnet.ReceiveTimeout = Me._ReceiveTimeout

                    Me._NetMode = Net_Mode.ConnectionRun
                    Me._LSEnet.Connect(_IpAddress, Me._PlcPort)
                    Me._NetMode = Net_Mode.NetOK
                    RaiseEvent ENET_EVENT(NetEvent.Connection)
                    _IpAddress = Nothing

                    Me.LogWrite("PLC 와 연결 확인 하였습니다.")
                    Me._NetErrCount = 0

                Catch ex As Exception

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    _IpAddress = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)

                    If Me._NetErrCount < 1 Then
                        Me.LogWrite("PLC 와 연결 실패 하였습니다.")
                        Me.NetNg()
                    Else
                        If Me._NetErrCount > 500 Then
                            Me.LogWrite("PLC 와 연결 실패 하였습니다.")
                            Me.NetNg()
                            Me._NetErrCount = 1
                        End If
                    End If
                    Me._NetErrCount += 1

                    GoTo Deleay

                End Try

            End If
            '--------------------------------------------------
            ' mode = disconnection
            '--------------------------------------------------




            '--------------------------------------------------
            ' mode 가 커넥션 되어 있지 않은 경우 다음 로직을 실행하지 않는다.
            '--------------------------------------------------
            If Me._NetMode <> Net_Mode.NetOK Then
                GoTo Deleay
            End If
            '--------------------------------------------------
            ' mode 가 커넥션 되어 있지 않은 경우 다음 로직을 실행하지 않는다.
            '--------------------------------------------------




            '--------------------------------------------------
            ' WriteBlock를 검색하여 큐에 넣는다.
            '--------------------------------------------------
            If Me._WRITE_MES.Count > 0 Then
                ' Write. 프레임 전송
                Me._NetMode = Net_Mode.ReadWrite
                Dim NS As System.Net.Sockets.NetworkStream = Me._LSEnet.GetStream
                Dim SData() As Byte
                Dim ReTryCount As Integer = 0

                If Me._WRITE_MES.Count <= 0 Then
                    NS = Nothing
                    Me._NetMode = Net_Mode.NetOK
                    GoTo Deleay
                End If

ReTry:

                Try

                    ReTryCount += 1
                    SData = Me._WRITE_MES.Dequeue
                    NS.Write(SData, 0, SData.Length)

                Catch ex As Exception

                    Me.LogWrite("PLC 에 데이타 쓰기실패 하였습니다.")

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    NS = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.SendErr)

                    Me.LogWrite("PLC 와 연결 해제 하였습니다.")
                    Me.NetNg()

                    GoTo Deleay

                End Try

                ' Ack. 데이타 받기
                Dim RData(Me._ReceiveBufferSize - 1) As Byte
                Dim RecvCount As Integer
                Try
                    RecvCount = NS.Read(RData, 0, Me._ReceiveBufferSize - 1)


                    ' rev 된 데이타 길이 check
                    If RecvCount <> 30 Then
                        Me._NetMode = Net_Mode.NetOK
                        Me.NetNg()
                        GoTo Deleay
                    End If

                    ' 헤더에 이상이 있을경우 메서드를 나간다.
                    s = ""
                    For i = 0 To 9
                        s &= Chr(RData(i))
                    Next
                    If s <> "LGIS-GLOFA" Then
                        Me._NetMode = Net_Mode.NetOK
                        Me.NetNg()
                        GoTo Deleay
                    End If
                    ' invokeid chaeck
                    If SData(14) <> RData(14) Then
                        Me._NetMode = Net_Mode.NetOK
                        Me.NetNg()
                        GoTo Deleay
                    End If


                    ' Err code 배열은 RData(26),(27)
                    If (rdata(26) + rdata(27) * 256) <> 0 Then
                        If ReTryCount <= 3 Then
                            GoTo ReTry
                        Else
                            Me._NetMode = Net_Mode.NetOK
                            Me.NetNg()
                            GoTo Deleay
                        End If
                    End If

                    Me.NetOk()

                Catch ex As Exception

                    Me.LogWrite("PLC 에 데이타 쓰기 응답이 없습니다.")

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    NS = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.RecErr)

                    Me.LogWrite("PLC 와 연결 해제 하였습니다.")
                    Me.NetNg()

                    GoTo Deleay

                End Try
                Me._NetMode = Net_Mode.NetOK


            End If
            '--------------------------------------------------
            ' WriteBlock를 검색하여 큐에 넣는다.
            '--------------------------------------------------




            '--------------------------------------------------
            ' ReadBlock를 검색하여 큐에 넣는다.
            '--------------------------------------------------
            Try
                For i = 0 To 31
                    If Me._READ_BLOCK(i).USE = True Then
                        Me._READ_BLOCK(i).REAL_INTERVAL += 1
                        If Me._READ_BLOCK(i).INTERVAL < Me._READ_BLOCK(i).REAL_INTERVAL Then
                            Me._READ_BLOCK(i).REAL_INTERVAL = 0
                            If Me._READ_MES.Count < 50 Then
                                Me._READ_MES.Enqueue(Me._READ_BLOCK(i).SEND_DATA)
                            End If

                        End If
                    End If
                Next
            Catch ex As Exception
                GoTo Deleay
            End Try
            '--------------------------------------------------
            ' ReadBlock를 검색하여 큐에 넣는다.
            '--------------------------------------------------




            '--------------------------------------------------
            ' ReadData 큐에 데이타가 있을경우읽기를 실행한다.
            '--------------------------------------------------
            If Me._READ_MES.Count > 0 Then

                ' Req. 프레임 전송
                Me._NetMode = Net_Mode.ReadWrite
                Dim NS As System.Net.Sockets.NetworkStream = Me._LSEnet.GetStream
                Dim SData() As Byte
                Try

                    If Me._READ_MES.Count <= 0 Then
                        NS = Nothing
                        Me._NetMode = Net_Mode.NetOK
                        GoTo Deleay
                    End If
                    SData = Me._READ_MES.Dequeue
                    NS.Write(SData, 0, SData.Length)

                Catch ex As Exception

                    Me.LogWrite("PLC 에 데이타 읽기실패 하였습니다.")

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    NS = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.SendErr)

                    Me.LogWrite("PLC 와 연결 해제 하였습니다.")
                    Me.NetNg()

                    GoTo Deleay

                End Try

                ' Rspon. 데이타 받기
                Dim RData(Me._ReceiveBufferSize - 1) As Byte
                Dim RecvCount As Integer
                Try
                    RecvCount = NS.Read(RData, 0, Me._ReceiveBufferSize - 1)


                    Me.LogWrite("RecvCount = " + RecvCount.ToString())
                    Me.LogWrite("Size = " + (32 + (RData(30) + RData(31) * 256)).ToString())

                    ' rev 된 데이타 길이 check
                    If RecvCount <> 32 + (RData(30) + RData(31) * 256) Then
                        Me._NetMode = Net_Mode.NetOK
                        Me.NetNg()
                        GoTo Deleay
                    End If

                    ' 헤더에 이상이 있을경우 메서드를 나간다.
                    s = ""
                    For i = 0 To 9
                        s &= Chr(RData(i))
                    Next
                    If s <> "LGIS-GLOFA" Then
                        Me._NetMode = Net_Mode.NetOK
                        Me.NetNg()
                        GoTo Deleay
                    End If
                    ' invokeid chaeck
                    If Me._READ_BLOCK(RData(14) - 100).INVOKEID <> RData(14) Then
                        Me._NetMode = Net_Mode.NetOK
                        Me.NetNg()
                        GoTo Deleay
                    End If

                    ' 블럭 번호 저장 배열은 RData(14)-100
                    ' 데이타 byte 갯수 배열은 RData(30)
                    ' 포인터 선언 및 8 바이트 할당

                    i = Me._memDic(_READ_BLOCK(RData(14) - 100).DEVICE).ToInt32 + Me._READ_BLOCK(RData(14) - 100).SAVE_ADD
                    Dim pTH As New IntPtr(i)
                    System.Runtime.InteropServices.Marshal.Copy(RData, 32, pTH, RData(30) + RData(31) * 256)
                    pTH = Nothing

                    Me.NetOk()

                Catch ex As Exception

                    Me.LogWrite("PLC 에 데이타 읽기 응답이 없습니다.")

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    NS = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.RecErr)

                    Me.LogWrite("PLC 와 연결 해제 하였습니다.")
                    Me.NetNg()

                    GoTo Deleay

                End Try
                Me._NetMode = Net_Mode.NetOK
            End If
            '--------------------------------------------------
            ' ReadData 큐에 데이타가 있을경우읽기를 실행한다.
            '--------------------------------------------------


            ' 통신을 하지 않았을경우 0.005 초 휴식
            System.Threading.Thread.Sleep(1)

Deleay:

        End While

    End Sub
    ' 통신 관련 쓰래드 메서드

#End Region

#Region " 기타 지원 기능 메서드"

    Public Sub ViewMem(ByVal Device As String)

        Dim MemView As New Frm_MemoryView(Me._memDic(Device).ToInt32)
        MemView.Show()

    End Sub
    ' 메모리뷰를 보이게한다.

#End Region

End Class







