using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LearnUnity
{
  public class PlayerController : MonoBehaviour
  {
    private PlayerControls inputActions;
    [SerializeField] private float moveSpeed = 10;
    private Transform playerTf;
    private Gun gun;
    private GameObject Bullet;


    void Awake()
    {
      inputActions = new();
      gun = transform.Find("Gun").gameObject.GetComponent<Gun>();
    }

    void Start()
    {
      playerTf = transform;
      Bullet = Resources.Load<GameObject>("Bullet");
    }

    void OnEnable()
    {
      inputActions.Enable();
    }

    void OnDisable()
    {
      inputActions.Disable();
    }

    void Update()
    {
      //玩家移动
      Vector2 moveDir = inputActions.Player.Move.ReadValue<Vector2>().normalized;
      Move(moveDir);


      //玩家脸朝向
      Mouse mouse = Mouse.current;

      Vector2 mousePosScreen = Camera.main.ScreenToWorldPoint(mouse.position.ReadValue()).normalized;

      FaceRotation(mousePosScreen);

      //枪支朝向
      Vector2 dir = mousePosScreen - (Vector2)transform.position;

      float deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
      if (gun != null)
      {
        gun.FaceRotation(dir);
      }
      bool mouse0Down = mouse.leftButton.wasPressedThisFrame;
      if (mouse0Down)
        gun.GenericBullet(Bullet, dir);

    }

    void Move(Vector2 moveDir)
    {
      transform.Translate(moveDir * moveSpeed * Time.deltaTime);
    }

    private void FaceRotation(Vector2 mousePosScreen)
    {
      Vector3 scale = transform.localScale;
      if (mousePosScreen.x > transform.position.x)
      {
        transform.localScale = new Vector3(MathF.Abs(transform.localScale.x), scale.y, scale.z);
      }
      else
      {
        transform.localScale = new Vector3(-MathF.Abs(transform.localScale.x), scale.y, scale.z);
      }

    }
  }
}
