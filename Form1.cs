using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Serial_port
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            cboxPort.Items.AddRange(ports);
            toolTipSpeed.SetToolTip(lblSpeed, "Recommended value between 0.5 and 3.0.\n"+
                "The higher the speed the higher the supply voltage needs to be set for the motors.\n"+
                "the below values are guidelines:\n1 turns/sec - 12V\n3 turns/sec - 24V");
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.Close();
                btnConnect.Text = "Connect";
                return;
            }
            try
            {
                serialPort1.PortName = cboxPort.Text;
                serialPort1.Open();
                btnConnect.Text = "Disconnect";
                send_Message("C");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private int coil_width = 0, turns = 0; double thickness, speed = 1.0, total_time;
        bool winding_started = false;
        private double start_ticks = 0;

        private void btnStart_Click(object sender, EventArgs e)
        {
            bool result = serialPort1.IsOpen &&
                          double.TryParse(txtThickness.Text, out thickness) &&
                          int.TryParse(txtTurns.Text, out turns) && 
                          int.TryParse(txtWidth.Text, out coil_width) && 
                          double.TryParse(txtSpeed.Text, out speed);
            if (!result)
            {
                MessageBox.Show("Please, select port and fill in all the details. Use only numbers.");
                return;
            }
            total_time = turns / speed;
            TimeSpan t = TimeSpan.FromSeconds(total_time);
            lblTotalTime.Text = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t.Hours, t.Minutes, t.Seconds);
            string Msg = (thickness*1000).ToString() + ',' + 
                          coil_width.ToString() + ',' + 
                          txtTurns.Text + ',' +
                          (speed * 10).ToString() + ',' +
                          'Z';

            winding_running = true;
            winding_started = true;
            start_ticks = DateTime.Now.TimeOfDay.TotalSeconds;
            send_Message(Msg);
        }

        private void send_Message(string Msg)
        {
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.Write(Msg);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private bool winding_running = false;

        private void btnPause_Click(object sender, EventArgs e)
        {
            pause_winding();
        }

        private void pause_winding()
        {
            if (serialPort1.IsOpen == false)
            {
                MessageBox.Show("Winder is not connected.");
                return;
            }
            if (winding_started == false) return;
            if (winding_running == true)
            {
                btnPause.Text = "Resume winding";
                winding_running = false;
            }
            else
            {
                btnPause.Text = "Pause winding";
                winding_running = true;
            }
            send_Message("P");
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                MessageBox.Show("Winder is not connected.");
                return;
            }
            if (winding_running == false) return;
            winding_running = false;
            send_Message("S");
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string inData = serialPort1.ReadExisting();
            displayToWindow(inData);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Space))
            {
                pause_winding();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private Transformer_calculator.Transformer_calculator_form tr_calc_form;
        private void btn_Transform_Click(object sender, EventArgs e)
        {
            tr_calc_form = new Transformer_calculator.Transformer_calculator_form();
            tr_calc_form.Show();
        }

        private void btnClear_status_Click(object sender, EventArgs e)
        {
            if (winding_running) return;
            txtStatus.Text = "";
            lblCounter.Text = "0";
            winding_running = false;
            winding_started = false;
            turns = 0;
        }

        int current_turn = 0;
        private void displayToWindow(string inData)
        {
            string resultString = Regex.Match(inData, @"\d+").Value;
            if (int.TryParse(resultString, out current_turn))
            {
                BeginInvoke(new EventHandler(delegate
                {
                    lblCounter.Text = resultString;
                    TimeSpan t = TimeSpan.FromSeconds(DateTime.Now.TimeOfDay.TotalSeconds - start_ticks);
                    lblElapsedTime.Text = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t.Hours, t.Minutes, t.Seconds);
                }));
                if (current_turn == turns)
                {
                    winding_running = false;
                    BeginInvoke(new EventHandler(delegate
                    {
                        txtStatus.AppendText(inData);
                    }));
                }
                else return;
            }
            else
            {
                BeginInvoke(new EventHandler(delegate
                {
                    txtStatus.AppendText(inData);
                }));
            }
        }
    }
}
