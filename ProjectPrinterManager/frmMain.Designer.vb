﻿<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
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
        components = New ComponentModel.Container()
        Label1 = New Label()
        txtHost = New TextBox()
        TabControl1 = New TabControl()
        TabPage1 = New TabPage()
        logType = New TextBox()
        Label5 = New Label()
        Label4 = New Label()
        cmdPort = New TextBox()
        Label3 = New Label()
        configFile = New TextBox()
        Label2 = New Label()
        TabPage2 = New TabPage()
        Button4 = New Button()
        Button2 = New Button()
        Button3 = New Button()
        Button1 = New Button()
        devPDF = New CheckBox()
        devAuto = New CheckBox()
        devDest = New TextBox()
        Label11 = New Label()
        devConn = New ComboBox()
        Label10 = New Label()
        devOS = New ComboBox()
        Label9 = New Label()
        devType = New ComboBox()
        Label8 = New Label()
        devDescription = New TextBox()
        Label7 = New Label()
        devName = New TextBox()
        Label6 = New Label()
        ListOfDevs = New ListBox()
        getButton = New Button()
        putButton = New Button()
        DataLight = New Label()
        Timer1 = New Timer(components)
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
        txtHost.Location = New Point(86, 35)
        txtHost.Name = "txtHost"
        txtHost.Size = New Size(382, 30)
        txtHost.TabIndex = 1
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
        TabPage1.Controls.Add(logType)
        TabPage1.Controls.Add(Label5)
        TabPage1.Controls.Add(Label4)
        TabPage1.Controls.Add(cmdPort)
        TabPage1.Controls.Add(Label3)
        TabPage1.Controls.Add(configFile)
        TabPage1.Controls.Add(Label2)
        TabPage1.Location = New Point(4, 32)
        TabPage1.Name = "TabPage1"
        TabPage1.Padding = New Padding(3)
        TabPage1.Size = New Size(838, 387)
        TabPage1.TabIndex = 0
        TabPage1.Text = "Server"
        TabPage1.UseVisualStyleBackColor = True
        ' 
        ' logType
        ' 
        logType.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        logType.Location = New Point(189, 204)
        logType.Name = "logType"
        logType.Size = New Size(486, 30)
        logType.TabIndex = 8
        ' 
        ' Label5
        ' 
        Label5.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label5.Font = New Font("Segoe UI", 10.2F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label5.ForeColor = Color.Firebrick
        Label5.Location = New Point(64, 266)
        Label5.Name = "Label5"
        Label5.Size = New Size(753, 25)
        Label5.TabIndex = 7
        Label5.Text = "Changes made on this tab require a server shutdown and restart."
        Label5.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Label4
        ' 
        Label4.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label4.AutoSize = True
        Label4.Location = New Point(64, 207)
        Label4.Name = "Label4"
        Label4.Size = New Size(80, 23)
        Label4.TabIndex = 5
        Label4.Text = "Log type:"
        ' 
        ' cmdPort
        ' 
        cmdPort.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        cmdPort.Location = New Point(189, 159)
        cmdPort.Name = "cmdPort"
        cmdPort.Size = New Size(140, 30)
        cmdPort.TabIndex = 4
        ' 
        ' Label3
        ' 
        Label3.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label3.AutoSize = True
        Label3.Location = New Point(64, 162)
        Label3.Name = "Label3"
        Label3.Size = New Size(95, 23)
        Label3.TabIndex = 3
        Label3.Text = "Listen port:"
        ' 
        ' configFile
        ' 
        configFile.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        configFile.Location = New Point(189, 112)
        configFile.Name = "configFile"
        configFile.Size = New Size(486, 30)
        configFile.TabIndex = 1
        ' 
        ' Label2
        ' 
        Label2.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Label2.AutoSize = True
        Label2.Location = New Point(64, 115)
        Label2.Name = "Label2"
        Label2.Size = New Size(119, 23)
        Label2.TabIndex = 0
        Label2.Text = "Configuration:"
        ' 
        ' TabPage2
        ' 
        TabPage2.Controls.Add(Button4)
        TabPage2.Controls.Add(Button2)
        TabPage2.Controls.Add(Button3)
        TabPage2.Controls.Add(Button1)
        TabPage2.Controls.Add(devPDF)
        TabPage2.Controls.Add(devAuto)
        TabPage2.Controls.Add(devDest)
        TabPage2.Controls.Add(Label11)
        TabPage2.Controls.Add(devConn)
        TabPage2.Controls.Add(Label10)
        TabPage2.Controls.Add(devOS)
        TabPage2.Controls.Add(Label9)
        TabPage2.Controls.Add(devType)
        TabPage2.Controls.Add(Label8)
        TabPage2.Controls.Add(devDescription)
        TabPage2.Controls.Add(Label7)
        TabPage2.Controls.Add(devName)
        TabPage2.Controls.Add(Label6)
        TabPage2.Controls.Add(ListOfDevs)
        TabPage2.Location = New Point(4, 32)
        TabPage2.Name = "TabPage2"
        TabPage2.Padding = New Padding(3)
        TabPage2.Size = New Size(838, 387)
        TabPage2.TabIndex = 1
        TabPage2.Text = "Devices"
        TabPage2.UseVisualStyleBackColor = True
        ' 
        ' Button4
        ' 
        Button4.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Button4.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Button4.Location = New Point(119, 250)
        Button4.Name = "Button4"
        Button4.Size = New Size(94, 32)
        Button4.TabIndex = 17
        Button4.Text = "Delete"
        Button4.UseVisualStyleBackColor = True
        ' 
        ' Button2
        ' 
        Button2.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Button2.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Button2.Location = New Point(19, 250)
        Button2.Name = "Button2"
        Button2.Size = New Size(94, 32)
        Button2.TabIndex = 16
        Button2.Text = "New"
        Button2.UseVisualStyleBackColor = True
        ' 
        ' Button3
        ' 
        Button3.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Button3.Location = New Point(119, 289)
        Button3.Name = "Button3"
        Button3.Size = New Size(94, 40)
        Button3.TabIndex = 15
        Button3.Text = "Cancel"
        Button3.UseVisualStyleBackColor = True
        ' 
        ' Button1
        ' 
        Button1.Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        Button1.Location = New Point(19, 289)
        Button1.Name = "Button1"
        Button1.Size = New Size(94, 40)
        Button1.TabIndex = 14
        Button1.Text = "Update"
        Button1.UseVisualStyleBackColor = True
        ' 
        ' devPDF
        ' 
        devPDF.AutoSize = True
        devPDF.Location = New Point(659, 332)
        devPDF.Name = "devPDF"
        devPDF.Size = New Size(137, 27)
        devPDF.TabIndex = 5
        devPDF.Text = "Generate PDF"
        devPDF.UseVisualStyleBackColor = True
        ' 
        ' devAuto
        ' 
        devAuto.AutoSize = True
        devAuto.Location = New Point(370, 332)
        devAuto.Name = "devAuto"
        devAuto.Size = New Size(135, 27)
        devAuto.TabIndex = 13
        devAuto.Text = "Auto connect"
        devAuto.UseVisualStyleBackColor = True
        ' 
        ' devDest
        ' 
        devDest.Location = New Point(372, 296)
        devDest.Name = "devDest"
        devDest.Size = New Size(426, 30)
        devDest.TabIndex = 12
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
        ' devConn
        ' 
        devConn.AutoCompleteCustomSource.AddRange(New String() {"TCP/IP SockDev", "Flat File"})
        devConn.FormattingEnabled = True
        devConn.Items.AddRange(New Object() {"TCP/IP (sockdev)", "Flat file"})
        devConn.Location = New Point(481, 222)
        devConn.Name = "devConn"
        devConn.Size = New Size(200, 31)
        devConn.TabIndex = 10
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
        ' devOS
        ' 
        devOS.FormattingEnabled = True
        devOS.Items.AddRange(New Object() {"MVS 3.8j (Hercules)", "VMS (VAX/Alpha)", "MPE (HP3000)", "RSTS/E (PDP-11)"})
        devOS.Location = New Point(481, 185)
        devOS.Name = "devOS"
        devOS.Size = New Size(200, 31)
        devOS.TabIndex = 8
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
        ' devType
        ' 
        devType.FormattingEnabled = True
        devType.Items.AddRange(New Object() {"Printer", "Reader"})
        devType.Location = New Point(481, 148)
        devType.Name = "devType"
        devType.Size = New Size(200, 31)
        devType.TabIndex = 6
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
        ' devDescription
        ' 
        devDescription.Location = New Point(370, 103)
        devDescription.Name = "devDescription"
        devDescription.Size = New Size(426, 30)
        devDescription.TabIndex = 4
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
        ' devName
        ' 
        devName.Location = New Point(370, 44)
        devName.Name = "devName"
        devName.Size = New Size(426, 30)
        devName.TabIndex = 2
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
        ' ListOfDevs
        ' 
        ListOfDevs.FormattingEnabled = True
        ListOfDevs.Location = New Point(19, 18)
        ListOfDevs.Name = "ListOfDevs"
        ListOfDevs.Size = New Size(321, 234)
        ListOfDevs.TabIndex = 0
        ' 
        ' getButton
        ' 
        getButton.Enabled = False
        getButton.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        getButton.Location = New Point(492, 35)
        getButton.Name = "getButton"
        getButton.Size = New Size(94, 29)
        getButton.TabIndex = 5
        getButton.Text = "Get data"
        getButton.UseVisualStyleBackColor = True
        ' 
        ' putButton
        ' 
        putButton.Enabled = False
        putButton.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        putButton.Location = New Point(592, 35)
        putButton.Name = "putButton"
        putButton.Size = New Size(94, 29)
        putButton.TabIndex = 6
        putButton.Text = "Put data"
        putButton.UseVisualStyleBackColor = True
        ' 
        ' DataLight
        ' 
        DataLight.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        DataLight.BackColor = Color.Black
        DataLight.BorderStyle = BorderStyle.FixedSingle
        DataLight.Font = New Font("Consolas", 13.8F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        DataLight.ForeColor = Color.DarkGray
        DataLight.Location = New Point(704, 18)
        DataLight.Name = "DataLight"
        DataLight.Size = New Size(160, 58)
        DataLight.TabIndex = 16
        DataLight.Text = "Data Transfer"
        DataLight.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Timer1
        ' 
        Timer1.Interval = 500
        ' 
        ' frmMain
        ' 
        AutoScaleDimensions = New SizeF(9F, 23F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.PaleGoldenrod
        ClientSize = New Size(900, 517)
        Controls.Add(DataLight)
        Controls.Add(putButton)
        Controls.Add(getButton)
        Controls.Add(TabControl1)
        Controls.Add(txtHost)
        Controls.Add(Label1)
        Font = New Font("Segoe UI", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Margin = New Padding(4)
        Name = "frmMain"
        Text = "ProjectPrinter Manager [pre]"
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
    Friend WithEvents TabControl1 As TabControl
    Friend WithEvents TabPage1 As TabPage
    Friend WithEvents TabPage2 As TabPage
    Friend WithEvents configFile As TextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents Label5 As Label
    Friend WithEvents Label4 As Label
    Friend WithEvents cmdPort As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents ListOfDevs As ListBox
    Friend WithEvents devDescription As TextBox
    Friend WithEvents Label7 As Label
    Friend WithEvents devName As TextBox
    Friend WithEvents Label6 As Label
    Friend WithEvents devType As ComboBox
    Friend WithEvents Label8 As Label
    Friend WithEvents devOS As ComboBox
    Friend WithEvents Label9 As Label
    Friend WithEvents devConn As ComboBox
    Friend WithEvents Label10 As Label
    Friend WithEvents devDest As TextBox
    Friend WithEvents Label11 As Label
    Friend WithEvents devPDF As CheckBox
    Friend WithEvents devAuto As CheckBox
    Friend WithEvents getButton As Button
    Friend WithEvents putButton As Button
    Friend WithEvents Button3 As Button
    Friend WithEvents Button1 As Button
    Friend WithEvents DataLight As Label
    Friend WithEvents logType As TextBox
    Friend WithEvents Timer1 As Timer
    Friend WithEvents Button2 As Button
    Friend WithEvents Button4 As Button

End Class
