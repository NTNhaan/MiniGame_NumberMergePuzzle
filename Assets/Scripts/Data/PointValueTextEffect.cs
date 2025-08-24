using DG.Tweening;
using UnityEngine;
namespace Data
{
    public class PointValueTextEffect : MonoBehaviour
    {
        void Start()
        {
            transform.DOMove(transform.position + Vector3.up * 1f, 0.6f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}