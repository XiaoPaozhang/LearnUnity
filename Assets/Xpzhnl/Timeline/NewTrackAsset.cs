using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace LearnUnity
{
    // 1）创建自定义轨道
  [TrackColor(0, 1, 1)]
  [TrackBindingType(typeof(Transform))]
  // 2）绑定轨道clip类型（PlayableAsset就是clip）
  [TrackClipType(typeof(NewPlayableAsset))]
  public class NewTrackAsset : TrackAsset
  {
        // 6) 想做混合可以创建mixer 
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            // 传 NewPlayableMixBehaviour 泛型
            return ScriptPlayable<NewPlayableMixBehaviour>.Create(graph, inputCount);
        }
  }
}
