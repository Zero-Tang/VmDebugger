<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
	Inherits System.Windows.Forms.Form

	'Form overrides dispose to clean up the component list.
	<System.Diagnostics.DebuggerNonUserCode()>
	Protected Overrides Sub Dispose(ByVal disposing As Boolean)
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
		StatusStrip1 = New StatusStrip()
		MenuStrip1 = New MenuStrip()
		ToolStripMenuItem1 = New ToolStripMenuItem()
		ToolStripMenuItem2 = New ToolStripMenuItem()
		ToolStripSeparator1 = New ToolStripSeparator()
		ToolStripMenuItem3 = New ToolStripMenuItem()
		ToolStripMenuItem4 = New ToolStripMenuItem()
		TabControl1 = New TabControl()
		TabPage1 = New TabPage()
		TextBox2 = New TextBox()
		TextBox1 = New TextBox()
		TabPage2 = New TabPage()
		MenuStrip1.SuspendLayout()
		TabControl1.SuspendLayout()
		TabPage1.SuspendLayout()
		SuspendLayout()
		' 
		' StatusStrip1
		' 
		StatusStrip1.Location = New Point(0, 739)
		StatusStrip1.Name = "StatusStrip1"
		StatusStrip1.Size = New Size(1264, 22)
		StatusStrip1.TabIndex = 0
		StatusStrip1.Text = "StatusStrip1"
		' 
		' MenuStrip1
		' 
		MenuStrip1.Items.AddRange(New ToolStripItem() {ToolStripMenuItem1})
		MenuStrip1.Location = New Point(0, 0)
		MenuStrip1.Name = "MenuStrip1"
		MenuStrip1.Size = New Size(1264, 24)
		MenuStrip1.TabIndex = 1
		MenuStrip1.Text = "MenuStrip1"
		' 
		' ToolStripMenuItem1
		' 
		ToolStripMenuItem1.DropDownItems.AddRange(New ToolStripItem() {ToolStripMenuItem2, ToolStripSeparator1, ToolStripMenuItem3, ToolStripMenuItem4})
		ToolStripMenuItem1.Name = "ToolStripMenuItem1"
		ToolStripMenuItem1.Size = New Size(37, 20)
		ToolStripMenuItem1.Text = "File"
		' 
		' ToolStripMenuItem2
		' 
		ToolStripMenuItem2.Name = "ToolStripMenuItem2"
		ToolStripMenuItem2.Size = New Size(140, 22)
		ToolStripMenuItem2.Text = "New Session"
		' 
		' ToolStripSeparator1
		' 
		ToolStripSeparator1.Name = "ToolStripSeparator1"
		ToolStripSeparator1.Size = New Size(137, 6)
		' 
		' ToolStripMenuItem3
		' 
		ToolStripMenuItem3.Name = "ToolStripMenuItem3"
		ToolStripMenuItem3.Size = New Size(140, 22)
		ToolStripMenuItem3.Text = "Go"
		' 
		' ToolStripMenuItem4
		' 
		ToolStripMenuItem4.Name = "ToolStripMenuItem4"
		ToolStripMenuItem4.Size = New Size(140, 22)
		ToolStripMenuItem4.Text = "Break"
		' 
		' TabControl1
		' 
		TabControl1.Controls.Add(TabPage1)
		TabControl1.Controls.Add(TabPage2)
		TabControl1.Location = New Point(12, 27)
		TabControl1.Name = "TabControl1"
		TabControl1.SelectedIndex = 0
		TabControl1.Size = New Size(1240, 709)
		TabControl1.TabIndex = 2
		' 
		' TabPage1
		' 
		TabPage1.Controls.Add(TextBox2)
		TabPage1.Controls.Add(TextBox1)
		TabPage1.Location = New Point(4, 24)
		TabPage1.Name = "TabPage1"
		TabPage1.Padding = New Padding(3)
		TabPage1.Size = New Size(1232, 681)
		TabPage1.TabIndex = 0
		TabPage1.Text = "Command"
		TabPage1.UseVisualStyleBackColor = True
		' 
		' TextBox2
		' 
		TextBox2.Enabled = False
		TextBox2.Location = New Point(6, 652)
		TextBox2.Name = "TextBox2"
		TextBox2.Size = New Size(1220, 23)
		TextBox2.TabIndex = 1
		' 
		' TextBox1
		' 
		TextBox1.BackColor = Color.White
		TextBox1.Font = New Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
		TextBox1.Location = New Point(6, 6)
		TextBox1.Multiline = True
		TextBox1.Name = "TextBox1"
		TextBox1.ReadOnly = True
		TextBox1.Size = New Size(1220, 640)
		TextBox1.TabIndex = 0
		' 
		' TabPage2
		' 
		TabPage2.Location = New Point(4, 24)
		TabPage2.Name = "TabPage2"
		TabPage2.Padding = New Padding(3)
		TabPage2.Size = New Size(1232, 681)
		TabPage2.TabIndex = 1
		TabPage2.Text = "TabPage2"
		TabPage2.UseVisualStyleBackColor = True
		' 
		' Form1
		' 
		AutoScaleDimensions = New SizeF(7F, 15F)
		AutoScaleMode = AutoScaleMode.Font
		ClientSize = New Size(1264, 761)
		Controls.Add(TabControl1)
		Controls.Add(StatusStrip1)
		Controls.Add(MenuStrip1)
		KeyPreview = True
		MainMenuStrip = MenuStrip1
		Name = "Form1"
		StartPosition = FormStartPosition.WindowsDefaultBounds
		Text = "VmDebugger - Powered by Zero Tang"
		MenuStrip1.ResumeLayout(False)
		MenuStrip1.PerformLayout()
		TabControl1.ResumeLayout(False)
		TabPage1.ResumeLayout(False)
		TabPage1.PerformLayout()
		ResumeLayout(False)
		PerformLayout()
	End Sub

	Friend WithEvents StatusStrip1 As StatusStrip
	Friend WithEvents MenuStrip1 As MenuStrip
	Friend WithEvents TabControl1 As TabControl
	Friend WithEvents TabPage1 As TabPage
	Friend WithEvents TabPage2 As TabPage
	Friend WithEvents ToolStripMenuItem1 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem2 As ToolStripMenuItem
	Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
	Friend WithEvents ToolStripMenuItem3 As ToolStripMenuItem
	Friend WithEvents ToolStripMenuItem4 As ToolStripMenuItem
	Friend WithEvents TextBox1 As TextBox
	Friend WithEvents TextBox2 As TextBox

End Class
