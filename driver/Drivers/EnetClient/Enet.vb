Imports System.Windows.Forms.Application
Imports System.Runtime.InteropServices

Public Class Enet

#Region " ���� ���� "

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


    Private _READ_BLOCK(31) As NET_BLOCK               '�б� ����Ÿ ���� ���� ����


    Private _READ_MES As New Queue
    Private _WRITE_MES As New Queue
    Private _WriteID As Integer = 1                    'write invoke Id

    Private _ReadWriteCount As Integer                 '�а� ���� BfCount

    Private _GcCount As Integer                        '������ ī����

    ' ������ ����
    'Private _pMemory As IntPtr                         ' ����Ÿ�� ���� �ϴ� ������
    Private _memDic As Dictionary(Of String, IntPtr)

    Private _NetRunCount As Long                       '��� �õ� ī����
    Private _NetOkCount As Long                        '��� ���� ī����
    Private _NetNgCount As Long                        '��� ���� ī����


#End Region

#Region " ����ü �� ������ ��� "

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
    ' �б� ����Ÿ�� �����ϴ� ����

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
    ' �̺�Ʈ�� �����ϴ� ������ ���

#End Region

#Region " ������ �Ҹ� "

    Public Sub New()
        MyBase.New()

        Me._memDic = New Dictionary(Of String, IntPtr)
        Me.Timer_SEND.Interval = 500
        Me.Timer_SEND.Enabled = False
        Me.Timer_SEND.AutoReset = True
        Me.LogWrite("--------------------------------------------")
        Me.LogWrite("LS-ENET ������ ���� �Ͽ����ϴ�.")

    End Sub
    ' �޸� ����� ���� ���� ���� ���¿��� ����̺긦 ����

    Protected Overrides Sub Finalize()
        MyBase.Finalize()

        Me.LogWrite("LS-ENET ������ ���� �Ͽ����ϴ�.")

    End Sub

#End Region

#Region " �Ӽ� "

    Public Property SET_IP() As String
        Get
            Return Me._PlcIP
        End Get
        Set(ByVal Value As String)
            Me._PlcIP = Value
        End Set
    End Property
    ' Plc ip �Ӽ�

    Public Property SET_PORT() As Integer
        Get
            Return Me._PlcPort
        End Get
        Set(ByVal Value As Integer)
            Me._PlcPort = Value
        End Set
    End Property
    ' Plc port �Ӽ�

    Public Property SendBufferSize() As Integer
        Get
            Return Me._SendBufferSize
        End Get
        Set(ByVal Value As Integer)
            Me._SendBufferSize = Value
        End Set
    End Property
    ' ���� ������ �Ӽ�

    Public Property ReceiveBufferSize() As Integer
        Get
            Return Me._ReceiveBufferSize
        End Get
        Set(ByVal Value As Integer)
            Me._ReceiveBufferSize = Value
        End Set
    End Property
    ' ���� ������ �Ӽ�

    Public Property SendTimeout() As Integer
        Get
            Return Me._SendTimeout
        End Get
        Set(ByVal Value As Integer)
            Me._SendTimeout = Value
        End Set
    End Property
    ' Ÿ�Ӿƿ� �Ӽ�

    Public Property ReceiveTimeout() As Integer
        Get
            Return Me._ReceiveTimeout
        End Get
        Set(ByVal Value As Integer)
            Me._ReceiveTimeout = Value
        End Set
    End Property
    ' Ÿ�Ӿƿ� �Ӽ�

    Public Property Blocks As NET_BLOCK()
        Get
            Return Me._READ_BLOCK
        End Get
        Set(value As NET_BLOCK())

        End Set
    End Property

#End Region

#Region " �ܺ� �޼��� "

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

        Me.LogWrite("PLC IP �� " & Me._PlcIP & " �� �����Ͽ����ϴ�.")
        Me.LogWrite("PLC PORT �� " & Me._PlcPort.ToString & " �� �����Ͽ����ϴ�.")
        Me.LogWrite("SEND BUFFER SIZE �� " & Me._SendBufferSize.ToString & " �� �����Ͽ����ϴ�.")
        Me.LogWrite("RECEIVE BUFFER SIZE �� " & Me._ReceiveBufferSize.ToString & " �� �����Ͽ����ϴ�.")
        Me.LogWrite("SEND TIME OUT �� " & Me._SendTimeout.ToString & " �� �����Ͽ����ϴ�.")
        Me.LogWrite("RECEIVE TIME OUT �� " & Me._ReceiveTimeout.ToString & " �� �����Ͽ����ϴ�.")

        Me.LogWrite("����� ���� �Ͽ����ϴ�.")

        RaiseEvent ENET_EVENT(NetEvent.NetStart)

    End Sub
    ' plc �� �����ϴ� �޼���

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
    '' �������� �����带 �����Ѵ�.

    Public Sub NetEnd()

        Me._RunCommand = False


    End Sub
    ' PLC���� ��� �ߴ�

    Public Function SET_NetBlock(ByVal Device As String, ByVal MemAddress As Integer, ByVal SaveAddress As Integer, ByVal ReadCount As Integer, ByVal ReadTime As Integer, ByVal FrameNum As Integer) As Integer
        ' "��� �� ���� %*B100,100,10 ��巹��,��������,����ֱ�100ms"
        ' BLOCK Ÿ���� "%*B100,��������,����ֱ�"

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
        SEND_DATA(22) = &H14                       ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    ' read block �� ����Ѵ�.

    Public Function NetCount() As Net_Count

        Dim ReturnD As New Net_Count
        ReturnD.NetCount = Me._NetRunCount
        ReturnD.NetOk = Me._NetOkCount
        ReturnD.NetNg = Me._NetNgCount
        Return ReturnD

    End Function
    ' ��� Ƚ���� ���� �Ѵ�.

#End Region

#Region " �ܺ� WRITE FUNCTION "


    Public Function WriteW_Bool(ByVal Device As String, ByVal MemAddress As Integer, ByVal BitOffset As Integer, ByVal value As Boolean) As Integer

        ' ��Ʈ ����
        ' ��Ʈ ����
        ' ��Ʈ ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        ' "��巹��,��/����:DEC"
        ' ��Ʈ���� �� ���� ���⸦ �и��Ѵ�. 

        Dim SEND_DATA(42) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &HA                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    ' %*W100.0 �� ������ bool ����Ÿ�� ����.

    Public Function WriteB_Bool(ByVal Device As String, ByVal MemAddress As Integer, ByVal BitOffset As Integer, ByVal value As Boolean) As Integer

        ' ��Ʈ ����
        ' ��Ʈ ����
        ' ��Ʈ ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        ' "��巹��,��/����:DEC"
        ' ��Ʈ���� �� ���� ���⸦ �и��Ѵ�. 

        Dim SEND_DATA(42) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &HA                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    ' %*B100.0 �� ������ bool ����Ÿ�� ����.

    Public Function WriteX_Bool(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Boolean) As Integer

        ' ��Ʈ ����
        ' ��Ʈ ����
        ' ��Ʈ ����

        On Error GoTo BlockErr

        If MemAddress > 9999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        ' "��巹��,��/����:DEC"
        ' ��Ʈ���� �� ���� ���⸦ �и��Ѵ�. 

        Dim SEND_DATA(42) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &HA                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    ' %*X100 �� ������ bool ����Ÿ�� ����.

    Public Function Write_UI1(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Byte) As Integer

        ' ����Ʈ ����
        ' ����Ʈ ����
        ' ����Ʈ ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(41) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H14                       ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    ' %*B100 �� ������ BYTE ����Ÿ�� ����.

    Public Function Write_UI2(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Integer) As Integer

        ' ���� ����
        ' ���� ����
        ' ���� ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(42) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H14                       ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    ' %*B100 �� ������ ���� ����Ÿ�� ����.

    '    Public Function WriteW_UI2(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Integer) As Integer

    '        ' ���� ����
    '        ' ���� ����
    '        ' ���� ����

    '        On Error GoTo BlockErr

    '        If MemAddress > 499999 Then
    '            Return -1
    '        End If

    '        ' LSIS-GLOFA
    '        Dim SEND_DATA(42) As Byte

    '        ' invoke Id ����
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
    '        SEND_DATA(22) = &H2                        ' ����Ÿ Ÿ��
    '        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
    '        SEND_DATA(24) = &H0                        ' Don��t Care
    '        SEND_DATA(25) = &H0                        ' Don��t Care
    '        SEND_DATA(26) = &H1                        ' �� �ѹ�
    '        SEND_DATA(27) = &H0                        ' �� �ѹ�
    '        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
    '        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    '    ' %*W100 �� ������ ���� ����Ÿ�� ����.

    Public Function Write_UI4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Long) As Integer

        ' ������� ����
        ' ������� ����
        ' ������� ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(44) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H14                      ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
        ' ������ ���� �� 8 ����Ʈ �Ҵ�
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Long
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 4)

        ' �޸� ��ȯ
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 �� ������ UI4 ����Ÿ�� ����.

    '    Public Function WriteD_UI4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Long) As Integer

    '        ' ������� ����
    '        ' ������� ����
    '        ' ������� ����

    '        On Error GoTo BlockErr

    '        If MemAddress > 999999 Then
    '            Return -1
    '        End If

    '        ' LSIS-GLOFA
    '        Dim SEND_DATA(44) As Byte

    '        ' invoke Id ����
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
    '        SEND_DATA(22) = &H3                        ' ����Ÿ Ÿ��
    '        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
    '        SEND_DATA(24) = &H0                        ' Don��t Care
    '        SEND_DATA(25) = &H0                        ' Don��t Care
    '        SEND_DATA(26) = &H1                        ' �� �ѹ�
    '        SEND_DATA(27) = &H0                        ' �� �ѹ�
    '        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
    '        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    '        ' ������ ���� �� 8 ����Ʈ �Ҵ�
    '        Dim Pt As IntPtr
    '        Pt = Marshal.AllocHGlobal(8)

    '        Dim RValue(0) As Long
    '        RValue(0) = value

    '        Marshal.Copy(RValue, 0, Pt, 1)
    '        Marshal.Copy(Pt, SEND_DATA, 41, 4)

    '        ' �޸� ��ȯ
    '        Marshal.FreeHGlobal(Pt)

    '        Me._WRITE_MES.Enqueue(SEND_DATA)

    '        Return Me._WriteID

    'BlockErr:

    '        Return -1

    '    End Function
    '    ' %*D100 �� ������ UI4 ����Ÿ�� ����.

    Public Function Write_SI2(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Short) As Integer

        ' ���� ����
        ' ���� ����
        ' ���� ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(42) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H14                       ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
        ' ������ ���� �� 8 ����Ʈ �Ҵ�
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Short
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 2)

        ' �޸� ��ȯ
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 �� ������ SI2 ����Ÿ�� ����.

    '    Public Function WriteW_SI2(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Short) As Integer

    '        ' ���� ����
    '        ' ���� ����
    '        ' ���� ����

    '        On Error GoTo BlockErr

    '        If MemAddress > 499999 Then
    '            Return -1
    '        End If

    '        ' LSIS-GLOFA
    '        Dim SEND_DATA(42) As Byte

    '        ' invoke Id ����
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
    '        SEND_DATA(22) = &H2                        ' ����Ÿ Ÿ��
    '        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
    '        SEND_DATA(24) = &H0                        ' Don��t Care
    '        SEND_DATA(25) = &H0                        ' Don��t Care
    '        SEND_DATA(26) = &H1                        ' �� �ѹ�
    '        SEND_DATA(27) = &H0                        ' �� �ѹ�
    '        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
    '        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    '        ' ������ ���� �� 8 ����Ʈ �Ҵ�
    '        Dim Pt As IntPtr
    '        Pt = Marshal.AllocHGlobal(8)

    '        Dim RValue(0) As Short
    '        RValue(0) = value

    '        Marshal.Copy(RValue, 0, Pt, 1)
    '        Marshal.Copy(Pt, SEND_DATA, 41, 2)

    '        ' �޸� ��ȯ
    '        Marshal.FreeHGlobal(Pt)

    '        Me._WRITE_MES.Enqueue(SEND_DATA)

    '        Return Me._WriteID

    'BlockErr:

    '        Return -1

    '    End Function
    '    ' %*W100 �� ������ SI2 ����Ÿ�� ����.

    Public Function Write_SI4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Integer) As Integer

        ' ������� ����
        ' ������� ����
        ' ������� ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(44) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H14                       ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
        ' ������ ���� �� 8 ����Ʈ �Ҵ�
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Integer
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 4)

        ' �޸� ��ȯ
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 �� ������ SI4 ����Ÿ�� ����.

    '    Public Function WriteD_SI4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Integer) As Integer

    '        ' ������� ����
    '        ' ������� ����
    '        ' ������� ����

    '        On Error GoTo BlockErr

    '        If MemAddress > 249999 Then
    '            Return -1
    '        End If

    '        ' LSIS-GLOFA
    '        Dim SEND_DATA(44) As Byte

    '        ' invoke Id ����
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
    '        SEND_DATA(22) = &H3                        ' ����Ÿ Ÿ��
    '        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
    '        SEND_DATA(24) = &H0                        ' Don��t Care
    '        SEND_DATA(25) = &H0                        ' Don��t Care
    '        SEND_DATA(26) = &H1                        ' �� �ѹ�
    '        SEND_DATA(27) = &H0                        ' �� �ѹ�
    '        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
    '        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
    '        ' ������ ���� �� 8 ����Ʈ �Ҵ�
    '        Dim Pt As IntPtr
    '        Pt = Marshal.AllocHGlobal(8)

    '        Dim RValue(0) As Integer
    '        RValue(0) = value

    '        Marshal.Copy(RValue, 0, Pt, 1)
    '        Marshal.Copy(Pt, SEND_DATA, 41, 4)

    '        ' �޸� ��ȯ
    '        Marshal.FreeHGlobal(Pt)

    '        Me._WRITE_MES.Enqueue(SEND_DATA)

    '        Return Me._WriteID

    'BlockErr:

    '        Return -1

    '    End Function
    '    ' %*D100 �� ������ SI4 ����Ÿ�� ����.

    Public Function Write_FP4(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Single) As Integer

        ' ������� ����
        ' ������� ����
        ' ������� ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(44) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H14                       ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
        ' ������ ���� �� 8 ����Ʈ �Ҵ�
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Single
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 4)

        ' �޸� ��ȯ
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 �� ������ FP4 ����Ÿ�� ����.

    Public Function Write_FP8(ByVal Device As String, ByVal MemAddress As Integer, ByVal value As Double) As Integer

        ' ������� ����
        ' ������� ����
        ' ������� ����

        On Error GoTo BlockErr

        If MemAddress > 999999 Then
            Return -1
        End If

        ' LSIS-GLOFA
        Dim SEND_DATA(48) As Byte

        ' invoke Id ����
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
        SEND_DATA(22) = &H14                       ' ����Ÿ Ÿ��
        SEND_DATA(23) = &H0                        ' ����Ÿ Ÿ��
        SEND_DATA(24) = &H0                        ' Don��t Care
        SEND_DATA(25) = &H0                        ' Don��t Care
        SEND_DATA(26) = &H1                        ' �� �ѹ�
        SEND_DATA(27) = &H0                        ' �� �ѹ�
        SEND_DATA(28) = &H9                        ' ��巹�� ���� 
        SEND_DATA(29) = &H0                        ' ��巹�� ����
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
        ' ������ ���� �� 8 ����Ʈ �Ҵ�
        Dim Pt As IntPtr
        Pt = Marshal.AllocHGlobal(8)

        Dim RValue(0) As Double
        RValue(0) = value

        Marshal.Copy(RValue, 0, Pt, 1)
        Marshal.Copy(Pt, SEND_DATA, 41, 8)

        ' �޸� ��ȯ
        Marshal.FreeHGlobal(Pt)

        Me._WRITE_MES.Enqueue(SEND_DATA)

        Return Me._WriteID

BlockErr:

        Return -1

    End Function
    ' %*B100 �� ������ FP8 ����Ÿ�� ����.

#End Region

#Region " �ܺ� READ FUNCTION "

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
    ' UI1 �� �����ϴ� ���

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
    ' UI1 �� �����ϴ� ���

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
    ' UI2 �� �����ϴ� ���

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
    ' UI4 �� �����ϴ� ���

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
    ' SI2 �� �����ϴ� ���

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
    ' SI4 �� �����ϴ� ���

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
    ' FP4 �� �����ϴ� ���

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
    ' FP8 �� �����ϴ� ���

    Public Function ReadAddressPoint(ByVal Device As String, ByVal MemAddress As Integer) As Integer


        Dim i As Integer = Me._memDic(Device).ToInt32 + MemAddress

        Return i

    End Function
    ' ��巹���� �޸� �ּҸ� ���� �Ѵ�.

    Public Function ReadMemory(ByVal Device As String, ByVal MemAddress As Integer) As IntPtr

        Return Me._memDic(Device).ToInt32 + MemAddress

    End Function

#End Region

#Region "���� FUNCTION "

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
    ' Log �� ��� �޼���

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
    ' ��� ���� ī����

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
    ' ��� ���� ī����

#End Region

#Region " ���� �̺�Ʈ "

    Private Sub Timer_SEND_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles Timer_SEND.Elapsed


        Me.Timer_SEND.Enabled = False

        '������ ����
        Me._GcCount += 1
        If Me._GcCount > 200 Then
            GC.Collect()
            Me._GcCount = 0
        End If

        Me.Timer_SEND.Enabled = True

    End Sub
    ' ��Ű��� ���� ��Ʈ��

    Private Sub TH_ReadWrite()

        Dim i As Integer
        Dim s As String

        '--------------------------------------------------
        ' ������ �ȿ��� ���ѷ����� ����.
        '--------------------------------------------------
        While True


            '--------------------------------------------------
            ' ���� ��� run ����尡 �ƴѻ��´� ���μ����� ���������ʰ�
            ' ���� ��⸦ �Ѵ�.
            '--------------------------------------------------
            If Me._RunCommand = False Then
                System.Threading.Thread.Sleep(10)
                GoTo Deleay
            End If


            '--------------------------------------------------
            ' mode = disconnection
            ' �� ��尡 disconnection �̸� connection �� �����Ѵ�.
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

                    Me.LogWrite("PLC �� ���� Ȯ�� �Ͽ����ϴ�.")
                    Me._NetErrCount = 0

                Catch ex As Exception

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    _IpAddress = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)

                    If Me._NetErrCount < 1 Then
                        Me.LogWrite("PLC �� ���� ���� �Ͽ����ϴ�.")
                        Me.NetNg()
                    Else
                        If Me._NetErrCount > 500 Then
                            Me.LogWrite("PLC �� ���� ���� �Ͽ����ϴ�.")
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
            ' mode �� Ŀ�ؼ� �Ǿ� ���� ���� ��� ���� ������ �������� �ʴ´�.
            '--------------------------------------------------
            If Me._NetMode <> Net_Mode.NetOK Then
                GoTo Deleay
            End If
            '--------------------------------------------------
            ' mode �� Ŀ�ؼ� �Ǿ� ���� ���� ��� ���� ������ �������� �ʴ´�.
            '--------------------------------------------------




            '--------------------------------------------------
            ' WriteBlock�� �˻��Ͽ� ť�� �ִ´�.
            '--------------------------------------------------
            If Me._WRITE_MES.Count > 0 Then
                ' Write. ������ ����
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

                    Me.LogWrite("PLC �� ����Ÿ ������� �Ͽ����ϴ�.")

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    NS = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.SendErr)

                    Me.LogWrite("PLC �� ���� ���� �Ͽ����ϴ�.")
                    Me.NetNg()

                    GoTo Deleay

                End Try

                ' Ack. ����Ÿ �ޱ�
                Dim RData(Me._ReceiveBufferSize - 1) As Byte
                Dim RecvCount As Integer
                Try
                    RecvCount = NS.Read(RData, 0, Me._ReceiveBufferSize - 1)


                    ' rev �� ����Ÿ ���� check
                    If RecvCount <> 30 Then
                        Me._NetMode = Net_Mode.NetOK
                        Me.NetNg()
                        GoTo Deleay
                    End If

                    ' ����� �̻��� ������� �޼��带 ������.
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


                    ' Err code �迭�� RData(26),(27)
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

                    Me.LogWrite("PLC �� ����Ÿ ���� ������ �����ϴ�.")

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    NS = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.RecErr)

                    Me.LogWrite("PLC �� ���� ���� �Ͽ����ϴ�.")
                    Me.NetNg()

                    GoTo Deleay

                End Try
                Me._NetMode = Net_Mode.NetOK


            End If
            '--------------------------------------------------
            ' WriteBlock�� �˻��Ͽ� ť�� �ִ´�.
            '--------------------------------------------------




            '--------------------------------------------------
            ' ReadBlock�� �˻��Ͽ� ť�� �ִ´�.
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
            ' ReadBlock�� �˻��Ͽ� ť�� �ִ´�.
            '--------------------------------------------------




            '--------------------------------------------------
            ' ReadData ť�� ����Ÿ�� ��������б⸦ �����Ѵ�.
            '--------------------------------------------------
            If Me._READ_MES.Count > 0 Then

                ' Req. ������ ����
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

                    Me.LogWrite("PLC �� ����Ÿ �б���� �Ͽ����ϴ�.")

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    NS = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.SendErr)

                    Me.LogWrite("PLC �� ���� ���� �Ͽ����ϴ�.")
                    Me.NetNg()

                    GoTo Deleay

                End Try

                ' Rspon. ����Ÿ �ޱ�
                Dim RData(Me._ReceiveBufferSize - 1) As Byte
                Dim RecvCount As Integer
                Try
                    RecvCount = NS.Read(RData, 0, Me._ReceiveBufferSize - 1)


                    Me.LogWrite("RecvCount = " + RecvCount.ToString())
                    Me.LogWrite("Size = " + (32 + (RData(30) + RData(31) * 256)).ToString())

                    ' rev �� ����Ÿ ���� check
                    If RecvCount <> 32 + (RData(30) + RData(31) * 256) Then
                        Me._NetMode = Net_Mode.NetOK
                        Me.NetNg()
                        GoTo Deleay
                    End If

                    ' ����� �̻��� ������� �޼��带 ������.
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

                    ' �� ��ȣ ���� �迭�� RData(14)-100
                    ' ����Ÿ byte ���� �迭�� RData(30)
                    ' ������ ���� �� 8 ����Ʈ �Ҵ�

                    i = Me._memDic(_READ_BLOCK(RData(14) - 100).DEVICE).ToInt32 + Me._READ_BLOCK(RData(14) - 100).SAVE_ADD
                    Dim pTH As New IntPtr(i)
                    System.Runtime.InteropServices.Marshal.Copy(RData, 32, pTH, RData(30) + RData(31) * 256)
                    pTH = Nothing

                    Me.NetOk()

                Catch ex As Exception

                    Me.LogWrite("PLC �� ����Ÿ �б� ������ �����ϴ�.")

                    Me._NetMode = Net_Mode.DisConnection
                    Me._LSEnet.Close()
                    RaiseEvent ENET_EVENT(NetEvent.DisConnection)
                    _LSEnet = New System.Net.Sockets.TcpClient(System.Net.Sockets.AddressFamily.InterNetwork)
                    NS = Nothing
                    RaiseEvent ENET_EVENT(NetEvent.RecErr)

                    Me.LogWrite("PLC �� ���� ���� �Ͽ����ϴ�.")
                    Me.NetNg()

                    GoTo Deleay

                End Try
                Me._NetMode = Net_Mode.NetOK
            End If
            '--------------------------------------------------
            ' ReadData ť�� ����Ÿ�� ��������б⸦ �����Ѵ�.
            '--------------------------------------------------


            ' ����� ���� �ʾ������ 0.005 �� �޽�
            System.Threading.Thread.Sleep(1)

Deleay:

        End While

    End Sub
    ' ��� ���� ������ �޼���

#End Region

#Region " ��Ÿ ���� ��� �޼���"

    Public Sub ViewMem(ByVal Device As String)

        Dim MemView As New Frm_MemoryView(Me._memDic(Device).ToInt32)
        MemView.Show()

    End Sub
    ' �޸𸮺並 ���̰��Ѵ�.

#End Region

End Class







