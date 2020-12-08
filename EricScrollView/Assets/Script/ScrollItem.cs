using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollItem : MonoBehaviour
{
    private EricScrollView scrollView;

    public void Init(EricScrollView scr)
    {
        scrollView = scr;
    }
    // Update is called once per frame
    void Update()
    {
        SmoothItemSize();
    }

    private void SmoothItemSize()
    {
        var distanceWithMidPos = (EricScrollView.ViewMidPos - (Vector2)transform.position).magnitude;
        var scale = Mathf.Clamp(1-distanceWithMidPos * scrollView.ScaleInfluenceDistanceReciprocal,scrollView.cellScale,1);
        gameObject.transform.localScale = new Vector2(scale,scale);
    }
}
