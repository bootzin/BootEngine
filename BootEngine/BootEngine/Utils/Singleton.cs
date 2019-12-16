using System;
using System.Threading;

namespace BootEngine.Utils
{
	public abstract class Singleton<T> where T : Singleton<T>
	{
		protected static readonly ThreadLocal<T> Lazy = new ThreadLocal<T>(() =>
		{
			if (Lazy.IsValueCreated)
				throw new InvalidOperationException(typeof(T).Name + " has already been initialized.");
			return Activator.CreateInstance(typeof(T)) as T;
		});

		public static T Instance => Lazy.Value;
	}
}
