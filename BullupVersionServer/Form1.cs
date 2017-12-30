using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TCPLib;

namespace BullupVersionServer {
    public partial class Form1 : Form {
        public Form1() {
            InitializeComponent();
        }

        private String bullupPath = "";
        private String autoprogramPath = "";

        private void Form1_Load(object sender, EventArgs e) {

        }

        private void button2_Click(object sender, EventArgs e) {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowDialog();
            autoprogramPath = folderDlg.SelectedPath;
            if (server != null) {
                server.autoprogramPath = autoprogramPath;
            }
            textBox2.Text = autoprogramPath;
        }

        private void button1_Click(object sender, EventArgs e) {
            FolderBrowserDialog folderDlg = new FolderBrowserDialog();
            folderDlg.ShowDialog();
            bullupPath = folderDlg.SelectedPath;
            if (server != null) {
                server.bullupPath = bullupPath;
            }
            textBox1.Text = bullupPath;
        }

        private TCPServer server = null;

        private void button3_Click(object sender, EventArgs e) {
            if (textBox1.Text == "") {
                MessageBox.Show("请选择最新版Bullup路径");
                return;
            }
            if (textBox2.Text == "") {
                MessageBox.Show("请选择最新版auto_program路径");
                return;
            }

            server = new TCPServer("127.0.0.1", 6001, 10);
            server.bullupPath = bullupPath;
            server.autoprogramPath = autoprogramPath;
            server.Start();
            button3.Enabled = false;
            button3.Text = "服务已开启";

          
        }

        private void ThreadChild() {
            
        }
    }
}
