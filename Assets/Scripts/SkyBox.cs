using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class SkyBox : MonoBehaviour
{
    public float fSpeed;

    private void Start()
    {
        AnimateSkybox();
    }

    void AnimateSkybox()
    {
        transform.DORotate(new Vector3(0, 1, 0), fSpeed).SetEase(Ease.Linear).SetLoops(-1, LoopType.Incremental);
    }
}
