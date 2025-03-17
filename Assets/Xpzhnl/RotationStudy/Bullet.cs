using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  public class Bullet : MonoBehaviour
  {
    void Awake()
    {
      transform.Translate(transform.right);
      Debug.Log($"我被创建了,我的方向是{transform.right}");

      // Destroy(gameObject, 3);
    }

    void OnDestroy()
    {
      // Debug.Log($"我被销毁了,我的方向是{transform.right}");
    }
  }
}
