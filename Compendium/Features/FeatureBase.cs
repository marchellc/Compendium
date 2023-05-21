namespace Compendium.Features
{
    public class FeatureBase : IFeature
    {
        private bool m_IsDisabled;
        private bool m_IsRunning;

        public virtual string Name { get; }

        public bool IsDisabled => m_IsDisabled;
        public bool IsRunning => m_IsRunning;

        public virtual void Update() { }
        public virtual void Reload() { }
        public virtual void OnLoad() { }
        public virtual void OnUnload() { }

        public void Enable() => m_IsDisabled = false;
        public void Disable() => m_IsDisabled = true;

        void IFeature.Load() => m_IsRunning = true;
        void IFeature.Unload() => m_IsRunning = false;
    }
}