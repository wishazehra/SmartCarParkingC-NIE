using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ParkingSystem;
using System.Net.Sockets;
using System.Net;
using System.Threading;

using FireSharp.Config;
using FireSharp.Response;
using FireSharp.Interfaces;

namespace ParkingDemo
{

	public partial class Form1 : Form
    {
		IFirebaseConfig config = new FirebaseConfig
		{
			AuthSecret = "BpuJWm4yJiEUGifgu14LfCdgScdVpPJoPbkJUbJE",
			BasePath = "https://route-a7e68.firebaseio.com/"


		};

		IFirebaseClient firebaseClient;
        /// <summary>
        /// <remarks>serialPort State</remarks>
        /// </summary>
        private bool commState = false;
       
        /// <summary>
        /// <remarks>SerialPort Instantiation</remarks>
        /// </summary>
        public static ParkingSystem.ParkingSerialPort serialPort = new ParkingSerialPort();
        /// <summary>
        /// <remarks>TCP Instantiation</remarks>
        /// </summary>
        public static ParkingSystem.ParkingRemoteTCP wapper = new ParkingRemoteTCP();
        
        /// <summary>
        /// <remarks>Tcp Client</remarks>
        /// </summary>
        private static TcpClient client = new TcpClient();
        
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
			light73.ForeColor = Color.Red;
			light74.ForeColor = Color.Red;
			light75.ForeColor = Color.Red;
			light76.ForeColor = Color.Red;
			light77.ForeColor = Color.Red;
			light78.ForeColor = Color.Red;
			light79.ForeColor = Color.Red;
			light7A.ForeColor = Color.Red;
			light7B.ForeColor = Color.Red;
			light81.ForeColor = Color.Red;
			

			firebaseClient = new FireSharp.FirebaseClient(config);
			if (firebaseClient != null)
			{
				MessageBox.Show("Firebase Connected");
			}
			
			CommInit();
           // EthCommInit();
            ParkingOriginalPacket.EvProcessReceivedPacket += sp_ProcessReceivedPacket;
        }


		#region SerialPort Operation
		/// <summary>
		/// SerialPort Initialize
		/// </summary>
		private void CommInit()
        {
            foreach (string name in serialPort.portNames)
            {
                if (!CommPort_comboBox.Items.Contains(name))
                {
                    CommPort_comboBox.Items.Add(name);
                }
                CommPort_comboBox.Text = name;
            }

            CommBaud_comboBox.Text = "115200";
        }
        /// <summary>
        /// Set SerialPort Open
        /// </summary>
        private void SetOpen()
        {
            OpenClosePort_Button.Text = "Close  Comm";
            CommStatus_label.ForeColor = Color.Lime;
            commState = true;
        }
        /// <summary>
        /// Set SerialPort Close
        /// </summary>
        private void SetClose()
        {
            OpenClosePort_Button.Text = "Open  Comm";
            CommStatus_label.ForeColor = Color.DarkGray;
            commState = false;
        }
        /// <summary>
        /// Open Close SerialPort Oper
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void OpenClosePort_Button_Click(object sender, EventArgs e)
        {
            try
            {
                if (OpenClosePort_Button.Text == "Open  Comm")
                {
                    serialPort = new ParkingSerialPort(CommPort_comboBox.Text, 
						Convert.ToInt32(CommBaud_comboBox.Text));
                    serialPort.Open();
                    SetOpen();
                }
                else if (OpenClosePort_Button.Text == "Close  Comm")
                {
                    if (!serialPort.IsOpen)
                    {
                        SetClose();
                        return;
                    }
                    serialPort.Close();
                    SetClose();

                }


            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
                SetClose();
                return;
            }
        }
        #endregion

        #region Device Operation
        
        /// <summary>
        /// Get device name
        /// </summary>
        /// <param name="devname">devicetype</param>
        /// <returns>devicename</returns>
        public string GetDevName(byte byteName)
        {
            string devname = "";
            switch (byteName)
            {
                case 0x01:
                    devname = "WDC-4003";
                    break;
                case 0x02:
                    devname = "WDC-4005";
                    break;
                case 0x03:
                    devname = "WDC-4008";
                    break;
                case 0x04:
                    devname = "WDC-4007";
                    break;
                case 0x05:
                    devname = "WPSD-340S3";
                    break;
                case 0x06:
                    devname = "WPSD-340S5";
                    break;
                case 0x07:
                    devname = "WPSD-340S8";
                    break;
                case 0x08:
                    devname = "WPSD-340S7";
                    break;
                case 0x09:
                    devname = "WPSD-340E3";
                    break;
                case 0x0A:
                    devname = "WPSD-340E5";
                    break;
                case 0x0B:
                    devname = "WPSD-340E8";
                    break;
                case 0x0C:
                    devname = "WPSD-340E7";
                    break;

                default:
                    devname = "WDC-400x";
                    break;
            }
            return devname;
        }
        #endregion


        #region Received Process
        /// <summary>
        /// <remarks>Process Received Packet</remarks>
        /// </summary>
        /// <param name="pk">Received Packet</param>
        private void sp_ProcessReceivedPacket(baseReceivedPacket pk)
        {
            try
            {
                byte revType = Convert.ToByte(pk.type_ver >> 8);
                string wpsdid = "";
                string WDCid = "";
                string RSSI = "";
                byte carState = 0;
                string voltage = "";
                string hardVer = "";
                string softVer = "";
                string deviceName = "";
                string hbPeriod = "";
                this.Invoke((EventHandler)async delegate
                {
                    #region Senser Heart Beat
                    if (pk is SensorHBeat)
                       {
                           SensorHBeat hb = (SensorHBeat)pk;
                            wpsdid = (hb.WPSD_ID).ToString("X2").PadLeft(8, '0');
                            WDCid = (hb.WDC_ID).ToString("X2").PadLeft(8, '0');
                            softVer = "v" + int.Parse(hb.APP_VER.ToString("X2").Substring(0, 1)).ToString() 
						+ "." + int.Parse(hb.APP_VER.ToString("X2").Substring(1, 1)).ToString().PadLeft(2, '0');

							hardVer = ((int)(hb.HARD_VER) + 10).ToString();
                            hardVer = "v" + hardVer.Substring(0, 1) + "." + hardVer.Substring(1, 1);
                            voltage = (Math.Round((decimal)hb.VOLT / 10, 2)).ToString()+"V";
							RSSI = ((Int16)hb.RSSI - 30).ToString();
                            hbPeriod = hb.HB_PERIOD.ToString();
                            deviceName = GetDevName(hb.DEV_TYPE);
                            carState = hb.CAR_STATE;

						string car = "";
                            if (carState == 0x01)
                            {
								car = "occupied";
							}
                            else
                            {
								car = "vacant";
							}
							
						var data = new Data
						{
							sensorId = wpsdid,
							voltage = voltage,
							signalStrength = RSSI,
							spaceState = car
						};
						
						setColor(wpsdid, car);
						if (data != null)
						{
							SetResponse response = 
							await firebaseClient.SetTaskAsync("Sensors/sensor" + wpsdid, data);

							Data result = response.ResultAs<Data>();
							MessageBox.Show("Data Inserted");
						}
						
					}
                      #endregion
                    #region Senser Detect
                    else if (pk is SensorDetect)
                    {
                        SensorDetect decbeat = (SensorDetect)pk;
                        wpsdid = (decbeat.WPSD_ID).ToString("X2").PadLeft(8, '0');
                        WDCid = (decbeat.WDC_ID).ToString("X2").PadLeft(8, '0');
                        hardVer = ((int)(decbeat.HARD_VER) + 10).ToString();
                        hardVer = "v" + hardVer.Substring(0, 1) + "." + hardVer.Substring(1, 1);
                        deviceName = GetDevName(decbeat.DEV_TYPE);
                        carState = decbeat.CAR_STATE;

						string car = "";
                        if (carState == 0x01)
                        {
                          car = "occupied";
                        }
                        else
                        {
                        	car = "vacant";
						}
						setColor(wpsdid, car);
						var data = new Data
						{
							sensorId = wpsdid,
							voltage = voltage,
							signalStrength = RSSI,
							spaceState = car
						};
						SetResponse response = await firebaseClient.SetTaskAsync("Sensors/sensor" + wpsdid, data);
						Data result = response.ResultAs<Data>();
						MessageBox.Show("Data Inserted");
					}
                    #endregion
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

		private void setColor(string wpsdid, string car)
		{
			if (wpsdid.Equals("283A7351"))
			{
				if (car.Equals("vacant"))
				{
					light73.ForeColor = Color.Lime;
				}
				else
				{
					light73.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A7451"))
			{
				if (car.Equals("vacant"))
				{
					light74.ForeColor = Color.Lime;
				}
				else
				{
					light74.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A7551"))
			{
				if (car.Equals("vacant"))
				{
					light75.ForeColor = Color.Lime;
				}
				else
				{
					light75.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A7651"))
			{
				if (car.Equals("vacant"))
				{
					light76.ForeColor = Color.Red;
				}
				else
				{
					light76.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A7751"))
			{
				if (car.Equals("vacant"))
				{
					light77.ForeColor = Color.Lime;
				}
				else
				{
					light77.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A7851"))
			{
				if (car.Equals("vacant"))
				{
					light78.ForeColor = Color.Lime;
				}
				else
				{
					light78.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A7951"))
			{
				if (car.Equals("vacant"))
				{
					light79.ForeColor = Color.Lime;
				}
				else
				{
					light79.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A7A51"))
			{
				if (car.Equals("vacant"))
				{
					light7A.ForeColor = Color.Lime;
				}
				else
				{
					light7A.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A7B51"))
			{
				if (car.Equals("vacant"))
				{
					light7B.ForeColor = Color.Lime;
				}
				else
				{
					light7B.ForeColor = Color.Red;
				}
			}
			else if (wpsdid.Equals("283A8151"))
			{
				if (car.Equals("vacant"))
				{
					light81.ForeColor = Color.Lime;
				}
				else
				{
					light81.ForeColor = Color.Red;
				}
			}
		}

		/// <summary>
		/// <remarks>Received Data Show</remarks>
		/// </summary>
		/// <param name="text">Received data</param>
		/*public void reshow(byte[] text,bool source)
        {
            string restr = "";
            if (text != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    restr += text[i].ToString("X2");
                    restr += " ";
                }
            }
            if (source)
            {
                richTextBox1.AppendText(System.DateTime.Now.ToString() + "[Received]:  " + restr + "\n");
            }
            else { richTextBox1.AppendText(System.DateTime.Now.ToString() + "[Send]:  " + restr + "\n"); }
        }
        */
        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {

                System.Environment.Exit(System.Environment.ExitCode);
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message);
            }
        }

		private void groupBox2_Enter(object sender, EventArgs e)
		{

		}

		private void label1_Click(object sender, EventArgs e)
		{

		}

		private void light74_Click(object sender, EventArgs e)
		{

		}

		private void close_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void richTextBox1_TextChanged(object sender, EventArgs e)
		{

		}

		private void label12_Click(object sender, EventArgs e)
		{

		}
	}
}
