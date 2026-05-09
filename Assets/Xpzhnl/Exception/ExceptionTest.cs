
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  public class ExceptionTest : MonoBehaviour
  {
    [ContextMenu("test")]
    void Start()
    {
        var ints = new List<int>();
        A obja = null;
        ints.Add(1);
        Debug.LogError("错误");
        Debug.Log(ints[0]);
        try{
            obja = obja.a == 0 ? new A() : null;
        }catch(Exception e){
            Debug.LogError("异常："+e);
        }
        Debug.Log(obja);
    }

    void Update()
    {
      
    }
  }

  public class A {
    public int a;
  }
}

