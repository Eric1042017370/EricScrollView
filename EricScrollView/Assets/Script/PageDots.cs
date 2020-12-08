using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PageDots : MonoBehaviour
{
    #region Field

    private EricScrollView m_ScrollView;
    private GameObject m_DotTemplate;
    private HorizontalLayoutGroup m_LayoutGroup;
    private ContentSizeFitter m_SizeFitter;
    public int space = 50;
    private GameObject[] m_Dots;

    public Action OnDotClick;
    #endregion
    
 

    private void InitDots(byte pageDotCount, Sprite dotImage)
    {
        Debug.Assert(transform.childCount>0,"[EricScrollView] PageDot Child is Missing");
        m_Dots = new GameObject[pageDotCount];
        m_DotTemplate = transform.GetChild(0).gameObject;
        m_DotTemplate.GetComponent<Image>().sprite = dotImage;
        for (int i = 0; i < pageDotCount; i++)
        {
            byte index = (byte)i;
            m_Dots[i] = Instantiate(m_DotTemplate, transform);
            m_Dots[i].AddComponent<Button>().onClick.AddListener(() => { OnClick(index); });
        }
        m_DotTemplate.SetActive(false);
    }

    private void InitSelf(bool useLayOutGroup)
    {
        if (!useLayOutGroup)
            return;
        m_LayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();
        m_SizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        m_SizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        m_LayoutGroup.spacing = space;
        m_LayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        m_LayoutGroup.childControlWidth = false;
        m_LayoutGroup.childControlHeight = false;
    }

    /// <summary>
    /// 点击Dot回调
    /// </summary>
    /// <param name="index"></param>
    private void OnClick(byte index)
    {
        if (OnDotClick!=null&&OnDotClick.GetInvocationList().Length>0)
        {
            OnDotClick.Invoke();
            return;
        }
        //移动到dot相应的page页面
        m_ScrollView.MoveToMid(index);
    }

    #region Interface
    
    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="usePageDot"></param>
    /// <param name="pageDotCount"></param>
    /// <param name="dotImage"></param>
    public void Init(EricScrollView scrollView,bool usePageDot, byte pageDotCount, Sprite dotImage)
    {
        if (!usePageDot)
        {
            Destroy(gameObject);
            return;
        }

        m_ScrollView = scrollView;
        InitSelf(pageDotCount>1);
        InitDots(pageDotCount,dotImage);
        OnDotClick = null;
    }

    /// <summary>
    /// 注册dot点击事件
    /// </summary>
    /// <param name="callback"></param>
    public void RegisterDotClick(Action callback)
    {
        OnDotClick += callback;
    }
    
    #endregion
}