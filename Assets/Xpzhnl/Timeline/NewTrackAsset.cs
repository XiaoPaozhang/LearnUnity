using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace LearnUnity
{
  [TrackColor(0, 1, 1)]
  [TrackBindingType(typeof(Transform))]
  [TrackClipType(typeof(NewPlayableAsset))]
  public class NewTrackAsset : TrackAsset
  {

  }
}
