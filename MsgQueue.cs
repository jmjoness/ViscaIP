using System;
using System.Collections.Generic;
using System.Linq;

namespace ViscaIP
{
	enum MessageType {
		MSG_Command, MSG_Inquiry
	}

	enum CommandType {
		None,
		CMD_IFClear, CMD_Power, CMD_PanTilt, CMD_PanTiltRel, CMD_PanTiltHome,
		CMD_PanTiltDirect,
		CMD_Zoom, CMD_Memory,
		CMD_Focus, CMD_FocusMode, CMD_ExposureMode, CMD_ExposurePos, CMD_Backlight,
		CMD_ExpCompOn, CMD_ExpCompPos, CMD_BalanceMode, CMD_BalanceRed, CMD_BalanceBlue,
		CMD_ExpGain, CMD_ExpCompUpDn, CMD_RedUpDn, CMD_BlueUpDn,
		INQ_Power, INQ_FocusMode, INQ_AEMode, INQ_BrightPos, INQ_BacklightMode,
		INQ_Memory, INQ_BalanceMode, INQ_BalanceRed, INQ_BalanceBlue, INQ_ExpCompOn,
		INQ_ExpCompPos, INQ_DeviceType, INQ_PanTiltPos
	}


	//enum MsgType {
	//	None, CPower, CZoom, CFocus, CFocusMode, CExposure, CBright, CBacklight, CMemory, CPanTilt, CAddressSet,
	//	CBalanceMode, CBalanceTrigger, CBalanceRed, CBalanceBlue, CExpCompOn, CExpCompPos, CExpCompUpDn, CGainUpDn, CIrReceive,
	//	IPower, IZoomPos, IFocusMode, IFocusPos, IAEMode, IBrightPos, IBacklightMode, IMemory,
	//	IBalanceMode, IBalanceRed, IBalanceBlue, IExpCompOn, IExpCompPos, IShutter, IIris, IGain, IVersion
	//}
	enum PayloadType : ushort {
		Command = 0x0100,
		Inquiry = 0x0110,
		ComInqReply = 0x0111,
		Setting = 0x0120,
		Control = 0x0200,
		ControlReply = 0x0201
	}
	struct VMessage {
		public MessageType msgType = MessageType.MSG_Command;
		public CommandType cmdType = CommandType.None;
		public PayloadType payloadType = PayloadType.Command;
		public byte address = 1;
		public byte[] data = [];
		public string comment = "";
		public uint sequence = 0;
		public VMessage() { }
		public VMessage(MessageType msgType, CommandType cmdType, byte address, byte[] data, string comment = "") {
			this.msgType = msgType;
			this.cmdType = cmdType;
			this.address = address;
			this.data = data;
			this.comment = comment;
		}
	}

	struct DeviceInfo(string url, string port, string name, string vendor, string model,uint restrict = 0) {
		public string url = url;
		public string port = port;
		public string name = name;
		public string vendor = vendor;
		public string model = model;
		public uint restrict = restrict;
	}


	class MsgQueue
	{
		readonly List<VMessage> list = [];

		private string dataToString(VMessage msg) {
			string msgStr = "";
			foreach (byte b in msg.data) {
				msgStr += b.ToString("X2");
			}
			return msgStr;
		}

		public void Enqueue(VMessage msg) {
			bool found = false;
			lock (this) {
				int lastI = 0;
				try {
					if (msg.cmdType == CommandType.CMD_PanTilt) {
						for (int i = 0; i < list.Count; i++) {
							lastI = i;
							if ((list[i].cmdType == CommandType.CMD_PanTilt) && (msg.data == list[i].data)) {
								list[i] = msg;
								found = true;
							}
						}
					}
				} catch (IndexOutOfRangeException ) {
					string msgStr = dataToString(msg);
					msgStr += "\n";
					for (int i = 0; i < list.Count; i++) {
						msgStr += dataToString(list[i]);
						msgStr += "\n";
					}
					//MessageBox.Show(exc.Message + "\n" + msgStr + "\n" + lastI.ToString() + ", " + list.Count.ToString());
				}
				if (!found) {
					list.Add(msg);
				}
			}
		}

		public bool Dequeue(ref VMessage msg) {
			bool rtn = false;
			lock (this) {
				if (list.Count > 0) {
					msg = list[0];
					list.RemoveAt(0);
				}
			}
			return rtn;
		}

		public void Clear() {
			lock(this) {
				list.Clear();
			}
		}

		public bool Empty {
			get {
				bool rtn = true;
				lock (this) {
					rtn = (list.Count == 0);
				}
				return rtn;
			}
		}

		public int Count {
			get { return list.Count; }
		}
	}
}
