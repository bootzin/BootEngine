using System;
using System.Collections.Generic;

namespace BootEngine.Layers
{
    public class LayerStack
    {
        #region Properties
        public List<LayerBase> Layers { get; }

        private uint layerInsertIndex = 0;
        #endregion

        #region Constructor
        public LayerStack()
        {
            Layers = new List<LayerBase>();
        }
        #endregion

        #region Methods
        public void PushLayer(LayerBase layer)
        {
            Layers.Insert((int)layerInsertIndex++, layer);
            layer.OnAttach();
        }

        public void PushOverlay(LayerBase overlay)
        {
            Layers.Add(overlay);
            overlay.OnAttach();
        }

        public void PopLayer(LayerBase layer)
        {
            if (Layers.LastIndexOf(layer, (int)layerInsertIndex) > -1)
            {
                layer.OnDetach();
                Layers.Remove(layer);
                layerInsertIndex--;
            }
        }

        public void PopOverlay(LayerBase overlay)
        {
            if (Layers.LastIndexOf(overlay) > layerInsertIndex)
            {
                overlay.OnDetach();
                Layers.Remove(overlay);
            }
        }
        #endregion
    }
}
