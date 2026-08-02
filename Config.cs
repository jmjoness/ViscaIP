using System.Drawing;
using Windows.Storage;

namespace ViscaIP {
	//enum Mode { D70, D30 };

	class Config {
		const string PortStr = "Port";
		const string SpeedStr = "Speed";
		const string LocXStr = "LocX";
		const string LocYStr = "LocY";
		const string ModeStr = "Mode";
		const string DebugStr = "Debug";
		const string MiniStr = "Mini";
		const string InitStr = "Init";
		const string LogStr = "Log";
		const string InstanceStr = "Instance";
		static string CalibratedStr = "Calibrated";
		static string CalibrationXMinStr = "CalibrationXMin";
		static string CalibrationXMaxStr = "CalibrationXMax";
		static string CalibrationYMinStr = "CalibrationYMin";
		static string CalibrationYMaxStr = "CalibrationYMax";
		static string CalibrationZMinStr = "CalibrationZMin";
		static string CalibrationZMaxStr = "CalibrationZMax";

		public static string[] CameraModelStrs = { "Aver cam520 Pro", "Aver cam520 Pro v3" };

		static int m_number = 0;

		static bool GetBool(string keyStr, bool defVal) {
			var localSettings = ApplicationData.Current.LocalSettings;
			bool rtn = localSettings.Values[keyStr] as bool? ?? defVal;
			return rtn;
		}

		static int GetInt(string keyStr, int defVal) {
			var localSettings = ApplicationData.Current.LocalSettings;
			int rtn = localSettings.Values[keyStr] as int? ?? defVal;
			return rtn;
		}

		static string GetString(string keyStr, string defVal) {
			var localSettings = ApplicationData.Current.LocalSettings;
			string rtn = localSettings.Values[keyStr] as string ?? defVal;
			return rtn;
		}

		static bool SetBool(string keyStr, bool value) {
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[keyStr] = value;
			return value;
		}

		static int SetInt(string keyStr, int value) {
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[keyStr] = value;
			return value;
		}

		static string SetString(string keyStr, string value) {
			var localSettings = ApplicationData.Current.LocalSettings;
			localSettings.Values[keyStr] = value;
			return value;
		}
		static public bool Init
		{
			get { return GetBool(InitStr, false); } 
			set { SetBool(InitStr, value); }
		}

		static public string Port
		{
			get { return GetString(PortStr, ""); }
			set {	SetString(PortStr, value); }
		}

		static public int Speed
		{
			get { return GetInt(SpeedStr, 9600); }
			set { SetInt(SpeedStr, value); }
		}

		static public Point Location
		{
			get {
				Point p = new();
				p.X = GetInt(LocXStr, 0);
				p.Y = GetInt(LocYStr, 0);
				return p;
			}
			set {
				SetInt(LocXStr, value.X);
				SetInt(LocYStr, value.Y);
			}
		}

		static public bool Debug
		{
			get {	return GetBool(DebugStr, false); }
			set { SetBool(DebugStr, value); }
		}

		static public bool Log
		{
			get { return GetBool(LogStr, false); }
			set { SetBool(LogStr, value); }
		}
		static public bool Mini
		{
			get { return GetBool(MiniStr, false); }
			set { SetBool(MiniStr, value); }
		}

		//static public Mode Mode
		//{
		//	get { return (GetString(ModeStr, "D70") == "D30") ? Mode.D30 : Mode.D70; }
		//	set { SetString(ModeStr, (value == Mode.D30) ? "D30" : "D70"); }
		//}

		static public bool Calibrated {
			get { return GetBool(CalibratedStr, false); }
			set { SetBool(CalibratedStr, value); }
		}

		static public int CalXMin {
			get { return GetInt(CalibrationXMinStr, 0); }
			set { SetInt(CalibrationXMinStr, value); }
		}

		static public int CalXMax {
			get { return GetInt(CalibrationXMaxStr, 65535); }
			set { SetInt(CalibrationXMaxStr, value); }
		}

		static public int CalYMin {
			get { return GetInt(CalibrationYMinStr, 0); }
			set { SetInt(CalibrationYMinStr, value); }
		}

		static public int CalYMax {
			get { return GetInt(CalibrationYMaxStr, 65535); }
			set { SetInt(CalibrationYMaxStr, value); }
		}

		static public int CalZMin {
			get { return GetInt(CalibrationZMinStr, 0); }
			set { SetInt(CalibrationZMinStr, value); }
		}

		static public int CalZMax {
			get { return GetInt(CalibrationZMaxStr, 65535); }
			set { SetInt(CalibrationZMaxStr, value); }
		}

		static public string[] GetCamera(int n)
		{
			string key = $"Camera{n}";
			string str = GetString(key, "");
			string[] data = str.Split('|');
			return data;
		}

		static public void SetCamera(int n, string[] ary)
		{
			string key = $"Camera{n}";
			string str = "";
			for (int i  = 0; i < ary.Length; i++) {
				if (str.Length > 0) {
					str += "|";
				}
				str += ary[i];
			}
			SetString(key, str);
		}

		static public void SetPreset(int camNum, uint n, string str)
		{
			if (n < 6) {
				string[] ary = GetCamera(camNum);
				ary[4 + n] = str;
				SetCamera(camNum, ary);
			}
		}

		static public int Instance
		{
			get { return GetInt(InstanceStr, 0); }
			set { SetInt(InstanceStr, value); }
		}

		public Config()
		{
			m_number = 0;
		}

		public Config(int number)
		{
			m_number = number;
		}
	}
}
