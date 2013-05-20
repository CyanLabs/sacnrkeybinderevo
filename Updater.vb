'Add this just after class ...
'---------------------------------
'Private UpdateChecker As System.Threading.Thread = New Thread(AddressOf Updater.IsLatest)
'---------------------------------

'Add this in a method
'---------------------------------
'UpdateChecker.IsBackground = True
'UpdateChecker.Start()
'---------------------------------
Public Module Updater
    Dim wc As New Net.WebClient
    Dim localversion As String = My.Application.Info.Version.ToString
    Public Sub IsLatest(Optional ByVal server As String = "ftp://199.96.156.121/")
        If Environment.GetCommandLineArgs.Length > 1 Then
            For Each x As String In Environment.GetCommandLineArgs
                If x.Contains("-v=") Then localversion = x.Replace("-v=", "")
            Next
        End If
        Try
            wc.Credentials = New Net.NetworkCredential("anonymous@cyanlabs.co.uk", "anonymous")
            Dim xm As New Xml.XmlDocument
            xm.LoadXml(wc.DownloadString(server & "updates/versions.xml"))
            Dim latestVersion As String = xm.SelectSingleNode("//" & My.Application.Info.AssemblyName.Replace(" ", "_") & "//CurrentVersion").InnerText.Trim
            If localversion < latestVersion Then
                If MsgBox("A new update is available for download" & vbNewLine & vbNewLine & "Would you like to download version v" & latestVersion & " now?", MsgBoxStyle.Information + MsgBoxStyle.YesNo, "Update Avaliable") = vbYes Then
                    If IO.File.Exists(Application.StartupPath & "\AutoUpdater.exe") Then
                        Dim updaterversion As FileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(Application.StartupPath & "\AutoUpdater.exe")
                        If updaterversion.FileVersion < xm.SelectSingleNode("//autoupdater//CurrentVersion").InnerText.Trim Then
                            DownloadUpdater(latestVersion)
                        Else
                            Process.Start(Application.StartupPath & "\AutoUpdater.exe ", "-product=" & My.Application.Info.AssemblyName.Replace(" ", "_") & " -v=" & latestVersion)
                            Application.Exit()
                        End If
                    Else
                        DownloadUpdater(latestVersion)
                    End If
                End If
            End If
        Catch ex As Net.WebException
            MsgBox(ex.Message.ToString)
        Catch ex As NullReferenceException
            MsgBox("This application currently doesn't support autoupdating")
        Catch ex As System.Xml.XPath.XPathException
            MsgBox(ex.Message.ToString)
        End Try
    End Sub
    Private Sub DownloadUpdater(ByVal latestversion As String, Optional ByVal server As String = "ftp://199.96.156.121/")
        Try
            If IO.File.Exists(Application.StartupPath & "\AutoUpdater.exe") Then IO.File.Delete(Application.StartupPath & "\AutoUpdater.exe")
            wc.DownloadFile(New Uri(server & "/updates/" & "AutoUpdater.exe"), Application.StartupPath & "\AutoUpdater.exe")
            Process.Start(Application.StartupPath & "\AutoUpdater.exe ", "-product=" & My.Application.Info.AssemblyName.Replace(" ", "_") & " -v=" & latestversion)
            Application.Exit()
        Catch ex As Net.WebException
            MsgBox(ex.Message.ToString)
        End Try
    End Sub
End Module
