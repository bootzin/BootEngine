using BootEngine.Events;

namespace BootEngine.Layers
{
    public abstract class Layer
    {
        #region Properties
        protected string DebugName { get; }
        #endregion

        #region Constructor
        protected Layer(string debugName = "Layer")
        {
            DebugName = debugName;
        }
        #endregion

        #region Methods
        public virtual void OnAttach() { }
        public virtual void OnDetach() { }
        public virtual void OnUpdate() { }
        public virtual void OnImGuiRender() { }
        public virtual void OnEvent(EventBase @event) { }
        #endregion
    }
}
