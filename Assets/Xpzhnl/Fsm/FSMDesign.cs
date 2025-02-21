using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LearnUnity
{
  public class FSM<TE_State>
  {
    private Dictionary<TE_State, State<TE_State>> statesDict = new();
    private State<TE_State> curState;//当前正在进行的状态
    public void AddState(TE_State e_state, State<TE_State> state)
    {
      statesDict.Add(e_state, state);
    }

    public void StartFSM(TE_State e_State)
    {
      ChangeState(e_State);
    }

    public void ChangeState(TE_State e_State)
    {
      curState?.OnLeave();
      curState = statesDict[e_State];
      curState.OnEnter();
    }

    public void Update()
    {
      curState.OnUpdate();
    }
  }

  public abstract class State<TE_State>
  {
    public State(FSM<TE_State> fsm)
    {
    }

    public virtual void OnEnter()
    {

    }
    public virtual void OnUpdate()
    {

    }
    public virtual void OnLeave()
    {

    }
  }
}
