using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class ShineFX : MonoBehaviour
{

    public Transform shineT;
    public float fOffSet;
    public float fSpeed;
    public float fMinDelay;
    public float fMaxDelay;

    private void Start()
    {
        Animate();
    }

    private void Animate()
    {
        shineT.DOLocalMoveX(fOffSet, fSpeed).SetDelay(Random.Range(fMinDelay, fMaxDelay)).SetEase(Ease.Linear).OnComplete(() =>
        {
            shineT.DOLocalMoveX(-fOffSet, 0);
            Animate();
        });
    }
}
