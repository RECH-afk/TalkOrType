using UnityEngine;
using DG.Tweening;

public class SharpPaperShake : MonoBehaviour
{
    public float posStrength = 10f;
    public float rotStrength = 8f;
    public float stepTime = 0.025f;

    private RectTransform rect;
    private Vector2 basePos;
    private float baseRot;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        basePos = rect.anchoredPosition;
        baseRot = rect.localEulerAngles.z;
    }

    void OnEnable()
    {
        StartShake();
    }

    void OnDisable()
    {
        rect.DOKill();
        ResetTransform();
    }

    public void StartShake()
    {
        rect.DOKill();

        Sequence seq = DOTween.Sequence().SetLoops(-1);

        seq.AppendCallback(() =>
        {
            Vector2 offset = new Vector2(
                Random.Range(-posStrength, posStrength),
                Random.Range(-posStrength, posStrength)
            );

            float rot = Random.Range(-rotStrength, rotStrength);

            rect.DOAnchorPos(basePos + offset, stepTime)
                .SetEase(Ease.Linear);

            rect.DORotate(new Vector3(0, 0, baseRot + rot), stepTime)
                .SetEase(Ease.Linear);
        });

        seq.AppendInterval(stepTime);

        seq.AppendCallback(() =>
        {
            rect.DOAnchorPos(basePos, stepTime)
                .SetEase(Ease.Linear);

            rect.DORotate(new Vector3(0, 0, baseRot), stepTime)
                .SetEase(Ease.Linear);
        });

        seq.AppendInterval(stepTime);
    }

    void ResetTransform()
    {
        rect.anchoredPosition = basePos;
        rect.localRotation = Quaternion.Euler(0, 0, baseRot);
    }
}