using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace LearnUnity
{
    // 3）PlayableAsset就是clip
  public class NewPlayableAsset : PlayableAsset
  {
    public NewPlayableBehaviour newPlayableBehaviour1 =  new ();
    public string str;
    // 4）创建playable
    public override Playable CreatePlayable(PlayableGraph graph, GameObject go)
    {
        // 5） 使用ScriptPlayable创建 Playable，传入视图和行为，行为是直接new的，它有钩子函数，传给cteate之后，由playable内部调用
      ScriptPlayable<NewPlayableBehaviour> playable = ScriptPlayable<NewPlayableBehaviour>.Create(graph,newPlayableBehaviour1);
      NewPlayableBehaviour newPlayableBehaviour = playable.GetBehaviour();
      newPlayableBehaviour.str = str;
      return playable;
    }
  }
}
