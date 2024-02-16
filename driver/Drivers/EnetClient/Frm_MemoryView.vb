Imports System.Runtime.InteropServices
Public Class Frm_MemoryView
    Inherits System.Windows.Forms.Form

    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents TextBox_Value As System.Windows.Forms.TextBox
    Friend WithEvents Button_Write As System.Windows.Forms.Button
    Friend WithEvents Timer_Update As System.Windows.Forms.Timer
    Private _pAdd As Integer

#Region " Windows Form 디자이너에서 생성한 코드 "

    Public Sub New()
        MyBase.New()

        '이 호출은 Windows Form 디자이너에 필요합니다.
        InitializeComponent()

        'InitializeComponent()를 호출한 다음에 초기화 작업을 추가하십시오.

    End Sub

    Public Sub New(ByRef Address As Integer)

        MyBase.New()

        '이 호출은 Windows Form 디자이너에 필요합니다.
        InitializeComponent()

        Me._pAdd = Address

        'InitializeComponent()를 호출한 다음에 초기화 작업을 추가하십시오.

    End Sub

    'Form은 Dispose를 재정의하여 구성 요소 목록을 정리합니다.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Windows Form 디자이너에 필요합니다.
    Private components As System.ComponentModel.IContainer

    '참고: 다음 프로시저는 Windows Form 디자이너에 필요합니다.
    'Windows Form 디자이너를 사용하여 수정할 수 있습니다.  
    '코드 편집기를 사용하여 수정하지 마십시오.
    Friend WithEvents TabControl1 As System.Windows.Forms.TabControl
    Friend WithEvents TabPage1 As System.Windows.Forms.TabPage
    Friend WithEvents Label1 As System.Windows.Forms.Label
    Friend WithEvents Button_Read As System.Windows.Forms.Button
    Friend WithEvents CheckBox_AutoRead As System.Windows.Forms.CheckBox
    Friend WithEvents TextBox_Address As System.Windows.Forms.TextBox
    Friend WithEvents ListView_View As System.Windows.Forms.ListView
    Friend WithEvents ColumnHeader1 As System.Windows.Forms.ColumnHeader
    Friend WithEvents TextBox_MemView As System.Windows.Forms.TextBox
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.TabControl1 = New System.Windows.Forms.TabControl()
        Me.TabPage1 = New System.Windows.Forms.TabPage()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.TextBox_Value = New System.Windows.Forms.TextBox()
        Me.Button_Write = New System.Windows.Forms.Button()
        Me.CheckBox_AutoRead = New System.Windows.Forms.CheckBox()
        Me.Button_Read = New System.Windows.Forms.Button()
        Me.TextBox_Address = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TextBox_MemView = New System.Windows.Forms.TextBox()
        Me.ListView_View = New System.Windows.Forms.ListView()
        Me.ColumnHeader1 = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.Timer_Update = New System.Windows.Forms.Timer(Me.components)
        Me.TabControl1.SuspendLayout()
        Me.TabPage1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TabControl1
        '
        Me.TabControl1.Controls.Add(Me.TabPage1)
        Me.TabControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TabControl1.Location = New System.Drawing.Point(0, 0)
        Me.TabControl1.Name = "TabControl1"
        Me.TabControl1.SelectedIndex = 0
        Me.TabControl1.Size = New System.Drawing.Size(672, 603)
        Me.TabControl1.TabIndex = 1
        '
        'TabPage1
        '
        Me.TabPage1.Controls.Add(Me.Label2)
        Me.TabPage1.Controls.Add(Me.TextBox_Value)
        Me.TabPage1.Controls.Add(Me.Button_Write)
        Me.TabPage1.Controls.Add(Me.CheckBox_AutoRead)
        Me.TabPage1.Controls.Add(Me.Button_Read)
        Me.TabPage1.Controls.Add(Me.TextBox_Address)
        Me.TabPage1.Controls.Add(Me.Label1)
        Me.TabPage1.Controls.Add(Me.TextBox_MemView)
        Me.TabPage1.Location = New System.Drawing.Point(4, 22)
        Me.TabPage1.Name = "TabPage1"
        Me.TabPage1.Size = New System.Drawing.Size(664, 577)
        Me.TabPage1.TabIndex = 0
        Me.TabPage1.Text = "MEMORY"
        '
        'Label2
        '
        Me.Label2.BackColor = System.Drawing.SystemColors.Control
        Me.Label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label2.Font = New System.Drawing.Font("굴림", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(129, Byte))
        Me.Label2.ForeColor = System.Drawing.Color.Black
        Me.Label2.Location = New System.Drawing.Point(389, 8)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(71, 24)
        Me.Label2.TabIndex = 8
        Me.Label2.Text = "VALUE"
        Me.Label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'TextBox_Value
        '
        Me.TextBox_Value.Font = New System.Drawing.Font("돋움", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(129, Byte))
        Me.TextBox_Value.Location = New System.Drawing.Point(466, 8)
        Me.TextBox_Value.Name = "TextBox_Value"
        Me.TextBox_Value.Size = New System.Drawing.Size(100, 25)
        Me.TextBox_Value.TabIndex = 7
        Me.TextBox_Value.Text = "0"
        Me.TextBox_Value.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'Button_Write
        '
        Me.Button_Write.Location = New System.Drawing.Point(572, 8)
        Me.Button_Write.Name = "Button_Write"
        Me.Button_Write.Size = New System.Drawing.Size(84, 24)
        Me.Button_Write.TabIndex = 6
        Me.Button_Write.Text = "Write"
        '
        'CheckBox_AutoRead
        '
        Me.CheckBox_AutoRead.Location = New System.Drawing.Point(291, 8)
        Me.CheckBox_AutoRead.Name = "CheckBox_AutoRead"
        Me.CheckBox_AutoRead.Size = New System.Drawing.Size(92, 24)
        Me.CheckBox_AutoRead.TabIndex = 5
        Me.CheckBox_AutoRead.Text = "Auto Read"
        '
        'Button_Read
        '
        Me.Button_Read.Location = New System.Drawing.Point(201, 8)
        Me.Button_Read.Name = "Button_Read"
        Me.Button_Read.Size = New System.Drawing.Size(84, 24)
        Me.Button_Read.TabIndex = 4
        Me.Button_Read.Text = "Read"
        '
        'TextBox_Address
        '
        Me.TextBox_Address.Font = New System.Drawing.Font("돋움", 11.25!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(129, Byte))
        Me.TextBox_Address.Location = New System.Drawing.Point(95, 8)
        Me.TextBox_Address.Name = "TextBox_Address"
        Me.TextBox_Address.Size = New System.Drawing.Size(100, 25)
        Me.TextBox_Address.TabIndex = 3
        Me.TextBox_Address.Text = "0"
        Me.TextBox_Address.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'Label1
        '
        Me.Label1.BackColor = System.Drawing.SystemColors.Control
        Me.Label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label1.Font = New System.Drawing.Font("굴림", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, CType(129, Byte))
        Me.Label1.ForeColor = System.Drawing.Color.Black
        Me.Label1.Location = New System.Drawing.Point(4, 8)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(85, 24)
        Me.Label1.TabIndex = 2
        Me.Label1.Text = "ADDRESS"
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter
        '
        'TextBox_MemView
        '
        Me.TextBox_MemView.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox_MemView.BackColor = System.Drawing.SystemColors.Window
        Me.TextBox_MemView.Font = New System.Drawing.Font("Courier New", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, CType(0, Byte))
        Me.TextBox_MemView.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.TextBox_MemView.Location = New System.Drawing.Point(4, 40)
        Me.TextBox_MemView.Multiline = True
        Me.TextBox_MemView.Name = "TextBox_MemView"
        Me.TextBox_MemView.ReadOnly = True
        Me.TextBox_MemView.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TextBox_MemView.Size = New System.Drawing.Size(656, 533)
        Me.TextBox_MemView.TabIndex = 1
        '
        'ListView_View
        '
        Me.ListView_View.Location = New System.Drawing.Point(176, 180)
        Me.ListView_View.Name = "ListView_View"
        Me.ListView_View.Size = New System.Drawing.Size(121, 97)
        Me.ListView_View.TabIndex = 6
        Me.ListView_View.UseCompatibleStateImageBehavior = False
        '
        'Timer_Update
        '
        Me.Timer_Update.Interval = 1000
        '
        'Frm_MemoryView
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(6, 14)
        Me.ClientSize = New System.Drawing.Size(672, 603)
        Me.Controls.Add(Me.TabControl1)
        Me.Name = "Frm_MemoryView"
        Me.Text = "Memory View"
        Me.TopMost = True
        Me.TabControl1.ResumeLayout(False)
        Me.TabPage1.ResumeLayout(False)
        Me.TabPage1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private Sub CheckBox_AutoRead_CheckedChanged(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CheckBox_AutoRead.CheckedChanged

        Timer_Update.Enabled = CheckBox_AutoRead.Checked

    End Sub
    ' 자동 읽기 체크 이벤

    Private Sub Timer_Update_Tick(sender As System.Object, e As System.EventArgs) Handles Timer_Update.Tick

        Me.AddRead(True)

    End Sub
    ' 자동 읽기 타이머 이벤트

    Private Sub AddRead(ByVal ReadAuto As Boolean)

        Dim LineNum As Integer = (Me.TextBox_MemView.Height - 8) \ 16
        Dim StartAdd As Integer
        Dim i, j As Integer
        Dim SLine As String

        ' 숫자값에 이외의 값이 있는지 체크
        For i = 1 To Me.TextBox_Address.TextLength
            If AscW(Mid(Me.TextBox_Address.Text, i, 1)) < 48 Or AscW(Mid(Me.TextBox_Address.Text, i, 1)) > 57 Then
                If ReadAuto = True Then
                    Me.CheckBox_AutoRead.Checked = False
                End If
                Me.TextBox_Address.Focus()
                Exit Sub
            End If
        Next

        StartAdd = Val(Me.TextBox_Address.Text)

        Dim VByte(9) As Byte
        Dim _pInt As New IntPtr
        Dim s As String = ""

        For i = 1 To LineNum

            SLine &= Format(StartAdd, " <0000000#>")
            _pInt = New IntPtr(Me._pAdd + StartAdd)

            System.Runtime.InteropServices.Marshal.Copy(_pInt, VByte, 0, 10)

            For j = 0 To 9
                s = Hex(VByte(j))
                If s.Length = 1 Then
                    s = "0" & s
                End If
                SLine &= "  " & s
            Next
            SLine &= "  ||  "
            For j = 0 To 9
                If VByte(j) >= 32 And VByte(j) <= 128 Then
                    SLine &= ChrW(VByte(j)) & " "
                Else
                    SLine &= ". "
                End If
            Next

            SLine &= vbCrLf
            StartAdd += 10

        Next

        Me.TextBox_MemView.Text = SLine

        VByte = Nothing
        '_pInt = Nothing


    End Sub
    ' 데이타를 읽어 텍스트 박스에 넣는다.

    Private Sub Button_Read_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button_Read.Click

        Me.AddRead(False)

    End Sub
    ' 수동 읽기 보기

    Private Sub Frm_MemoryView_SizeChanged(ByVal sender As Object, ByVal e As System.EventArgs) Handles MyBase.SizeChanged

        Dim FSize As System.Drawing.Size
        FSize = Me.Size
        If FSize.Width < 680 Then
            FSize.Width = 680
            Me.Size = FSize
        End If
        FSize = Nothing

    End Sub

    Private Sub Button_Write_Click(sender As System.Object, e As System.EventArgs) Handles Button_Write.Click
        Dim StartAdd As Integer
        Dim _pInt As New IntPtr

        StartAdd = Val(Me.TextBox_Address.Text)
        _pInt = New IntPtr(Me._pAdd + StartAdd)

        Dim VByte(1) As Byte

        VByte(0) = CByte(Me.TextBox_Value.Text)
        System.Runtime.InteropServices.Marshal.Copy(VByte, 0, _pInt, 1)
    End Sub

End Class
