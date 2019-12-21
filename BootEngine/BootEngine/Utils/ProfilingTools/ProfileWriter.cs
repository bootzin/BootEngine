using System.IO;

namespace BootEngine.Utils.ProfilingTools
{
	public class ProfileWriter : Singleton<ProfileWriter>
	{
		private int ProfileCount;
		private StreamWriter file;

		public string CurrentSession { get; private set; }

		public void BeginSession(string name, string filePath = "results.json")
		{
			file = new StreamWriter(filePath);
			WriteHeader();
			CurrentSession = name;
		}

		public void WriteProfile(Profiler profiler)
		{
			if (ProfileCount++ > 0)
				file.Write(",");

			file.Write("{");
			file.Write("\"cat\":\"function\",");
			file.Write("\"dur\":" + profiler.Sw.Elapsed.TotalMilliseconds * 1000 + ",");
			file.Write("\"name\":\"" + profiler.Name + "\",");
			file.Write("\"ph\":\"X\",");
			file.Write("\"pid\":0,");
			file.Write("\"tid\":" + profiler.ThreadID + ",");
			file.Write("\"ts\":" + profiler.StartTime);
			file.Write("}");

			file.Flush();
		}

		public void EndSesison()
		{
			WriteFooter();
			file.Close();
			CurrentSession = null;
			ProfileCount = 0;
		}

		private void WriteHeader()
		{
			file.Write("{\"otherData\": {},\"traceEvents\":[");
			file.Flush();
		}

		private void WriteFooter()
		{
			file.Write("]}");
			file.Flush();
		}
	}
}
