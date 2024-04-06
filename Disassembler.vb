' Disassembler Library from wrapping zydis.
Imports System.Runtime.InteropServices
Imports System.Text
Public Class Disassembler
	Public Enum ZydisMachineMode As Integer
		ZYDIS_MACHINE_MODE_LONG_64
		ZYDIS_MACHINE_MODE_LONG_COMPAT_32
		ZYDIS_MACHINE_MDOE_LONG_COMPAT_16
		ZYDIS_MACHINE_MODE_LEGACY_32
		ZYDIS_MACHINE_MODE_LEGACY_16
		ZYDIS_MACHINE_MODE_REAL_16
	End Enum
	Public Enum ZydisStackWidth As Integer
		ZYDIS_STACK_WIDTH_16
		ZYDIS_STACK_WIDTH_32
		ZYDIS_STACK_WIDTH_64
	End Enum
	Public Enum ZydisFormatterStyle As Integer
		ZYDIS_FORMATTER_STYLE_ATT
		ZYDIS_FORMATTER_STYLE_INTEL
		ZYDIS_FORMATTER_STYLE_INTEL_MASM
	End Enum
	Public Enum ZydisFormatterFunction As Integer
		ZYDIS_FORMATTER_FUNC_PRE_INSTRUCTION
		ZYDIS_FORMATTER_FUNC_POST_INSTRUCTION
		ZYDIS_FORMATTER_FUNC_FORMAT_INSTRUCTION
		ZYDIS_FORMATTER_FUNC_PRE_OPERAND
		ZYDIS_FORMATTER_FUNC_POST_OPERAND
		ZYDIS_FORMATTER_FUNC_FORMAT_OPERAND_REG
		ZYDIS_FORMATTER_FUNC_FORMAT_OPERAND_MEM
		ZYDIS_FORMATTER_FUNC_FORMAT_OPERAND_PTR
		ZYDIS_FORMATTER_FUNC_FORMAT_OPERAND_IMM
		ZYDIS_FORMATTER_FUNC_PRINT_MNEMONIC
		ZYDIS_FORMATTER_FUNC_PRINT_REGISTER
		ZYDIS_FORMATTER_FUNC_PRINT_ADDRESS_ABS
		ZYDIS_FORMATTER_FUNC_PRINT_ADDRESS_REL
		ZYDIS_FORMATTER_FUNC_PRINT_DISP
		ZYDIS_FORMATTER_FUNC_PRINT_IMM
		ZYDIS_FORMATTER_FUNC_PRINT_TYPECAST
		ZYDIS_FORMATTER_FUNC_PRINT_SEGMENT
		ZYDIS_FORMATTER_FUNC_PRINT_PREFIXES
		ZYDIS_FORMATTER_FUNC_PRINT_DECORATOR
	End Enum
	' It seems only unmanaged buffer is safe to pass structures to APIs.
	Private Declare Function ZydisDecoderInit Lib "zydis.dll" (ByVal Decoder As IntPtr, ByVal MachineMode As ZydisMachineMode, ByVal StackWidth As ZydisStackWidth) As Integer
	Private Declare Function ZydisFormatterInit Lib "zydis.dll" (ByVal Formatter As IntPtr, ByVal FormatterStyle As ZydisFormatterStyle) As Integer
	Private Declare Function ZydisDecoderDecodeFull Lib "zydis.dll" (ByVal Decoder As IntPtr, ByVal Buffer As IntPtr, ByVal Length As Integer, ByVal DecodedInstruction As IntPtr, ByVal Operands As IntPtr) As Integer
	Private Declare Function ZydisFormatterFormatInstruction Lib "zydis.dll" (ByVal Formatter As IntPtr, ByVal DecodedInstruction As IntPtr, ByVal DecodedOperands As IntPtr, ByVal OperandCount As Byte, ByVal MnemonicBuffer As IntPtr, ByVal MnemonicLength As Integer, ByVal RuntimeAddress As Long, ByVal UserData As IntPtr) As Integer
	Dim Decoder16 As IntPtr = Marshal.AllocHGlobal(24), Decoder32 As IntPtr = Marshal.AllocHGlobal(24), Decoder64 As IntPtr = Marshal.AllocHGlobal(24)
	Dim FormatterBuffer As IntPtr = Marshal.AllocHGlobal(1024)   ' Structure is too complex, so I don't want to define the structure here. 1024 bytes should be good enough.
	''' <summary>
	''' Instantiates a full-fledged disassembler
	''' </summary>
	Sub New()
		Debug.Print("Initializing 16-bit decoder...")
		ZydisDecoderInit(Decoder16, ZydisMachineMode.ZYDIS_MACHINE_MODE_REAL_16, ZydisStackWidth.ZYDIS_STACK_WIDTH_16)
		ZydisDecoderInit(Decoder32, ZydisMachineMode.ZYDIS_MACHINE_MODE_LEGACY_32, ZydisStackWidth.ZYDIS_STACK_WIDTH_32)
		ZydisDecoderInit(Decoder64, ZydisMachineMode.ZYDIS_MACHINE_MODE_LONG_64, ZydisStackWidth.ZYDIS_STACK_WIDTH_64)
		ZydisFormatterInit(FormatterBuffer, ZydisFormatterStyle.ZYDIS_FORMATTER_STYLE_INTEL_MASM)
		Debug.Print("Disassembler initialized!")
	End Sub
	Public Function Decode64(ByVal Buffer() As Byte) As Instruction
		Dim RawIns As IntPtr = Marshal.AllocHGlobal(15)
		Dim InsBuff As IntPtr = Marshal.AllocHGlobal(&H300)
		Dim OpBuff As IntPtr = Marshal.AllocHGlobal(&H380)
		Marshal.Copy(Buffer, 0, RawIns, 15)
		Dim ZyanResult As Integer = ZydisDecoderDecodeFull(Decoder64, RawIns, 15, InsBuff, OpBuff)
		Marshal.FreeHGlobal(RawIns)
		Return New Instruction(InsBuff, OpBuff)
	End Function
	Public Function Format64(ByVal SourceInstruction As Instruction, ByVal RuntimeAddress As Long) As String
		Dim MnemonicBuffer As IntPtr = Marshal.AllocHGlobal(128)
		ZydisFormatterFormatInstruction(FormatterBuffer, SourceInstruction.RawInsBuff, SourceInstruction.RawOpBuff, 10, MnemonicBuffer, 128, RuntimeAddress, IntPtr.Zero)
		Dim MnemonicText As String = Marshal.PtrToStringAnsi(MnemonicBuffer)
		Debug.Print("Disassembled instruction: {0}", MnemonicText)
		Marshal.FreeHGlobal(MnemonicBuffer)
		Return MnemonicText
	End Function
End Class
Public Class Instruction
	Public MachineMode As Disassembler.ZydisMachineMode
	Public Length As Byte
	Public Opcode As Byte
	Public NumberOfVisibleOperands As Byte
	Public NumberOfOperands As Byte
	Public OperandWidth As Byte
	Public AddressWidth As Byte
	Public StackWidth As Byte
	Public Operands As New List(Of Operand)
	Public RawInsBuff As IntPtr, RawOpBuff As IntPtr
	Sub New(ByVal DecodedInstruction As IntPtr, ByVal DecodedOperands As IntPtr)
		MachineMode = Marshal.ReadInt32(DecodedInstruction, &H0)
		Length = Marshal.ReadInt32(DecodedInstruction, &H8)
		Opcode = Marshal.ReadByte(DecodedInstruction, &H14)
		StackWidth = Marshal.ReadByte(DecodedInstruction, &H15)
		OperandWidth = Marshal.ReadByte(DecodedInstruction, &H16)
		AddressWidth = Marshal.ReadByte(DecodedInstruction, &H17)
		NumberOfOperands = Marshal.ReadByte(DecodedInstruction, &H18)
		NumberOfVisibleOperands = Marshal.ReadByte(DecodedInstruction, &H19)
		Dim i As Integer
		For i = 0 To NumberOfOperands - 1 Step 1
			Dim SubBuff(&H37) As Byte
			Marshal.Copy(DecodedOperands + i * &H38, SubBuff, 0, &H38)
			Operands.Add(New Operand(SubBuff))
		Next
		RawInsBuff = DecodedInstruction
		RawOpBuff = DecodedOperands
	End Sub
End Class
Public Class Operand
	Public Index As Byte
	Public Visibility As Boolean
	Public Actions As Byte
	Public Size As Short

	Sub New(ByVal Buffer As Byte())
		Index = Buffer(&H0)
		Actions = Buffer(&H8)
		Size = BitConverter.ToInt16(Buffer, &H10)

	End Sub
End Class