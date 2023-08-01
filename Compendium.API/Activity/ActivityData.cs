using helpers.Time;

using System.Collections.Generic;
using System.Linq;

namespace Compendium.Activity
{
    public class ActivityData
    {
        private ActivitySession _curSession;

        public string Id { get; set; }

        public List<ActivitySession> Sessions { get; set; } = new List<ActivitySession>();

        public void BeginSession()
        {
            _curSession = new ActivitySession();
            _curSession.StartedAt = TimeUtils.LocalTime;

            Sessions.Add(_curSession);
        }

        public void EndSession()
        {
            _curSession.EndedAt = TimeUtils.LocalTime;
            _curSession = null;
        }

        public ActivitySession GetCurrentSession(bool startNew = false)
        {
            if (_curSession is null)
            {
                if (!Sessions.Any() || startNew)
                {
                    BeginSession();
                    return _curSession;
                }

                return (_curSession = Sessions.Last());
            }

            return _curSession;
        }
    }
}
