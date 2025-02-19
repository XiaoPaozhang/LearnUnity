using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace LearnUnity
{

  public class Timer
  {
    private int id;
    private float maxTime;//最大时间,刚开始就确定了
    private float curTime;//当前时间,每次更新都会变
    public bool isFinish;//是否完成
    private Action onCopmplete;

    public Timer(float maxTime, Action onCopmplete)
    {
      this.maxTime = maxTime;
      this.onCopmplete = onCopmplete;
    }

    public void UpdateTime(float time)
    {
      curTime += time;
      if (curTime >= maxTime)
      {
        onCopmplete?.Invoke();
        isFinish = true;
      }
    }
  }

  public class TimerMgr
  {
    private static TimerMgr instance;
    public static TimerMgr Instance => instance ?? (instance = new());


    /// <summary>
    /// timer的id,timer本体
    /// </summary>
    private Dictionary<int, Timer> timers = new();//维护的数据
    private static int id;

    //  增
    public int StartTimer(float maxTime, Action onCopmplete)
    {
      int timerId = GetId();
      Timer timer = new Timer(maxTime, onCopmplete);
      timers.Add(timerId, timer);
      return timerId;
    }

    //改
    public void Update(float time)
    {
      if (timers.Count <= 0) return;

      List<int> removeTimerIds = new();

      foreach (var (timerId, timer) in timers)
      {
        timer.UpdateTime(time);

        if (timer.isFinish)
        {
          removeTimerIds.Add(timerId);
        }
      }

      foreach (var timerId in removeTimerIds)
      {
        timers.Remove(timerId);
      }
    }

    private int GetId()
    {
      return ++id;
    }
  }

}
