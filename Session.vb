Imports System.IO.Packaging
Imports System.IO.Pipes
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.InteropServices.Marshalling
Imports System.Text
Public Class DebuggerConnection
	Public Enum ConnectionStyle
		Stream
		Packet
		Unknown
	End Enum
	Public ConnectionType As ConnectionStyle
	Dim InternalSocket As Socket = Nothing
	Dim RemoteTarget As EndPoint = Nothing
	Sub New(ByRef ConnectionSocket As Socket, Optional ByVal TargetAddress As EndPoint = Nothing)
		InternalSocket = ConnectionSocket
		Select Case InternalSocket.ProtocolType
			Case ProtocolType.Tcp
				ConnectionType = ConnectionStyle.Stream
				TargetAddress = ConnectionSocket.RemoteEndPoint
			Case ProtocolType.Udp
				ConnectionType = ConnectionStyle.Packet
				TargetAddress = TargetAddress
			Case Else
				ConnectionType = ConnectionStyle.Unknown
		End Select
	End Sub
	Public Function Send(ByVal Message As Byte(), Optional ByVal SendAll As Boolean = True) As Integer
		If ConnectionType = ConnectionStyle.Stream Then
			If SendAll Then
				Dim SentLength As Integer = 0
				Do While SentLength < Message.Length
					SentLength += InternalSocket.Send(Message, SentLength, Message.Length - SentLength, SocketFlags.None)
				Loop
				Return SentLength
			Else
				Return InternalSocket.Send(Message, SocketFlags.None)
			End If
		ElseIf ConnectionType = ConnectionStyle.Packet Then
			Return InternalSocket.SendTo(Message, RemoteTarget)
		Else
			Throw New NotImplementedException(String.Format("Unknown Protocol ({0}) is specified!", ConnectionType))
		End If
	End Function
	Public Function Receive(ByVal Length As Integer, Optional ByVal ReceiveAll As Boolean = True) As Byte()
		Dim Received(Length - 1) As Byte
		Dim ReceivedLength As Integer = 0
		If ConnectionType = ConnectionStyle.Stream Then
			If ReceiveAll Then
				Do While ReceivedLength < Length
					ReceivedLength += InternalSocket.Receive(Received, ReceivedLength, Length - ReceivedLength, SocketFlags.None)
				Loop
			Else
				ReceivedLength = InternalSocket.Receive(Received, SocketFlags.None)
				Array.Resize(Received, ReceivedLength)
			End If
		ElseIf ConnectionType = ConnectionStyle.Packet Then
			ReceivedLength = InternalSocket.ReceiveFrom(Received, RemoteTarget)
			Array.Resize(Received, ReceivedLength)
		Else
			Throw New NotImplementedException(String.Format("Unknown Protocol ({0}) is specified!", ConnectionType))
		End If
		Return Received
	End Function
End Class

Public Class GdbSession
	Dim InternalConnection As DebuggerConnection
	Dim TypeOfVM As VmType
	Public Structure BreakStatus
		Dim Signal As Integer
		Dim ProcessorId As Integer
		Dim CoreId As Integer
	End Structure
	Public Enum VmType
		Qemu
		Unknown
	End Enum
	''' <summary>
	''' Creates a GDB Session
	''' </summary>
	''' <param name="Connection">Specifies the connection toward the GDB Server. For TCP connections, you must make sure it is connected!</param>
	''' <param name="VirtualMachineType">Specifies the type of virtual machine. Each type of virtual machine has different extensions.</param>
	Sub New(Connection As DebuggerConnection, Optional ByVal VirtualMachineType As VmType = VmType.Unknown)
		InternalConnection = Connection
		TypeOfVM = VirtualMachineType
		Connection.Send(MakePacket("?"))
RetryReceive:
		Dim RecvData() As Byte = ReceivePacket()
		Dim RecvString As String = Encoding.ASCII.GetString(RecvData)
		If RecvString = "+" Then GoTo RetryReceive
		Debug.Print("Reason of Target Halt: " + RecvString)
		SendAcknowledgement()
	End Sub
	''' <summary>
	''' Creates a raw packet of data, including its checksums.
	''' </summary>
	''' <param name="Command">The command string to be sent to GDB Server.</param>
	''' <returns>The raw data to be sent via connection.</returns>
	Private Function MakePacket(ByVal Command As String) As Byte()
		Dim CommandData() As Byte = Encoding.ASCII.GetBytes(Command)
		' Checksum
		Dim CSum As UInteger = 0
		For Each CmdByte As Byte In CommandData
			CSum += CmdByte
			CSum = CSum And &HFF
		Next
		' Forge Packet
		Dim PacketString As String = String.Format("${0}#{1:x2}", Command, CSum)
		Debug.Print(String.Format("Sending Packet: {0}", PacketString))
		Return Encoding.ASCII.GetBytes(PacketString)
	End Function
	Private Sub SendAcknowledgement(Optional ByVal Acknowledged As Boolean = True)
		Dim CommandData(0) As Byte
		CommandData(0) = IIf(Acknowledged, Asc("+"), Asc("-"))
		InternalConnection.Send(CommandData)
	End Sub
	''' <summary>
	''' Receives the full content of the packet.
	''' </summary>
	''' <returns>The content of the packet. It can be either an acknowledgement or a packet.</returns>
	Private Function ReceivePacket() As Byte()
		If InternalConnection.ConnectionType = DebuggerConnection.ConnectionStyle.Stream Then
			Dim PacketData(-1) As Byte
			Dim BreakLoop As Boolean = True
			Do
				Dim NewData() As Byte = InternalConnection.Receive(1)
				Select Case Chr(NewData(0))
					Case "+"
						' Packet is Acknowledged
						Array.Resize(PacketData, PacketData.Length + 1)
						PacketData(PacketData.Length - 1) = NewData(0)
						BreakLoop = True
					Case "-"
						' Packet is Not-Acknowledged
						Array.Resize(PacketData, PacketData.Length + 1)
						PacketData(PacketData.Length - 1) = NewData(0)
						BreakLoop = True
					Case "$"
						' Start of packet.
						Array.Resize(PacketData, PacketData.Length + 1)
						PacketData(PacketData.Length - 1) = NewData(0)
						BreakLoop = False
					Case "#"
						' Contents are all received. Remainder is the checksum.
						Dim CheckSumData() As Byte = InternalConnection.Receive(2)
						Array.Resize(PacketData, PacketData.Length + 3)
						PacketData(PacketData.Length - 3) = NewData(0)
						PacketData(PacketData.Length - 2) = CheckSumData(0)
						PacketData(PacketData.Length - 1) = CheckSumData(1)
						BreakLoop = True
					Case Else
						Array.Resize(PacketData, PacketData.Length + 1)
						PacketData(PacketData.Length - 1) = NewData(0)
				End Select
			Loop Until BreakLoop
			Return PacketData
		ElseIf InternalConnection.ConnectionType = DebuggerConnection.ConnectionStyle.Packet Then
			Return InternalConnection.Receive(65536)
		Else
			Throw New NotImplementedException(String.Format("Unknown Protocol ({0}) is specified!", InternalConnection.ConnectionType))
		End If
	End Function
	''' <summary>
	''' Resumes the execution of the target.
	''' This method is not intended for implementing single-stepping.
	''' </summary>
	Public Sub ContinueExecution()
		Dim CmdData() As Byte = MakePacket("c")
		InternalConnection.Send(CmdData)
		Dim RecvData() As Byte = ReceivePacket()
		Dim RecvString As String = Encoding.ASCII.GetString(RecvData)
		Debug.Print(String.Format("[Length={0}] Received on Continue: {1}", RecvData.Length, RecvString))
	End Sub
	''' <summary>
	''' Breaks the execution of the target.
	''' </summary>
	Public Sub BreakExecution(ByRef Status As BreakStatus)
		Dim CmdData(0) As Byte
		CmdData(0) = 3
		InternalConnection.Send(CmdData)
		Dim RecvData() As Byte = ReceivePacket()
		Dim RecvString As String = Encoding.ASCII.GetString(RecvData)
		Debug.Print(String.Format("[Length={0}] Received on Manual Break: {1}", RecvData.Length, RecvString))
		SendAcknowledgement()
		Dim StatusString As String = RecvString.Substring(1, RecvString.Length - 4)
		Select Case StatusString.Substring(0, 1)
			Case "S"
				Status.Signal = CInt(StatusString.Substring(1, 2))
			Case "T"
				Status.Signal = CInt(StatusString.Substring(1, 2))
			Case Else
				Debug.Print("Unknown Stop-Reply Packet ID!")
		End Select
	End Sub
	''' <summary>
	''' Reads memory from the target.
	''' For QEMU targets, make sure if GDB stub is in physical mode or virtual mode.
	''' </summary>
	''' <param name="Address">The starting address of the memory.</param>
	''' <param name="Length">The length in bytes of the memory.</param>
	''' <returns>The memory contents specified by the range.</returns>
	Public Function ReadMemory(ByVal Address As Long, ByVal Length As Integer) As Byte()
		Dim CmdData() As Byte = MakePacket(String.Format("m {0:x16},{1:x}", Address, Length))
		InternalConnection.Send(CmdData)
RetryReceive:
		Dim RecvData() As Byte = ReceivePacket()
		Dim RecvString As String = Encoding.ASCII.GetString(RecvData)
		Debug.Print(String.Format("[Length={0}] Received on Read-Mem: {1}", RecvData.Length, RecvString))
		If RecvString = "+" Then GoTo RetryReceive
		SendAcknowledgement()
		Dim MemoryHexData As String = RecvString.Substring(1, RecvData.Length - 4)
		Dim MemoryData(MemoryHexData.Length / 2 - 1) As Byte
		Dim i As Integer
		For i = 0 To MemoryHexData.Length - 1 Step 2
			MemoryData(i / 2) = Convert.ToByte(MemoryHexData.Substring(i, 2), 16)
		Next
		Return MemoryData
	End Function
End Class