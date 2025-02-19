using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  public class Test : MonoBehaviour
  {
    // Start is called before the first frame update
    void Start()
    {
      TimerMgr.Instance.StartTimer(3, () =>
        {
          Debug.Log("我在3秒后打印");
        });

      TimerMgr.Instance.StartTimer(2, () =>
        {
          Debug.Log("我在2秒后打印");
        });
    }

    // Update is called once per frame
    void Update()
    {
      TimerMgr.Instance.Update(Time.deltaTime);
    }
  }
}
