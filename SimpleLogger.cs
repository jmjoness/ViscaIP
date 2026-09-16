using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ViscaIP {
	public static class SimpleLogger {
		private readonly static string appDataPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
		private static string LogFilePath = Path.Combine(appDataPath, "Visca-01.log");
		private static bool initialized = false;
		private static bool logExists = false;
		private static readonly Queue<string> waitingMsg = new();

		private static async void Init() {
			try {
				string logFolderPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
				string[] logFiles = Directory.GetFiles(appDataPath, "Visca-*.log");

				int logNo = 1;
				DateTime newestDate = new(2000, 1, 1);

				foreach (var file in logFiles) {
					string fileName = Path.GetFileName(file);
					string nStr = fileName.Replace("Visca-", "").Replace(".log", "");
					if (Int32.TryParse(nStr, out int n)) {
						DateTime created = File.GetCreationTime(file);
						DateTime modified = File.GetLastWriteTime(file);
						DateTime accessed = File.GetLastAccessTime(file);
						if (created > newestDate) {
							newestDate = created;
							logNo = n + 1;
						}
					}
				}

				if (logNo > 10) {
					logNo = 1;
				}

				initialized = true;

				int nextLog = logNo;
				string fName = $"Visca-{nextLog:D2}.log";
				string fPath = Path.Combine(appDataPath, fName);
				Debug.WriteLine($"log path: {fPath}");
				if (File.Exists(fPath)) {
					File.Delete(fPath);
				}

				Debug.WriteLine($"--- Creating log file: {fPath}");
				string logEntry = $"{DateTime.Now:MM-dd} - ViscaUI data log {Environment.NewLine}";
				File.WriteAllText(fPath, logEntry);
				if (File.Exists(fPath)) {
					LogFilePath = fPath;
					logExists = true;
				}
			} catch (System.IO.IOException exc) {
				Debug.WriteLine($"Init() system IO exception: {exc}");
			} catch (Exception exc) {
				Debug.WriteLine($"Init() exception: {exc}");
			}
		}

		public static async Task LogAsync(string message) {
			if (!initialized) {
				Init();
			}

			string logEntry = $"{DateTime.Now:MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}";

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
				using FileStream stream = new(LogFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
				stream.Close();
			} catch (IOException) {
				return true;
			}

			return false;
		}
	}
}