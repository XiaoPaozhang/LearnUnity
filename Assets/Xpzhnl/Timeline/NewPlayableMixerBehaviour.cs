using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace LearnUnity
{
    // 7) mixer和behavior一样都是继承 PlayableBehaviour
    public class NewPlayableMixBehaviour : PlayableBehaviour
    {
        // 都有生命周期
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var inp = playable.GetInputWeight(0);
            Debug.Log("第一个clip的权重:" + inp);
        }
    }
}
