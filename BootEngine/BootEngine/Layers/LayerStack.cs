using System.Collections.Generic;

namespace BootEngine.Layers
{
    public class LayerStack
    {
        #region Properties
        public List<Layer> Layers { get; }

        private uint layerInsertIndex = 0;
        #endregion

        #region Constructor
        public LayerStack()
        {
            Layers = new List<Layer>();
        }

        ~LayerStack()
        {
            Layers.Clear();
        }
        #endregion

        #region Methods
        public void PushLayer(Layer layer)
        {
            Layers.Insert((int)layerInsertIndex++, layer);
            layer.OnAttach();
        }

        public void PushOverlay(Layer overlay)
        {
            Layers.Add(overlay);
            overlay.OnAttach();
        }

        public void PopLayer(Layer layer)
        {
            if (Layers.LastIndexOf(layer, (int)layerInsertIndex) > -1)
            {
                layer.OnDetach();
                Layers.Remove(layer);
                layerInsertIndex--;
            }
        }

        public void PopOverlay(Layer overlay)
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
