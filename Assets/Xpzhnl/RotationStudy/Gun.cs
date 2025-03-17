using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LearnUnity
{
  public class Gun : MonoBehaviour
  {
    private Transform GunPoint;
    void Start()
    {
      GunPoint = transform.Find("GunPoint");
    }
    public void FaceRotation(Vector2 dir)
    {
      float deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
      transform.right = dir.x >= 0 ? dir : -dir;
      // transform.localRotation = Quaternion.Euler(0, 0, dir.x >= 0 ? deg : -deg + 180);
    }



    public void GenericBullet(GameObject bullet, Vector2 dir)
    {
      float deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
      GameObject go = Instantiate(bullet, GunPoint);
      go.transform.SetParent(null);
      transform.localRotation = Quaternion.Euler(0, 0, dir.x >= 0 ? deg : -deg + 180);
    }
  }
}
