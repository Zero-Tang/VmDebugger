Imports System.IO
Imports System.Net.Sockets
Imports Microsoft.VisualBasic.FileIO
Public Class Form1
	Private Delegate Function ConsoleHandler(ByRef CommandArguments() As String) As Integer
	Dim TargetSession As GdbSession
	Dim SymMgr As New SymbolManager(Application.StartupPath, New List(Of String))
	' We will use dictionary to register all commands, and - maybe in future - support custom scripted extensions.
	Dim CommandHandlers As New Dictionary(Of String, ConsoleHandler)
	Private Function LoadModule(ByRef CommandArguments() As String) As Integer
		If CommandArguments.Length < 4 Then PrintToCommand(String.Format("Command Format: .modload [Image-Name] [Image-Base] [Image-End]"))
		Dim ImgName As String = CommandArguments(1)
		Dim Starting As Long, Ending As Long
		If CommandArguments(2).StartsWith("0x") Then
			Starting = Convert.ToInt64(CommandArguments(2).Substring(2), 16)
		Else
			Starting = Convert.ToInt64(CommandArguments(2), 16)
		End If
		If CommandArguments(3).StartsWith("0x") Then
			Ending = Convert.ToInt64(CommandArguments(3).Substring(2), 16)
		Else
			Ending = Convert.ToInt64(CommandArguments(3), 16)
		End If
		Dim PEImg As New PEImage(TargetSession, Starting, CInt(Ending - Starting))
		PrintToCommand(String.Format("Image is loaded! CodeView GUID={0}", PEImg.CvGuid))
		Dim SymFile As String = SymMgr.DownloadSymbol(PEImg.CvFileName, PEImg.CvGuidString)
		Dim SymMod As New SymbolModule(SymMgr, ImgName, SymFile, Starting, CInt(Ending - Starting))
		Return 0
	End Function

	Private Function ContinueTarget(ByRef CommandArguments() As String) As Integer
		TargetSession.ContinueExecution()
		ToolStripMenuItem3.Enabled = False
		ToolStripMenuItem4.Enabled = True
		Return 0
	End Function

	Private Function ReadMemory(ByRef CommandArguments() As String) As Integer
		Dim Address As Long
		Dim Length As Integer = &H80
		If CommandArguments.Length < 2 Then
			PrintToCommand(String.Format("Additional parameter required for target address!"))
			Return 1
		Else
			If CommandArguments(1).StartsWith("0n") Then
				Address = Convert.ToInt64(CommandArguments(1).Substring(2), 10)
			ElseIf CommandArguments(1).StartsWith("0x") Then
				Address = Convert.ToInt64(CommandArguments(1).Substring(2), 16)
			Else
				Try
					Address = Convert.ToInt64(CommandArguments(1), 16)
				Catch Ex As FormatException
					Dim Sym As Symbol = SymMgr.SymbolFromName(CommandArguments(1))
					Address = Sym.Address
				End Try
			End If
		End If
		Dim MemoryData As Byte() = TargetSession.ReadMemory(Address, Length)
		If CommandArguments(0) = "db" Then
			' Format in bytes
			Dim i As Integer
			' Each line shows 16 bytes
			For i = 0 To Length - 1 Step 16
				Dim j As Integer
				' Show bytes in hexadecimal
				PrintToCommand(String.Format("0x{0:X16}", Address + i), vbTab)
				For j = i To i + 15 Step 1
					If j = Length Then Exit For
					PrintToCommand(String.Format("{0:X2}", MemoryData(j)), " ")
				Next
				' Show bytes in ASCII
				PrintToCommand(Strings.StrDup(i + 17 - j, vbTab), "")
				For j = i To i + 15 Step 1
					If j = Length Then Exit For
					' Make sure the byte is visible as ASCII character.
					If MemoryData(j) >= &H20 AndAlso MemoryData(j) <= &H7F Then
						PrintToCommand(Chr(MemoryData(j)), "")
					Else
						PrintToCommand(".", "")
					End If
				Next
				PrintToCommand("")
			Next
		ElseIf CommandArguments(0) = "dw" Then
			' Format in words
		End If
		Return 0
	End Function

	Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
		' Register commands to dictionary.
		CommandHandlers.Add("g", AddressOf ContinueTarget)
		CommandHandlers.Add("db", AddressOf ReadMemory)
		CommandHandlers.Add(".modload", AddressOf LoadModule)
	End Sub

	Private Sub PrintToCommand(ByVal Text As String, Optional ByVal Ending As String = vbCrLf)
		TextBox1.Text += Text + Ending
	End Sub

	Private Sub ToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem2.Click
		' Connect to the GDB server and establish the session
		Dim TargetAddress As String() = InputBox("Input the address of GDB Server", "Input Address").Split(":")
		Dim TargetSocket As New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
		TargetSocket.Connect(TargetAddress(0), CInt(TargetAddress(1)))
		Dim TargetConnection As New DebuggerConnection(TargetSocket)
		TargetSession = New GdbSession(TargetConnection, GdbSession.VmType.Qemu)
		PrintToCommand(String.Format("Connected to GDB Session at {0}!", TargetAddress))
		TextBox2.Enabled = True
		TextBox2.Focus()
		ToolStripMenuItem2.Enabled = False
	End Sub

	Private Sub TextBox2_KeyPress(sender As Object, e As KeyPressEventArgs) Handles TextBox2.KeyPress
		If e.KeyChar = vbCr Then
			Dim TFP As New TextFieldParser(New StringReader(TextBox2.Text)) With {.HasFieldsEnclosedInQuotes = True, .Delimiters = New String() {" "}}
			Dim CmdRaw As String() = TFP.ReadFields()
			PrintToCommand("vd>" + TextBox2.Text)
			If CommandHandlers.ContainsKey(CmdRaw(0)) Then
				CommandHandlers.Item(CmdRaw(0))(CmdRaw)
			Else
				PrintToCommand(String.Format("Unknown command: {0}!", CmdRaw(0)))
			End If
			TextBox2.Text = ""
		End If
	End Sub

	Private Sub Form1_SizeChanged(sender As Object, e As EventArgs) Handles Me.SizeChanged
		With TabControl1
			.Width = Me.Width - 40
			.Height = Me.Height - 91
		End With
		With TextBox1
			.Width = Me.Width - 60
			.Height = Me.Height - 160
		End With
		With TextBox2
			.Width = Me.Width - 60
			.Top = Me.Height - 148
		End With
	End Sub

	Private Sub ToolStripMenuItem4_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem4.Click
		Dim BS As New GdbSession.BreakStatus
		TargetSession.BreakExecution(BS)
		PrintToCommand(String.Format("You triggered a Manual Break! Target received signal: {0}", BS.Signal))
		ToolStripMenuItem3.Enabled = True
		ToolStripMenuItem4.Enabled = False
	End Sub

	Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
		' Dim BS As New GdbSession.BreakStatus
		' If e.KeyCode = Keys.Cancel Then TargetSession.BreakExecution(BS)
	End Sub
End Class
