using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class UiMove_JCY : MonoBehaviour
{
    private bool _isDiseplay;
    private RectTransform _rectTransform;
    [SerializeField] private float moveAmount;
    [SerializeField] private float duration;
    private Tween moveTween;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            DisPlay();
        }
    }

    private void DisPlay()
    {
        if (_isDiseplay)
        {
            moveTween.Complete();
            _isDiseplay = !_isDiseplay;
            moveTween = _rectTransform.DOAnchorPosX(_rectTransform.anchoredPosition.x - moveAmount, duration).SetEase(Ease.OutCubic);
        }
        else if(!_isDiseplay)
        {
            moveTween.Complete();
            _isDiseplay = !_isDiseplay;
            moveTween =  _rectTransform.DOAnchorPosX(_rectTransform.anchoredPosition.x + moveAmount, duration).SetEase(Ease.OutCubic);
        }
    }
}
