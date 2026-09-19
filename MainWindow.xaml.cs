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
using System.Reflection;
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
		enum BalanceType { Auto, Indoor, Outdoor, OnePush, AutoTrace, Manual }
		static BalanceType balanceType = BalanceType.Auto;

		readonly static Dictionary<BalanceType, string> balanceStrMap = new() {
			{ BalanceType.Auto, "Auto" },
			{ BalanceType.Indoor, "Indoor" },
			{ BalanceType.Outdoor, "Outdoor" },
			{ BalanceType.OnePush, "One Push" },
			{ BalanceType.AutoTrace, "Auto Trace" },
			{ BalanceType.Manual, "Manual" }
		};

		readonly static Dictionary<BalanceType, byte> balanceCmdMap = new() {
			{ BalanceType.Auto, 0x00 },
			{ BalanceType.Indoor, 0x01 },
			{ BalanceType.Outdoor, 0x02 },
			{ BalanceType.OnePush, 0x03 },
			{ BalanceType.AutoTrace, 0x04 },
			{ BalanceType.Manual, 0x05 }
		};

		readonly static Dictionary<MessageType, byte> msgByteMap = new() {
			{ MessageType.MSG_Command, 0x01 },
			{ MessageType.MSG_Inquiry, 0x09 }
		};
		readonly static Dictionary<CommandType, byte> cmdByteMap = new() {
			{ CommandType.None, 0x00 },
			{ CommandType.CMD_IFClear, 0x00 },
			{ CommandType.CMD_Power, 0x00 },
			{ CommandType.CMD_PanTilt, 0x01 },
			{ CommandType.CMD_PanTiltRel, 0x03 },
			{ CommandType.CMD_PanTiltHome, 0x04 },
			{ CommandType.CMD_PanTiltDirect, 0x02 },

			{ CommandType.CMD_Zoom, 0x07 },
			{ CommandType.CMD_Memory, 0x3F },
			{ CommandType.CMD_Focus, 0x08 },
			{ CommandType.CMD_FocusMode, 0x38 },
			{ CommandType.CMD_ExposureMode, 0x39 },
			{ CommandType.CMD_ExposurePos, 0x4D },
			{ CommandType.CMD_Backlight, 0x33 },
			{ CommandType.CMD_ExpCompOn, 0x3E },
			{ CommandType.CMD_ExpCompPos, 0x4E },
			{ CommandType.CMD_BalanceMode, 0x35 },
			{ CommandType.CMD_BalanceRed, 0x43 },
			{ CommandType.CMD_BalanceBlue, 0x44 },
			{ CommandType.CMD_ExpGain, 0x0C },
			{ CommandType.CMD_ExpCompUpDn, 0x0E },
			{ CommandType.CMD_RedUpDn, 0x03 },
			{ CommandType.CMD_BlueUpDn, 0x04 },

			{ CommandType.INQ_Power, 0x00 },
			{ CommandType.INQ_FocusMode, 0x38 },
			{ CommandType.INQ_AEMode, 0x39 },
			{ CommandType.INQ_BrightPos, 0x4D },
			{ CommandType.INQ_BacklightMode, 0x33 },
			{ CommandType.INQ_Memory, 0x3F },
			{ CommandType.INQ_BalanceMode, 0x35 },
			{ CommandType.INQ_BalanceRed, 0x43 },
			{ CommandType.INQ_BalanceBlue, 0x44 },
			{ CommandType.INQ_ExpCompOn, 0x3E },
			{ CommandType.INQ_ExpCompPos, 0x4E },
			{ CommandType.INQ_DeviceType, 0x02 },
			{ CommandType.INQ_PanTiltPos, 0x12 }
		};

		readonly Dictionary<int, string> vendorMap = new() {
			{ 0x01, "Sony" },
			{ 0x03, "Everet" },
			{ 0x10, "Sony" },
			{ 0x20, "Canon" },
			{ 0x30, "Panasonic" },
			{ 0x220, "Datavideo" },
			{ 0x800, "Lumens" },
			{ 0x0F0F, "GNT" },
			{ 0x2574, "AVer" }
		};

		readonly Dictionary<int, string> sonyModelMap = new() {
			{ 0x400, "EVI-D30" },
			{ 0x401, "EVI-D100" },
			{ 0x403, "EVI-D70" },
			{ 0x404, "EVI-D70" },
			{ 0x40D, "EVI-D100" },
			{ 0x40E, "EVI-D70" },
			{ 0x504, "EVI-HD1" },
			{ 0x505, "EVI-HD3" },
			{ 0x507, "EVI-HD7" },
			{ 0x514, "EVI-H100" }
		};
		readonly Dictionary<int, string> averModelMap = new() {
			{ 0x559, "MD330" },
			{ 0x565, "MD120" },
			{ 0x500, "PTZ210" },
			{ 0x510, "PTC500" },
			{ 0x505A, "CAM520" }
		};

		readonly uint NoWbManual = 0x01;
		readonly	uint NoBrightDirect = 0x02;
		readonly uint NoFocusRate = 0x04;
		readonly uint NoPanTiltCenter = 0x08;
		readonly uint NoWbInOut = 0x10;
		readonly uint NoExpCompDirect = 0x20;

		//readonly string[] IrisStrings = { " --", "F22", "F19", "F16", "F14", "F11", "F9.6", "F8.0", "F6.8", "F5.6", "F4.8",
		//								 "F4.0", "F3.4", "F2.8", "F2.4", "F2.0", "F1.6", "F1.4", "F1.4", "F1.4", "F1.4",
		//								 "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4", "F1.4",
		//								 "F1.4", "F1.4" };
		//string[] GainStrings = { "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB",
		//								 "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "0 dB", "2 dB", "4 dB",
		//								 "6 dB", "8 dB", "10 dB", "12 dB", "14 dB", "16 dB", "18 dB", "20 dB", "22 dB", "24 dB",
		//								 "26 dB", "28 dB" };
		//string[] IrisD30Strings = { " --", "F28", "F22", "F19", "F16", "F14", "F11", "F9.6", "F8.0", "F6.8", "F5.6", "F4.8",
		//								 "F4.0", "F3.4", "F2.8", "F2.4", "F2.0", "F1.8" };
		//string[] GainD30Strings = { "-3 dB", "0 dB", "3 dB", "6 dB", "9 dB", "12 dB", "15 dB", "18 dB", "21 dB", "24 dB", "27 dB",
		//									 "30 dB", "33 dB", "36 dB", "39 dB", "42 dB", "45 dB" };
		//string[] ExpCompStrings = { "-10.5 dB", "-9 dB", "-7.5 dB", "-6 dB", "-4.5 dB", "-3 dB", "-1.5 dB", "0 dB",
		//									 "1.5 dB", "3 dB", "4.5 dB", "6 dB", "7.5 dB", "9 dB", "10.5 dB" };
		//string[] ShutterStrings = { "1/1", "1/2", "1/4", "1/8", "1/15", "1/30", "1/60", "1/90", "1/100", "1/125",
		//									 "1/180", "1/250", "1/350", "1/500", "1/725", "1/1000", "1/1500", "1/2000", "1/3000", "1/4000",
		//									 "1/6000", "1/10000" };

		enum ControlFeature { ExpCompFull, AEFull, BrightFull, BalanceFull, FocusFull, ShutterFull, GainFull, WBFull, NoCenter }
		#endregion
		#region Class Variables
		//Dictionary<string, ControlFeature[]> CameraFeatures = new Dictionary<string, ControlFeature[]>();
		//CameraFeatures.Add(Config.CameraModelStrs[0], new ControlFeature[] { ControlFeature.AEFull });
		//Dictionary<string, ControlFeature[]> cameraFeatures = new Dictionary<string, ControlFeature[]> {
		//	{ Config.CameraModelStrs[0], new ControlFeature[] { ControlFeature.NoCenter } },
		//	{ Config.CameraModelStrs[1], new ControlFeature[] { ControlFeature.ExpCompFull, ControlFeature.NoCenter } }
		//};

		uint SequenceNumber = 0;

		readonly	static MsgQueue msgQueue = new();
		static CommandType lastCmdType = CommandType.None;
		//static DateTime lastSend = DateTime.Now;
		//static int lastMsgNum = 0;

		public bool keepReading = false;

		static int cameraCount = 0;
		static uint deviceId = 1;
		static bool powerOn = true;
		readonly static List<DeviceInfo> deviceInfos = [];
		static int cameraNum = 1;
		static string cameraAddress = "";
		static int cameraPort = 0;
		static string cameraModel = "";
		readonly static ControlFeature[]? modelFeatures = [];
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
		static int lastFocusYInd = 0;

		static bool loggingOn = false;
		static bool debugOn = false;
		static bool loaded = false;
		static bool inBalanceSetup = false;
		static bool inDisplayBright = false;

		Microsoft.UI.Windowing.AppWindow? appWindow = null;

		#endregion

		#region UI Elements

		public List<Button> presetButtons = [];
		public List<TextBox> presetTexts = [];
		public List<Panel> presetPanels = [];
		public List<RadioButton> deviceButtons = [];
		//public List<TextBox> cameraTexts = [];
		//public List<Button> ptzButtons = [];
		public List<Control> focusControls = [];
		public List<Control> exposureControls = [];

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

		public void DFO(string txt) {
			if (loggingOn) {
#pragma warning disable CS4014
				SimpleLogger.LogAsync(txt);
#pragma warning restore CS4014
			}

			if (debugOn) {
				ResponseListAdd(txt);
			}
		}

		public MainWindow() {
			try {
				InitializeComponent();
			Title = $"ViscaIP v{Assembly.GetExecutingAssembly().GetName().Version}";

				AppWindow.SetIcon("camera.ico");

			Init();
			} catch (FileNotFoundException exc) {
				Debug.WriteLine($"MainWindow()  file not found exception: {exc}");
			} catch (Exception exc) {
				Debug.WriteLine($"MainWindow()  unexpected exception: {exc}");
			}
		}

		private void ContentLoaded(object sender, RoutedEventArgs e) {
			try {
				SetControlsState();

				loaded = true;

				//SendInquiry(CommandType.INQ_Power);
				SendInquiry(CommandType.INQ_DeviceType);
			} catch (FileNotFoundException exc) {
				Debug.WriteLine($"ContentLoaded()  file not found exception: {exc}");
			} catch (Exception exc) {
				Debug.WriteLine($"ContentLoaded()  unexpected exception: {exc}");
			}
		}

		private void Init() {
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

				loggingOn = Config.Log;
				debugOn = Config.Debug;

				for (uint i = 1; i < 8; i++) {
					string[] camAry = Config.GetCamera(i);
					if ((camAry.Length > 0) && (camAry[0] != "")) {
						DeviceInfo info = new() {
							url = camAry[0],
							port = camAry[1],
							name = camAry[2],
							vendor = camAry[3],
							model = camAry[4],
							restrict = 0
						};

						if (info.model == "AVer CAM520") {
							info.restrict = NoWbInOut | NoBrightDirect | NoFocusRate | NoPanTiltCenter | NoExpCompDirect;
						}

						deviceInfos.Add(info);
						cameraCount++;
						if (firstCamera == 0) {
							firstCamera = (int)i;
						}
					}
				}

				DFO("start");

				deviceButtons = [C1, C2, C3, C4, C5, C6, C7];
				presetButtons = [p1Btn, p2Btn, p3Btn, p4Btn, p5Btn, p6Btn];
				presetTexts = [p1TextBox, p2TextBox, p3TextBox, p4TextBox, p5TextBox, p6TextBox];
				presetPanels = [p1Panel, p2Panel, p3Panel, p4Panel, p5Panel, p6Panel];

				ShowPowerState(powerOn);
				ShowCameras();
				ShowFocusState(true);

				RadioButton btn = deviceButtons[firstCamera - 1];
				btn.IsChecked = true;

				foreach (Button b in presetButtons) {
					b.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler(PresetDown), handledEventsToo: true);
					b.AddHandler(UIElement.PointerReleasedEvent, new PointerEventHandler(PresetUp), handledEventsToo: true);
				}

				string[] presets = Config.GetPresets(deviceId - 1);
				for (int i = 0; i < presets.Length; i++) {
					presetTexts[i].Text = presets[i];
				}

				//string modelName = cameraSettings[3];
				string modelName = deviceInfos[firstCamera - 1].model;
				modelTextBox.Text = modelName;

				//cameraFeatures.TryGetValue(modelName, out modelFeatures);

				//if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.FocusFull))) {
				//	focusButtonStack.Visibility = Visibility.Collapsed;
				//	focusRect.Visibility = Visibility.Visible;
				//} else {
				//	focusRect.Visibility = Visibility.Collapsed;
				//	focusButtonStack.Visibility = Visibility.Visible;
				//}
				//if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.ExpCompFull))) {
				//	expCompButtonStack.Visibility = Visibility.Collapsed;
				//} else {
				//	expCompSlideStack.Visibility = Visibility.Collapsed;
				//	gainUpBtn.IsEnabled = false;
				//	gainDownBtn.IsEnabled = false;
				//}

				//if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.AEFull))) {
				//	gainButtonStack.Visibility = Visibility.Collapsed;
				//} else {
				//	expCompManStack.Visibility = Visibility.Collapsed;
				//	expSlideStack.Visibility = Visibility.Collapsed;
				//}

				//if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.BrightFull))) {
				//	//k.Visibility = Visibility.Collapsed;
				//} else {
				//	expBrightChk.Visibility = Visibility.Collapsed;
				//}

				//if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.WBFull))) {
				//	wbRedButtonStack.Visibility = Visibility.Collapsed;
				//	wbBlueButtonStack.Visibility = Visibility.Collapsed;
				//} else {
				//	wbRedSliderStack.Visibility = Visibility.Collapsed;
				//	wbBlueSliderStack.Visibility = Visibility.Collapsed;
				//}

				//focusControlPanel.Visibility = Visibility.Collapsed;

			} catch (FileNotFoundException exc) {
				Debug.WriteLine($"init()  file not found exception: {exc}");
			} catch (Exception exc) {
				Debug.WriteLine($"init()  unexpected exception: {exc}");
			}
			DFO("init");
		}

		private void ShowCameras() {
			for (uint i = 0; i < 7; i++) {
				if (i < cameraCount) {
					deviceButtons[(int)i].Visibility = Visibility.Visible;
					deviceButtons[(int)i].Content = deviceInfos[(int)i].name;
					if (firstCamera == 0) {
						firstCamera = (int)i;
					}
				} else {
					deviceButtons[(int)i].Visibility = Visibility.Collapsed;
				}
				//string[] camAry = Config.GetCamera(i);
				//if ((camAry.Length > 0) && (camAry[0] != "")) {
				//	deviceButtons[(int)i - 1].Content = camAry[2];
				//	cameraCount++;
				//	if (firstCamera == 0) {
				//		firstCamera = (int)i;
				//	}
				//} else {
				//	deviceButtons[(int)i - 1].Visibility = Visibility.Collapsed;
				//}
			}
		}

		private void SetControlsState() {
			this.DispatcherQueue.TryEnqueue(() => {

				foreach (Button b in presetButtons) {
					b.IsEnabled = true;
				}

				foreach (TextBox t in presetTexts) { 
					t.IsEnabled = true;
				}

				DisplayBrightMode(false);
				DisplayExpComp(false);
				BalanceSetup();
				ExposureSetup();
			});
		}

		private void ShowPowerState(bool on) {
			try {
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
			bool debug = Config.Debug;
			SettingsDialog dialog = new() { XamlRoot = this.Content.XamlRoot }; // Required for WinUI 3
			await dialog.ShowAsync();

			if (debug != Config.Debug) {
				appWindow?.Resize(new Windows.Graphics.SizeInt32(510, Config.Debug ? 850 : 510));
			}

			ShowCameras();
		}

		private void PowerClick(object sender, RoutedEventArgs e) {
			if (!powerOn) {
				byte[] d = [ 0x02 ];
				SendCommand(CommandType.CMD_Power, d, "on");
				powerOn = true;
				ShowPowerState(powerOn);
				SetControlsState();
			} else {
				byte[] d = [ 0x03];
				SendCommand(CommandType.CMD_Power, d, "off");
				powerOn = false;
				ShowPowerState(powerOn);
			}
		}

		#region Send
		private static List<byte> MakeMessage(VMessage msg) {
			List<byte> msgList = [];
			ushort pLength = (ushort)(msg.data.Length + 4);

			msgList.Add((byte)(((ushort)msg.payloadType >> 8) & 0xFF));
			msgList.Add((byte)(((ushort)msg.payloadType & 0xFF)));
			msgList.Add((byte)((pLength >> 8) & 0xFF));	
			msgList.Add((byte)((pLength & 0xFF)));
			msgList.Add((byte)((msg.sequence >> 24) & 0xFF));
			msgList.Add((byte)((msg.sequence >> 16) & 0xFF));
			msgList.Add((byte)((msg.sequence >> 8) & 0xFF));
			msgList.Add((byte)((msg.sequence & 0xFF)));
			
			msgList.Add(0x81);
			msgList.Add(msgByteMap[msg.msgType]);
			byte typeByte = normCmd;
			if ((msg.cmdType == CommandType.CMD_PanTilt)
				|| (msg.cmdType == CommandType.CMD_PanTiltRel)
				|| (msg.cmdType == CommandType.CMD_PanTiltHome)
				|| (msg.cmdType == CommandType.CMD_PanTiltDirect)
				|| (msg.cmdType == CommandType.INQ_PanTiltPos)) {
				typeByte = 0x06;
			} else if (msg.cmdType == CommandType.INQ_DeviceType) {
				typeByte = 0x00;
			}
			msgList.Add(typeByte);
			msgList.Add(cmdByteMap[msg.cmdType]);
			msgList.AddRange(msg.data);
			msgList.Add(0xFF);

			//SequenceNumber++;

			return msgList;
		}

		private void SendMessage(ref VMessage msg) {
			if (lastCmdType == CommandType.None) {
				SequenceNumber++;
				msg.sequence = SequenceNumber;
				List<byte> msgBytes = MakeMessage(msg);
				Send(msg, msgBytes);
			} else {
				DFO($"QUE: {msg.cmdType}");
				msgQueue.Enqueue(msg);
			}
		}

		private void Send(VMessage msg, List<byte> msgBytes) {
			UdpClient client = new();

			try {
				IPEndPoint? ep = new(IPAddress.Parse(cameraAddress), cameraPort); // Destination

				byte[] msgAry = [..msgBytes];
					// Send the message to the specified endpoint
					string msgStr = "";
				for (int i = 8; i < msgBytes.Count; i++) {
					msgStr += msgBytes[i].ToString("X2") + " ";
				}
				string info = $"{msg.cmdType} {msg.comment}";
					
				DFO($"SND: {info, -20} - {msgStr}");
				int sendCount = client.Send(msgAry, msgBytes.Count, ep);
				lastCmdType = msg.cmdType;

				// Safely obtain the local port; LocalEndPoint can be null
				if (client.Client.LocalEndPoint is IPEndPoint localEp) {
					int localPort = localEp.Port;
					client.Close();
					_ = StartListener(localPort, msg); // fire-and-forget as before
				} else {
					Debug.WriteLine("LocalEndPoint is null; cannot start listener.");
					client.Close();
				}

				//SequenceNumber++;
			} catch (Exception exc) {
				Debug.WriteLine(exc.ToString());
			} finally {
				client.Close();
			}
		}

		UdpClient? udpClient = null;

		private async Task StartListener(int port, VMessage msg) {
			try {
				Debug.WriteLine($"start listener, port: {port}, {msg.cmdType}, {lastCmdType}");
				udpClient = new UdpClient(port);
				while (udpClient != null) {
					//Debug.WriteLine("wait for receive");
					using CancellationTokenSource cts = new(TimeSpan.FromSeconds(3));
					UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token);
					byte[] bytes = result.Buffer;
					string hex = Convert.ToHexString(bytes);
					//Debug.WriteLine($"received {Convert.ToHexString(bytes)}");
					int len = bytes.Length - 8;
					if ((bytes[0] == 0x01) && (bytes[1] == 0x11)) {
						byte[] reply = new byte[len];
						Array.Copy(bytes, 8, reply, 0, len);
						HandleResponse(reply);
					}
				}
			} catch (OperationCanceledException) {
				DFO($"---receive timed out {msg.cmdType}, {msg.sequence}");
				FinishMessage();
				//if (lastCmdType == CommandType.CMD_Power) {
				//	Debug.WriteLine("IPower");
				//	SendInquiry(CommandType.INQ_Power);
				//}
			} catch (Exception exc) {
				Debug.WriteLine(exc.ToString());
			}
		}

		private void FinishMessage() {
			udpClient?.Close();
			udpClient = null;

			lastCmdType = CommandType.None;
			if (!msgQueue.Empty) {
				VMessage msg = new();
				msgQueue.Dequeue(ref msg);
				SendMessage(ref msg);
			}
		}

		private void SendCommand(CommandType cmdType, byte[] data, string more = "") {
			VMessage msg = new() {
				msgType = MessageType.MSG_Command,
				cmdType = cmdType,
				payloadType = PayloadType.Command,
				data = data,
				comment = more
			};

			SendMessage(ref msg);
		}

		private void SendInquiry(CommandType type) {
			VMessage msg = new() {
				msgType = MessageType.MSG_Inquiry,
				cmdType = type,
				payloadType = PayloadType.Inquiry
			};

			SendMessage(ref msg);
		}
		#endregion

		#region Receive

		private void HandleResponse(byte[] response) {
			string str = "";
			foreach (byte b in response) {
				str += b.ToString("X2") + " ";
			}
			//ResponseListAdd("Data received: " + str);
			int count = response.Length;
			string receiveString;
			if (count == 3) {
				receiveString = Handle3ByteResponse(response);
			} else if (count == 4) {
				receiveString = "RCV: " + Handle4ByteResponse(response);
			} else if (count == 7) {
				receiveString = "RCV: " + Handle7ByteResponse(response);
			} else if (count == 10) {
				receiveString = "RCV: " + Handle10ByteResponse(response);
			} else {
				receiveString = "Unknown";
			}
			
			if (receiveString.Length > 0) {
				DFO($"{receiveString,-25} - {str}");
			}

			if (lastCmdType == CommandType.None) {
				if (!msgQueue.Empty) {
					VMessage msg = new();
					msgQueue.Dequeue(ref msg);
					SendMessage(ref msg);
				}
			}
		}

		private async void ShowMessage(string msg) {
			if (messageDialogShowing) {
				ResponseListAdd("message dialog already showing");
				return;
			}

			messageDialogShowing = true;
			ResponseListAdd("Show: " + msg);
			try {
				this.DispatcherQueue.TryEnqueue(() => {
					ContentDialog dialog = new() {
						Title = "ViscaUI Message",
						Content = msg,
						CloseButtonText = "OK",
						XamlRoot = ContentFrame.XamlRoot // Critical requirement
					};

					dialog.Closed += ContentDialog_Closed;
					_ = dialog.ShowAsync();
				});
			} catch (Exception exc) {
				ResponseListAdd($"show message exception: {exc}");
			}
		}

		private void ContentDialog_Closed(ContentDialog sender, ContentDialogClosedEventArgs args) {
			ResponseListAdd("Dialog closed");
			messageDialogShowing = false;
		}

		private string Handle3ByteResponse(byte[] response) {
			string rtn = "";
			if ((response[1] & 0xF0) == 0x40) {
				//rtn = "ACK: ";
			} else if ((response[1] & 0xF0) == 0x50) {
				rtn = "FIN: ";
				FinishMessage();
			}

			return rtn;
		}

		private string Handle4ByteResponse(byte[] response) {
			string rtn = "";
			try {
				if ((response[0] & 0x8F) == 0x80) {
					if ((response[1] & 0xF0) == 0x60) {    // error message
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
						switch (lastCmdType) {
							case CommandType.INQ_Power:
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
								SetControlsState();
								break;
							case CommandType.INQ_FocusMode:
								if (response[2] == 0x02) {
									ShowFocusState(true);
									rtn = "Focus Auto";
								} else if (response[2] == 0x03) {
									ShowFocusState(false);
									rtn = "Focus Manual";
								}
								FinishMessage();
								break;
							case CommandType.INQ_AEMode:
								if (response[2] == 0x00) {
									SetBrightType(false);
									rtn = "Exposure Auto ";
								} else if (response[2] == 0x0D) {
									SetBrightType(true);
									rtn = "Exposure Bright";
								}
								FinishMessage();
								break;
							case CommandType.INQ_BacklightMode:
								if (response[2] == 0x02) {
									expBacklitChk.IsChecked = true;
									rtn = "Backlight On";
								} else if (response[2] == 0x03) {
									expBacklitChk.IsChecked = false;
									rtn = "Backlight Off";
								}
								FinishMessage();
								break;
							case CommandType.INQ_BalanceMode:
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
									//case 3:
									//	bal = BalanceType.OnePush;
									//	break;
									//case 4:
									//	bal = BalanceType.AutoTracing;
									//	break;
									case 5:
										bal = BalanceType.Manual;
										break;
								}
								SetBalanceType(bal);
								rtn = "Balance Mode: " + bal.ToString();
								FinishMessage();
								break;	
							case CommandType.INQ_ExpCompOn:
								bool on = false;
								switch (response[2]) {
									case 2:
										on = true;
										break;
									case 3:
										on = false;
										break;
								}
								SetExpComp(on);
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

		private string Handle7ByteResponse(byte[] response) {
			string rtn = "";
			if (response[1] == 0x50) {  // inquiry response
				if (lastCmdType == CommandType.INQ_BrightPos) {
					int pos = ((response[4] << 4) | response[5]);
					ShowExposure(pos);
					//expSlider.Value = pos;
					rtn = $"Bright {pos}";
					FinishMessage();
					//expText.Text = IrisStrings[pos] + " " + IrisStrings[pos];
				} else if (lastCmdType == CommandType.INQ_BalanceRed) {
					int pos = ((response[4] << 4) | response[5]);
					wbRedSlider.Value = pos;
					rtn = $"Red balance {pos}";
					FinishMessage();
					//wbRedText.Text = pos.ToString();
				} else if (lastCmdType == CommandType.INQ_BalanceBlue) {
					int pos = ((response[4] << 4) | response[5]);
					wbBlueSlider.Value = pos;
					rtn = $"Blue balance {pos}";
					FinishMessage();
					//wbBlueText.Text = pos.ToString();
				} else if (lastCmdType == CommandType.INQ_ExpCompPos) {
					int pos = ((response[4] << 4) | response[5]);
					ShowExposureComp(pos);
					expCompSlider.Value = pos;
					rtn = $"Exp Comp {pos}";
					FinishMessage();
					//expCompText.Text = ExpCompStrings[pos];
				//} else if (lastCmdType == CommandType.INQ_Shutter) {
				//	int pos = ((response[4] << 4) | response[5]);
				//	rtn = $"Shutter Speed {pos}";
				//	FinishMessage();
				//	//shutterLabel.Text = ShutterStrings[pos];
				//} else if (lastCmdType == CommandType.INQ_Iris) {
				//	int pos = ((response[4] << 4) | response[5]);
				//	rtn = $"Iris {pos}";
				//	FinishMessage();
				//	//irisLabel.Text = IrisD30Strings[pos];
				//} else if (lastCmdType == CommandType.INQ_Gain) {
				//	int pos = ((response[4] << 4) | response[5]);
				//	rtn = $"Gain {pos}";
				//	FinishMessage();
				//	//gainLabel.Text = GainD30Strings[pos];
				}
			}

			return rtn;
		}

		private static string Handle10ByteResponse(byte[] response) {
			string rtn;
			//int dev = response[0] & 0x7;
			int vendorId = ((int)response[2]) << 8 + response[3];
			int modelId = ((int)response[4]) << 8 + response[5];
			//int versionId = ((int)response[6]) << 8 + response[7];
			rtn = $"V: {vendorId}, M: {modelId} ";

			return rtn;
		}

		private void ResponseListAdd(string text) {
			this.DispatcherQueue.TryEnqueue(() => {
				responseListBox.Items.Insert(0, text);
				//DFO(text);
			});
		}

		private void PresetChangeInquiry() {
			if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.AEFull))) {
				SendInquiry(CommandType.INQ_AEMode);
			}
			if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.FocusFull))) {
				SendInquiry(CommandType.INQ_FocusMode);
			}
			if ((modelFeatures != null) && (modelFeatures.Contains(ControlFeature.BalanceFull))) {
				SendInquiry(CommandType.INQ_BalanceMode);
			}
		}
		#endregion

		#region Pan Tilt
		private void PanTiltStop() {
			byte[] d = [ 0x00, 0x00, 0x03, 0x03 ];
			SendCommand(CommandType.CMD_PanTilt, d, "stop");
		}

		private void PanTiltStart(byte b6, byte b7) {
			byte[] d = [ panRate, tiltRate, b6, b7 ];
			SendCommand(CommandType.CMD_PanTilt, d, "start");
		}

		private void CenterBtnClick(object sender, RoutedEventArgs e) {
			if ((deviceInfos[(int)deviceId - 1].restrict & NoPanTiltCenter) == 0) {
				byte[] d = [];
				SendCommand(CommandType.CMD_PanTiltHome, d, "center");
			} else {
				byte[] d = [ 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 ];
				SendCommand(CommandType.CMD_PanTiltDirect, d, "center");
			}
		}

		private void PtRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			ptRectDragging = true;
		 PtRect_MouseMove(sender, e);
		}

		private void PtRect_MouseUp(object _1, PointerRoutedEventArgs _2) {
			ptRectDragging = false;
			PanTiltStop();
			lastPanRate = 0;
			lastTiltRate = 0;
		}

		const int ptRectWidth = 140;
		const int ptInc = 10;
		const int ptRectHeight = 120;
		private static readonly int[] panRates = [ 1, 3, 6, 10, 15, 24 ];
		private static readonly int[] tiltRates = [ 1, 3, 6, 10, 15 ];

		private void PtRect_MouseMove(object _1, PointerRoutedEventArgs e) {
			if (ptRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(ptRect);
				Point pos = ptrPt.Position;
				double x = Math.Max(Math.Min(pos.X, ptRectWidth), 0) - (ptRectWidth / 2);
				double y = Math.Max(Math.Min(pos.Y, ptRectHeight), 0) - (ptRectHeight / 2);
				double ax = Math.Abs(x);
				double ay = Math.Abs(y);
				int pr = 0;
				int tr = 0;

				int xInd = Math.Min((int)(ax / ptInc), 5);
				int yInd = Math.Min((int)(ay / ptInc), 4);
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
						PanTiltStop();
					} else {
						byte lr = (byte)((xInd == 0) ? 3 : ((x < 0) ? 1 : 2));
						byte ud = (byte)((yInd == 0) ? 3 : ((y < 0) ? 1 : 2));
						PanTiltStart(lr, ud);
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

		#region Zoom
		private void ZoomStop() {
			//ResponseListAdd("zoom stop");
			byte[] d = [ 0x00 ];
			SendCommand(CommandType.CMD_Zoom, d, "stop");
		}

		private void ZoomIn() {
			byte cmd = (byte)(0x20 | zoomRate);
			byte[] d = [ cmd ];
			SendCommand(CommandType.CMD_Zoom, d, $"in {zoomRate}");
		}

		private void ZoomOut() {
			byte cmd = (byte)(0x30 | zoomRate);
			byte[] d = [ cmd ];
			SendCommand(CommandType.CMD_Zoom, d, $"out {zoomRate}");
		}

		private void ZmRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			zmRectDragging = true;
			ZmRect_MouseMove(sender, e);
		}

		private void ZmRect_MouseUp(object _1, PointerRoutedEventArgs _2) {
			zmRectDragging = false;
			ZoomStop();
			lastZoomRate = 0;
		}

		const int zoomInc = 12;
		const int zoomRectHeight = 120;
		private static readonly int[] zoomRates = [ 0, 1, 2, 4, 7 ];

		private void ZmRect_MouseMove(object _1, PointerRoutedEventArgs e) {
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
						ZoomStop();
					} else {
						if (y < 0) {
							ZoomIn();
						} else {
							ZoomOut();
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
				focusRect.Visibility = !auto ? Visibility.Visible : Visibility.Collapsed;
			});
		}

		private void SetFocusType(bool manual) {
			focusRect.Visibility = manual ? Visibility.Visible : Visibility.Collapsed;
			if (lastCmdType == CommandType.None) {
				byte[] d = [ (byte)(manual ? 0x03 : 0x02) ];
				SendCommand(CommandType.CMD_FocusMode, d, $"{(manual ? "manual" : "auto")}");
			}
		}

		private void NearBtnClick(object _1, RoutedEventArgs _2) {
			byte[] d = [ 0x20 ];
			SendCommand(CommandType.CMD_Focus, d, $"near ");
		}

		private void FocusStop() {
			byte[] d = [ 0x00 ];
			SendCommand(CommandType.CMD_Focus, d, "stop");
		}

		private void FocusIn() {
			byte cmd = (byte)(0x30 | focusRate);
			byte[] d = [ cmd ];
			SendCommand(CommandType.CMD_Focus, d, $"in {focusRate}");
		}

		private void FocusOut() {
			byte cmd = (byte)(0x20 | focusRate);
			byte[] d = [ cmd ];
			SendCommand(CommandType.CMD_Focus, d	, $"out {focusRate}");
		}

		private void FocusManualClick(object _1, RoutedEventArgs _2) {
			SetFocusType(focusManual.IsChecked == true);
		}

		private void FocusRect_MouseDown(object sender, PointerRoutedEventArgs e) {
			focusRectDragging = true;
			FocusRect_MouseMove(sender, e);
		}
		private void FocusRect_MouseUp(object _1, PointerRoutedEventArgs _2) {
			focusRectDragging = false;
			FocusStop();
			lastFocusRate = 0;
		}

		const int focusInc = 9;
		const int focusRectHeight = 90;
		private static readonly int[] focusRates = [ 0, 0, 1, 2, 4, 7 ];

		private void FocusRect_MouseMove(object sender, PointerRoutedEventArgs e) {
			if (focusRectDragging) {
				PointerPoint ptrPt = e.GetCurrentPoint(focusRect);
				Point pos = ptrPt.Position;
				double y = Math.Max(Math.Min(pos.Y, focusRectHeight), 0) - (focusRectHeight / 2);
				double ay = Math.Abs(y);
				int yInd = Math.Min((int)(ay / focusInc), 5);
				bool change = false;

				if ((deviceInfos[(int)deviceId - 1].restrict & NoFocusRate) == 0) {
					int fr = focusRates[yInd];

					if (ay >= 10) {
						fr = (int)((Math.Log10(ay) - 1.0) * 6);
					}

					focusRate = (byte)fr;

					if (lastFocusRate != fr) {
						lastFocusRate = fr;
						change = true;
					}
				} else {
					focusRate = 0;
					if (lastFocusYInd != yInd) {
						change = true;
					}
				}

				if (change) {
					if (yInd == 0) {
						FocusStop();
					} else {
						if (y < 0) {
							FocusIn();
						} else {
							FocusOut();
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
				if ((cameraNum < cameraCount) && (deviceInfos.Count >= cameraNum)) {
					deviceId = (uint)cameraNum;
					string vmText = deviceInfos[(int)deviceId - 1].vendor + "/" + deviceInfos[(int)deviceId - 1].model;
					modelTextBox.Text = vmText;
				} else {
					deviceId = 1;
				}
				string[] camAry = Config.GetCamera(deviceId);
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
			lastPreset = (Button)sender;
			string? lpStr = lastPreset.Content.ToString();
			lastPresetNumber = (lpStr is not null) ? int.Parse(lpStr) - 1 : 0;
			settingPreset = false;
			presetTimer = new System.Timers.Timer(1000) { Enabled = true };
			presetTimer.Elapsed += PresetTimer_Tick;
		}

		private void PresetUp(object sender, PointerRoutedEventArgs e) {
			if (presetTimer is not null) {
				presetTimer.Stop();
				presetTimer.Dispose();
			}
			lastPreset = null;
			lastPresetNumber = -1;
			string? btnStr = ((Button)sender).Content.ToString();
			if (btnStr is not null) {
				int btnNbr = int.Parse(btnStr) - 1;
				HandlePreset((byte)(btnNbr), presetTimer);
			}
		}

		private void PresetTimer_Tick(object? sender, object e) {
			settingPreset = true;
			this.DispatcherQueue.TryEnqueue(() => {
				if (lastPreset is not null) {
					presetPanels[lastPresetNumber].Background = new SolidColorBrush(Colors.Red);
				}
			});
			if (presetTimer is not null) {
				presetTimer.Stop();
				presetTimer.Dispose();
			}
		}

		private void HandlePreset(byte number, System.Timers.Timer? presetTimer1) {
			this.DispatcherQueue.TryEnqueue(() => {
				for (int i = 0; i < 6; i++) {
					presetPanels[i].Background = new SolidColorBrush(Colors.Transparent);
				}
				presetButtons[number].Background = new SolidColorBrush(Colors.Azure);
				presetButtons[number].Foreground = new SolidColorBrush(Colors.Black);
				presetPanels[number].Background = new SolidColorBrush(Colors.Maroon);
			});

			byte[] d = [ 0x01, number ];
			d[0] = settingPreset ? (byte)0x01 : (byte)0x02;
			SendCommand(CommandType.CMD_Memory, d, $"preset {number + 1}");

			if (presetTimer1 is not null) {
				presetTimer1.Enabled = false;
			}
			lastPreset = null;
			lastPresetNumber = number;

			if (!settingPreset) {
				PresetChangeInquiry();
			}

			settingPreset = false;
		}

		private void PresetTextChanged(object sender, RoutedEventArgs e) {
			string[] texts = new string[presetTexts.Count];
			for (int i = 0; i < presetTexts.Count; i++) {
				texts[i] = presetTexts[i].Text;
			}

			Config.SetPresets(deviceId - 1, texts);
		}
		#endregion

		#region Exposure
		private void ExposureSetup() {
			if ((deviceInfos[(int)deviceId - 1].restrict & NoBrightDirect) == 0) {
				expSlideStack.Visibility = Visibility.Visible;
				gainButtonStack.Visibility = Visibility.Collapsed;
			} else {
				expSlideStack.Visibility = Visibility.Collapsed;
				gainButtonStack.Visibility = Visibility.Visible;
			}

			if ((deviceInfos[(int)deviceId - 1].restrict & NoExpCompDirect) == 0) {
				//expCompChk.Visibility = Visibility.Visible;
				expCompSlideStack.Visibility = Visibility.Visible;
				expCompButtonStack.Visibility = Visibility.Collapsed;
			} else {
				//expCompChk.Visibility = Visibility.Collapsed;
				expCompSlideStack.Visibility = Visibility.Collapsed;
				expCompButtonStack.Visibility = Visibility.Visible;
			}

			bool expManual = (expManualChk.IsChecked == true);
			gainUpBtn.IsEnabled = expManual;
			gainDownBtn.IsEnabled = expManual;
			SetBrightType(expManual);

			DisplayExpComp(true);
		}

		private void ExpManualClick(object sender, RoutedEventArgs e) {
			if (expManualChk.IsChecked == true) {
				gainUpBtn.IsEnabled = true;
				gainDownBtn.IsEnabled = true;
				byte[] d = [ 0x00 ];
				SendCommand(CommandType.CMD_ExposureMode, d, "manual");
			} else {
				gainUpBtn.IsEnabled = false;
				gainDownBtn.IsEnabled = false;
				byte[] d = [ 0x03 ];
				SendCommand(CommandType.CMD_ExposureMode, d, "auto");
			}
		}

		private void GainUp(object sender, RoutedEventArgs e) {
			byte[] d = [ 0x02 ];
			SendCommand(CommandType.CMD_ExpGain, d, "up");
		}

		private void GainDown(object sender, RoutedEventArgs e) {
			byte[] d = [ 0x03 ];
			SendCommand(CommandType.CMD_ExpGain, d, "down");
		}

		private void ExpBrightClick(object _1, RoutedEventArgs _2) {
			SetBrightType(expManualChk.IsChecked == true);
		}

		private void DisplayBrightMode(bool manual) {
			this.DispatcherQueue.TryEnqueue(() => {
				expManualChk.IsChecked = manual;
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

		private void SetBrightType(bool manual) {
			DisplayBrightMode(manual);

			if (manual) {
				SetExpComp(false);
			} else {
				ShowExposure(0);
			}

			byte[] d = [ (byte)(manual ? 0x0D : 0x00) ];
			string str = manual ? "manual" : "auto";
			SendCommand(CommandType.CMD_ExposureMode, d, str);

			if (!manual) {
				SendInquiry(CommandType.INQ_BacklightMode);
			} else {
				SendInquiry(CommandType.INQ_BrightPos);
			}
		}

		private void ExpSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (lastCmdType == CommandType.None) {
				int value = (int)e.NewValue;
				byte p = (byte)((value >> 4) & 1);
				byte q = (byte)(value & 0x0F);
				byte[] d = [ 0x00, 0x00, p, q ];
				SendCommand(CommandType.CMD_ExposurePos, d, $"{value}");
				//ShowExposure(value);
			}
		}

		private void ExpBacklitClick(object sender, RoutedEventArgs e) {
			bool? chk = expBacklitChk.IsChecked;
			if (chk.HasValue) {
				SetBacklight(chk.Value);
			}
		}

		private void SetBacklight(bool on) {
			//if (lastCmdType == CommandType.None) {
				byte[] d = [ (byte)(on ? 0x02 : 0x03) ];
				string more = $"backlight {(on ? "on" : "off")}";
				SendCommand(CommandType.CMD_Backlight, d, more);
			//}
		}

		private void ExpCompClick(object sender, RoutedEventArgs e) {
			bool? chk = expCompChk.IsChecked;
			if (chk.HasValue) {
				SetExpComp(chk.Value);
			}
		}

		private void SetExpComp(bool on) {
			DisplayExpComp(on);

			if ((deviceInfos[(int)deviceId - 1].restrict & NoExpCompDirect) == 0) {
				byte[] d = [(byte)(on ? 0x02 : 0x03)];
				string more = $"exposure comp {(on ? "on" : "off")}";
				SendCommand(CommandType.CMD_ExpCompOn, d, more);

				if (on) {
					SendInquiry(CommandType.INQ_ExpCompPos);
				}
			} else {
				if (on) {
					SetBacklight(false);
				}
			}
		}

		private void DisplayExpComp(bool on) {
			this.DispatcherQueue.TryEnqueue(() => {
				expCompChk.IsChecked = on;
				expCompSlider.IsEnabled = on;
				expCompUpBtn.IsEnabled = on;
				expCompDownBtn.IsEnabled = on;
			});
		}

		private void ShowExposureComp(int pos) {
			this.DispatcherQueue.TryEnqueue(() => {
				expCompSlider.Value = pos;
				//expCompText.Text = ExpCompStrings[pos];
			});
		}

		private void ExpCompSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			int value = (int)e.NewValue;
			byte p = 0;
			byte q = (byte)(value & 0x0f);
			byte[] d = [ 0x00, 0x00, p, q ];
			SendCommand(CommandType.CMD_ExpCompPos, d, $"{value}");
			ShowExposureComp(value);
		}

		private void ExpCompUp(object sender, RoutedEventArgs e) {
			byte[] d = [ 0x02 ];
			SendCommand(CommandType.CMD_ExpCompUpDn, d, "up");
		}

		private void ExpCompDown(object sender, RoutedEventArgs e) {
			byte[] d = [ 0x03 ];
			SendCommand(CommandType.CMD_ExpCompUpDn, d, "down");
		}
		#endregion

		#region White Balance
		private void BalanceSetup() {
			wbSelectCombo.Items.Clear();
			wbSelectCombo.Items.Add("Auto");

			if ((deviceInfos[(int)deviceId - 1].restrict & NoWbInOut) == 0) {
				wbSelectCombo.Items.Add("Indoor");
				wbSelectCombo.Items.Add("Outdoor");
			}

			uint noManual = (deviceInfos[(int)deviceId - 1].restrict & NoWbManual);
			Debug.WriteLine($"--- restrict | NoWbManual {deviceInfos[(int)deviceId - 1].restrict}, {NoWbManual}, {noManual}");

			if ((deviceInfos[(int)deviceId - 1].restrict & NoWbManual) != 0) {
				wbRedButtonStack.Visibility = Visibility.Collapsed;
				wbBlueButtonStack.Visibility = Visibility.Collapsed;
				wbRedSliderStack.Visibility = Visibility.Collapsed;
				wbBlueSliderStack.Visibility = Visibility.Collapsed;
			} else {
				wbSelectCombo.Items.Add("Manual");
				wbRedButtonStack.Visibility = Visibility.Visible;
				wbBlueButtonStack.Visibility = Visibility.Visible;
				wbRedSliderStack.Visibility = Visibility.Visible;
				wbBlueSliderStack.Visibility = Visibility.Visible;
			}

			wbSelectCombo.SelectedIndex = 0;
		}

		private void SetBalanceType(BalanceType balance) {
			balanceType = balance;

			if (wbSelectCombo.Items.Count > 0) {
				//wbSelectCombo.SelectedIndex = (int)balance;
				//wbTriggerBtn.IsEnabled = (balance == BalanceType.OnePush);
				bool manual = (balance == BalanceType.Manual);
				bool wbAuto = (balance == BalanceType.Auto);
				//bool onePush = (balance == BalanceType.OnePush);

				wbRedSlider.IsEnabled = manual;
				wbBlueSlider.IsEnabled = manual;

				if (manual) {
					byte[] d = [];
					SendCommand(CommandType.CMD_BalanceMode, d, "manual");
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
						byte[] d = [ 0x00 ];
						SendCommand(CommandType.CMD_BalanceMode, d, "auto");
					//} else if (onePush) {
					//	byte[] d = { 0x35, 0x03 };
					//	SendCommand(normCmd, d, MsgType.CBalanceMode, "one push");
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

		//private void BalTriggerBtnClick(object sender, RoutedEventArgs e) {
		//	byte[] d = {0x10, 0x05 };
		//	SendCommand(normCmd, d, MsgType.CBalanceTrigger);
		//}

		//private void ShowGainText(TextBox text, int value) {
		//	this.DispatcherQueue.TryEnqueue(() => {
		//		if (text != null) {
		//			text.Text = value.ToString();
		//		}
		//	});
		//}

		private void BalRedSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (loaded) {
				int value = (int)e.NewValue;

				byte p = (byte)(value >> 4);
				byte q = (byte)(value & 0x0f);
				byte[] d = [ 0x00, 0x00, p, q ];
				SendCommand(CommandType.CMD_BalanceRed, d, $"red gain {value}");
				//ShowGainText(wbRedText, value);
			}
		}

		private void BalBlueSliderChanged(object sender, RangeBaseValueChangedEventArgs e) {
			if (loaded) {
				int value = (int)e.NewValue;
				byte p = (byte)(value >> 4);
				byte q = (byte)(value & 0x0f);
				byte[] d = [ 0x00, 0x00, p, q ];
				SendCommand(CommandType.CMD_BalanceBlue, d, $"blue gain {value}");
				//ShowGainText(wbBlueText, value);
			}
		}

		private void BalSelectChanged(object sender, SelectionChangedEventArgs e) {
			if (loaded) {
				BalanceType typ = BalanceType.Auto;
				string? bal = wbSelectCombo.SelectedItem as string;
				foreach (var kvp in balanceStrMap) {
					if (bal == kvp.Value) {
						typ = kvp.Key;
						break;
					}
				}
				//for (int i = 0; i < BalanceStrings.Length; i++) {
				//	if (bal == BalanceStrings[i]) {
				//		typ = (BalanceType)i;
				//	}
				//}

				SetBalanceType(typ);
			}
		}

		private void RedGainUp(object sender, RoutedEventArgs e) {
			byte[] d = [ 0x02 ];
			SendCommand(CommandType.CMD_RedUpDn, d, $"red gain up");
		}

		private void RedGainDown(object sender, RoutedEventArgs e) {
			byte[] d = [ 0x03 ];
			SendCommand(CommandType.CMD_RedUpDn, d, $"red gain down");
		}

		private void BlueGainUp(object sender, RoutedEventArgs e) {
			byte[] d = [ 0x02 ];
			SendCommand(CommandType.CMD_BlueUpDn, d, $"blue gain up");
		}

		private void BlueGainDown(object sender, RoutedEventArgs e) {
			byte[] d = [ 0x03 ];
			SendCommand(CommandType.CMD_BlueUpDn, d, $"blue gain down");
		}

		#endregion
	}
}
