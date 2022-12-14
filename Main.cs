using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using TextBox = System.Windows.Forms.TextBox;

namespace FastLink
{
    public partial class Form : System.Windows.Forms.Form
    {
        public Form()
        {
            InitializeComponent();

            ElevatedDragDropManager filter = new ElevatedDragDropManager();
            filter.EnableDragDrop(this.textBox1.Handle);
            filter.EnableDragDrop(this.textBox2.Handle);
            Application.AddMessageFilter(filter);
            filter.ElevatedDragDrop += this.ElevatedDragDrop;
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
        private void ElevatedDragDrop(System.Object sender, ElevatedDragDropArgs e)
        {
            try
            {
                if (e.HWnd == this.textBox1.Handle)
                {
                    foreach (string file in e.Files)
                    {
                        textBox1.Text = file;
                    }
                }
                if (e.HWnd == this.textBox2.Handle)
                {
                    foreach (string file in e.Files)
                    {
                        textBox2.Text = file;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ElevatedDragDrop error=" + (ex.TargetSite?.Name) + "!");
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
            label4.Text = "符号链接: 类似快捷方式,可远程";
        }
        
        private void uiRadioButton2_CheckedChanged(object sender, EventArgs e)
        {
            label4.Text = "目录链接: 类似指针,只能本地";
        }

        private void uiRadioButton3_CheckedChanged(object sender, EventArgs e)
        {
            label4.Text = "硬链接: 镜像,消耗相同空间,同一盘符内,同一文件";
        }

        private void uiButton3_Click(object sender, EventArgs e)
        {
            FolderBrowser fb = new FolderBrowser();
            fb.Description = "请选择源路径";
            fb.IncludeFiles = true;
            fb.InitialDirectory = textBox1.Text;
            if (fb.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = fb.SelectedPath;
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
            {
                e.Effect = DragDropEffects.All;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        
        private void uiButton4_Click(object sender, EventArgs e)
        {
            FolderBrowser fb = new FolderBrowser();
            fb.Description = "选择链接路径";
            //fb.IncludeFiles = true;
            fb.InitialDirectory = textBox2.Text;
            if (fb.ShowDialog() == DialogResult.OK)
            {
                textBox2.Text = fb.SelectedPath;
            }
        }
        [DllImport("user32.dll", EntryPoint = "HideCaret")]
        public static extern bool HideCaret(IntPtr hWnd);
        private void HideCaret(object sender, MouseEventArgs e)
        {
            HideCaret(((TextBox)sender).Handle);
        }

        public static string Exec(string str)
        {
            string output = "";
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.StandardInput.WriteLine(str + "&exit");
                p.StandardInput.AutoFlush = true;
                output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
            }
            catch
            {
                Console.WriteLine("shell权限错误");
            }
            return output;
        }
        private void uiButton5_Click(object sender, EventArgs e)
        {
            string path = "";

            if (uiRadioButton1.Checked | uiRadioButton2.Checked)
            {
                if (!Directory.Exists(textBox1.Text))
                {
                    MessageBox.Show($"D模式源路径必须是正确的目录");
                    return;
                }
            }
            else
            {
                if (!File.Exists(textBox1.Text))
                {
                    MessageBox.Show($"H模式源路径必须是正确的文件");
                    return;
                }
            }   

            if (!Directory.Exists(textBox2.Text))
            {
                MessageBox.Show($"目标路径必须是正确的目录");
                return;
            }

            path = textBox2.Text+ "\\"+textBox1.Text.Split("\\")[textBox1.Text.Split("\\").Length -1];

            if (uiRadioButton1.Checked)
            {
                string cmd = "mklink /d \"" + path+ "\" \"" + textBox1.Text + "\"";
                string[] output = Exec(cmd).Split("\r\n");

                if (output[output.Length - 2].Contains("&exit"))
                {
                    MessageBox.Show("创建失败,请检查文件名是否重复");
                    return;
                }
                MessageBox.Show("创建成功,链接位置  " + path);
            }
            else if (uiRadioButton2.Checked)
            {
                string cmd = "mklink /j \"" + path + "\" \"" + textBox1.Text + "\"";
                string[] output = Exec(cmd).Split("\r\n");
                if (output[output.Length - 2].Contains("&exit"))
                {
                    MessageBox.Show("创建失败,请检查文件名是否重复");
                    return;
                }
                MessageBox.Show("创建成功,链接位置  " + path);
            }
            else if (uiRadioButton3.Checked)
            {
                string cmd = "mklink /h \"" + path + "\" \"" + textBox1.Text + "\"";
                string[] output = Exec(cmd).Split("\r\n");
                if (output[output.Length - 2].Contains("&exit"))
                {
                    MessageBox.Show("创建失败,请检查文件名是否重复");
                    return;
                }
                MessageBox.Show("创建成功,链接位置  " + path);
            }
        }
    }
}
