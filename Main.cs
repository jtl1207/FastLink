using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Runtime.InteropServices;


namespace FastLink
{
    public partial class Form : System.Windows.Forms.Form
    {
        public Form()
        {
            InitializeComponent();
        }
        bool beginMove = false;
        int currentXPosition;
        int currentYPosition;
        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                beginMove = true;
                currentXPosition = MousePosition.X;
                currentYPosition = MousePosition.Y;
            }
        }
        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (beginMove)
            {
                this.Left += MousePosition.X - currentXPosition;
                this.Top += MousePosition.Y - currentYPosition;
                currentXPosition = MousePosition.X;
                currentYPosition = MousePosition.Y;
            }
        }
        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                currentXPosition = 0;
                currentYPosition = 0;
                beginMove = false;
            }
        }
       
        private void uiButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void uiButton2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }
        private void uiRadioButton1_CheckedChanged(object sender, EventArgs e)
        {
            label4.Text = "符号链接: 类似快捷方式,可移动";
        }
        
        private void uiRadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            label4.Text = "目录链接: 类似指针,链接全部子文件";
        }

        private void uiRadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            label4.Text = "硬链接: 镜像,消耗相同空间,为同一文件";
        }

        private void uiButton3_Click(object sender, EventArgs e)
        {
            var dlg = new FolderPicker();
            dlg.InputPath = textBox1.Text;
            if (dlg.ShowDialog(IntPtr.Zero) == true)
            {
                textBox1.Text= dlg.ResultPath;
            }
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            string path = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();       
            textBox2.Text = path;
        }
        private void textBox2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;      
            else
                e.Effect = DragDropEffects.None;
        }
        
        private void uiButton4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dilog = new FolderBrowserDialog();
            dilog.SelectedPath = textBox2.Text;
            dilog.Description = "请选择链接位置";
            if (dilog.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = dilog.SelectedPath;
            }
        }
        [DllImport("user32.dll", EntryPoint = "HideCaret")]
        public static extern bool HideCaret(IntPtr hWnd);
        private void HideCaret(object sender, MouseEventArgs e)
        {
            HideCaret(((TextBox)sender).Handle);
        }
    }
}
