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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace hex_to_bin_calculator
{
    struct valid_text
    {
        public int pos;
        public string text;
        public bool ignore;
    }

    public partial class Form1 : Form
    {
        private System.Windows.Forms.Timer SaveMemoTimer;
        private System.Windows.Forms.Timer Periodic_Timer;

        const int MAX_BITS = 32;
        const string const_memo_path = "memo_ho.txt";
        string memo_path = const_memo_path;
        readonly string memo_init = "cur.ini";
        private valid_text dec_valid_text = new valid_text();
        private valid_text hex_valid_text = new valid_text();
        private string raw_title = "PCHSU's HEX Operation 2.0";

		Mutex mutex = new Mutex(false, "hex_to_bin_cal_lock");

        private int mounse_status = 0;

        public Form1()
        {
            InitializeComponent();

            this.Text = raw_title;

            save_hex_status();
            save_dec_status();

            RestoreMemo();
            display_error_message(false);

            LinkLabel.Link link = new LinkLabel.Link();
            this.linkLabel1.Text = "PCHSU's HEX Operation";
            link.LinkData = "http://pc-hsu.blogspot.tw/2018/02/hex-operation.html";
            linkLabel1.Links.Add(link);

            SaveMemoTimer = new System.Windows.Forms.Timer();
            SaveMemoTimer.Tick += new EventHandler(TimerEven_SaveMemoTimer);
            SaveMemoTimer.Interval = 5000;
            SaveMemoTimer.Start();

            Periodic_Timer = new System.Windows.Forms.Timer();
            Periodic_Timer.Tick += new EventHandler(TimerEven_PeriodTimer);
            Periodic_Timer.Interval = 100;
            Periodic_Timer.Start();

            this.textBox6.Text = "Select : " + "None";

            set_to_zero();
        }

        private void form_status_from_active_to_inactive()
        {
            SaveMemo();
        }

        private void form_status_from_inactive_to_active()
        {
            RestoreMemo();
        }

        private string write_and_check_to_memo_init(string path)
        {
            string get_relative_path = Path.GetFileName(path);

            if (File.Exists(get_relative_path) == false)
                get_relative_path = const_memo_path;

            File.WriteAllText(memo_init, get_relative_path);

            return get_relative_path;
        }

        private string set_cur_memo_path(string cur_memo)
        {
            write_and_check_to_memo_init(cur_memo);
            return File.ReadAllText(memo_init);
        }

        private string get_and_check_cur_memo_path()
        {
            if(File.Exists(memo_init))
            {
                memo_path = File.ReadAllText(memo_init);
            }
            else
            {
                write_and_check_to_memo_init(const_memo_path);
                memo_path = File.ReadAllText(memo_init);
            }

            if(File.Exists(memo_path) == false)
            {
                write_and_check_to_memo_init(const_memo_path);
                memo_path = File.ReadAllText(memo_init);
            }

            display_error_message(false);

            return memo_path;
        }

        private const int WM_NCLBUTTONDBLCLK = 0xA3;
        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCLBUTTONDBLCLK:
                    min_the_form();
                    return;
            }
            base.WndProc(ref m);
        }

        private void min_the_form()
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void check_mouse_status_string()
        {
            if (latest_mounse_status == mounse_status)
                return;

            latest_mounse_status = mounse_status;

            string text = "Select : ";
            if (mounse_status == 0)
            {
                this.textBox6.Text = text + "None";
            }
            else if (mounse_status == 1)
            {
                this.textBox6.Text = text + "Enable";
            }
            else if (mounse_status == 2)
            {
                this.textBox6.Text = text + "Disable";
            }
            else
            {
                this.textBox6.Text = text + "None";
            }
        }

        private int latest_mounse_status = 0;
        private void TimerEven_PeriodTimer(Object myObject, EventArgs myEventArgs)
        {
            check_mouse_status_string();
        }

        private void TimerEven_SaveMemoTimer(Object myObject, EventArgs myEventArgs)
        {
            SaveMemoTimer.Stop();
            SaveMemo();
        }

        private void RestoreMemo()
        {
            mutex.WaitOne();
                if (File.Exists(get_and_check_cur_memo_path()))
                {
                    string cur_memo = this.textBox2.Text;
                    string update_memo = File.ReadAllText(memo_path);
                    if (cur_memo.Equals(update_memo) == false)
                        this.textBox2.Text = update_memo;
                }
            mutex.ReleaseMutex();
        }

        private void SaveMemo()
        {
            mutex.WaitOne();
                File.WriteAllText(get_and_check_cur_memo_path(), this.textBox2.Text);
			mutex.ReleaseMutex();
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

        private string get_raw_hex(string hex)
        {
            string input_text = hex.Replace("0x", "").Replace("0X", "").Replace("x", "").Replace("X", "");
            if (input_text.Equals(""))
                input_text = "0";

            return input_text;
        }

        private string get_raw_dec(string dec)
        {
            if (textBox3.Text.Equals(""))
                return "0";
            else
                return dec;
        }

        private bool hex_dec_is_same(string hex, string dec)
        {
            uint hex_uint32 = UInt32.Parse(get_raw_hex(hex), NumberStyles.HexNumber);
            uint dec_uint32 = UInt32.Parse(get_raw_dec(dec), NumberStyles.Integer);

            if (hex_uint32 == dec_uint32)
                return true;
            else
                return false;
        }

        private bool check_hex_is_valid(string hex)
        {
            try
            {
                uint uint32 = UInt32.Parse(get_raw_hex(hex), NumberStyles.HexNumber);
                return true;
            }
            catch(Exception ee)
            {
                return false;
            }
        }

        private void save_hex_status()
        {
            hex_valid_text.pos = this.textBox1.SelectionStart;
            hex_valid_text.text = this.textBox1.Text;
        }

        private void restore_hex_status()
        {
            this.textBox1.Text = hex_valid_text.text;
            this.textBox1.SelectionStart = hex_valid_text.pos;
        }

        private void display_error_message(bool err)
        {
            if (err)
                this.Text = raw_title + " (輸入錯誤)";
            else
            {
                string cur_memo_name = Path.GetFileName(memo_path);
                string cur_memo_ext = Path.GetExtension(cur_memo_name);
                string show_name = cur_memo_name.Replace(cur_memo_ext, "").Replace("_ho", "");

                this.Text = raw_title + " ( " + show_name + " )";
            }
        }
        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (hex_valid_text.ignore)
                {
                    hex_valid_text.ignore = false;
                    return;
                }

                if(check_hex_is_valid(this.textBox1.Text) == false)
                {
                    hex_valid_text.ignore = true;
                    restore_hex_status();
                    display_error_message(true);
                    return;
                }
                display_error_message(false);

                uint uint32 = UInt32.Parse(get_raw_hex(this.textBox1.Text), NumberStyles.HexNumber);

                //use X8 to append 0 to prefix
                BitArray_to_checkbox_gourp(ConvertHexToBitArray(uint32.ToString("X8")));

                save_hex_status();

                if (hex_dec_is_same(this.textBox1.Text, this.textBox3.Text) == false)
                {
                    this.textBox3.Text = uint32.ToString();
                    this.textBox3.SelectionStart = this.textBox3.Text.Length;
                    save_dec_status();
                }
            }
            catch(Exception ee)
            {
                MessageBox.Show(ee.ToString());
            }
        }

        private void response_checkbox_gourp_change()
        {
            uint uint32 = checkbox_gourp_to_uint();
            this.textBox1.Text = "0x" + uint32.ToString("X8");
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            mounse_status = 0;

            response_checkbox_gourp_change();
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            prevent_hotkey_from_effecting_mouse_status(e);

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

        private bool check_dec_is_valid(string dec)
        {
            try
            {
                uint val = Convert.ToUInt32(get_raw_dec(textBox3.Text));
                return true;
            }
            catch (Exception ee)
            {
                return false;
            }
        }

        private void save_dec_status()
        {
            dec_valid_text.pos = this.textBox3.SelectionStart;
            dec_valid_text.text = this.textBox3.Text;
        }

        private void restore_dec_status()
        {
            this.textBox3.Text = dec_valid_text.text;
            this.textBox3.SelectionStart = dec_valid_text.pos;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (dec_valid_text.ignore)
                {
                    dec_valid_text.ignore = false;
                    return;
                }

                if (check_dec_is_valid(this.textBox3.Text) == false)
                {
                    dec_valid_text.ignore = true;
                    restore_dec_status();
                    display_error_message(true);
                    return;
                }
                display_error_message(false);

                uint val = Convert.ToUInt32(get_raw_dec(textBox3.Text));

                save_dec_status();

                if (hex_dec_is_same(this.textBox1.Text, this.textBox3.Text) == false)
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
            this.textBox1.SelectionStart = this.textBox1.Text.Length;
            save_hex_status();
            display_error_message(false);
        }

        private void set_mouse_status()
        {
            mounse_status++;
            if (mounse_status > 2)
                mounse_status = 0;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            string tmp = e.KeyCode.ToString();
            if (tmp.CompareTo("Escape") == 0)
            {
                mounse_status = 0;
                set_to_zero();
            }

            if (e.Modifiers == Keys.Control)
            {
                set_mouse_status();
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
					mutex.WaitOne();
                        try
                        {
                            SaveMemo();
                            set_cur_memo_path(fp);
                            RestoreMemo();
                        }
                        catch(Exception ee)
                        {
                            MessageBox.Show(ee.ToString());
                        }
					mutex.ReleaseMutex();
                }
            }
        }

        private void textBox3_MouseClick(object sender, MouseEventArgs e)
        {
            mounse_status = 0;

            save_dec_status();
        }

        private void textBox1_MouseClick(object sender, MouseEventArgs e)
        {
            save_hex_status();
        }

        private void textBox3_KeyUp(object sender, KeyEventArgs e)
        {
            string instruct = e.KeyCode.ToString();
            if (instruct.CompareTo("Left") == 0 ||
                instruct.CompareTo("Right") == 0)
            {
                save_dec_status();
            }
        }

        private void textBox1_KeyUp(object sender, KeyEventArgs e)
        {
            string instruct = e.KeyCode.ToString();
            if (instruct.CompareTo("Left") == 0 ||
                instruct.CompareTo("Right") == 0)
            {
                save_hex_status();
            }
        }

        private void checkBox1_MouseEnter(object sender, EventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            bool is_checked = checkbox.Checked;
            if(mounse_status == 1)
            {
                checkbox.Checked = true;
                if(is_checked != true)
                    response_checkbox_gourp_change();
            }

            if (mounse_status == 2)
            {
                checkbox.Checked = false;
                if (is_checked == true)
                    response_checkbox_gourp_change();
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData as string);
        }

        private void Form1_Click(object sender, EventArgs e)
        {
            mounse_status = 0;
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            mounse_status = 0;
        }

        private void textBox6_Click(object sender, EventArgs e)
        {
            set_mouse_status();
        }

        private void textBox2_Click(object sender, EventArgs e)
        {
            mounse_status = 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Process.Start("explorer.exe", Directory.GetCurrentDirectory());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("notepad++.exe");
            }
            catch(Exception ee)
            {
                MessageBox.Show("開啟失敗，請確認有安裝Notepad++。");
            }
        }

        private void checkBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                mounse_status = 0;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            prevent_hotkey_from_effecting_mouse_status(e);
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            prevent_hotkey_from_effecting_mouse_status(e);
        }

        private void prevent_hotkey_from_effecting_mouse_status(KeyEventArgs e)
        {
            foreach (System.Windows.Forms.Keys suit in (System.Windows.Forms.Keys[])Enum.GetValues(typeof(System.Windows.Forms.Keys)))
            {
                if ((e.KeyCode != Keys.ControlKey &&
                    e.KeyCode != Keys.Control)
                    && e.Control && e.KeyCode == suit)
                {
                    mounse_status = 0;
                }
            }
        }

        private void Form1_Deactivate(object sender, EventArgs e)
        {
            form_status_from_active_to_inactive();
        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            form_status_from_inactive_to_active();
        }
    }
}
