using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LearnUnity.TestFSM;

namespace LearnUnity
{
  public class TestFSM : MonoBehaviour
  {
    public enum E_State
    {
      A, B, C
    }
    private FSM<E_State> fsm;

    void Start()
    {
      fsm = new();
      fsm.AddState(E_State.A, new A(fsm));
      fsm.AddState(E_State.B, new B(fsm));
      fsm.AddState(E_State.C, new C(fsm));
      fsm.StartFSM(E_State.A);
    }

    private void Update()
    {
      fsm.Update();

      if (Input.GetKeyDown(KeyCode.Alpha1))
      {
        fsm.ChangeState(E_State.B);
      }

      if (Input.GetKeyDown(KeyCode.Alpha2))
      {
        fsm.ChangeState(E_State.C);
      }

      if (Input.GetKeyDown(KeyCode.Alpha3))
      {
        fsm.ChangeState(E_State.A);
      }
    }

    public class A : State<E_State>
    {
      public A(FSM<E_State> fsm) : base(fsm)
      {
      }

      public override void OnEnter()
      {
        base.OnEnter();
        Debug.Log("A状态进入");
      }

      public override void OnUpdate()
      {
        base.OnUpdate();
        Debug.Log("A状态持续更新");
      }

      public override void OnLeave()
      {
        base.OnLeave();
        Debug.Log("A状态离开");
      }
    }

    public class B : State<E_State>
    {
      public B(FSM<E_State> fsm) : base(fsm)
      {
      }
      public override void OnEnter()
      {
        base.OnEnter();
        Debug.Log("B状态进入");
      }

      public override void OnUpdate()
      {
        base.OnUpdate();
        Debug.Log("B状态持续更新");
      }

      public override void OnLeave()
      {
        base.OnLeave();
        Debug.Log("B状态离开");
      }
    }
  }

  public class C : State<E_State>
  {
    public C(FSM<E_State> fsm) : base(fsm)
    {
    }


    public override void OnEnter()
    {
      base.OnEnter();
      Debug.Log("C进入");
    }
    public override void OnLeave()
    {
      base.OnEnter();
      Debug.Log("C离开");
    }
    public override void OnUpdate()
    {
      base.OnEnter();
      Debug.Log("C持续更新");
    }
  }
}
