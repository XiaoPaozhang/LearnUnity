
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  public class ExceptionTest : MonoBehaviour
  {
    Dog dog = null;
    string str = string.Empty;

    [ContextMenu("test")]
    void Start()
    {
        // ---unity报错不会【打断】整个函数体后续代码执行
        Debug.LogError("错误");

        // ---这里dog是空，直接拿name会报空指针异常
        // ---此异常是c#语法异常，会【打断】整个函数体内后续代码执行
        // str = dog.name;

        // ---更换为下方try-catch的方式
        // ---可以在出现异常时，不【打断】后续代码，并执行catch体里的代码
        try{
            str = dog.name;
        }catch(Exception e){
            Debug.LogError("异常："+e);
        }

        Debug.Log("我被打印了，证明前面的异常没有打断我");
    }
    
    class Dog {
        public string name;
    }
  }
}

