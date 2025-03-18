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
      if (dir == Vector2.zero) return;
      Debug.Log(dir);
      transform.right = dir.x >= 0 ? dir : -dir;
    }

    public void GenericBullet(GameObject bulletPrefab, Vector2 dir)
    {
      if (dir == Vector2.zero) return;
      // 计算子弹朝向
      float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

      // 实例化子弹（直接使用世界坐标）
      GameObject bullet = Instantiate(
          bulletPrefab,
      GunPoint.position,
      Quaternion.Euler(0, 0, angle)
      );


    }
  }
}
