using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace MU3Input
{
    public partial class IOTest : Form
    {
        private MixedIO _io;

        private CheckBox[] _left;
        private CheckBox[] _right;
        //bool testDown = false;
        //private object _data;
        //private int ncoins;

        public IOTest(MixedIO io)
        {
            InitializeComponent();

            _left = new[] {
                lA,
                lB,
                lC,
                lS,
                lM,
            };

            _right = new[] {
                rA,
                rB,
                rC,
                rS,
                rM,
            };

            _io = io;
        }

        public static byte[] StringToByteArray(string hex)
        {
            var numberChars = hex.Length;
            var bytes = new byte[numberChars / 2];
            for (var i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        internal void UpdateData()
        {
            if (!Enabled && Handle == IntPtr.Zero) return;

            try
            {
                BeginInvoke(new Action(() =>
                {
                    bool hidIsConnected = _io.Items.Where(io => io.Key.GetType() == typeof(HidIO)).Any(io => io.Key.IsConnected);
                    lblStatus.Text = hidIsConnected ? "Nageki 已连接" : "Nageki 未连接";


                    if (!_io.IsConnected) return;


                    for (var i = 0; i < 5; i++)
                    {
                        if (i == 3)
                        {
                            _left[i].Checked = (Convert.ToBoolean(_io.Data.Buttons[i]));
                            _right[i].Checked = (Convert.ToBoolean(_io.Data.Buttons[i + 5]));
                        }
                        else
                        {
                            _left[i].Checked = Convert.ToBoolean(_io.Data.Buttons[i]);
                            _right[i].Checked = Convert.ToBoolean(_io.Data.Buttons[i + 5]);
                        }

                    }


                    trackBar1.Value = _io.Lever;

                    label3.Text = Convert.ToString(_io.Data.Aime.Scan);
                    if (_io.Data.Aime.Scan == 0)
                    {
                        //textAimiId.Text = BitConverter.ToString(_io.AimiId).Replace("-", "");
                    }

                    if (_io.Aime.Scan == 0)
                    {
                        label4.Text = "00000000000000000000";
                    }
                    else if (_io.Aime.Scan == 1)
                    {
                        label4.Text = BitConverter.ToString(_io.Aime.ID).Replace("-", "");
                    }
                    else if (_io.Aime.Scan == 2)
                    {
                        label4.Text = "0x" + BitConverter.ToUInt64(BitConverter.GetBytes(_io.Aime.IDm).Reverse().ToArray(), 0).ToString("X16");
                    }
                }));
            }
            catch
            {
                // ignored
            }
        }

        public void SetColor(uint data)
        {
            try
            {
                BeginInvoke(new Action(() =>
                {
                    _left[0].BackColor = Color.FromArgb(
                        (int)((data >> 23) & 1) * 255,
                        (int)((data >> 19) & 1) * 255,
                        (int)((data >> 22) & 1) * 255
                    );
                    _left[1].BackColor = Color.FromArgb(
                        (int)((data >> 20) & 1) * 255,
                        (int)((data >> 21) & 1) * 255,
                        (int)((data >> 18) & 1) * 255
                    );
                    _left[2].BackColor = Color.FromArgb(
                        (int)((data >> 17) & 1) * 255,
                        (int)((data >> 16) & 1) * 255,
                        (int)((data >> 15) & 1) * 255
                    );
                    _right[0].BackColor = Color.FromArgb(
                        (int)((data >> 14) & 1) * 255,
                        (int)((data >> 13) & 1) * 255,
                        (int)((data >> 12) & 1) * 255
                    );
                    _right[1].BackColor = Color.FromArgb(
                        (int)((data >> 11) & 1) * 255,
                        (int)((data >> 10) & 1) * 255,
                        (int)((data >> 9) & 1) * 255
                    );
                    _right[2].BackColor = Color.FromArgb(
                        (int)((data >> 8) & 1) * 255,
                        (int)((data >> 7) & 1) * 255,
                        (int)((data >> 6) & 1) * 255
                    );
                }));
            }
            catch
            {
                // ignored
            }
        }



        private void btnSetOption_Click(object sender, EventArgs e)
        {

            ///
            //byte[] aimiId;
            // MessageBox.Show("已写入卡号，请长按menu刷卡.");
            // try
            // {
            //     aimiId = StringToByteArray(textAimiId.Text);
            // }
            // catch
            //  {
            //      MessageBox.Show("无效卡号，卡号需要20个数字组成.", "错误");
            //       return;
            //   }

            //   if (aimiId.Length != 10)
            //   {
            //       MessageBox.Show("无效卡号，卡号需要20个数字组成.");
            //       return;
            //   }

            //   _io.SetAimiId(aimiId);
            ///


        }



        private void lblStatus_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void rB_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rS_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void IOTest_Load(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {


        }

        private void Test_Click(object sender, EventArgs e)
        {

        }

        private void Service_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void lA_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rA_CheckedChanged(object sender, EventArgs e)
        {

        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            return;

        }

        private void rC_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void lB_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void Test2_Click(object sender, EventArgs e)
        {

        }

        private void Test3_Click(object sender, EventArgs e)
        {

        }

        private void lS_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rM_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textAimiId_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_2(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Aime_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void button1_Click_3(object sender, EventArgs e)
        {
            label1.BackColor = Color.FromArgb(0, 0, 0);
        }

        private void label3_Click_1(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click_2(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("有问题请联系群内管理");
            System.Threading.Thread.Sleep(1000);
            System.Diagnostics.Process.Start("https://qm.qq.com/cgi-bin/qm/qr?k=kCSpQvmiFVdB7tvXwyMsl20NaDuAaDss&jump_from=webapi");
            return;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请仔细阅读并配置");
            System.Threading.Thread.Sleep(1000);
            System.Diagnostics.Process.Start("https://nananana.net/Nageki%20%E4%BD%BF%E7%94%A8%E8%AF%B4%E6%98%8E%E6%96%87%E6%A1%A3%20new.pdf");
            return;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请在网页上方输入卡号.下方输入nageki.nananana.net");
            System.Threading.Thread.Sleep(1000);
            System.Diagnostics.Process.Start("http://nageki.nananana.net/");
            return;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show("下载后请自行重命名为MU3Input并拖入package文件夹");
            System.Threading.Thread.Sleep(1000);
            System.Diagnostics.Process.Start("http://nageki.nananana.net:810/ongeki/NAGEKIO/MU3Input(Card).dll");
            return;
        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void lM_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void lC_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
