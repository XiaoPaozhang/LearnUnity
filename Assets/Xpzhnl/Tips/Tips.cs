using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Xpzhnl
{
  public class Tips : MonoBehaviour
  {
    void Start()
    {
      btn.onClick.AddListener(() =>
      {
        AddReward(new List<int> { 1, 2, 3 });
      });
    }



    [SerializeField] private Transform tipsGroupTF;
    [SerializeField] private Button btn;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float space = 20f;
    [SerializeField] private Image img;

    private Queue<int> 待加入的tips = new();
    private Queue<RectTransform> 正在展示的tips = new();
    private bool isPlaying = false;
    public void AddReward(List<int> rewards)
    {
      // 别人给我,我先加进 待加入,先不展示动画
      foreach (var reward in rewards)
      {
        待加入的tips.Enqueue(reward);
      }

      // 如果正在播放,就不展示了
      if (isPlaying) return;


      StartAnim();
    }

    // 展示每一个item的动画
    private void StartAnim()
    {
      if (待加入的tips.Count == 0) return;
      isPlaying = true;


      //加载对象
      GameObject rewardObj = Instantiate(img.gameObject, tipsGroupTF);
      //设置文本

      //删除数据
      待加入的tips.Dequeue();
      //设置位置
      rewardObj.transform.localPosition = Vector3.zero;
      rewardObj.transform.localScale = Vector3.one;
      //设置移动动画
      Sequence movesequence = SetMoveAnim(rewardObj.transform as RectTransform);
      movesequence.AppendCallback(Next);

      //设置透明度
      CanvasGroup canvasGroup = rewardObj.GetComponent<CanvasGroup>();
      canvasGroup.alpha = 0;
      //设置动画

      正在展示的tips.Enqueue(rewardObj.transform as RectTransform);
      Sequence sequence = DOTween.Sequence();
      // 显示动画
      sequence.Append(canvasGroup.DOFade(1, 0.5f));

      // 停留时间
      sequence.AppendInterval(duration);

      // 隐藏动画
      sequence.Append(canvasGroup.DOFade(0, 0.5f));

      // 动画结束
      sequence.OnComplete(() =>
      {
        rewardObj.SetActive(false);
        正在展示的tips.Dequeue();
      });

    }

    private Sequence SetMoveAnim(RectTransform rectTransform)
    {
      Sequence sequence = DOTween.Sequence();
      foreach (RectTransform item in 正在展示的tips)
      {
        sequence.Join(item.DOLocalMoveY(item.localPosition.y - rectTransform.rect.height - space, 0.2f)); // 这里产生持续位移
      }
      return sequence;
    }

    private void Next()
    {
      // 如果待加入的tips有值,就展示下一个
      if (待加入的tips.Count > 0)
      {
        StartAnim();
      }
      else
      {
        isPlaying = false;
      }
    }
  }
}
