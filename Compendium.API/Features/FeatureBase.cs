using helpers;
using helpers.Events;

namespace Compendium.Features
{
    public class FeatureBase : IFeature
    {
        private bool _isEnabled;

        public virtual string Name => "Feature Base";
        public virtual bool IsPatch => true;

        public bool IsEnabled => _isEnabled;

        public readonly EventProvider OnLoad = new EventProvider();
        public readonly EventProvider OnUnload = new EventProvider();
        public readonly EventProvider OnReload = new EventProvider();
        public readonly EventProvider OnUpdate = new EventProvider();
        public readonly EventProvider OnRestart = new EventProvider();
        public readonly EventProvider OnWaitingForPlayers = new EventProvider();

        public virtual void CallUpdate()
        {
            OnUpdate.Invoke();
        }

        public virtual void Load()
        {
            _isEnabled = true;
            OnLoad.Invoke();
        }

        public virtual void Reload() 
        {
            OnReload.Invoke();
        }

        public virtual void Restart() 
        {
            OnRestart.Invoke();
        }

        public virtual void OnWaiting() 
        {
            OnWaitingForPlayers.Invoke();

            if (Plugin.Config.ApiSetttings.ReloadOnRestart)
                Reload();
        }

        public virtual void Unload()
        {
            _isEnabled = false;
            OnUnload.Invoke();
        }
    }
}