﻿using System;
using System.Collections.Generic;

namespace BootEngine.Renderer
{
	public abstract class Scene : IDisposable
	{
		public List<Renderable> RenderableList { get; internal set; } = new List<Renderable>();

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			//
		}
	}
}
