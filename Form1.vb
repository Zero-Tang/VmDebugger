﻿Imports System.IO
Imports System.Net.Sockets
Imports Microsoft.VisualBasic.FileIO
Public Class Form1
	Private Delegate Function ConsoleHandler(ByRef CommandArguments() As String) As Integer
	Dim TargetSession As GdbSession
	' We will use dictionary to register all commands, and - maybe in future - support custom scripted extensions.
	Dim CommandHandlers As New Dictionary(Of String, ConsoleHandler)
	Private Function ContinueTarget(ByRef CommandArguments() As String) As Integer
		TargetSession.ContinueExecution()
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
				Address = Convert.ToInt64(CommandArguments(1), 16)
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
End Class
