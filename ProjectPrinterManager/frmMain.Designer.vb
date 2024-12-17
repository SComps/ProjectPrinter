<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class frmMain
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Label1 = New Label()
        txtHost = New TextBox()
        cmdConnect = New Button()
        MenuStrip1 = New MenuStrip()
        ServerToolStripMenuItem = New ToolStripMenuItem()
        LoadServerConfigurationToolStripMenuItem = New ToolStripMenuItem()
        SaveServerConfigurationToolStripMenuItem = New ToolStripMenuItem()
        ToolStripMenuItem1 = New ToolStripSeparator()
        ExitToolStripMenuItem = New ToolStripMenuItem()
        DevicesToolStripMenuItem = New ToolStripMenuItem()
        LoadDevicesToolStripMenuItem = New ToolStripMenuItem()
        SaveDevicesToolStripMenuItem = New ToolStripMenuItem()
        TabControl1 = New TabControl()
        TabPage1 = New TabPage()
        Label5 = New Label()
        ComboBox1 = New ComboBox()
        Label4 = New Label()
        TextBox3 = New TextBox()
        Label3 = New Label()
        Button2 = New Button()
        TextBox2 = New TextBox()
        Label2 = New Label()
        TabPage2 = New TabPage()
        CheckBox2 = New CheckBox()
        CheckBox1 = New CheckBox()
        TextBox6 = New TextBox()
        Label11 = New Label()
        ComboBox4 = New ComboBox()
        Label10 = New Label()
        ComboBox3 = New ComboBox()
        Label9 = New Label()
        ComboBox2 = New ComboBox()
        Label8 = New Label()
        TextBox5 = New TextBox()
        Label7 = New Label()
        TextBox4 = New TextBox()
        Label6 = New Label()
        ListBox1 = New ListBox()
        MenuStrip1.SuspendLayout()
        TabControl1.SuspendLayout()
        TabPage1.SuspendLayout()
        TabPage2.SuspendLayout()
        SuspendLayout()
        ' 
        ' Label1
        ' 
        Label1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Label1.AutoSize = True
        Label1.Location = New Point(22, 37)
        Label1.Name = "Label1"
        Label1.Size = New Size(61, 23)
        Label1.TabIndex = 0
        Label1.Text = "Server:"
        ' 
        ' txtHost
        ' 
        txtHost.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtHost.Location = New Point(86, 35)
        txtHost.Name = "txtHost"
        txtHost.Size = New Size(382, 30)
        txtHost.TabIndex = 1
        ' 
        ' cmdConnect
        ' 
        cmdConnect.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        cmdConnect.Location = New Point(482, 31)
        cmdConnect.Name = "cmdConnect"
        cmdConnect.Size = New Size(85, 36)
        cmdConnect.TabIndex = 2
        cmdConnect.Text = "Connect"
        cmdConnect.UseVisualStyleBackColor = True
        ' 
        ' MenuStrip1
        ' 
        MenuStrip1.ImageScalingSize = New Size(20, 20)
        MenuStrip1.Items.AddRange(New ToolStripItem() {ServerToolStripMenuItem, DevicesToolStripMenuItem})
        MenuStrip1.Location = New Point(0, 0)
        MenuStrip1.Name = "MenuStrip1"
        MenuStrip1.Size = New Size(900, 28)
        MenuStrip1.TabIndex = 3
        MenuStrip1.Text = "MenuStrip1"
        ' 
        ' ServerToolStripMenuItem
        ' 
        ServerToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {LoadServerConfigurationToolStripMenuItem, SaveServerConfigurationToolStripMenuItem, ToolStripMenuItem1, ExitToolStripMenuItem})
        ServerToolStripMenuItem.Name = "ServerToolStripMenuItem"
        ServerToolStripMenuItem.Size = New Size(64, 24)
        ServerToolStripMenuItem.Text = "Server"
        ' 
        ' LoadServerConfigurationToolStripMenuItem
        ' 
        LoadServerConfigurationToolStripMenuItem.Name = "LoadServerConfigurationToolStripMenuItem"
        LoadServerConfigurationToolStripMenuItem.Size = New Size(261, 26)
        LoadServerConfigurationToolStripMenuItem.Text = "Load server configuration"
        ' 
        ' SaveServerConfigurationToolStripMenuItem
        ' 
        SaveServerConfigurationToolStripMenuItem.Name = "SaveServerConfigurationToolStripMenuItem"
        SaveServerConfigurationToolStripMenuItem.Size = New Size(261, 26)
        SaveServerConfigurationToolStripMenuItem.Text = "Save server configuration"
        ' 
        ' ToolStripMenuItem1
        ' 
        ToolStripMenuItem1.Name = "ToolStripMenuItem1"
        ToolStripMenuItem1.Size = New Size(258, 6)
        ' 
        ' ExitToolStripMenuItem
        ' 
        ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        ExitToolStripMenuItem.Size = New Size(261, 26)
        ExitToolStripMenuItem.Text = "Exit"
        ' 
        ' DevicesToolStripMenuItem
        ' 
        DevicesToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {LoadDevicesToolStripMenuItem, SaveDevicesToolStripMenuItem})
        DevicesToolStripMenuItem.Name = "DevicesToolStripMenuItem"
        DevicesToolStripMenuItem.Size = New Size(74, 24)
        DevicesToolStripMenuItem.Text = "Devices"
        ' 
        ' LoadDevicesToolStripMenuItem
        ' 
        LoadDevicesToolStripMenuItem.Name = "LoadDevicesToolStripMenuItem"
        LoadDevicesToolStripMenuItem.Size = New Size(178, 26)
        LoadDevicesToolStripMenuItem.Text = "Load devices"
        ' 
        ' SaveDevicesToolStripMenuItem
        ' 
        SaveDevicesToolStripMenuItem.Name = "SaveDevicesToolStripMenuItem"
        SaveDevicesToolStripMenuItem.Size = New Size(178, 26)
        SaveDevicesToolStripMenuItem.Text = "Save Devices"
        ' 
        ' TabControl1
        ' 
        TabControl1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        TabControl1.Controls.Add(TabPage1)
        TabControl1.Controls.Add(TabPage2)
        TabControl1.Location = New Point(22, 82)
        TabControl1.Name = "TabControl1"
        TabControl1.SelectedIndex = 0
        TabControl1.Size = New Size(846, 423)
        TabControl1.TabIndex = 4
        ' 
        ' TabPage1
        ' 
        TabPage1.Controls.Add(Label5)
        TabPage1.Controls.Add(ComboBox1)
        TabPage1.Controls.Add(Label4)
        TabPage1.Controls.Add(TextBox3)
        TabPage1.Controls.Add(Label3)
        TabPage1.Controls.Add(Button2)
        TabPage1.Controls.Add(TextBox2)
        TabPage1.Controls.Add(Label2)
        TabPage1.Location = New Point(4, 32)
        TabPage1.Name = "TabPage1"
        TabPage1.Padding = New Padding(3)
        TabPage1.Size = New Size(838, 387)
        TabPage1.TabIndex = 0
        TabPage1.Text = "Server"
        TabPage1.UseVisualStyleBackColor = True
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label5.Font = New Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label5.ForeColor = Color.Firebrick
        Label5.Location = New Point(38, 173)
        Label5.Name = "Label5"
        Label5.Size = New Size(753, 25)
        Label5.TabIndex = 7
        Label5.Text = "Changes made on this tab require a server shutdown and restart."
        Label5.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' ComboBox1
        ' 
        ComboBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        ComboBox1.FormattingEnabled = True
        ComboBox1.Items.AddRange(New Object() {"None (silent)", "Default", "File (define)"})
        ComboBox1.Location = New Point(163, 114)
        ComboBox1.Name = "ComboBox1"
        ComboBox1.Size = New Size(236, 31)
        ComboBox1.TabIndex = 6
        ' 
        ' Label4
        ' 
        Label4.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label4.AutoSize = True
        Label4.Location = New Point(38, 114)
        Label4.Name = "Label4"
        Label4.Size = New Size(80, 23)
        Label4.TabIndex = 5
        Label4.Text = "Log type:"
        ' 
        ' TextBox3
        ' 
        TextBox3.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        TextBox3.Location = New Point(163, 66)
        TextBox3.Name = "TextBox3"
        TextBox3.Size = New Size(140, 30)
        TextBox3.TabIndex = 4
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label3.AutoSize = True
        Label3.Location = New Point(38, 69)
        Label3.Name = "Label3"
        Label3.Size = New Size(95, 23)
        Label3.TabIndex = 3
        Label3.Text = "Listen port:"
        ' 
        ' Button2
        ' 
        Button2.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Button2.Location = New Point(670, 12)
        Button2.Name = "Button2"
        Button2.Size = New Size(94, 43)
        Button2.TabIndex = 2
        Button2.Text = "Browse"
        Button2.UseVisualStyleBackColor = True
        ' 
        ' TextBox2
        ' 
        TextBox2.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        TextBox2.Location = New Point(163, 19)
        TextBox2.Name = "TextBox2"
        TextBox2.Size = New Size(486, 30)
        TextBox2.TabIndex = 1
        ' 
        ' Label2
        ' 
        Label2.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label2.AutoSize = True
        Label2.Location = New Point(38, 22)
        Label2.Name = "Label2"
        Label2.Size = New Size(119, 23)
        Label2.TabIndex = 0
        Label2.Text = "Configuration:"
        ' 
        ' TabPage2
        ' 
        TabPage2.Controls.Add(CheckBox2)
        TabPage2.Controls.Add(CheckBox1)
        TabPage2.Controls.Add(TextBox6)
        TabPage2.Controls.Add(Label11)
        TabPage2.Controls.Add(ComboBox4)
        TabPage2.Controls.Add(Label10)
        TabPage2.Controls.Add(ComboBox3)
        TabPage2.Controls.Add(Label9)
        TabPage2.Controls.Add(ComboBox2)
        TabPage2.Controls.Add(Label8)
        TabPage2.Controls.Add(TextBox5)
        TabPage2.Controls.Add(Label7)
        TabPage2.Controls.Add(TextBox4)
        TabPage2.Controls.Add(Label6)
        TabPage2.Controls.Add(ListBox1)
        TabPage2.Location = New Point(4, 29)
        TabPage2.Name = "TabPage2"
        TabPage2.Padding = New Padding(3)
        TabPage2.Size = New Size(838, 390)
        TabPage2.TabIndex = 1
        TabPage2.Text = "Devices"
        TabPage2.UseVisualStyleBackColor = True
        ' 
        ' CheckBox2
        ' 
        CheckBox2.AutoSize = True
        CheckBox2.Location = New Point(659, 332)
        CheckBox2.Name = "CheckBox2"
        CheckBox2.Size = New Size(137, 27)
        CheckBox2.TabIndex = 5
        CheckBox2.Text = "Generate PDF"
        CheckBox2.UseVisualStyleBackColor = True
        ' 
        ' CheckBox1
        ' 
        CheckBox1.AutoSize = True
        CheckBox1.Location = New Point(370, 332)
        CheckBox1.Name = "CheckBox1"
        CheckBox1.Size = New Size(135, 27)
        CheckBox1.TabIndex = 13
        CheckBox1.Text = "Auto connect"
        CheckBox1.UseVisualStyleBackColor = True
        ' 
        ' TextBox6
        ' 
        TextBox6.Location = New Point(372, 296)
        TextBox6.Name = "TextBox6"
        TextBox6.Size = New Size(426, 30)
        TextBox6.TabIndex = 12
        ' 
        ' Label11
        ' 
        Label11.AutoSize = True
        Label11.Location = New Point(372, 270)
        Label11.Name = "Label11"
        Label11.Size = New Size(135, 23)
        Label11.TabIndex = 11
        Label11.Text = "Remote location"
        ' 
        ' ComboBox4
        ' 
        ComboBox4.AutoCompleteCustomSource.AddRange(New String() {"TCP/IP SockDev", "Flat File"})
        ComboBox4.FormattingEnabled = True
        ComboBox4.Items.AddRange(New Object() {"TCP/IP (sockdev)", "Flat file"})
        ComboBox4.Location = New Point(481, 222)
        ComboBox4.Name = "ComboBox4"
        ComboBox4.Size = New Size(200, 31)
        ComboBox4.TabIndex = 10
        ' 
        ' Label10
        ' 
        Label10.AutoSize = True
        Label10.Location = New Point(370, 225)
        Label10.Name = "Label10"
        Label10.Size = New Size(98, 23)
        Label10.TabIndex = 9
        Label10.Text = "Connection"
        ' 
        ' ComboBox3
        ' 
        ComboBox3.FormattingEnabled = True
        ComboBox3.Items.AddRange(New Object() {"MVS 3.8j (Hercules)", "VMS (VAX/Alpha)", "MPE (HP3000)", "RSTS/E (PDP-11)"})
        ComboBox3.Location = New Point(481, 185)
        ComboBox3.Name = "ComboBox3"
        ComboBox3.Size = New Size(200, 31)
        ComboBox3.TabIndex = 8
        ' 
        ' Label9
        ' 
        Label9.AutoSize = True
        Label9.Location = New Point(370, 188)
        Label9.Name = "Label9"
        Label9.Size = New Size(64, 23)
        Label9.TabIndex = 7
        Label9.Text = "System"
        ' 
        ' ComboBox2
        ' 
        ComboBox2.FormattingEnabled = True
        ComboBox2.Items.AddRange(New Object() {"Printer", "Reader"})
        ComboBox2.Location = New Point(481, 148)
        ComboBox2.Name = "ComboBox2"
        ComboBox2.Size = New Size(200, 31)
        ComboBox2.TabIndex = 6
        ' 
        ' Label8
        ' 
        Label8.AutoSize = True
        Label8.Location = New Point(370, 151)
        Label8.Name = "Label8"
        Label8.Size = New Size(45, 23)
        Label8.TabIndex = 5
        Label8.Text = "Type"
        ' 
        ' TextBox5
        ' 
        TextBox5.Location = New Point(370, 103)
        TextBox5.Name = "TextBox5"
        TextBox5.Size = New Size(426, 30)
        TextBox5.TabIndex = 4
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(370, 77)
        Label7.Name = "Label7"
        Label7.Size = New Size(96, 23)
        Label7.TabIndex = 3
        Label7.Text = "Description"
        ' 
        ' TextBox4
        ' 
        TextBox4.Location = New Point(370, 44)
        TextBox4.Name = "TextBox4"
        TextBox4.Size = New Size(426, 30)
        TextBox4.TabIndex = 2
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(370, 18)
        Label6.Name = "Label6"
        Label6.Size = New Size(108, 23)
        Label6.TabIndex = 1
        Label6.Text = "Device name"
        ' 
        ' ListBox1
        ' 
        ListBox1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        ListBox1.FormattingEnabled = True
        ListBox1.Location = New Point(19, 18)
        ListBox1.Name = "ListBox1"
        ListBox1.Size = New Size(321, 326)
        ListBox1.TabIndex = 0
        ' 
        ' frmMain
        ' 
        AutoScaleDimensions = New SizeF(9F, 23F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.PaleGoldenrod
        ClientSize = New Size(900, 517)
        Controls.Add(TabControl1)
        Controls.Add(cmdConnect)
        Controls.Add(txtHost)
        Controls.Add(Label1)
        Controls.Add(MenuStrip1)
        Font = New Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        MainMenuStrip = MenuStrip1
        Margin = New Padding(4)
        Name = "frmMain"
        Text = "ProjectPrinter Manager [pre]"
        MenuStrip1.ResumeLayout(False)
        MenuStrip1.PerformLayout()
        TabControl1.ResumeLayout(False)
        TabPage1.ResumeLayout(False)
        TabPage1.PerformLayout()
        TabPage2.ResumeLayout(False)
        TabPage2.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents txtHost As TextBox
    Friend WithEvents cmdConnect As Button
    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents ServerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LoadServerConfigurationToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SaveServerConfigurationToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ToolStripMenuItem1 As ToolStripSeparator
    Friend WithEvents ExitToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents DevicesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LoadDevicesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SaveDevicesToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TabPage1 As TabPage
    Friend WithEvents TabPage2 As TabPage
    Friend WithEvents TextBox2 As TextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Button2 As Button
    Friend WithEvents Label5 As Label
    Friend WithEvents ComboBox1 As ComboBox
    Friend WithEvents Label4 As Label
    Friend WithEvents TextBox3 As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents ListBox1 As ListBox
    Friend WithEvents TextBox5 As TextBox
    Friend WithEvents Label7 As Label
    Friend WithEvents TextBox4 As TextBox
    Friend WithEvents Label6 As Label
    Friend WithEvents ComboBox2 As ComboBox
    Friend WithEvents Label8 As Label
    Friend WithEvents ComboBox3 As ComboBox
    Friend WithEvents Label9 As Label
    Friend WithEvents ComboBox4 As ComboBox
    Friend WithEvents Label10 As Label
    Friend WithEvents TextBox6 As TextBox
    Friend WithEvents Label11 As Label
    Friend WithEvents CheckBox2 As CheckBox
    Friend WithEvents CheckBox1 As CheckBox

End Class
