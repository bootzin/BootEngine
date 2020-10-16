﻿using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
#if !DEBUG
using Utils.Exceptions;
#endif

namespace BootEngine.Utils.ProfilingTools
{
	public sealed class Profiler : IDisposable
	{
		private const long MS_FREQUENCY = TimeSpan.TicksPerMillisecond / 1000L;

		#region Properties
		public Stopwatch Sw { get; private set; }
		public int ThreadID { get; private set; }
		public long StartTime { get; private set; }
		public string Name { get; }
		#endregion

		public Profiler(string name)
		{
#if !DEBUG
			throw new BootEngineException("Profiler should not be called without the DEBUG compilation flag.");
#endif
			Name = name;
			Init();
		}

		public Profiler(Type type, [System.Runtime.CompilerServices.CallerMemberName] string name = null)
		{
#if !DEBUG
			throw new BootEngineException("Profiler should not be called without the DEBUG compilation flag.");
#endif
			Name = type.Name + "_" + name;
			Init();
		}

		public void Dispose()
		{
			Sw.Stop();
			Task.Run(() => ProfileWriter.WriteProfile(this));
		}

		private void Init()
		{
			//prevent "Normal" Processes from interrupting Threads
			Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

			//prevent "Normal" Threads from interrupting this thread
			Thread.CurrentThread.Priority = ThreadPriority.Highest;

			ThreadID = Thread.CurrentThread.ManagedThreadId;

			StartTime = DateTime.Now.Ticks / MS_FREQUENCY;

			Sw = Stopwatch.StartNew();
		}
	}
}