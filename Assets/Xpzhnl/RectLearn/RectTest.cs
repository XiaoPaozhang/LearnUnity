using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LearnUnity
{
  public class RectTest : MonoBehaviour
  {
    // void Awake()
    void Start()
    {
      var rectTransform = GetComponent<RectTransform>();

      Canvas.ForceUpdateCanvases();
      LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
      var width = rectTransform.rect.width;
      var height = rectTransform.rect.height;

      var sizeX = rectTransform.sizeDelta.y;
      var sizeY = rectTransform.sizeDelta.y;
      Debug.Log("Width: " + width + " Height: " + height);
      Debug.Log("Size X: " + sizeX + " Size Y: " + sizeY);
    }
  }
}