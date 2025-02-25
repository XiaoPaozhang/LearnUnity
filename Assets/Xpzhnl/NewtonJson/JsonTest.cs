using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace LearnUnity
{
  public class JsonTest : MonoBehaviour
  {
    private string jsonStr = "{\"name\":\"xpzhnl\",\"age\":18}";
    void Start()
    {
      Person resP = JsonConvert.DeserializeObject<Person>(jsonStr);
      Debug.Log(resP.name);

      string resStr = JsonConvert.SerializeObject(resP);
      Debug.Log(resStr);
    }


  }
  public class Person
  {
    public string name;
    public int age;
  }
}
