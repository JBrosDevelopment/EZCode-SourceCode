﻿namespace EZ_IDE
{
    partial class Settings_Preferences
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabPage3 = new TabPage();
            HighLight_Line = new CheckBox();
            Pause_Debug = new CheckBox();
            label4 = new Label();
            Breakpoint_button = new Button();
            tabPage1 = new TabPage();
            button1 = new Button();
            Save_On_Play = new CheckBox();
            label3 = new Label();
            DefaultZoom = new NumericUpDown();
            current_project = new TextBox();
            label2 = new Label();
            Auto_Save = new CheckBox();
            label1 = new Label();
            Save_Folder = new CheckBox();
            tabControl1 = new TabControl();
            tabPage3.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)DefaultZoom).BeginInit();
            tabControl1.SuspendLayout();
            SuspendLayout();
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(HighLight_Line);
            tabPage3.Controls.Add(Pause_Debug);
            tabPage3.Controls.Add(label4);
            tabPage3.Controls.Add(Breakpoint_button);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(518, 391);
            tabPage3.TabIndex = 3;
            tabPage3.Text = "Debug";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // HighLight_Line
            // 
            HighLight_Line.AutoSize = true;
            HighLight_Line.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            HighLight_Line.Location = new Point(11, 114);
            HighLight_Line.Name = "HighLight_Line";
            HighLight_Line.Size = new Size(325, 24);
            HighLight_Line.TabIndex = 5;
            HighLight_Line.Text = "Highlight Current Line (Not Always Correct)";
            HighLight_Line.UseVisualStyleBackColor = true;
            HighLight_Line.CheckedChanged += checkBox1_CheckedChanged_2;
            // 
            // Pause_Debug
            // 
            Pause_Debug.AutoSize = true;
            Pause_Debug.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            Pause_Debug.Location = new Point(11, 84);
            Pause_Debug.Name = "Pause_Debug";
            Pause_Debug.Size = new Size(362, 24);
            Pause_Debug.TabIndex = 4;
            Pause_Debug.Text = "Allow Pausing Debug Session with Next Segment";
            Pause_Debug.UseVisualStyleBackColor = true;
            Pause_Debug.CheckedChanged += Pause_Debug_CheckedChanged;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            label4.Location = new Point(11, 8);
            label4.Name = "label4";
            label4.Size = new Size(164, 30);
            label4.TabIndex = 3;
            label4.Text = "Debug Settings";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // Breakpoint_button
            // 
            Breakpoint_button.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            Breakpoint_button.Location = new Point(10, 50);
            Breakpoint_button.Name = "Breakpoint_button";
            Breakpoint_button.Size = new Size(499, 28);
            Breakpoint_button.TabIndex = 0;
            Breakpoint_button.Text = "Breakpoints";
            Breakpoint_button.UseVisualStyleBackColor = true;
            Breakpoint_button.Click += button1_Click;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(button1);
            tabPage1.Controls.Add(Save_On_Play);
            tabPage1.Controls.Add(label3);
            tabPage1.Controls.Add(DefaultZoom);
            tabPage1.Controls.Add(current_project);
            tabPage1.Controls.Add(label2);
            tabPage1.Controls.Add(Auto_Save);
            tabPage1.Controls.Add(label1);
            tabPage1.Controls.Add(Save_Folder);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(518, 391);
            tabPage1.TabIndex = 2;
            tabPage1.Text = "Settings";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.AutoSize = true;
            button1.Font = new Font("Segoe UI Semibold", 10.25F, FontStyle.Bold, GraphicsUnit.Point);
            button1.Location = new Point(11, 41);
            button1.Name = "button1";
            button1.Size = new Size(113, 29);
            button1.TabIndex = 9;
            button1.Text = "Reset IDE";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click_1;
            // 
            // Save_On_Play
            // 
            Save_On_Play.AutoSize = true;
            Save_On_Play.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            Save_On_Play.Location = new Point(11, 209);
            Save_On_Play.Name = "Save_On_Play";
            Save_On_Play.Size = new Size(219, 24);
            Save_On_Play.TabIndex = 8;
            Save_On_Play.Text = "Save File When Start Paying";
            Save_On_Play.UseVisualStyleBackColor = true;
            Save_On_Play.CheckedChanged += Save_On_Play_CheckedChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            label3.Location = new Point(11, 175);
            label3.Name = "label3";
            label3.Size = new Size(107, 20);
            label3.TabIndex = 7;
            label3.Text = "Default Zoom:";
            // 
            // DefaultZoom
            // 
            DefaultZoom.Font = new Font("Segoe UI Semibold", 10.75F, FontStyle.Bold, GraphicsUnit.Point);
            DefaultZoom.Location = new Point(124, 173);
            DefaultZoom.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            DefaultZoom.Name = "DefaultZoom";
            DefaultZoom.Size = new Size(386, 27);
            DefaultZoom.TabIndex = 6;
            DefaultZoom.Value = new decimal(new int[] { 100, 0, 0, 0 });
            DefaultZoom.ValueChanged += numericUpDown1_ValueChanged;
            // 
            // current_project
            // 
            current_project.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            current_project.Location = new Point(134, 134);
            current_project.Name = "current_project";
            current_project.Size = new Size(376, 27);
            current_project.TabIndex = 5;
            current_project.TextChanged += current_project_TextChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            label2.Location = new Point(11, 137);
            label2.Name = "label2";
            label2.Size = new Size(117, 20);
            label2.TabIndex = 4;
            label2.Text = "Current Project:";
            // 
            // Auto_Save
            // 
            Auto_Save.AutoSize = true;
            Auto_Save.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            Auto_Save.Location = new Point(11, 106);
            Auto_Save.Name = "Auto_Save";
            Auto_Save.Size = new Size(125, 24);
            Auto_Save.TabIndex = 3;
            Auto_Save.Text = "Auto Save File";
            Auto_Save.UseVisualStyleBackColor = true;
            Auto_Save.CheckedChanged += checkBox1_CheckedChanged_1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 15.75F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Location = new Point(11, 8);
            label1.Name = "label1";
            label1.Size = new Size(132, 30);
            label1.TabIndex = 2;
            label1.Text = "IDE Settings";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // Save_Folder
            // 
            Save_Folder.AutoSize = true;
            Save_Folder.Font = new Font("Segoe UI Semibold", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            Save_Folder.Location = new Point(11, 76);
            Save_Folder.Name = "Save_Folder";
            Save_Folder.Size = new Size(286, 24);
            Save_Folder.TabIndex = 1;
            Save_Folder.Text = "Save Tree View Folder Across Startups";
            Save_Folder.UseVisualStyleBackColor = true;
            Save_Folder.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(526, 419);
            tabControl1.TabIndex = 0;
            // 
            // Settings_Preferences
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(526, 419);
            Controls.Add(tabControl1);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MinimumSize = new Size(542, 456);
            Name = "Settings_Preferences";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Settings";
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)DefaultZoom).EndInit();
            tabControl1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TabPage tabPage3;
        private TabPage tabPage1;
        private CheckBox Save_On_Play;
        private Label label3;
        private NumericUpDown DefaultZoom;
        private TextBox current_project;
        private Label label2;
        private CheckBox Auto_Save;
        private Label label1;
        private CheckBox Save_Folder;
        private TabControl tabControl1;
        private Button Breakpoint_button;
        private Label label4;
        private CheckBox Pause_Debug;
        private CheckBox HighLight_Line;
        private Button button1;
    }
}