using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Tubes : MonoBehaviour
{
    public float fSpeed;
    public float fAmount;

    private void Start()
    {
        AnimateTube();
    }

    void AnimateTube()
    {
        transform.DOMoveY(transform.position.y + fAmount, fSpeed).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
    }
}
