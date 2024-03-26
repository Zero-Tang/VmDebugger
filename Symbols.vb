Imports System.ComponentModel
Imports System.IO
Imports System.Net
Imports System.Runtime.InteropServices

Public Class SymbolManager
	Private Declare Unicode Function SymInitializeW Lib "dbghelp.dll" (ByVal ProcessHandle As IntPtr, ByVal UserSearchPath As String, ByVal InvadeProcess As Boolean) As Boolean
	Private Declare Function GetCurrentProcess Lib "kernel32.dll" () As IntPtr
	Public SymbolServers As List(Of String)
	Public LocalStorage As String
	Dim WebC As New WebClient
	''' <summary>
	''' 
	''' </summary>
	''' <param name="LocalDirectory"></param>
	''' <param name="RemoteUrls"></param>
	Sub New(ByVal LocalDirectory As String, ByVal RemoteUrls As List(Of String))
		SymbolServers = New List(Of String)(RemoteUrls)
		' Always add the default server if there is nothing in the list.
		If SymbolServers.Count = 0 Then SymbolServers.Add("http://msdl.microsoft.com/download/symbols")
		Dim SearchPath As String = String.Format("SRV*{0}*", LocalDirectory)
		For Each SymSrv In SymbolServers
			SearchPath += SymSrv + ";"
		Next
		SearchPath = SearchPath.Substring(0, SearchPath.Length - 1)
		Debug.Print("Symbol Search Path: " + SearchPath)
		If Not SymInitializeW(GetCurrentProcess(), SearchPath, False) Then
			Dim ErrCode As Integer = Err.LastDllError
			Throw New Win32Exception(ErrCode)
		End If
	End Sub
	''' <summary>
	''' Download a symbol from servers.
	''' This is especially useful for remote debuggings without original copy of executables.
	''' </summary>
	''' <param name="SymbolName">The file name with ".pdb" suffix.</param>
	''' <param name="SymbolGuid">The GUID of symbol in order to identify the correct copy of symbol.</param>
	Public Sub DownloadSymbol(ByVal SymbolName As String, ByVal SymbolGuid As Guid)
		Dim LocalFile As String = String.Format("{0}\{1}\{2}\{1}", LocalStorage, SymbolName, SymbolGuid, SymbolName)
		For Each SymSrv In SymbolServers
			Dim Url As String = String.Format("{0}/{1}/{2}/{1}", SymSrv, SymbolName, SymbolGuid, SymbolName)
			Debug.Print("Trying to download from {0}...", Url)
			Try
				WebC.DownloadFile(Url, LocalFile)
				Exit Sub
			Catch Ex As WebException
				Debug.Print("Failed to download from {0}!", Url)
			End Try
		Next
		' If no hit, throw an exception.
		Throw New WebException(String.Format("Failed to download symbol file {0} with GUID of {1}!", SymbolName, SymbolGuid))
	End Sub
End Class

Public Class SymbolModule
	Private Declare Unicode Function SymLoadModuleExW Lib "dbghelp.dll" (ByVal ProcessHandle As IntPtr, ByVal FileHandle As IntPtr, ByVal ImageName As String, ByVal ModuleName As String, ByVal BaseOfDll As Long, ByVal DllSize As Integer, ByVal Data As IntPtr, ByVal Flags As Integer) As Long
	Private Declare Unicode Function SymGetModuleInfoW64 Lib "dbghelp.dll" (ByVal ProcessHandle As IntPtr, ByVal Address As Long, ByVal ModuleInfo As IntPtr) As Boolean
	Private Declare Function GetCurrentProcess Lib "kernel32.dll" () As IntPtr
	Private Const IMAGEHLP_MODULEW64_SIZE As Integer = 2000
	Dim Manager As SymbolManager
	''' <summary>
	''' Initializes a Module for navigating symbols.
	''' </summary>
	''' <param name="Parent">Specifies the SymbolManager instance.</param>
	''' <param name="ImageName">Specifies the image name in short.</param>
	''' <param name="ModulePath">Specifies the file to PDB or executable.</param>
	''' <param name="Address">Specifies the memory address in the target.</param>
	''' <param name="Length">Specifies the length of image in the target.</param>
	Sub New(ByVal Parent As SymbolManager, ByVal ImageName As String, ByVal ModulePath As String, ByVal Address As Long, ByVal Length As Integer)
		Manager = Parent
		Dim ModBase As Long = SymLoadModuleExW(GetCurrentProcess(), IntPtr.Zero, ImageName, ModulePath, Address, Length, IntPtr.Zero, 0)
		If ModBase = 0 Then
			Dim ErrCode As Integer = Err.LastDllError
			Throw New Win32Exception(ErrCode, "Failed to execute SymLoadModuleExW!")
		End If
		Dim ImgInfo As IntPtr = Marshal.AllocHGlobal(IMAGEHLP_MODULEW64_SIZE)
		Marshal.WriteInt32(ImgInfo, IMAGEHLP_MODULEW64_SIZE)
		If Not SymGetModuleInfoW64(GetCurrentProcess(), Address, ImgInfo) Then
			Dim ErrCode As Int128 = Err.LastDllError
			Marshal.FreeHGlobal(ImgInfo)        ' Release this structure since we failed.
			Throw New Win32Exception(ErrCode, "Failed to execute SymGetModuleInfoW64!")
		End If
		Dim fs As New FileStream("imagehlp_modulew64.bin", FileMode.Create, FileAccess.Write, FileShare.Read)
		Dim ImgInfoBuff(IMAGEHLP_MODULEW64_SIZE - 1) As Byte
		Marshal.Copy(ImgInfo, ImgInfoBuff, 0, IMAGEHLP_MODULEW64_SIZE)
		fs.Write(ImgInfoBuff, 0, IMAGEHLP_MODULEW64_SIZE)
		fs.Close()
	End Sub
End Class