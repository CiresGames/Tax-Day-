using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Container of the three main transform properties so we can restore them later.
    /// </summary>
    public readonly struct V3
    {
        public readonly Vector3 position;
        public readonly Vector3 rotation;
        public readonly Vector3 scale;

        public V3(Vector3 pos, Vector3 scale, Vector3 rot)
        {
            position = pos;
            this.scale = scale;
            rotation = rot;
        }
    }
}
