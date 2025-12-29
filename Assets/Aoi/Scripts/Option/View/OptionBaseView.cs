using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Option
{
    public class OptionBaseView : MonoBehaviour
    {
        [System.Serializable]
        private struct AnimationData
        {
            public float pivot;
            public float moveSpeed;
            public Ease moveEasing;
            public float spriteChangeTime;
            public bool isOpen;
        }


        [SerializeField] private List<Sprite> m_bases = new List<Sprite>();
        [SerializeField] private Image m_image;
        [SerializeField] private float m_changeTime = 0.2f;
        [SerializeField] private GameObject m_insideBase;

        [SerializeField]RectTransform m_rectTransform;

        //Open情報
        [SerializeField]AnimationData m_opendata = new AnimationData();
        [SerializeField] AnimationData m_closedata = new AnimationData();

        private Tween m_currentTween;

        private void Start()
        {
            m_insideBase.transform.localScale = new Vector3(0,1,1);
            m_image.sprite = m_bases[0];

            
        }



        public void Open()
        {
            m_currentTween?.Kill();

            //画像変更シーケンス
            Sequence seq = DOTween.Sequence();

            //下移動
            seq.Append(m_rectTransform.DOPivotY(m_opendata.pivot, m_opendata.moveSpeed).SetEase(m_opendata.moveEasing));
            //画像変更
            seq.AppendCallback(() => StartCoroutine(SpriteChange(0, m_bases.Count - 1, m_opendata.spriteChangeTime)));
            //サイズ変更
            float changeTotalTime = m_opendata.spriteChangeTime * m_bases.Count;
            seq.Append(m_insideBase.transform.DOScale(Vector3.one, changeTotalTime).
                SetEase(Ease.Linear));

            


            m_currentTween = seq;
        }

        public void Close()
        {

            m_currentTween?.Kill();

            //画像変更シーケンス
            Sequence seq = DOTween.Sequence();

            float changeTotalTime = m_closedata.spriteChangeTime * m_bases.Count;
            //サイズ変更
            seq.Append(m_insideBase.transform.DOScale(new Vector3(0, 1, 1), changeTotalTime).
                SetEase(Ease.Linear));
            //画像変更
            StartCoroutine(SpriteChange(m_bases.Count - 1,0, m_closedata.spriteChangeTime));
            //上移動
            seq.Append(m_rectTransform.DOPivotY(m_closedata.pivot, m_closedata.moveSpeed).
                SetEase(m_closedata.moveEasing).SetDelay(m_closedata.spriteChangeTime));

            m_currentTween = seq;
        }

        IEnumerator SpriteChange(int begin, int end, float interval)
        {
            if (begin < 0 || begin >= m_bases.Count || end < 0 || end >= m_bases.Count)
                yield break;

            int step = begin < end ? 1 : -1;

            while (begin != end)
            {
                m_image.sprite = m_bases[begin];
                yield return new WaitForSeconds(interval);
                begin += step;
            }

            m_image.sprite = m_bases[end];
        }


        private void PlaySequence(AnimationData data)
        {
            

            ////総アニメ時間を計算
            //float totalTime = m_changeTime * m_bases.Count;

            ////スケールアニメ
            //m_insideBase.transform.DOScale(isopen ? Vector3.one : new Vector3(0, 1, 1), totalTime).SetEase(Ease.Linear);

            

            //if (isopen)
            //{
            //    for (int i = 0; i < m_bases.Count; i++)
            //    {
            //        int index = i;
            //        seq.AppendCallback(() => m_bacgroundImage.sprite = m_bases[index]);
            //        seq.AppendInterval(m_changeTime);
            //    }
            //}
            //else
            //{
            //    for (int i = m_bases.Count - 1; i >= 0; i--)
            //    {
            //        int index = i;
            //        seq.AppendCallback(() => m_bacgroundImage.sprite = m_bases[index]);
            //        seq.AppendInterval(m_changeTime);
            //    }
            //}

           
        }
    } 
}
