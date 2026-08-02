using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging; // WinUI 3
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using WinRT.Interop;
using static System.Runtime.InteropServices.JavaScript.JSType;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ViscaIP {
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window {
		#region Enums and Constants

		enum BalanceType { Auto, Indoor, Outdoor, OnePush, AutoTracing, Manual }
		static BalanceType balanceType = BalanceType.Auto;
		static string[] BalanceStrs = { "Auto", "Indoor", "Outdoor", "One Push", "Auto Tracing", "Manual" };

		static byte[] MessageByte = { 0x00, 0x00, 0x07, 0x08, 0x38, 0x39, 0x0D, 0x33, 0x3F, 0x01, 0x00,
										0x35, 0x10, 0x43, 0x44, 0x3E, 0x33, 0x0E, 0x0C, 0x08,
										0x00, 0x47, 0x38, 0x48, 0x39, 0x4D, 0x33, 0x3F,
										0x35, 0x43, 0x44, 0x3E, 0x4E, 0x4A, 0x4B, 0x4C, 0x02 };
		byte[] BalanceCmd = { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
		string[] BalanceStrings = { "Auto", "Indoor", "Outdoor", "One Push", "Auto Tracing", "Manual" };
		string[] IrisStrings = { " --", "F22", "F19", "F16", "F14", "F11", "F9.6", "F8.0", "F6.8", "F5.6", "F4.8",
										 "F4.0", "F3.4", "F2.8", "F2.4", "F2.0", "F1.6", "F1.4", "F1.4", "F1.4", "F1.4",
										 "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4",
										 "F1.4", "F1.4" };
		string[] GainStrings = { "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB",
										 "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "2 dB", "4 dB",
										 "6 dB", "8 dB", "10 dB", "12 dB", "14 dB", "16 dB", "18 dB", "20 dB", "22 dB", "24 dB",
										 "26 dB", "28 dB" };
		string[] IrisD30Strings = { " --", "F28", "F22", "F19", "F16", "F14", "F11", "F9.6", "F8.0", "F6.8", "F5.6", "F4.8",
										 "F4.0", "F3.4", "F2.8", "F2.4", "F2.0", "F1.8" };
		string[] GainD30Strings = { "-3 dB", "0 dB", "3 dB", "6 dB", "9 dB", "12 dB", "15 dB", "18 dB", "21 dB", "24 dB", "27 dB",
											 "30 dB", "33 dB", "36 dB", "39 dB", "42 dB", "45 dB" };
		string[] ExpCompStrings = { "-10.5 dB", "-9 dB", "-7.5 dB", "-6 dB", "-4.5 dB", "-3 dB", "-1.5 dB", "0 dB",
											 "1.5 dB", "3 dB", "4.5 dB", "6 dB", "7.5 dB", "9 dB", "10.5 dB" };
		string[] ShutterStrings = { "1/1", "1/2", "1/4", "1/8", "1/15", "1/30", "1/60", "1/90", "1/100", "1/125",
											 "1/180", "1/250", "1/350", "1/500", "1/725", "1/1000", "1/1500", "1/2000", "1/3000", "1/4000",
											 "1/6000", "1/10000" };

		enum ControlFeature { ExpCompFull, AEFull, BrightFull, BalanceFull, FocusFull, ShutterFull, GainFull, WBFull, NoCenter }
		#endregion
		#region Class Variables
		//Dictionary<string, ControlFeature[]> CameraFeatures = new Dictionary<string, ControlFeature[]>();
		//CameraFeatures.Add(Config.CameraModelStrs[0], new ControlFeature[] { ControlFeature.AEFull });
		Dictionary<string, ControlFeature[]> cameraFeatures = new Dictionary<string, ControlFeature[]> {
			{ Config.CameraModelStrs[0], new ControlFeature[] { ControlFeature.NoCenter } },
			{ Config.CameraModelStrs[1], new ControlFeature[] { ControlFeature.ExpCompFull, ControlFeature.NoCenter } }
		};

		uint SequenceNumber = 0;

		static MsgQueue msgQueue = new MsgQueue();
		static MsgType lastMsg = MsgType.None;
		static DateTime lastSend = DateTime.Now;

		public bool keepReading = false;

		static bool powerOn = false;
		static int cameraNum = 1;
		static string cameraAddress = "";
		static int cameraPort = 0;
		static string cameraModel = "";
		static ControlFeature[]? modelFeatures = [];
		static int firstCamera = 0;

		static byte panRate = 1;
		static byte tiltRate = 1;
		static byte zoomRate = 1;
		static byte focusRate = 1;
		static bool ptRectDragging = false;
		static bool zmRectDragging = false;
		static bool focusRectDragging = false;
		static int lastPanRate = 0;
		static int lastTiltRate = 0;
		static int lastZoomRate = 0;
		static int lastFocusRate = 0;

		static bool log = false;
		static bool loaded = false;

		Microsoft.UI.Windowing.AppWindow? appWindow = null;

		#endregion

		#region UI Elements

		List<Button> presetButtons = new List<Button>();
		List<TextBox> presetTexts = new List<TextBox>();
		List<Panel> presetPanels = new List<Panel>();
		List<RadioButton> deviceButtons = new List<RadioButton>();
		List<TextBox> cameraTexts = new List<TextBox>();
		List<Button> ptzButtons = new List<Button>();
		List<Control> focusControls = new List<Control>();
		List<Control> exposureControls = new List<Control>();

		#endregion

		#region Constants
		const byte comByte0 = 0x81;
		const byte comByte1 = 0x01;
		const byte normCmd = 0x04;
		const byte panTiltCmd = 0x06;

		#endregion

		private static System.Timers.Timer? presetTimer;
		Button? lastPreset = null;
		int lastPresetNumber = 0;
		bool settingPreset = false;

		private static bool messageDialogShowing = false;


		//Color nor}malColor;

		//int xFactor = 1;
		//int xCenter = 0;
		//int yFactor = 1;
		//int yCenter = 0;
		//int zFactor = 1;
		//int zCenter = 0;

		//bool calibrateMode = false;
		//int calMaxX = 0;
		//int calMinX = 65535;
		//int calMaxY = 0;
		//int calMinY = 65535;
		//int calMaxZ = 0;
		//int calMinZ = 65535;

		//int currentPreset = 0;
		//Color panelBackgroundColor;
		//bool buttonDown = false;
		//bool povDown = false;

		//int LeftPresetBtn = 0;
		//int RightPresetBtn = 2;
		//int SetPresetBtn = 1;
		//int SelectPresetBtn = 3;
		//int TriggerBtn = 7;

		public static class SimpleLogger {
			static string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			static string logDirectory = Path.Combine(appDataPath, "ViscaUI");
			private static string LogFilePath = Path.Combine(logDirectory, "Visca-01.log");
			private static bool initialized = false;
			private static bool logExists = false;
			private static Queue<string> waitingMsg = new Queue<string>();

			private static async void Init() {
				try {
					Debug.WriteLine($"Init(), {logDirectory}");
					Directory.CreateDirectory(logDirectory);
					Debug.WriteLine("after log directory create");
					initialized = true;

					int lastLog = Config.Instance;
					int nextLog = lastLog + 1;
					if (nextLog > 10) {
						nextLog = 1;
					}
					Config.Instance = nextLog;
					string fName = $"Visca-{nextLog:D2}.log";
					string fPath = Path.Combine(logDirectory, fName);
					if (File.Exists(fPath)) {
						File.Delete(fPath);
					}

					string logEntry = $"{DateTime.Now:MM-dd} - ViscaUI data log {Environment.NewLine}";
					File.WriteAllText(fPath, logEntry);
					if (File.Exists(fPath)) {
						LogFilePath = fPath;
						logExists = true;
					}
				} catch (Exception exc) {
					Debug.WriteLine($"Init() exception: {exc}");
				}
			}

			public static async Task LogAsync(string message) {
				if (!initialized) {
					Init();
				}

				string logEntry = $"{DateTime.Now:MM-dd HH:mm:ss} - {message}{Environment.NewLine}";

				if (FileLocked()) {
					waitingMsg.Enqueue(logEntry);
				} else {
					if (logExists) {
						while (waitingMsg.Count > 0) {
							string msg = waitingMsg.Dequeue();
							await File.AppendAllTextAsync(LogFilePath, msg);
						}

						await File.AppendAllTextAsync(LogFilePath, logEntry);
					}
				}
			}

			private static bool FileLocked() {
				try {
					using (FileStream stream = new FileStream(LogFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
						stream.Close();
					}
				} catch (IOException) {
					return true;
				}

				return false;
			}
		}

		public static void dfo(string txt) {
			if (log) {
#pragma warning disable CS4014
				SimpleLogger.LogAsync(txt);
#pragma warning restore CS4014
			}
		}

		public MainWindow() {
			try {
				InitializeComponent();

				AppWindow.SetIcon("camera.ico");

				init();
			} catch (FileNotFoundException exc) {
				Debug.WriteLine($"MainWindow()  file not found exception: {exc}");
			} catch (Exception exc) {
				Debug.WriteLine($"MainWindow()  unexpected exception: {exc}");
			}
		}

		private void ContentLoaded(object sender, RoutedEventArgs e) {
			try {
				setControlsState();

				loaded = true;

				sendInquiry(MsgType.IPower);
				sendInquiry(MsgType.IVersion);
			} catch (FileNotFoundException exc) {
				Debug.WriteLine($"ContentLoaded()  file not found exception: {exc}");
			} catch (Exception exc) {
				Debug.WriteLine($"ContentLoaded()  unexpected exception: {exc}");
			}
		}

		private void init() {
			try {
				IntPtr hWnd = WindowNative.GetWindowHandle(this);
				WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
				appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

				System.Drawing.Point location = Config.Location;
				appWindow.Move(new Windows.Graphics.PointInt32(location.X, location.Y));
				appWindow.Changed += (s, e) => {
					if (e.DidPositionChange) {
						var newPos = s.Position;
						Config.Location = new System.Drawing.Point(newPos.X, newPos.Y);
					}
				};

				appWindow.Resize(new Windows.Graphics.SizeInt32(510, Config.Debug ? 850 : 510));

				log = Config.Log;

				dfo("start");

				deviceButtons.Add(C1);
				deviceButtons.Add(C2);
				deviceButtons.Add(C3);
				deviceButtons.Add(C4);
				deviceButtons.Add(C5);
				deviceButtons.Add(C6);
				deviceButtons.Add(C7);

				showCameras();

				RadioButton btn = deviceButtons[firstCamera - 1];
				btn.IsChecked = true;

				presetButtons.Add(p1Btn);
				presetButtons.Add(p2Btn);
				presetButtons.Add(p3Btn);
				presetButtons.Add(p4Btn);
				presetButtons.Add(p5Btn);
				presetButtons.Add(p6Btn);

				presetTexts.Add(p1TextBox);
				presetTexts.Add(p2TextBox);
				presetTexts.Add(p3TextBox);
				presetTexts.Add(p4TextBox);
				presetTexts.Add(p5TextBox);
				presetTexts.Add(p6TextBox);

				presetPanels.Add(p1Panel);
				presetPanels.Add(p2Panel);
				presetPanels.Add(p3Panel);
				presetPanels.Add(p4Panel);
				presetPanels.Add(p5Panel);
				presetPanels.Add(p6Panel);

				foreach (Button b in presetButtons) {
					b.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(PresetDown), handledEventsToo: true);
					b.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(PresetUp), handledEventsToo: true);
				}

				string[] cameraSettings = Config.GetCamera(cameraNum);
				for (int i = 0; i < presetTexts.Count; i++) {
					presetTexts[i].Text = cameraSettings[4 + i];
				}

				string modelName = cameraSettings[3];
				modelTextBox.Text = modelName;

				cameraFeatures.TryGetValue(modelName, out modelFeatures);

				if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.FocusFull))) {
					focusButtonStack.Visibility = Visibility.Collapsed;
					focusRect.Visibility = Visibility.Visible;
				} else {
					focusRect.Visibility = Visibility.Collapsed;
					focusButtonStack.Visibility = Visibility.Visible;
				}
				if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.ExpCompFull))) {
					expCompButtonStack.Visibility = Visibility.Collapsed;
				} else {
					expCompSlideStack.Visibility = Visibility.Collapsed;
					gainUpBtn.IsEnabled = false;
					gainDownBtn.IsEnabled = false;
				}

				if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.AEFull))) {
					gainButtonStack.Visibility = Visibility.Collapsed;
				} else {
					expCompManStack.Visibility = Visibility.Collapsed;
					expSlideStack.Visibility = Visibility.Collapsed;
				}

				if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.BrightFull))) {
					//k.Visibility = Visibility.Collapsed;
				} else {
					expBrightChk.Visibility = Visibility.Collapsed;
				}

				if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.WBFull))) {
					wbRedButtonStack.Visibility = Visibility.Collapsed;
					wbBlueButtonStack.Visibility = Visibility.Collapsed;
				} else {
					wbRedSliderStack.Visibility = Visibility.Collapsed;
					wbBlueSliderStack.Visibility = Visibility.Collapsed;
				}

				focusControlPanel.Visibility = Visibility.Collapsed;

			} catch (FileNotFoundException exc) {
				Debug.WriteLine($"init()  file not found exception: {exc}");
			} catch (Exception exc) {
				Debug.WriteLine($"init()  unexpected exception: {exc}");
			}
			dfo("init");
		}

		private void showCameras() {
			int cameraCount = 0;
			for (uint i = 1; i < 8; i++) {
				string[] camAry = Config.GetCamera((int)i);
				if (camAry.Length > 1) {
					deviceButtons[(int)i - 1].Content = camAry[2];
					cameraCount++;
					if (firstCamera == 0) {
						firstCamera = (int)i;
					}
				} else {
					deviceButtons[(int)i - 1].IsEnabled = false;
				}
			}
		}

		private void setControlsState() {
			this.DispatcherQueue.TryEnqueue(() => {

				//foreach (Button b in ptzButtons) {
				//	b.IsEnabled = enable;
				//}

				//foreach (Button b in presetButtons) {
				//	b.IsEnabled = enable;
				//}

				//foreach (TextBox t in presetTexts) 
				//	t.IsEnabled = enable;
				//}

				displayBrightMode(false);
				displayExpComp(false);
				balanceSetup();
			});
		}

		private void ShowPowerState(bool on) {
			try {
				Debug.WriteLine($"ShowPowerState({on})");	
				this.DispatcherQueue.TryEnqueue(() => {
					string state = on ? "on" : "off";
					powerImg.Source = new SvgImageSource(new Uri($"ms-appx:///Assets/power-{state}.svg"));
				});
			} catch (FileNotFoundException exc) {
				Debug.WriteLine($"ShowPowerState()  file not found exception: {exc}");
			} catch (Exception exc) {
				Debug.WriteLine($"ShowPowerState()  unexpected exception: {exc}");
			}
		}

		private async void SettingsClick(object sender, RoutedEventArgs e) {
			responseListAdd("settings");
			bool debug = Config.Debug;
			var dialog = new SettingsDialog();
			dialog.XamlRoot = this.Content.XamlRoot; // Required for WinUI 3
			await dialog.ShowAsync();

			Debug.WriteLine("after settings");
			if (debug != Config.Debug) {
				if (appWindow is not null) {
					appWindow.Resize(new Windows.Graphics.SizeInt32(510, Config.Debug ? 850 : 510));
				}
			}

			showCameras();
		}

		private void PowerClick(object sender, RoutedEventArgs e) {
			if (!powerOn) {
				byte[] d = { 0x00, 0x03};
				sendCommand(normCmd, d, MsgType.CPower);
				powerOn = true;
				ShowPowerState(powerOn);
				setControlsState();
			} else {
				byte[] d = { 0x00, 0x02 };
				sendCommand(normCmd, d, MsgType.CPower);
				powerOn = false;
				ShowPowerState(powerOn);
			}
		}

		#region Send
		private byte[] MakeMessage(VMessage msg) {
			ushort pLength = (ushort)msg.data.Length;
			byte[] message = new byte[24];

			//Debug.WriteLine($"pType: {msg.payloadType}, SequenceNumber: {SequenceNumber}");
			message[0] = (byte)(((ushort)msg.payloadType >> 8) & 0xFF);
			message[1] = (byte)((ushort)msg.payloadType & 0xFF);
			message[2] = (byte)((pLength >> 8) & 0xFF);
			message[3] = (byte)((pLength) & 0xFF);
			message[4] = (byte)((SequenceNumber >> 24) & 0xFF);
			message[5] = (byte)((SequenceNumber >> 16) & 0xFF);
			message[6] = (byte)((SequenceNumber >> 8) & 0xFF);
			message[7] = (byte)(SequenceNumber & 0xFF);

			for (int i = 0; i < pLength; i++) {
				message[8 + i] = msg.data[i];
			}

			return message;
		}

		private void SendMessage(VMessage msg, string more = "") {
			//responseListAdd($"sendMessage() {msg.type}, {lastMsg}");
			if (lastMsg != MsgType.None) {
				responseListAdd($"QUE: {msg.type.ToString()}");
				msgQueue.Enqueue(msg);
			} else {
				lastMsg = msg.type;
				ushort pLength = (ushort)msg.data.Length;
				Debug.WriteLine($"SendMessage({msg.payloadType.ToString()}, {msg.type.ToString()},f {Convert.ToHexString(msg.data, 0, pLength)})");

				UdpClient client = new UdpClient();

				try {
					IPEndPoint? ep = new IPEndPoint(IPAddress.Parse(cameraAddress), cameraPort); // Destination

					byte[] message = MakeMessage(msg);
					//Debug.WriteLine($"message: {Convert.ToHexString(message)}");

					// Send the message to the specified endpoint
					string msgStr = "";
					foreach (byte b in msg.data) {
						msgStr += b.ToString("X2") + " ";
					}
					string info = $"{msg.type} {more}";
					responseListAdd($"SND: {info.PadRight(20)} - {msgStr}");
					int sendCount = client.Send(message, 8 + pLength, ep);

					// Safely obtain the local port; LocalEndPoint can be null
					if (client.Client.LocalEndPoint is IPEndPoint localEp) {
						int localPort = localEp.Port;
						client.Close();
						msg.seq = SequenceNumber;
						_ = StartListener(localPort, msg); // fire-and-forget as before
					} else {
						Debug.WriteLine("LocalEndPoint is null; cannot start listener.");
						client.Close();
					}

					SequenceNumber++;
				} catch (Exception exc) {
					Debug.WriteLine(exc.ToString());
				} finally {
					client.Close();
				}
			}
		}

		UdpClient? udpClient = null;

		private async Task StartListener(int port, VMessage msg) {
			try {
				Debug.WriteLine($"start listener, port: {port}, {msg.type}, {lastMsg}");
				udpClient = new UdpClient(port);
				while (udpClient != null) {
					//Debug.WriteLine("wait for receive");
					using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
					UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token);
					byte[] bytes = result.Buffer;
					string hex = Convert.ToHexString(bytes);
					//Debug.WriteLine($"received {Convert.ToHexString(bytes)}");
					int len = bytes.Length - 8;
					if ((bytes[0] == 0x01) && (bytes[1] == 0x11)) {
						byte[] reply = new byte[len];
						Array.Copy(bytes, 8, reply, 0, len);
						Debug.WriteLine($"payload: {Convert.ToHexString(reply)}");

						handleResponse(reply);
					}
				}
			} catch (OperationCanceledException) {
				Debug.WriteLine($"---receive timed out {msg.type}, {msg.seq}");
				FinishMessage();
				if (lastMsg == MsgType.IPower) {
					Debug.WriteLine("IPower");
					sendInquiry(MsgType.IPower);
				}
			} catch (Exception exc) {
				Debug.WriteLine(exc.ToString());
			}
		}

		private void FinishMessage() {
			Debug.WriteLine($"Finishing message {lastMsg}");
			if (udpClient != null) {
				udpClient.Close();
				udpClient = null;
			}

			lastMsg = MsgType.None;
			if (!msgQueue.Empty) {
				VMessage msg = new VMessage();
				msgQueue.Dequeue(ref msg);
				SendMessage(msg);
			}
		}

		private void sendCommand(byte cmdByte, byte[] data, MsgType type, string more = "") {
			VMessage msg = new VMessage();
			msg.type = type;
			msg.payloadType = PayloadType.Command;
			int dataLength = data.Length;
			byte[] payload = new byte[dataLength + 4];
			payload[0] = comByte0;
			payload[1] = comByte1;
			payload[2] = cmdByte;
			for (int i = 0; i < dataLength; i++) {
				payload[i + 3] = data[i];
			}
			payload[dataLength + 3] = 0xFF;
			msg.data = payload;
			SendMessage(msg, more);
		}

		private void sendInquiry(MsgType type) {
			VMessage msg = new VMessage();
			msg.type = type;
			msg.payloadType = PayloadType.Command;
			byte[] payload = new byte[] { 0x81, 0x09, 0x04, MessageByte[(int)type], 0xFF };
			if (type == MsgType.IVersion) {
				payload[2] = 0x00;
			}
			msg.data = payload;

			SendMessage(msg);
		}
		#endregion

		#region Receive

		private void handleResponse(byte[] response) {
			string str = "";
			foreach (byte b in response) {
				str += b.ToString("X2") + " ";
			}
			//responseListAdd("Data received: " + str);
			int count = response.Length;
			string receiveString = "";
			if (count == 3) {
				receiveString = handle3ByteResponse(response);
			} else if (count == 4) {
				receiveString = "RCV: " + handle4ByteResponse(response);
			} else if (count == 7) {
				receiveString = "RCV: " + handle7ByteResponse(response);
			} else if (count == 10) {
				receiveString = "RCV: " + handle10ByteResponse(response);
			} else {
				receiveString = "Unknown";
			}
			
			if (receiveString.Length > 0) {
				responseListAdd($"{receiveString.PadRight(25)} - {str}");
			}

			if (lastMsg == MsgType.None) {
				if (!msgQueue.Empty) {
					VMessage msg = new VMessage();
					msgQueue.Dequeue(ref msg);
					SendMessage(msg);
				}
			}
			Debug.WriteLine($"handleResponse() end, {lastMsg}, {receiveString}");
		}

		private async void ShowMessage(string msg) {
			if (messageDialogShowing) {
				responseListAdd("message dialog already showing");
				return;
			}

			messageDialogShowing = true;
			responseListAdd("Show: " + msg);
			try {
				this.DispatcherQueue.TryEnqueue(() => {
					ContentDialog dialog = new ContentDialog {
						Title = "ViscaUI Message",
						Content = msg,
						CloseButtonText = "OK",
						XamlRoot = ContentFrame.XamlRoot // Critical requirement
					};

					dialog.Closed += ContentDialog_Closed;
					_ = dialog.ShowAsync();
				});
			} catch (Exception exc) {
				responseListAdd($"show message exception: {exc}");
			}
		}

		private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args) {
			responseListAdd("Dialog closed");
			messageDialogShowing = false;
		}

		private string handle3ByteResponse(byte[] response) {
			//Debug.WriteLine("handle 3 byte response");
			string rtn = "";
			if ((response[1] & 0xF0) == 0x40) {
				rtn = "ACK: ";
			} else if ((response[1] & 0xF0) == 0x50) {
				rtn = "FIN: ";
				Debug.WriteLine(rtn);
				FinishMessage();
			}

			return rtn;
		}

		private string handle4ByteResponse(byte[] response) {
			string rtn = "";
			Debug.WriteLine($"handle 4 byte: lastMsg: {lastMsg}");
			try {
				if ((response[0] & 0x8F) == 0x80) {
					if ((response[1] & 0xF0) == 0x60) {    // error message
						Debug.WriteLine("Error message received");
						switch (response[2]) {
							case 0x41:
								rtn = "invalid command ";
								FinishMessage();
								break;
							case 0x02:
								rtn = "invalid param/format ";
								FinishMessage();
								break;
							default:
								rtn = "command error ";
								FinishMessage();
								break;
						}
					} else if (response[1] == 0x50) {  // inquiry response
						switch (lastMsg) {
							case MsgType.IPower:
								if (response[2] == 0x02) {
									FinishMessage();
									ShowPowerState(true);
									rtn = "Power On";
									PresetChangeInquiry();
								} else if (response[2] == 0x03) {
									FinishMessage();
									ShowPowerState(false);
									rtn = "Power Off";
								}
								setControlsState();
								break;
							case MsgType.IFocusMode:
								if (response[2] == 0x02) {
									ShowFocusState(true);
									rtn = "Focus Auto";
								} else if (response[2] == 0x03) {
									ShowFocusState(false);
									rtn = "Focus Manual";
								}
								FinishMessage();
								break;
							case MsgType.IAEMode:
								if (response[2] == 0x00) {
									setBrightType(false);
									rtn = "Exposure Auto ";
								} else if (response[2] == 0x0D) {
									setBrightType(true);
									rtn = "Exposure Bright";
								}
								FinishMessage();
								break;
							case MsgType.IBacklightMode:
								if (response[2] == 0x02) {
									expBacklitChk.IsChecked = true;
									rtn = "Backlight On";
								} else if (response[2] == 0x03) {
									expBacklitChk.IsChecked = false;
									rtn = "Backlight Off";
								}
								FinishMessage();
								break;
							case MsgType.IBalanceMode:
								BalanceType bal = BalanceType.Auto;
								switch (response[2]) {
									case 0:
										bal = BalanceType.Auto;
										break;
									case 1:
										bal = BalanceType.Indoor;
										break;
									case 2:
										bal = BalanceType.Outdoor;
										break;
									case 3:
										bal = BalanceType.OnePush;
										break;
									case 4:
										bal = BalanceType.AutoTracing;
										break;
									case 5:
										bal = BalanceType.Manual;
										break;
								}
								setBalanceType(bal);
								rtn = "Balance Mode: " + bal.ToString();
								FinishMessage();
								break;
							case MsgType.IExpCompOn:
								Debug.WriteLine("exp comp return");
								bool on = false;
								switch (response[2]) {
									case 2:
										on = true;
										break;
									case 3:
										on = false;
										break;
								}
								setExpComp(on);
								break;
						}
					}
				}

			} catch (COMException exc) {
				Debug.WriteLine("COMException in handle4ByteResponse: " + exc.Message);
				return rtn;
			} catch (Exception exc) {
				Debug.WriteLine("Exception in handle4ByteResponse: " + exc.Message);
				return rtn;
			}

			return rtn;
		}

		private string handle7ByteResponse(byte[] response) {
			string rtn = "";
			if (response[1] == 0x50) {  // inquiry response
				if (lastMsg == MsgType.IBrightPos) {
					int pos = ((response[4] << 4) | response[5]);
					ShowExposure(pos);
					//expSlider.Value = pos;
					rtn = $"Bright {pos}";
					FinishMessage();
					//expText.Text = IrisStrings[pos] + " " + IrisStrings[pos];
				} else if (lastMsg == MsgType.IBalanceRed) {
					int pos = ((response[4] << 4) | response[5]);
					wbRedSlider.Value = pos;
					rtn = $"Red balance {pos}";
					FinishMessage();
					wbRedText.Text = pos.ToString();
				} else if (lastMsg == MsgType.IBalanceBlue) {
					int pos = ((response[4] << 4) | response[5]);
					wbBlueSlider.Value = pos;
					rtn = $"Blue balance {pos}";
					FinishMessage();
					wbBlueText.Text = pos.ToString();
				} else if (lastMsg == MsgType.IExpCompPos) {
					int pos = ((response[4] << 4) | response[5]);
					ShowExposureComp(pos);
					expCompSlider.Value = pos;
					rtn = $"Exp Comp {pos}";
					FinishMessage();
					expCompText.Text = ExpCompStrings[pos];
				} else if (lastMsg == MsgType.IShutter) {
					int pos = ((response[4] << 4) | response[5]);
					rtn = $"Shutter Speed {pos}";
					FinishMessage();
					//shutterLabel.Text = ShutterStrings[pos];
				} else if (lastMsg == MsgType.IIris) {
					int pos = ((response[4] << 4) | response[5]);
					rtn = $"Iris {pos}";
					FinishMessage();
					//irisLabel.Text = IrisD30Strings[pos];
				} else if (lastMsg == MsgType.IGain) {
					int pos = ((response[4] << 4) | response[5]);
					rtn = $"Gain {pos}";
					FinishMessage();
					//gainLabel.Text = GainD30Strings[pos];
				}
			}

			return rtn;
		}

		private string handle10ByteResponse(byte[] response) {
			string rtn = "";
			//int dev = response[0] & 0x7;
			int vendorId = ((int)response[2]) << 8 + response[3];
			int modelId = ((int)response[4]) << 8 + response[5];
			int versionId = ((int)response[6]) << 8 + response[7];
			rtn = $"V: {vendorId}, M: {modelId} ";

			return rtn;
		}

		private void responseListAdd(string text) {
			this.DispatcherQueue.TryEnqueue(() => {
				responseListBox.Items.Insert(0, text);
				dfo(text);
			});
		}

		private void PresetChangeInquiry() {
			if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.AEFull))) {
				sendInquiry(MsgType.IAEMode);
			}
			if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.FocusFull))) {
				sendInquiry(MsgType.IFocusMode);
			}
			if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.BalanceFull))) {
				sendInquiry(MsgType.IBalanceMode);
			}
		}
		#endregion

		#region Pan Tilt
		private void panTiltStop() {
			byte[] d = { 0x01, panRate, tiltRate, 0x03, 0x03 };
			sendCommand(panTiltCmd, d, MsgType.CPanTilt, "stop");
		}

		private void panTiltStart(byte b6, byte b7) {
			byte[] d = { 0x01, panRate, tiltRate, b6, b7 };
			sendCommand(panTiltCmd, d, MsgType.CPanTilt, $"p {panRate}, t {tiltRate}");
		}

		private void CenterBtnClick(object sender, RoutedEventArgs e) {
			if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.NoCenter))) {
				byte[] d = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
				sendCommand(panTiltCmd, d, MsgType.CPanTilt, "center");
			} else {
				byte[] d = { 0x04 };
				sendCommand(panTiltCmd, d, MsgType.CPanTilt, "center");
			}
		}

		private void ptRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			ptRectDragging = true;
			ptRect_MouseMove(sender, e);
		}

		private void ptRect_MouseUp(object sender, PointerRoutedEventArgs e) {
			ptRectDragging = false;
			panTiltStop();
			lastPanRate = 0;
			lastTiltRate = 0;
		}

		const int ptRectWidth = 140;
		const int ptInc = 10;
		const int ptRectHeight = 120;
		private static readonly int[] panRates = new[] { 0, 1, 3, 6, 10, 15, 24 };
		private static readonly int[] tiltRates = new[] { 0, 1, 3, 6, 10, 1 };

		private void ptRect_MouseMove(object sender, PointerRoutedEventArgs e) {
			if (ptRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(ptRect);
				Point pos = ptrPt.Position;
				double x = Math.Max(Math.Min(pos.X, ptRectWidth), 0) - (ptRectWidth / 2);
				double y = Math.Max(Math.Min(pos.Y, ptRectHeight), 0) - (ptRectHeight / 2);
				double ax = Math.Abs(x);
				double ay = Math.Abs(y);
				int pr = 0;
				int tr = 0;

				int xInd = Math.Min((int)(ax / ptInc), 6);
				int yInd = Math.Min((int)(ay / ptInc), 5);
				pr = panRates[xInd];
				tr = tiltRates[yInd];

				panRate = (byte)pr;
				tiltRate = (byte)tr;

				bool change = false;
				if (lastPanRate != pr) {
					lastPanRate = pr;
					change = true;
				}
				if (lastTiltRate != tr) {
					lastTiltRate = tr;
					change = true;
				}

				if (change) {
					if ((tr == 0) && (pr == 0)) {
						panTiltStop();
					} else {
						byte lr = (byte)((pr == 0) ? 3 : ((x < 0) ? 1 : 2));
						byte ud = (byte)((tr == 0) ? 3 : ((y < 0) ? 1 : 2));
						panTiltStart(lr, ud);
					}
					if (lastPresetNumber != -1) {
						this.DispatcherQueue.TryEnqueue(() => {
							presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Transparent);
							lastPresetNumber = -1;
						});
					}
				}
			}
		}
		#endregion

		#region Zooom
		private void zoomStop() {
			//responseListAdd("zoom stop");
			byte[] d = { 0x07, 0x00 };
			sendCommand(normCmd, d, MsgType.CZoom, "stop");
		}

		private void zoomIn() {
			byte cmd = (byte)(0x20 | zoomRate);
			byte[] d = { 0x07, cmd };
			sendCommand(normCmd, d, MsgType.CZoom, $"in {zoomRate}");
		}

		private void zoomOut() {
			byte cmd = (byte)(0x30 | zoomRate);
			byte[] d = { 0x07, cmd };
			sendCommand(normCmd, d, MsgType.CZoom, $"out {zoomRate}");
		}

		private void zmRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			zmRectDragging = true;
			zmRect_MouseMove(sender, e);
		}

		private void zmRect_MouseUp(object sender, PointerRoutedEventArgs e) {
			zmRectDragging = false;
			zoomStop();
			lastZoomRate = 0;
		}

		const int zoomInc = 12;
		const int zoomRectHeight = 120;
		private static readonly int[] zoomRates = new[] { 0, 1, 2, 4, 7 };

		private void zmRect_MouseMove(object sender, PointerRoutedEventArgs e) {
			if (zmRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(zmRect);
				Point pos = ptrPt.Position;
				double y = Math.Max(Math.Min(pos.Y, zoomRectHeight), 0) - (zoomRectHeight / 2);
				double ay = Math.Abs(y);
				int zr = 0;

				int yInd = Math.Min((int)(ay / zoomInc), 4);
				zr = zoomRates[yInd];

				zoomRate = (byte)zr;

				bool change = false;
				if (lastZoomRate != zr) {
					lastZoomRate = zr;
					change = true;
				}

				if (change) {
					if (zr == 0) {
						zoomStop();
					} else {
						if (y < 0) {
							zoomIn();
						} else {
							zoomOut();
						}
						;
					}
					if (lastPresetNumber != -1) {
						this.DispatcherQueue.TryEnqueue(() => {
							presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Transparent);
							lastPresetNumber = -1;
						});
					}
				}
			}

		}
		#endregion

		#region Focus
		private void ShowFocusState(bool auto) {
			this.DispatcherQueue.TryEnqueue(() => {
				focusManual.IsChecked = !auto;
				focusControlPanel.Visibility = !auto ? Visibility.Visible : Visibility.Collapsed;
			});
		}

		private void setFocusType(bool manual) {
			focusControlPanel.Visibility = manual ? Visibility.Visible : Visibility.Collapsed;
			byte[] d = { 0x38, (byte)(manual ? 0x03 : 0x02)	 };
			sendCommand(normCmd, d, MsgType.CFocusMode, $"{(manual ? "manual" : "auto")}");
		}

		private void FarBtnClick(object sender, RoutedEventArgs e) {
			byte[] d = { 0x08, 0x30 };
			sendCommand(normCmd, d, MsgType.CFocus, $"far ");
		}

		private void NearBtnClick(object sender, RoutedEventArgs e) {
			byte[] d = { 0x08, 0x20 };
			sendCommand(normCmd, d, MsgType.CFocus, $"near ");
		}

		private void focusStop() {
			responseListAdd("focus stop");
			byte[] d = { 0x08, 0x00 };
			sendCommand(normCmd, d, MsgType.CFocus, "stop");
		}

		private void focusIn() {
			byte cmd = (byte)(0x30 | focusRate);
			byte[] d = { 0x08, cmd };
			sendCommand(normCmd, d, MsgType.CFocus, $"in {focusRate}");
		}

		private void focusOut() {
			byte cmd = (byte)(0x20 | focusRate);
			byte[] d = { 0x08, cmd };
			sendCommand(normCmd, d, MsgType.CFocus, $"out {focusRate}");
		}

		private void FocusManualClick(object sender, RoutedEventArgs e) {
			setFocusType(focusManual.IsChecked == true);
		}

		private void focusRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			focusRectDragging = true;
			focusRect_MouseMove(sender, e);
		}
		private void focusRect_MouseUp(object sender, PointerRoutedEventArgs e) {
			focusRectDragging = false;
			focusStop();
			lastFocusRate = 0;
		}

		const int focusInc = 9;
		const int focusRectHeight = 90;
		private static readonly int[] focusRates = new[] { 0, 1, 2, 4, 7 };

		private void focusRect_MouseMove(object sender, PointerRoutedEventArgs e) {
			if (focusRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(focusRect);
				Point pos = ptrPt.Position;
				double y = Math.Max(Math.Min(pos.Y, focusRectHeight), 0) - (focusRectHeight / 2);
				double ay = Math.Abs(y);
				int fr = 0;

				int yInd = Math.Min((int)(ay / focusInc), 4);
				fr = focusRates[yInd];

				if (ay >= 10) {
					fr = (int)((Math.Log10(ay) - 1.0) * 6);
				}

				focusRate = (byte)fr;

				bool change = false;
				if (lastFocusRate != fr) {
					lastFocusRate = fr;
					change = true;
				}

				if (change) {
					if (fr == 0) {
						focusStop();
					} else {
						if (y < 0) {
							focusIn();
						} else {
							focusOut();
						}
					}
				}
			}
		}

		#endregion

		#region Camera
		private void CameraChecked(object sender, RoutedEventArgs e) {
			RadioButton? rb = sender as RadioButton;
			if (rb != null && rb.IsChecked == true) {
				string? str = rb.Tag.ToString();
				cameraNum = (str is not null) ? int.Parse(str) : 1;
				string[] camAry = Config.GetCamera(cameraNum);
				if (camAry.Length > 1) {
					cameraAddress = camAry[0];
					cameraPort = int.Parse(camAry[1]);
					cameraModel = camAry[3];
					Debug.WriteLine($"Camera {cameraNum} selected: {cameraModel}, {cameraAddress}:{cameraPort}");
				}
			}
		}

		#endregion

		#region Presets
		private void PresetDown(object sender, PointerRoutedEventArgs e) {
			Debug.WriteLine("PresetDown");
			lastPreset = (Button)sender;
			string? lpStr = lastPreset.Content.ToString();
			lastPresetNumber = (lpStr is not null) ? int.Parse(lpStr) - 1 : 0;
			settingPreset = false;
			presetTimer = new System.Timers.Timer(1000);
			presetTimer.Elapsed += PresetTimer_Tick;
			presetTimer.Enabled = true;
			Debug.WriteLine("PresetDown exit");
		}

		private void PresetUp(object sender, PointerRoutedEventArgs e) {
			Debug.WriteLine("PresetUp");
			if (presetTimer is not null) {
				presetTimer.Stop();
				presetTimer.Dispose();
			}
			lastPreset = null;
			lastPresetNumber = -1;
			string? btnStr = ((Button)sender).Content.ToString();
			if (btnStr is not null) {
				int btnNbr = int.Parse(btnStr) - 1;
				handlePreset((byte)(btnNbr), presetTimer);
				presetButtons[btnNbr].Background = new SolidColorBrush(Colors.Azure);
				presetButtons[btnNbr].Foreground = new SolidColorBrush(Colors.Black);
				Debug.WriteLine("after colors set");
			}
			Debug.WriteLine("PresetUp exit");
		}

		private void PresetTimer_Tick(object? sender, object e) {
			Debug.WriteLine("PresetTick");
			settingPreset = true;
			this.DispatcherQueue.TryEnqueue(() => {
				if (lastPreset is not null) {
					presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Red);
					Debug.WriteLine("preset timer set red");
				}
			});
			if (presetTimer is not null) {
				presetTimer.Stop();
				presetTimer.Dispose();
			}
			Debug.WriteLine("PresetTick exit");
		}

		private void handlePreset(byte number, System.Timers.Timer? presetTimer1) {
			Debug.WriteLine("handlePreset");
			this.DispatcherQueue.TryEnqueue(() => {
				for (int i = 0; i < 6; i++) {
					presetPanels[i].Background = new SolidColorBrush(Colors.Transparent);
				}
				presetButtons[number].Background = new SolidColorBrush(Colors.Azure);
				presetButtons[number].Foreground = new SolidColorBrush(Colors.Black);
				presetPanels[number].Background = new SolidColorBrush(Colors.Maroon);
			});

			byte[] d = { 0x3F, 0x01, number };
			d[1] = settingPreset ? (byte)0x01 : (byte)0x02;
			sendCommand(normCmd, d, MsgType.CMemory, $"{(settingPreset ? "set" : "recall")} {number + 1}");

			if (presetTimer1 is not null) {
				presetTimer1.Enabled = false;
			}
			lastPreset = null;
			lastPresetNumber = number;

			if (!settingPreset) {
				PresetChangeInquiry();
			}

			settingPreset = false;
			Debug.WriteLine("handlePreset exit");
		}

		private void PresetTextChanged(object sender, RoutedEventArgs e) {
			TextBox? tb = sender as TextBox;
			if (tb != null) {
				int index = presetTexts.IndexOf(tb);
				if (index >= 0) {
					Config.SetPreset(cameraNum, (uint)index, tb.Text);
				}
			}
		}
		#endregion

		#region Exposure
		private void ExpManualClick(object sender, RoutedEventArgs e) {
			if (expManualChk.IsChecked == true) {
				gainUpBtn.IsEnabled = true;
				gainDownBtn.IsEnabled = true;
				byte[] d = { 0x39, 0x00 };
				sendCommand(normCmd, d, MsgType.CExposure, "manual");
			} else {
				gainUpBtn.IsEnabled = false;
				gainDownBtn.IsEnabled = false;
				byte[] d = { 0x39,  0x03 };
				sendCommand(normCmd, d, MsgType.CExposure, "auto");
			}
		}

		private void GainUp(object sender, RoutedEventArgs e) {
			byte[] d = { 0x0C, 0x02 };
			sendCommand(normCmd, d, MsgType.CGainUpDn, "up");
		}

		private void GainDown(object sender, RoutedEventArgs e) {
			byte[] d = { 0x0C, 0x03 };
			sendCommand(normCmd, d, MsgType.CGainUpDn, "down");
		}

		private void ExpBrightClick(object sender, RoutedEventArgs e) {
			setBrightType(expBrightChk.IsChecked == true);
		}

		private void displayBrightMode(bool manual) {
			this.DispatcherQueue.TryEnqueue(() => {
				expBrightChk.IsChecked = manual;
				//expSlider.IsEnabled = manual;
				//brightBtn.Enabled = manual;
				//darkBtn.Enabled = manual;
				expBacklitChk.IsEnabled = !manual;
			});
		}

		private void ShowExposure(int pos) {
			this.DispatcherQueue.TryEnqueue(() => {
				//expSlider.Value = pos;
				//expText.Text = $"{IrisStrings[pos]}-{GainStrings[pos]}";
			});
		}

		private void setBrightType(bool manual) {
			displayBrightMode(manual);

			if (manual) {
				setExpComp(false);
			} else {
				ShowExposure(0);
			}

			byte[] d = {0x39, (byte)(manual ? 0x0D : 0x00) };
			sendCommand(normCmd, d, MsgType.CBright);

			if (!manual) {
				sendInquiry(MsgType.IBacklightMode);
			} else {
				sendInquiry(MsgType.IBrightPos);
				sendInquiry(MsgType.IShutter);
			}
		}

		private void ExpSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (lastMsg == MsgType.None) {
				int value = (int)e.NewValue;
				byte p = (byte)((value >> 4) & 1);
				byte q = (byte)(value & 0x0F);
				byte[] d = { 0x4D, 0x00, 0x00, p, q };
				sendCommand(normCmd, d, MsgType.CBright);
				ShowExposure(value);
			}
		}

		private void ExpBacklitClick(object sender, RoutedEventArgs e) {
			bool? chk = expBacklitChk.IsChecked;
			if (chk.HasValue) {
				setBacklight(chk.Value);
			}
		}

		private void setBacklight(bool on) {
			if (lastMsg == MsgType.None) {
				byte[] d = { 0x33, (byte)(on ? 0x02 : 0x03) };
				sendCommand(normCmd, d, MsgType.CBacklight);
			}
		}

		private void ExpCompClick(object sender, RoutedEventArgs e) {
			bool? chk = expCompChk.IsChecked;
			if (chk.HasValue) {
				setExpComp(chk.Value);
			}
		}

		private void setExpComp(bool on) {
			displayExpComp(on);

			byte[] d = {0x3E, (byte)(on ? 0x02 : 0x03) };
			sendCommand(normCmd, d, MsgType.CExpCompOn);

			if (on) {
				sendInquiry(MsgType.IExpCompPos);
			}
		}

		private void displayExpComp(bool on) {
			this.DispatcherQueue.TryEnqueue(() => {
				expCompChk.IsChecked = on;
				expCompSlider.IsEnabled = on;
			});
		}

		private void ShowExposureComp(int pos) {
			this.DispatcherQueue.TryEnqueue(() => {
				expCompSlider.Value = pos;
				expCompText.Text = ExpCompStrings[pos];
			});
		}

		private void ExpCompSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			int value = (int)e.NewValue;
			byte p = 0;
			byte q = (byte)(value & 0x0f);
			byte[] d = { 0x4E, 0x00, 0x00, p, q };
			sendCommand(normCmd, d, MsgType.CExpCompPos);
			ShowExposureComp(value);
		}

		private void ExpCompUp(object sender, RoutedEventArgs e) {
			byte[] d = { 0x0E, 0x02 };
			sendCommand(normCmd, d, MsgType.CExpCompUpDn, "up");
		}

		private void ExpCompDown(object sender, RoutedEventArgs e) {
			byte[] d = { 0x0E, 0x03 };
			sendCommand(normCmd, d, MsgType.CExpCompUpDn, "down");
		}
		#endregion

		#region White Balance
		private void balanceSetup() {
			bool wbFull = (modelFeatures != null) && (modelFeatures.Contains(ControlFeature.WBFull));
			wbSelectCombo.Items.Clear();
			wbSelectCombo.Items.Add("Auto");
			if (wbFull) {
				wbSelectCombo.Items.Add("Indoor");
				wbSelectCombo.Items.Add("Outdoor");
			}
			wbSelectCombo.Items.Add("One Push");
			if (wbFull) {
				wbSelectCombo.Items.Add("Auto Tracing");
			}
			wbSelectCombo.Items.Add("Manual");

			wbSelectCombo.SelectedIndex = 0;
		}

		private void setBalanceType(BalanceType balance) {
			balanceType = balance;

			if (wbSelectCombo.Items.Count > 0) {
				//wbSelectCombo.SelectedIndex = (int)balance;
				wbTriggerBtn.IsEnabled = (balance == BalanceType.OnePush);
				bool manual = (balance == BalanceType.Manual);
				bool wbAuto = (balance == BalanceType.Auto);
				bool onePush = (balance == BalanceType.OnePush);

				wbRedSlider.IsEnabled = manual;
				wbBlueSlider.IsEnabled = manual;

				if (manual) {
					byte[] d = { 0x35, 0x05 };
					sendCommand(normCmd, d, MsgType.CBalanceMode, "manual");
					if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.WBFull))) {
						wbRedButtonStack.Visibility = Visibility.Collapsed;
						wbBlueButtonStack.Visibility = Visibility.Collapsed;
						wbRedSliderStack.Visibility = Visibility.Visible;
						wbBlueSliderStack.Visibility = Visibility.Visible;
					} else {
						wbRedSliderStack.Visibility = Visibility.Collapsed;
						wbBlueSliderStack.Visibility = Visibility.Collapsed;
						wbRedButtonStack.Visibility = Visibility.Visible;
						wbBlueButtonStack.Visibility = Visibility.Visible;
					}
				} else {
					if (wbAuto) {
						byte[] d = { 0x35, 0x00 };
						sendCommand(normCmd, d, MsgType.CBalanceMode, "auto");
					} else if (onePush) {
						byte[] d = { 0x35, 0x03 };
						sendCommand(normCmd, d, MsgType.CBalanceMode, "one push");
					}
					wbRedSliderStack.Visibility = Visibility.Collapsed;
					wbBlueSliderStack.Visibility = Visibility.Collapsed;
					wbRedButtonStack.Visibility = Visibility.Collapsed;
					wbBlueButtonStack.Visibility = Visibility.Collapsed;
				}
			} else {
				wbRedSliderStack.Visibility = Visibility.Collapsed;
				wbBlueSliderStack.Visibility = Visibility.Collapsed;
				wbRedButtonStack.Visibility = Visibility.Collapsed;
				wbBlueButtonStack.Visibility = Visibility.Collapsed;
			}
		}

		private void BalTriggerBtnClick(object sender, RoutedEventArgs e) {
			byte[] d = {0x10, 0x05 };
			sendCommand(normCmd, d, MsgType.CBalanceTrigger);
		}

		private void ShowGainText(TextBox text, int value) {
			this.DispatcherQueue.TryEnqueue(() => {
				if (text != null) {
					text.Text = value.ToString();
				}
			});
		}

		private void BalRedSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (loaded) {
				int value = (int)e.NewValue;

				byte p = (byte)(value >> 4);
				byte q = (byte)(value & 0x0f);
				byte[] d = { 0x43, 0x00, 0x00, p, q };
				sendCommand(normCmd, d, MsgType.CBalanceRed);
				//ShowGainText(wbRedText, value);
			}
		}

		private void BalBlueSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (loaded) {
				int value = (int)e.NewValue;
				byte p = (byte)(value >> 4);
				byte q = (byte)(value & 0x0f);
				byte[] d = { 0x44, 0x00, 0x00, p, q };
				sendCommand(normCmd, d, MsgType.CBalanceBlue);
				//ShowGainText(wbBlueText, value);
			}
		}

		private void BalSelectChanged(object sender, SelectionChangedEventArgs e) {
			if (loaded) {
				BalanceType typ = BalanceType.Auto;
				string? bal = wbSelectCombo.SelectedItem as string;
				for (int i = 0; i < BalanceStrings.Length; i++) {
					if (bal == BalanceStrings[i]) {
						typ = (BalanceType)i;
					}
				}

				setBalanceType(typ);
			}
		}

		private void RedGainUp(object sender, RoutedEventArgs e) {
			byte[] d = { 0x03, 0x02 };
			sendCommand(normCmd, d, MsgType.CBalanceRed);
		}

		private void RedGainDown(object sender, RoutedEventArgs e) {
			byte[] d = { 0x03, 0x03 };
			sendCommand(normCmd, d, MsgType.CBalanceRed);
		}

		private void BlueGainUp(object sender, RoutedEventArgs e) {
			byte[] d = { 0x04, 0x02 };
			sendCommand(normCmd, d, MsgType.CBalanceBlue);
		}

		private void BlueGainDown(object sender, RoutedEventArgs e) {
			byte[] d = { 0x04, 0x03 };
			sendCommand(normCmd, d, MsgType.CBalanceBlue);
		}

		#endregion
	}
}
