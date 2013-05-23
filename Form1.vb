'This program is free software: you can redistribute it and/or modify
'it under the terms of the GNU General Public License as publishedGetBoolean by
'the Free Software Foundation, either version 3 of the License, or
'(at your option) any later version.

'This program is distributed in the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty of
'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License for more details.
'You should have received a copy of the GNU General Public License
'along with this program.  If not, see <http://www.gnu.org/licenses/>.

Option Strict Off
Imports System.Threading
Imports System.Net
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Input
Imports System.Net.Mail
Imports WindowsHookLib

Public Class Form1
    Dim updated As Boolean = False, newversion As String = ""
    Dim running As Integer = 1, finishedload As Boolean = False, inisettings As ini, skipsavesettings As Boolean = False
    Dim loglocation As String = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) & "\GTA San Andreas User Files\SAMP"
    Dim wClient As WebClient
    Private trd2 As Thread
    Private UpdateChecker As System.Threading.Thread = New Thread(AddressOf Updater.IsLatest)
    Dim WithEvents kHook As New KeyboardHook, mHook As New MouseHook
    Private Declare Function GetForegroundWindow Lib "user32" Alias "GetForegroundWindow" () As IntPtr
    Private Declare Auto Function GetWindowText Lib "user32" (ByVal hWnd As System.IntPtr, ByVal lpString As System.Text.StringBuilder, ByVal cch As Integer) As Integer
    Dim CurrentVersion As String = "v" & System.Reflection.Assembly.GetEntryAssembly.GetName().Version.ToString
    Dim ProgramName As String = System.Reflection.Assembly.GetEntryAssembly().GetName().Name.Replace(" ", "_")
    Dim keybinderdisabled As Boolean = True, param_obj(2) As Object
    Dim CMDNumber As New Dictionary(Of String, Integer)()
#Region "Re-Usable Subs and Functions"
    'Function used to get title of window
    Private Function GetCaption() As String
        Dim Caption As New System.Text.StringBuilder(256)
        Dim hWnd As IntPtr = GetForegroundWindow()
        GetWindowText(hWnd, Caption, Caption.Capacity)
        Return Caption.ToString()
    End Function
    'Sub that handles all the splitting and toggling of the commands
    Sub macro(ByVal param_obj() As Object)
        Dim substr As String = param_obj(1)
        Dim pressed As String = param_obj(0)
        If substr.Contains("#") Then
            If Not CMDNumber.ContainsKey(pressed) Then
                CMDNumber(pressed) = 1
            End If
            Dim splitstring() As String = Split(substr, "#")
            Dim x As Integer = 0
            For Each item In splitstring
                x = x + 1
                If x >= CMDNumber(pressed) Then
                    SendKeys.SendWait("t" + item + "{Enter}")
                    CMDNumber(pressed) = CMDNumber(pressed) + 1
                    If splitstring.GetLength(0) = x Then
                        CMDNumber(pressed) = 1
                    End If
                    Exit Sub
                End If
            Next
        Else
            Dim splitstring() As String = Split(substr, "*")
            For Each item In splitstring
                If item(0) = "¬" Then
                    Thread.Sleep(item.Substring(1, 4))
                    item = item.Remove(0, 5)
                End If
                SendKeys.SendWait("t" + item + "{Enter}")
            Next
        End If
    End Sub
    'Sub to check key matches pressed key
    Public Sub KeyCheck(checkbox As ReactorCheckBox, pressedkey As String, chosenkey As String, cmd As ReactorTextBox)
        trd2 = New Thread(AddressOf macro)
        trd2.IsBackground = True
        If checkbox.Checked = True Then
            If pressedkey = chosenkey Then
                param_obj(1) = cmd.Text
                trd2.Start(param_obj)
            End If
        End If
    End Sub
    'Function to check whether -debug is set
    Function DebugCheck()
        If Environment.GetCommandLineArgs.Length > 1 Then
            For Each x As String In Environment.GetCommandLineArgs
                If x = "-debug" Then
                    Return "GTA:SA:MP"
                    Exit Function
                End If
            Next
            If chkDebug.Checked Then Return "GTA:SA:MP"
        End If
        Return GetCaption()
    End Function
    'Sub to savesettings
    Sub savesettings()
        If finishedload = True Then
            For Each ctrl In Me.ReactorTabControl2.TabPages(0).Controls
                If TypeOf ctrl Is ReactorTextBox Then
                    Dim optionname As String = ctrl.name.replace("ReactorTextBox", "Send")
                    inisettings.WriteString("SendKey", optionname, ctrl.Text)
                End If
                If TypeOf ctrl Is TextBox Then
                    Dim optionname As String = ctrl.name.replace("TextBox", "Key")
                    If ctrl.text = Nothing Then
                    Else
                        inisettings.WriteString("HotKey", optionname, ctrl.text.ToString)
                    End If
                End If
                If TypeOf ctrl Is ReactorCheckBox Then
                    Dim activation As String = ctrl.name.replace("ReactorCheckBox", "act")
                    inisettings.WriteString("Activate", activation, ctrl.checked.ToString)
                End If
            Next
            For Each ctrl In Me.ReactorTabControl2.TabPages(1).Controls
                If TypeOf ctrl Is ReactorTextBox Then
                    Dim optionname As String = ctrl.name.replace("ReactorTextBox", "Send")
                    inisettings.WriteString("SendKey", optionname, ctrl.Text)
                End If
                If TypeOf ctrl Is TextBox Then
                    Dim optionname As String = ctrl.name.replace("TextBox", "Key")
                    If ctrl.text = Nothing Then
                    Else
                        inisettings.WriteString("HotKey", optionname, ctrl.text.ToString)
                    End If
                End If
                If TypeOf ctrl Is ReactorCheckBox Then
                    Dim activation As String = ctrl.name.replace("ReactorCheckBox", "act")
                    inisettings.WriteString("Activate", activation, ctrl.checked.ToString)
                End If
            Next
            For Each ctrl In Me.ReactorTabControl1.TabPages(2).Controls
                If TypeOf ctrl Is ReactorTextBox Then
                    Dim optionname As String = ctrl.name.replace("txt", "360")
                    inisettings.WriteString("360", optionname, ctrl.text)
                ElseIf TypeOf ctrl Is ReactorCheckBox Then
                    Dim activation As String = ctrl.name.replace("chk", "360act")
                    inisettings.WriteString("360", activation, ctrl.checked.ToString)
                End If
            Next
            inisettings.WriteString("Mouse", "LeftClick", txtlmb.Text)
            inisettings.WriteString("Mouse", "RightClick", txtRMB.Text)
            inisettings.WriteString("Mouse", "MiddleClick", txtMMB.Text)
            inisettings.WriteString("Mouse", "WheelUp", txtWheelUp.Text)
            inisettings.WriteString("Mouse", "WheelDown", txtWheelDown.Text)
            inisettings.WriteString("Mouse", "SB1Click", txtSB1.Text)
            inisettings.WriteString("Mouse", "SB2Click", txtSB2.Text)
            inisettings.WriteString("Mouse", "LeftClickActivated", chkLMB.Checked.ToString)
            inisettings.WriteString("Mouse", "RightClickActivated", chkRMB.Checked.ToString)
            inisettings.WriteString("Mouse", "MiddleClickActivated", chkMMB.Checked.ToString)
            inisettings.WriteString("Mouse", "WheelUpActivated", chkWheelUp.Checked.ToString)
            inisettings.WriteString("Mouse", "WheelDownActivated", chkWheelDown.Checked.ToString)
            inisettings.WriteString("Mouse", "SB1ClickActivated", chkSB1.Checked.ToString)
            inisettings.WriteString("Mouse", "SB2ClickActivated", chkSB2.Checked.ToString)
            inisettings.WriteString("Settings", "ShowChangelog", chkSkipChangelog.Checked.ToString)
        End If
    End Sub
    'Function to check whethera process is running or not
    Public Function IsProcessRunning(name As String) As Boolean
        For Each clsProcess As Process In Process.GetProcesses()
            If clsProcess.ProcessName.StartsWith(name) Then
                Return True
            End If
        Next
        Return False
    End Function
#End Region
#Region "Binds (Mouse, Scroll, Keyboard and X360)"
    'Sub that is called when a mouse button is pressed then released
    Private Sub mHook_MouseUp(ByVal sender As Object, ByVal e As WindowsHookLib.MouseEventArgs) Handles mHook.MouseUp
        param_obj(0) = e.Button
        trd2 = New Thread(AddressOf macro)
        trd2.IsBackground = True
        If DebugCheck() = "GTA:SA:MP" Then
            If keybinderdisabled = False Then
                If e.Button = Windows.Forms.MouseButtons.Left Then
                    If chkLMB.Checked = True Then
                        param_obj(1) = txtlmb.Text
                        trd2.Start(param_obj)
                    End If
                ElseIf e.Button = Windows.Forms.MouseButtons.Middle Then
                    If chkMMB.Checked = True Then
                        param_obj(1) = txtMMB.Text
                        trd2.Start(param_obj)
                    End If
                ElseIf e.Button = Windows.Forms.MouseButtons.Right Then
                    If chkRMB.Checked = True Then
                        param_obj(1) = txtRMB.Text
                        trd2.Start(param_obj)
                    End If
                ElseIf e.Button = Windows.Forms.MouseButtons.XButton1 Then
                    If chkSB1.Checked = True Then
                        param_obj(1) = txtSB1.Text
                        trd2.Start(param_obj)
                    End If
                ElseIf e.Button = Windows.Forms.MouseButtons.XButton2 Then
                    If chkSB2.Checked = True Then
                        param_obj(1) = txtSB2.Text
                        trd2.Start(param_obj)
                    End If
                End If
            End If
        End If
    End Sub
    'Sub that is called when mouse scroll wheel is turned
    Private Sub mHook_MouseScroll(ByVal sender As Object, ByVal e As WindowsHookLib.MouseEventArgs) Handles mHook.MouseWheel
        param_obj(0) = e.Button
        trd2 = New Thread(AddressOf macro)
        trd2.IsBackground = True
        If DebugCheck() = "GTA:SA:MP" Then
            If keybinderdisabled = False Then
                If e.Delta > 0 Then
                    If chkWheelUp.Checked = True Then
                        param_obj(1) = txtWheelUp.Text
                        trd2.Start(param_obj)
                    End If
                Else
                    If chkWheelDown.Checked = True Then
                        param_obj(1) = txtWheelDown.Text
                        trd2.Start(param_obj)
                    End If
                End If
            End If
        End If
    End Sub
    'Sub that is called when a keyboard key is pressed and then released
    Private Sub kHook_KeyUp(ByVal sender As Object, ByVal e As WindowsHookLib.KeyboardEventArgs) Handles kHook.KeyUp
        If chkKeyUp.Checked = True Then
            DoKeybinds(sender, e)
        End If
    End Sub
    Private Sub kHook_KeyDown(ByVal sender As Object, ByVal e As WindowsHookLib.KeyboardEventArgs) Handles kHook.KeyDown
        If chkKeyUp.Checked = False Then
            DoKeybinds(sender, e)
        End If
    End Sub
    Private Sub DoKeybinds(ByVal semder As Object, ByVal e As WindowsHookLib.KeyboardEventArgs)
        If DebugCheck() = "GTA:SA:MP" Then
            If keybinderdisabled = False Then
                param_obj(0) = e.KeyData.ToString.ToUpper
                KeyCheck(ReactorCheckBox1, param_obj(0), TextBox1.Text.ToUpper, ReactorTextBox1)
                KeyCheck(ReactorCheckBox2, param_obj(0), TextBox2.Text.ToUpper, ReactorTextBox2)
                KeyCheck(ReactorCheckBox3, param_obj(0), TextBox3.Text.ToUpper, ReactorTextBox3)
                KeyCheck(ReactorCheckBox4, param_obj(0), TextBox4.Text.ToUpper, ReactorTextBox4)
                KeyCheck(ReactorCheckBox5, param_obj(0), TextBox5.Text.ToUpper, ReactorTextBox5)
                KeyCheck(ReactorCheckBox6, param_obj(0), TextBox6.Text.ToUpper, ReactorTextBox6)
                KeyCheck(ReactorCheckBox7, param_obj(0), TextBox7.Text.ToUpper, ReactorTextBox7)
                KeyCheck(ReactorCheckBox8, param_obj(0), TextBox8.Text.ToUpper, ReactorTextBox8)
                KeyCheck(ReactorCheckBox9, param_obj(0), TextBox9.Text.ToUpper, ReactorTextBox9)
                KeyCheck(ReactorCheckBox10, param_obj(0), TextBox10.Text.ToUpper, ReactorTextBox10)
                KeyCheck(ReactorCheckBox11, param_obj(0), TextBox11.Text.ToUpper, ReactorTextBox11)
                KeyCheck(ReactorCheckBox12, param_obj(0), TextBox12.Text.ToUpper, ReactorTextBox12)
                KeyCheck(ReactorCheckBox13, param_obj(0), TextBox13.Text.ToUpper, ReactorTextBox13)
                KeyCheck(ReactorCheckBox14, param_obj(0), TextBox14.Text.ToUpper, ReactorTextBox14)
                KeyCheck(ReactorCheckBox15, param_obj(0), TextBox15.Text.ToUpper, ReactorTextBox15)
                KeyCheck(ReactorCheckBox16, param_obj(0), TextBox16.Text.ToUpper, ReactorTextBox16)
                KeyCheck(ReactorCheckBox17, param_obj(0), TextBox17.Text.ToUpper, ReactorTextBox17)
                KeyCheck(ReactorCheckBox18, param_obj(0), TextBox18.Text.ToUpper, ReactorTextBox18)
                KeyCheck(ReactorCheckBox19, param_obj(0), TextBox19.Text.ToUpper, ReactorTextBox19)
                KeyCheck(ReactorCheckBox10, param_obj(0), TextBox20.Text.ToUpper, ReactorTextBox20)
            End If
        End If
        If e.KeyData.ToString = "F6" Or e.KeyData.ToString = "T" Or e.KeyData.ToString = "`" Then
            keybinderdisabled = True
        End If
        If e.KeyData.ToString = "Return" Or e.KeyData.ToString = "Escape" Then
            keybinderdisabled = False
        End If
    End Sub
    'Sub timer to control x360 binds (can't use a global hook like keyboard and mouse)
    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        If DebugCheck() = "GTA:SA:MP" Then
            If keybinderdisabled = False Then
                trd2 = New Thread(AddressOf macro)
                trd2.IsBackground = True
                Dim currentState As GamePadState = GamePad.GetState(PlayerIndex.One)
                If currentState.IsConnected Then
                    If chkButtonA.Checked = True Then
                        If currentState.Buttons.A = ButtonState.Pressed Then
                            param_obj(0) = "A"
                            param_obj(1) = txtButtonA.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkButtonX.Checked = True Then
                        If currentState.Buttons.X = ButtonState.Pressed Then
                            param_obj(0) = "XButton"
                            param_obj(1) = txtButtonX.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkButtonY.Checked = True Then
                        If currentState.Buttons.Y = ButtonState.Pressed Then
                            param_obj(0) = "YButton"
                            param_obj(1) = txtButtonY.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkButtonB.Checked = True Then
                        If currentState.Buttons.B = ButtonState.Pressed Then
                            param_obj(0) = "BButton"
                            param_obj(1) = txtButtonB.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkRB.Checked = True Then
                        If currentState.Buttons.RightShoulder = ButtonState.Pressed Then
                            param_obj(0) = "RB"
                            param_obj(1) = txtRB.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkLB.Checked = True Then
                        If currentState.Buttons.LeftShoulder = ButtonState.Pressed Then
                            param_obj(0) = "LB"
                            param_obj(1) = txtLb.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkDpadDown.Checked = True Then
                        If currentState.DPad.Down = ButtonState.Pressed Then
                            param_obj(0) = "DpadDown"
                            param_obj(1) = txtDpadDown.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkDpadLeft.Checked = True Then
                        If currentState.DPad.Left = ButtonState.Pressed Then
                            param_obj(0) = "DpadLeft"
                            param_obj(1) = txtDpadLeft.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkDpadRight.Checked = True Then
                        If currentState.DPad.Right = ButtonState.Pressed Then
                            param_obj(0) = "DpadRight"
                            param_obj(1) = txtDpadRight.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkDpadUp.Checked = True Then
                        If currentState.DPad.Up = ButtonState.Pressed Then
                            param_obj(0) = "DpadUp"
                            param_obj(1) = txtDpadUp.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkRightStick.Checked = True Then
                        If currentState.Buttons.RightStick = ButtonState.Pressed Then
                            param_obj(0) = "RS"
                            param_obj(1) = txtRightStickPress.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                    If chkLeftStick.Checked = True Then
                        If currentState.Buttons.LeftStick = ButtonState.Pressed Then
                            param_obj(0) = "LS"
                            param_obj(1) = txtLeftStickPress.Text
                            trd2.Start(param_obj)
                        End If
                    End If
                End If
            End If
        End If
    End Sub
#End Region

    'Loads sacnr.com when banner click
    Private Sub imgLogo2_Click(sender As Object, e As EventArgs) Handles imgLogo2.Click
        Process.Start("http://www.sacnr.com/?referer=keybinder")
    End Sub

    'Button that resets everything and restarts application
    Private Sub btnReset_Click(sender As Object, e As EventArgs) Handles btnReset.Click
        If MsgBox("Are you sure you wish to reset all settings and keybinds?", vbYesNo + MsgBoxStyle.Question, "Confirmation") = vbYes Then
            skipsavesettings = True
            If IO.File.Exists(Application.StartupPath & "\keybinds\" & txtSAMPUsername.Text & "_keybinds.sav") Then IO.File.Delete(Application.StartupPath & "\keybinds\" & txtSAMPUsername.Text & "_keybinds.sav")
            MsgBox("Default settings restored! Application will now restart", vbInformation, "Success!")
            Application.Restart()
        End If
    End Sub

    'Button which then calls the sub savesettings()
    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        savesettings()
    End Sub

    'Button to savesettings() and launch SACNR
    Private Sub btnLaunch_Click(sender As Object, e As EventArgs) Handles btnLaunch.Click
        savesettings()
        Dim gtalocation As String = ""
        If My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", Nothing) Is Nothing Or My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", Nothing) = "" Then
            MsgBox("gta_sa.exe could not be detected automatically, Please manually locate your gta_sa.exe, You only need to do this once.", vbCritical, "File not found")
            Using dialog As New OpenFileDialog
                If dialog.ShowDialog = DialogResult.Cancel Then
                    MsgBox("You did not select a file. Action aborted!", vbCritical, "ERROR")
                    Exit Sub
                Else
                    My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", dialog.FileName)
                    If MsgBox("gta_sa.exe was successfully detected." & vbNewLine & vbNewLine & "Do you want to launch ""San Andreas Cops n Robbers"" now?", vbInformation + MsgBoxStyle.OkCancel, "Success") <> MsgBoxResult.Ok Then Exit Sub
                End If
            End Using
        End If
        gtalocation = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\SAMP", "gta_sa_exe", Nothing).Replace("gta_sa.exe", "samp.exe")
        Process.Start(gtalocation, "server.sacnr.com:7777")
    End Sub

    'Form1 Closing Code
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If skipsavesettings = False Then
            savesettings()
        End If
        kHook.Dispose()
        mHook.Dispose()
    End Sub

    'Form1 Resize Code
    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        If Me.WindowState = FormWindowState.Minimized Then
            NotifyIcon1.Visible = True
            Me.ShowInTaskbar = False
        End If
    End Sub

    'Form1 Shown code
    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        CheckForIllegalCrossThreadCalls = False
        If Environment.GetCommandLineArgs.Length > 1 Then
            For Each x As String In Environment.GetCommandLineArgs
                If x = "-startup" Then
                    Me.WindowState = FormWindowState.Minimized
                    NotifyIcon1.Visible = True
                End If
                If x.Contains("-updated=") Then
                    updated = True
                    newversion = x.Replace("-updated=", "")
                End If
            Next
        End If
        IO.Directory.CreateDirectory(loglocation & "\Logs")

        Try
            kHook.InstallHook()
        Catch ex As Exception
            MessageBox.Show("Failed to install the keyboard hook!.")
        End Try
        Try
            mHook.InstallHook()
        Catch ex As Exception
            MessageBox.Show("Failed to install the mouse hook!")
        End Try
        txtSAMPUsername.Text = My.Computer.Registry.GetValue("HKEY_CURRENT_USER\Software\SAMP", "PlayerName", "Keybinds")
        If Not IO.Directory.Exists(Application.StartupPath & "\keybinds") Then IO.Directory.CreateDirectory(Application.StartupPath & "\keybinds")
        If IO.File.Exists(Application.StartupPath & "\keybinds.sav") Then
            IO.File.Copy(Application.StartupPath & "\keybinds.sav", Application.StartupPath & "\keybinds\" & txtSAMPUsername.Text & "_keybinds.sav", True)
            IO.File.Delete(Application.StartupPath & "\keybinds.sav")
        End If
        Dim di As New IO.DirectoryInfo(Application.StartupPath & "\keybinds")
        Dim fi As IO.FileInfo() = di.GetFiles("*.sav")
        For Each file In fi
            If Not file.Name.ToString = "_keybinds.sav" Then txtSAMPUsername.Items.Add(file.Name.ToString.Replace("_keybinds.sav", ""))
        Next

        inisettings = New ini(Application.StartupPath & "\keybinds\" & txtSAMPUsername.Text & "_keybinds.sav")
        If inisettings.GetString("Settings", "AutoUpdate", False) = True Then
            If updated Then
                MsgBox("You have successfully updated to V" & newversion, MsgBoxStyle.Information, "Update Successful")
            Else
                UpdateChecker.IsBackground = True
                UpdateChecker.Start()
            End If

        End If

        lblVersion.Text = CurrentVersion.ToString
        For Each ctrl In Me.ReactorTabControl2.TabPages(0).Controls
            If TypeOf ctrl Is ReactorTextBox Then
                Dim optionname As String = ctrl.name.replace("ReactorTextBox", "Send")
                ctrl.text = inisettings.GetString("SendKey", optionname, ctrl.text)
            ElseIf TypeOf ctrl Is TextBox Then
                Dim optionname As String = ctrl.name.replace("TextBox", "Key")
                ctrl.text = inisettings.GetString("HotKey", optionname, "")
            ElseIf TypeOf ctrl Is ReactorCheckBox Then
                Dim activation As String = ctrl.name.replace("ReactorCheckBox", "act")
                ctrl.checked = inisettings.GetString("Activate", activation, False)
            End If
        Next
        For Each ctrl In Me.ReactorTabControl2.TabPages(1).Controls
            If TypeOf ctrl Is ReactorTextBox Then
                Dim optionname As String = ctrl.name.replace("ReactorTextBox", "Send")
                ctrl.text = inisettings.GetString("SendKey", optionname, ctrl.text)
            ElseIf TypeOf ctrl Is TextBox Then
                Dim optionname As String = ctrl.name.replace("TextBox", "Key")
                ctrl.text = inisettings.GetString("HotKey", optionname, "")
            ElseIf TypeOf ctrl Is ReactorCheckBox Then
                Dim activation As String = ctrl.name.replace("ReactorCheckBox", "act")
                ctrl.checked = inisettings.GetString("Activate", activation, False)
            End If
        Next

        For Each ctrl In Me.ReactorTabControl1.TabPages(2).Controls
            If TypeOf ctrl Is ReactorTextBox Then
                Dim optionname As String = ctrl.name.replace("txt", "360")
                ctrl.text = inisettings.GetString("360", optionname, ctrl.text)
            ElseIf TypeOf ctrl Is ReactorCheckBox Then
                Dim activation As String = ctrl.name.replace("chk", "360act")
                ctrl.checked = inisettings.GetString("360", activation, False)
            End If
        Next

        txtlmb.Text = inisettings.GetString("Mouse", "LeftClick", Nothing)
        txtRMB.Text = inisettings.GetString("Mouse", "RightClick", Nothing)
        txtMMB.Text = inisettings.GetString("Mouse", "MiddleClick", Nothing)
        txtWheelUp.Text = inisettings.GetString("Mouse", "WheelUp", Nothing)
        txtWheelDown.Text = inisettings.GetString("Mouse", "WheelDown", Nothing)
        txtSB1.Text = inisettings.GetString("Mouse", "SB1Click", Nothing)
        txtSB2.Text = inisettings.GetString("Mouse", "SB2Click", Nothing)
        chkLMB.Checked = inisettings.GetString("Mouse", "LeftClickActivated", False)
        chkRMB.Checked = inisettings.GetString("Mouse", "RightClickActivated", False)
        chkMMB.Checked = inisettings.GetString("Mouse", "MiddleClickActivated", False)
        chkWheelUp.Checked = inisettings.GetString("Mouse", "WheelUpActivated", False)
        chkWheelDown.Checked = inisettings.GetString("Mouse", "WheelDownActivated", False)
        chkSB1.Checked = inisettings.GetString("Mouse", "SB1ClickActivated", False)
        chkSB2.Checked = inisettings.GetString("Mouse", "SB2ClickActivated", False)
        chkAutoupdates.Checked = inisettings.GetString("Settings", "AutoUpdate", False)
        chkEnableLogs.Checked = inisettings.GetString("Settings", "EnableLogManager", False)
        chkEnable360.Checked = inisettings.GetString("360", "MasterToggle", False)
        chkSkipChangelog.Checked = inisettings.GetString("Settings", "ShowChangelog", True)
        chkKeyUp.Checked = inisettings.GetString("Advanced Settings", "UseKeyUp", False)
        If chkEnable360.Checked = True Then
            Timer2.Start()
        Else
            Timer2.Stop()
        End If
        If chkEnableLogs.Checked = True Then
            Timer1.Start()
        Else
            Timer1.Stop()
        End If
        If My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).GetValue(Application.ProductName) Is Nothing Then
            chkStartup.Checked = False
        Else
            chkStartup.Checked = True
        End If
        finishedload = True
    End Sub

    'Code to filter key presses and make sure it gets the raw value
    Private Sub TextBox2_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox9.KeyDown, TextBox8.KeyDown, TextBox7.KeyDown, TextBox6.KeyDown, TextBox5.KeyDown, TextBox4.KeyDown, TextBox3.KeyDown, TextBox2.KeyDown, TextBox10.KeyDown, TextBox1.KeyDown, TextBox20.KeyDown, TextBox19.KeyDown, TextBox18.KeyDown, TextBox17.KeyDown, TextBox16.KeyDown, TextBox15.KeyDown, TextBox14.KeyDown, TextBox13.KeyDown, TextBox12.KeyDown, TextBox11.KeyDown
        sender.text = ""
        sender.tag = e.KeyCode
        sender.text = e.KeyCode.ToString.ToUpper
        e.SuppressKeyPress = True
    End Sub

    'Autoupdate check change
    Private Sub chkAutoupdates_CheckedChanged(sender As Object) Handles chkAutoupdates.CheckedChanged
        inisettings.WriteString("Settings", "AutoUpdate", sender.checked.ToString)
    End Sub

    'Code to monitor whether samp is still running or not, if it isn't save log
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If IsProcessRunning("gta_sa") = False AndAlso running = 2 Then
            Me.running = 0
        End If
        If (IsProcessRunning("gta_sa") = True) Then
            Me.running = 2
        End If
        If Me.running = 0 Then
            Dim str As String = (DateTime.Now.ToString("dd-MM-yy") & "_" & DateTime.Now.ToString("HH-mm"))
            If My.Computer.FileSystem.FileExists(loglocation & "\chatlog.txt") Then
                If Not IO.Directory.Exists(loglocation & "\Logs\" & txtSAMPUsername.Text) Then IO.Directory.CreateDirectory(loglocation & "\Logs\" & txtSAMPUsername.Text)
                My.Computer.FileSystem.CopyFile(loglocation & "\chatlog.txt", loglocation & "\Logs\" & txtSAMPUsername.Text & "\chatlog_" & str & ".txt", True)
                Me.NotifyIcon1.ShowBalloonTip(5000, "SACNR Keybinder 2013 Edition", "Log Saved (" & str & ")", ToolTipIcon.Info)
                Me.running = 1
            End If
        End If

    End Sub

    'Logmanager status
    Private Sub chkEnableLogs_CheckedChanged(sender As Object) Handles chkEnableLogs.CheckedChanged, chkSkipChangelog.CheckedChanged
        inisettings.WriteString("Settings", "EnableLogManager", sender.checked.ToString)
        If sender.Checked = True Then
            Timer1.Start()
        Else
            Timer1.Stop()
        End If
    End Sub

    'Change active profile and restart application
    Private Sub btnSaveRestart_Click(sender As Object, e As EventArgs) Handles btnSaveRestart.Click
        Dim result = MsgBox("This will change the SAMP username." & vbNewLine & "All settings and keybinds will be saved as 'OLDNAME_Keybinds.sav' and a new file called '" & txtSAMPUsername.Text & "_keybinds.sav' will be used. You can switch back to your old username at any time by changing this textbox back." & vbNewLine & vbNewLine & "Are you sure you want to change SAMP Username?", vbYesNo + MsgBoxStyle.Question, "Confirmation")
        If result = vbYes Then
            My.Computer.Registry.SetValue("HKEY_CURRENT_USER\Software\SAMP", "PlayerName", txtSAMPUsername.Text)
            Application.Restart()
        End If

    End Sub

    'save trackbar2 value to inifile
    Private Sub TrackBar2_Scroll(sender As Object, e As EventArgs)
        inisettings.WriteInteger("360", "Interval", sender.value)
    End Sub

    'Enable/Disable 360 bind timer
    Private Sub chkEnable360_CheckedChanged(sender As Object) Handles chkEnable360.CheckedChanged
        inisettings.WriteInteger("360", "MasterToggle", sender.checked)
        If sender.Checked = True Then
            Timer2.Start()
        Else
            Timer2.Stop()
        End If
    End Sub

    'Checks if value entered in to macro textbox is numerical and then sets ini to value of textbox
    Private Sub txtMacroDelay_TextChanged(sender As Object, e As EventArgs)
        If IsNumeric(sender.text) Then
            inisettings.WriteInteger("Settings", "MacroDelay", Convert.ToInt32(sender.text))
        Else
            MsgBox("You did not enter a numerical value, please enter only numbers and decimals", MsgBoxStyle.Critical, "Error")
        End If
    End Sub

    'Simply opens log folder in explorer
    Private Sub btnLogs_Click(sender As Object, e As EventArgs) Handles btnLogs.Click
        Try
            Process.Start("explorer.exe", loglocation & "\Logs\" & txtSAMPUsername.Text)
        Catch ex As Exception
            MsgBox("Log directory could not be opened as the directory does not seem to exist.", MsgBoxStyle.Critical, "Error")
        End Try
    End Sub

    'Sends email with feedback/suggestion/bug report
    Private Sub btnSendRequest_Click(sender As Object, e As EventArgs) Handles btnSendRequest.Click
        Dim emailcontents As String = txtFeedback.Text
        Dim result = MsgBox("This will send the feedback below to CyanLabs (Fma965)." & vbNewLine & vbNewLine & """" & emailcontents & """" & vbNewLine & vbNewLine & "Are you sure?", vbYesNo + MsgBoxStyle.Question, "Confirmation")
        If result = vbYes Then
            result = MsgBox("Do you want to include your SA-MP username with the email?", vbYesNo + MsgBoxStyle.Question, "Confirmation")

            If result = vbYes Then
                emailcontents &= vbNewLine & vbNewLine & "Feedback/Suggestion was posted by """ & txtSAMPUsername.Text & """"
            End If
            Try
                Dim SmtpServer As New SmtpClient()
                Dim mail As New MailMessage()
                SmtpServer.Port = 2525
                SmtpServer.Host = "smtpcorp.com"
                mail = New MailMessage()
                mail.From = New MailAddress("sacnrkeybinder2013@cyanlabs.co.uk")
                mail.To.Add("fma96580@gmail.com")
                mail.Subject = "SACNR Keybinder Evolution - Feedback and Suggestions"
                mail.Body = "New feedback or suggestion for 'SACNR Keybinder Evolution' has been recieved!" & vbNewLine & vbNewLine & emailcontents
                SmtpServer.Send(mail)
                MsgBox("Your feedback has been sent successfully, Thank you for helping make SACNR Keybinder Evolution better!")
            Catch ex As Exception
                MsgBox("The following error occured:" & vbNewLine & vbNewLine & ex.ToString, MsgBoxStyle.Critical, "Error")
            End Try
        End If
    End Sub

    'Clears textbox contents when clicked if value is default
    Private Sub txtFeedback_Enter(sender As Object, e As EventArgs) Handles txtFeedback.Enter
        If sender.text = "Leave feedback or suggest a new feature or change here." Then
            sender.text = ""
        End If
    End Sub

    'Add/Remove registry entry to start at windows startup
    Private Sub chkStartup_CheckedChanged(sender As Object) Handles chkStartup.CheckedChanged
        If sender.checked = True Then
            My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).SetValue(Application.ProductName, Application.ExecutablePath & " -startup")
        Else
            My.Computer.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True).DeleteValue(Application.ProductName)
        End If
    End Sub

    'Code that runs when notification icon is double clicked
    Private Sub NotifyIcon1_MouseDoubleClick(sender As Object, e As Windows.Forms.MouseEventArgs) Handles NotifyIcon1.MouseDoubleClick
        NotifyIcon1.Visible = False
        ShowInTaskbar = True
        Me.WindowState = FormWindowState.Normal
    End Sub

    'Code that runs when notification icon is right clicked
    Private Sub NotifyIcon1_MouseClick(sender As Object, e As Windows.Forms.MouseEventArgs) Handles NotifyIcon1.MouseClick
        If e.Button = Windows.Forms.MouseButtons.Right Then
            Application.Exit()
        End If
    End Sub

    Private Sub chkKeyUp_CheckedChanged(sender As Object) Handles chkKeyUp.CheckedChanged
        inisettings.WriteString("Advanced Settings", "UseKeyUp", sender.checked.ToString)
    End Sub
End Class