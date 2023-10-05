using Compendium.Extensions;

using UnityEngine;

namespace Compendium.Conditions
{
    public class RangeCondition : Condition
    {
        private Vector3 _pos;
        private float _range;

        public RangeCondition(Vector3 pos, float range)
        {
            _pos = pos;
            _range = range;
        }

        public override bool IsMatch(ReferenceHub hub)
            => hub.Position().IsWithinDistance(_pos, _range);
    }
}