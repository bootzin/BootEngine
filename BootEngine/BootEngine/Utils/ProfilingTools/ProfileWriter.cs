using System.IO;

namespace BootEngine.Utils.ProfilingTools
{
	public static class ProfileWriter
	{
		private static int ProfileCount;
		private static StreamWriter file;
		private static readonly object fileLock = new object();

		public static string CurrentSession { get; private set; }

		public static void BeginSession(string name, string filePath = "results.json")
		{
			lock (fileLock)
			{
				file = new StreamWriter(filePath);
				WriteHeader();
				CurrentSession = name;
			}
		}

		public static void WriteProfile(Profiler profiler)
		{
			lock (fileLock)
			{
				if (ProfileCount++ > 0)
					file.Write(",");

				file.Write("{");
				file.Write("\"cat\":\"function\",");
				file.Write("\"dur\":" + (profiler.Sw.Elapsed.TotalMilliseconds * 1000).ToString().Replace(',', '.') + ",");
				file.Write("\"name\":\"" + profiler.Name + "\",");
				file.Write("\"ph\":\"X\",");
				file.Write("\"pid\":0,");
				file.Write("\"tid\":" + profiler.ThreadID + ",");
				file.Write("\"ts\":" + profiler.StartTime);
				file.Write("}");

				file.Flush();
			}
		}

		public static void EndSesison()
		{
			lock (fileLock)
			{
				WriteFooter();
				file.Close();
				file.Dispose();
				CurrentSession = null;
				ProfileCount = 0;
			}
		}

		private static void WriteHeader()
		{
			file.Write("{\"otherData\": {},\"traceEvents\":[");
			file.Flush();
		}

		private static void WriteFooter()
		{
			file.Write("]}");
			file.Flush();
		}
	}
}
