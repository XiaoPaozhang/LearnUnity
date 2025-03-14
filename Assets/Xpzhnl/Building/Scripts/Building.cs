using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
  public bool Placed { get; private set; }
  public BoundsInt area;

  #region Build Methods

  public bool CanBePlaced()
  {
    // 直接访问GridBuildingSystem的验证方法
    return GridBuildingSystem.current.CanTakeArea(area);
  }

  public void Place()
  {
    Vector3Int positionInt = GridBuildingSystem.current.gridLayout.LocalToCell(transform.position);
    BoundsInt areaTemp = area;
    areaTemp.position = positionInt;
    Placed = true;
    GridBuildingSystem.current.TakeArea(areaTemp);
    // 放置时恢复不透明
    SpriteRenderer renderer = transform.Find("Sprite").GetComponent<SpriteRenderer>();
    if (renderer)
    {
      Color opaque = renderer.color;
      opaque.a = 1f;
      renderer.color = opaque;
    }
  }


  #endregion
}
