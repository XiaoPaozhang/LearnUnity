using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LearnUnity
{
  public class BuffHandle : MonoBehaviour
  {
    private SpriteRenderer spriteRenderer;
    public LinkedList<BuffInfo> buffInfos = new();

    void Awake()
    {
      spriteRenderer = GetComponent<SpriteRenderer>();
    }
    private void Update()
    {
      BuffTickAndRemove();
      Debug.Log(ListToString(buffInfos));
    }

    private void BuffTickAndRemove()
    {
      BuffInfo fristBuffInfo = buffInfos.FirstOrDefault();
      if (fristBuffInfo != null)
      {
        spriteRenderer.sprite = fristBuffInfo.buffConfig.buffIcon;
      }
      else
      {
        spriteRenderer.sprite = null;
      }
      List<BuffInfo> removeBuffInfoList = new();
      foreach (var buffinfo in buffInfos)
      {
        buffinfo.curTickTime -= Time.deltaTime;
        if (buffinfo.curTickTime < 0)
        {
          buffinfo.buffConfig.onTick?.ApplyEffect(buffinfo);
          buffinfo.curTickTime = buffinfo.buffConfig.tickTime;
        }

        buffinfo.curDuration -= Time.deltaTime;
        if (buffinfo.curDuration < 0)
        {
          removeBuffInfoList.Add(buffinfo);
        }
      }

      foreach (var removeBuffInfo in removeBuffInfoList)
      {
        RemoveBuff(removeBuffInfo);
      }
    }

    public void AddBuff(BuffInfo buffInfo)
    {
      BuffInfo findBufffInfo = FindBuff(buffInfo);
      //玩家身上有这个buff,此时添加要看添加方式
      if (findBufffInfo != null)
      {
        if (findBufffInfo.curStack < findBufffInfo.buffConfig.maxStack)
        {
          findBufffInfo.curStack++;
          switch (findBufffInfo.buffConfig.e_BuffUpdateTime)
          {
            case E_BuffUpdateTime.Add:
              findBufffInfo.curDuration += findBufffInfo.buffConfig.duration;
              break;
            case E_BuffUpdateTime.Replace:
              findBufffInfo.curDuration = findBufffInfo.buffConfig.duration;
              break;
          }

          findBufffInfo.buffConfig.onCreate.ApplyEffect(findBufffInfo);
        }
      }
      else
      {
        buffInfo.curDuration = buffInfo.buffConfig.duration;
        buffInfo.buffConfig.onCreate.ApplyEffect(buffInfo);
        buffInfos.AddLast(buffInfo);

        InsertionSort(buffInfos, buffInfo => buffInfo.buffConfig.priority);
      }
    }

    public void RemoveBuff(BuffInfo buffInfo)
    {
      switch (buffInfo.buffConfig.e_BuffRemoveStackUpdate)
      {
        case E_BuffRemoveStackUpdate.Clear:
          buffInfo.buffConfig.onRemove.ApplyEffect(buffInfo);
          buffInfos.Remove(buffInfo);
          break;
        case E_BuffRemoveStackUpdate.Reduce:
          buffInfo.curStack--;
          buffInfo.buffConfig.onRemove.ApplyEffect(buffInfo);
          if (buffInfo.curStack == 0)
          {
            buffInfos.Remove(buffInfo);
          }
          else
          {
            buffInfo.curDuration = buffInfo.buffConfig.duration;
          }
          break;
      }
    }

    /// <summary>
    /// 查找buff是否在容器中
    /// </summary>
    /// <param name="buffInfo"></param>
    /// <returns></returns>
    private BuffInfo FindBuff(BuffInfo buffInfo)
    {
      return buffInfos.FirstOrDefault(v => v.buffConfig.id == buffInfo.buffConfig.id);
    }

    /// <summary>
    /// 泛型插入排序，使用传入的 keySelector 委托获取比较属性
    /// </summary>
    /// <typeparam name="T">链表元素类型</typeparam>
    /// <typeparam name="TKey">用于比较的属性类型，需实现 IComparable</typeparam>
    /// <param name="list">要排序的链表</param>
    /// <param name="keySelector">用于返回比较属性的委托</param>
    /// <returns>排序后的新链表</returns>
    private LinkedList<T> InsertionSort<T, TKey>(LinkedList<T> list, Func<T, TKey> keySelector) where TKey : IComparable<TKey>
    {
      LinkedList<T> sortedList = new LinkedList<T>();

      foreach (T item in list)
      {
        // 如果 sortedList 为空，则直接添加
        if (sortedList.Count == 0)
        {
          sortedList.AddFirst(item);
        }
        else
        {
          LinkedListNode<T> current = sortedList.First;
          // 寻找第一个其比较属性不小于 item 的节点位置
          while (current != null && keySelector(current.Value).CompareTo(keySelector(item)) < 0)
          {
            current = current.Next;
          }
          if (current == null)
          {
            // item 大于所有已排序元素，插入到末尾
            sortedList.AddLast(item);
          }
          else
          {
            // 在当前节点之前插入 item
            sortedList.AddBefore(current, item);
          }
        }
      }
      return sortedList;
    }

    // 辅助方法，将链表内容转换为字符串输出
    private string ListToString<T>(LinkedList<T> list)
    {
      string result = "";
      foreach (T item in list)
      {
        result += item.ToString() + " ";
      }
      return result != "" ? result : "目前暂无buff";
    }
  }
}
