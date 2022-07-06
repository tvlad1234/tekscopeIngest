using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Net.Sockets;

namespace WindowsFormsApp3
{   

    public partial class Form1 : Form
    {
        static TcpClient client;
        static NetworkStream stream;
        static bool receiving = false, listening = false;
        static string payload;
        System.IO.StreamWriter writer;

        static int lineNum, atten;
        static double sampPer, offsetVoltage;

        void UpdateWave()
        {
            string message = "RECALL:WAVEFORM \"data.csv\",";
            if (radioButton1.Checked)
                message += "REF1\n";
            else if (radioButton2.Checked)
                message += "REF2\n";
            else if (radioButton3.Checked)
                message += "REF3\n";
            else if (radioButton4.Checked)
                message += "REF4\n";
           // MessageBox.Show(message);
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            try
            {
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lost connection to TekScope!");
                Application.Exit();
            }
        }

        void SetTekScopePath()
        {

            String message = "FILESystem:CWD \"" + Directory.GetCurrentDirectory() + "\"\n";
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            try
            {
                stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lost connection to TekScope!");
                Application.Exit();
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listening = false;
            if (serialPort1.IsOpen)
                serialPort1.Close();
            receiving = false;
            label3.Text = "Ready";
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            if (serialPort1.BytesToRead > 6 && listening)
            {
                string str = serialPort1.ReadLine();
                richTextBox1.Text += str;
                if (str.Equals("BeginWave!"))
                {
                    label3.Text = "Receiving";
                    payload = "";
                    lineNum = 0;
                    receiving = true;
                    payload += "Model,TekscopeSW\n\r";
                    payload += "Label,CH1\n\r";
                    payload += "Waveform Type,ANALOG\n\r";
                    payload += "Horizontal Units, s\n\r";

                }
                else if (str.Equals("\rSendWaveComplete!"))
                {
                    writer = new System.IO.StreamWriter("data.csv");
                    writer.Write(payload);
                    writer.Close();
                    UpdateWave();

                    if (checkBox1.Checked && listening)
                    {
                        richTextBox1.Clear();
                        serialPort1.DiscardInBuffer();
                        serialPort1.Write("       S");
                    }
                    else
                    {
                        receiving = false;
                        label3.Text = "Ready";
                        serialPort1.Close();
                    }
                }
                else
                {
                    str = str.Remove(0, 1);
                    if (lineNum == 0) //SampPer
                    {
                        sampPer = Convert.ToDouble(str);
                        payload += "Sample Interval," + str + "E-06\n\r";

                    }
                    else if (lineNum == 1) //RecordLength
                    {
                        payload += "Record Length," + str + "\n\r";
                    }
                    else if (lineNum == 2)
                    {
                        payload += "Zero Index," + str + "\n\r";

                        payload += "Vertical Units, V\n\r";
                        payload += ",\n\rLabels,\n\r";
                        payload += "TIME,CH1\n\r";

                    }
                    else if (lineNum == 3)
                    {
                        offsetVoltage = Convert.ToDouble(str);
                    }

                    else if (lineNum == 4)
                    {
                        atten = Convert.ToInt32(str);
                    }

                    else
                    {
                        string timeStamp = Convert.ToString((lineNum - 2) * sampPer);
                        int samp = Convert.ToInt32(str);
                        double voltage = 2 * atten * ((3.3 * samp / 4096.0) - offsetVoltage);
                        payload += timeStamp + "E-06," + voltage.ToString() + "\n\r";
                    }
                    lineNum++;

                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                serialPort1.PortName = comboBox1.Text;
                try
                {
                    serialPort1.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Couldn't open serial port!");
                }

            }

            if (serialPort1.IsOpen)
            {
                SetTekScopePath();
                serialPort1.DiscardInBuffer();
                richTextBox1.Clear();
                serialPort1.Write("S");
                label3.Text = "Waiting for data";
                listening = true;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                client = new TcpClient("localhost", 4000);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't connect to TekScope!");
                Application.Exit();
            }
            foreach (string s in SerialPort.GetPortNames())
            {
                comboBox1.Items.Add(s);
            }
            comboBox1.SelectedIndex = 0;
            radioButton1.Checked = true;
            label3.Text = "Ready";
        }

    }
}
