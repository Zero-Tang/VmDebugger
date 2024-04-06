Imports System.Text

Public Class PEImage
	' May add more definitions about PE Images.
	Private Const IMAGE_DOS_SIGNATURE As Short = &H5A4D
	Private Const IMAGE_NT_SIGNATURE As Short = &H4550

	Private Const IMAGE_NT_OPTIONAL_HDR32_MAGIC As Short = &H10B
	Private Const IMAGE_NT_OPTIONAL_HDR64_MAGIC As Short = &H20B

	Private Const IMAGE_DIRECTORY_ENTRY_EXPORT As Integer = 0
	Private Const IMAGE_DIRECTORY_ENTRY_IMPORT As Integer = 1
	Private Const IMAGE_DIRECTORY_ENTRY_RESOURCE As Integer = 2
	Private Const IMAGE_DIRECTORY_ENTRY_EXCEPTION As Integer = 3
	Private Const IMAGE_DIRECTORY_ENTRY_SECURITY As Integer = 4
	Private Const IMAGE_DIRECTORY_ENTRY_BASERELOC As Integer = 5
	Private Const IMAGE_DIRECTORY_ENTRY_DEBUG As Integer = 6
	Private Const IMAGE_DIRECTORY_ENTRY_ARCHITECTURE As Integer = 7
	Private Const IMAGE_DIRECTORY_ENTRY_GLOBALPTR As Integer = 8
	Private Const IMAGE_DIRECTORY_ENTRY_TLS As Integer = 9
	Private Const IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG As Integer = 10
	Private Const IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT As Integer = 11
	Private Const IMAGE_DIRECTORY_ENTRY_IAT As Integer = 12
	Private Const IMAGE_DIRECTORY_ENTRY_DELAY_IMPORT As Integer = 13
	Private Const IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR As Integer = 14
	'
	Dim InternalSession As GdbSession
	Public ImageBase As Long
	Public ImageSize As Long
	Public CvGuid As Guid
	Public CvGuidString As String
	Public CvAge As Integer
	Public CvFileName As String
	Sub New(ByVal Session As GdbSession, ByVal Address As Long, ByVal Length As Integer)
		InternalSession = Session
		ImageBase = Address
		ImageSize = Length
		' DOS Header
		Dim DosSig As Short = BitConverter.ToInt16(InternalSession.ReadMemory(ImageBase, 2), 0)
		If DosSig <> IMAGE_DOS_SIGNATURE Then Throw New Exception("DOS Signature is invalid!")
		' NT Header
		Dim e_lfanew As UShort = BitConverter.ToUInt16(InternalSession.ReadMemory(ImageBase + &H3C, 2), 0)
		Dim NtSig As Short = BitConverter.ToInt16(InternalSession.ReadMemory(ImageBase + e_lfanew, 2), 0)
		If NtSig <> IMAGE_NT_SIGNATURE Then Throw New Exception("NT Signature is invalid!")
		' Optional Header
		Dim OptSize As Short = BitConverter.ToInt16(InternalSession.ReadMemory(ImageBase + e_lfanew + &H14, 2), 0)
		Dim OptMagic As Short = BitConverter.ToInt16(InternalSession.ReadMemory(ImageBase + e_lfanew + &H18, 2), 0)
		' Debug Directory
		If OptMagic = IMAGE_NT_OPTIONAL_HDR64_MAGIC Then
			Dim DebugDirPtr As Long = ImageBase + e_lfanew + &H88 + IMAGE_DIRECTORY_ENTRY_DEBUG * 8
			Dim DebugDirBase As Long = ImageBase + BitConverter.ToInt32(InternalSession.ReadMemory(DebugDirPtr, 4), 0)
			Dim DebugDirLen As Integer = BitConverter.ToInt32(InternalSession.ReadMemory(DebugDirPtr + 4, 4), 0)
			Debug.Print("Debug Directory: 0x{0:X16}, Length: {1}", DebugDirBase, DebugDirLen)
			Dim DebugInfoType As Integer = BitConverter.ToInt32(InternalSession.ReadMemory(DebugDirBase + &HC, 4), 0)
			If DebugInfoType <> 2 Then Throw New Exception("Only CodeView Debug Information is supported!")
			Dim CvBase As Long = ImageBase + BitConverter.ToInt32(InternalSession.ReadMemory(DebugDirBase + &H14, 4), 0)
			Dim CvSize As Integer = BitConverter.ToInt32(InternalSession.ReadMemory(DebugDirBase + &H10, 4), 0)
			' CodeView Information
			Dim CvInfoRaw() As Byte = InternalSession.ReadMemory(CvBase, CvSize)
			Dim CvSig As String = Encoding.ASCII.GetString(CvInfoRaw, 0, 4)
			If CvSig <> "RSDS" Then Throw New Exception(String.Format("Invalid CodeView Signature: {0}!", CvSig))
			' Required Information for downloading symbols from Microsoft Server.
			Dim CvGuidRaw(15) As Byte
			Array.Copy(CvInfoRaw, 4, CvGuidRaw, 0, 16)
			CvGuid = New Guid(CvGuidRaw)
			CvAge = BitConverter.ToInt32(CvInfoRaw, &H14)
			CvFileName = Encoding.ASCII.GetString(CvInfoRaw, &H18, CvSize - &H19)
			Debug.Print("GUID: {0}, Age: {1}, File Name: {2}", CvGuid, CvAge, CvFileName)
			' Format the GUID string used for forging URLs.
			Dim CvGuid1 As Integer = BitConverter.ToInt32(CvGuidRaw, 0)
			Dim CvGuid2 As Short = BitConverter.ToInt16(CvGuidRaw, 4)
			Dim CvGuid3 As Short = BitConverter.ToInt16(CvGuidRaw, 6)
			CvGuidString = String.Format("{0:X8}{1:X4}{2:X4}", CvGuid1, CvGuid2, CvGuid3)
			Dim i As Integer
			For i = 8 To 15 Step 1
				CvGuidString += String.Format("{0:X2}", CvGuidRaw(i))
			Next
			CvGuidString += String.Format("{0:X}", CvAge)
			InternalSession.KnownModules.Add(Me)
		Else
			Throw New Exception(String.Format("Unknown Optional Header Magic: 0x{0:X}!", OptMagic))
		End If
	End Sub
End Class
