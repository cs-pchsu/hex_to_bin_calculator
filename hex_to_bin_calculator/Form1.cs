using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace hex_to_bin_calculator
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer SaveMemoTimer;

        const int MAX_BITS = 32;
        readonly string memo_path = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\memo.txt";
        private Object SaveMemo_Lock = new Object();
        public Form1()
        {
            InitializeComponent();

            SaveMemoTimer = new System.Windows.Forms.Timer();
            SaveMemoTimer.Tick += new EventHandler(TimerEven_SaveMemoTimer);
            SaveMemoTimer.Interval = 5000;
            SaveMemoTimer.Start();

            RestoreMemo();
        }

        private void TimerEven_SaveMemoTimer(Object myObject, EventArgs myEventArgs)
        {
            SaveMemoTimer.Stop();
            SaveMemo();
        }

        private void RestoreMemo()
        {
            lock (SaveMemo_Lock)
            {
                if (File.Exists(memo_path))
                {
                    this.textBox2.Text = File.ReadAllText(memo_path);
                }
            }
        }

        private void SaveMemo()
        {
            lock (SaveMemo_Lock)
            {
                File.WriteAllText(memo_path, this.textBox2.Text);
            }
        }

        private uint checkbox_gourp_to_uint()
        {
            uint val = 0;
            for (int a = 1; a <= MAX_BITS; a++)
            {
                string name = "checkBox" + a;
                CheckBox cb = (CheckBox)(this.Controls.Find(name, true))[0];

                if (cb.Checked)
                    val |= (uint)1 << a - 1;
            }

            return val;
        }

        private void BitArray_to_checkbox_gourp(BitArray ba)
        {
            for (int a = 1; a <= ba.Length; a++)
            {
                string name = "checkBox" + a;
                CheckBox cb = (CheckBox)(this.Controls.Find(name, true))[0];

                if (ba[MAX_BITS - a] == true)
                {
                    cb.ForeColor = System.Drawing.Color.Red;
                    cb.Checked = true;
                }
                else
                {
                    cb.ForeColor = System.Drawing.Color.Black;
                    cb.Checked = false;
                }
            }
        }

        public static BitArray ConvertHexToBitArray(string hexData)
        {
            if (hexData == null)
                return null; // or do something else, throw, ...

            BitArray ba = new BitArray(4 * hexData.Length);
            for (int i = 0; i < hexData.Length; i++)
            {
                byte b = byte.Parse(hexData[i].ToString(), NumberStyles.HexNumber);
                for (int j = 0; j < 4; j++)
                {
                    ba.Set(i * 4 + j, (b & (1 << (3 - j))) != 0);
                }
            }
            return ba;
        }
        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                string input_text = this.textBox1.Text.Replace("0x", "").Replace("0X", "").Replace("x", "").Replace("X", "");
                if (input_text.Equals(""))
                    input_text = "0";

                uint uint32 = UInt32.Parse(input_text, NumberStyles.HexNumber);

                //use X8 to append 0 to prefix
                BitArray_to_checkbox_gourp(ConvertHexToBitArray(uint32.ToString("X8")));

                ignore_set_textBox1 = true;
                this.textBox3.Text = uint32.ToString();
            }
            catch(Exception ee)
            {
                MessageBox.Show(ee.ToString());
            }
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            uint uint32 = checkbox_gourp_to_uint();
            this.textBox1.Text = "0x" + uint32.ToString("X8");
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            SaveMemoTimer.Stop();
            SaveMemoTimer.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveMemoTimer.Stop();
            SaveMemo();
        }

        private void Form1_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(System.IO.Path.GetDirectoryName(Application.ExecutablePath));
        }

        bool ignore_set_textBox1 = false;
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                uint val = 0;
                if (textBox3.Text.Equals(""))
                    val = 0;
                else
                    val = Convert.ToUInt32(textBox3.Text);

                if(ignore_set_textBox1)
                {
                    ignore_set_textBox1 = false;
                    return;
                }
                this.textBox1.Text = "0x" + val.ToString("X8");
            }
            catch(Exception ee)
            {
                MessageBox.Show(ee.ToString());
            }
        }

        private void checkBox33_Click(object sender, EventArgs e)
        {
            if(this.checkBox33.Checked)
                this.TopMost = true;
            else
                this.TopMost = false;
        }

        private void set_to_zero()
        {
            textBox1.Text = "0x0";
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            string tmp = e.KeyCode.ToString();
            if (tmp.CompareTo("Escape") == 0)
            {
                set_to_zero();
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length == 1)
            {
                string fp = files[0];
                string fn = Path.GetFileName(fp);
                if(fn.IndexOf("_ho") > 0)
                {
                    lock (SaveMemo_Lock)
                    {
                        try
                        {
                            string bak_path = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\";
                            string bak_full_path = bak_path + "memo_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".txt";
                            File.Copy(memo_path, bak_full_path, false);
                            File.Copy(fp, memo_path, true);
                            RestoreMemo();
                        }
                        catch(Exception ee)
                        {
                            MessageBox.Show(ee.ToString());
                        }
                    }
                }
            }
        }
    }
}
