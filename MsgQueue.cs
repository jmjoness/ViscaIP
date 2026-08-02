using System;
using System.Collections.Generic;
using System.Linq;

namespace ViscaIP
{
	enum MsgType {
		None, CPower, CZoom, CFocus, CFocusMode, CExposure, CBright, CBacklight, CMemory, CPanTilt, CAddressSet,
		CBalanceMode, CBalanceTrigger, CBalanceRed, CBalanceBlue, CExpCompOn, CExpCompPos, CExpCompUpDn, CGainUpDn, CIrReceive,
		IPower, IZoomPos, IFocusMode, IFocusPos, IAEMode, IBrightPos, IBacklightMode, IMemory,
		IBalanceMode, IBalanceRed, IBalanceBlue, IExpCompOn, IExpCompPos, IShutter, IIris, IGain, IVersion
	}
	enum PayloadType : ushort {
		Command = 0x0100,
		Inquiry = 0x0110,
		ComInqReply = 0x0111,
		Setting = 0x0120,
		Control = 0x0200,
		ControlReply = 0x0201
	}
	struct VMessage	{
		public MsgType type;
		public PayloadType payloadType;
		public byte[] data;
		public uint seq;
	}


	class MsgQueue
	{
		List<VMessage> list = new List<VMessage>();

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
					if ((msg.data.Length > 7) && (msg.data[2] == 6) && (msg.data[3] == 1)) {
						for (int i = 0; i < list.Count; i++) {
							lastI = i;
							if ((list[i].data.Length > 7) && (msg.data[6] == list[i].data[6]) && (msg.data[7] == list[i].data[7])) {
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
			get { return list.Count(); }
		}
	}
}
