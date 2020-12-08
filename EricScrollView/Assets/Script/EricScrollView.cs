using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class EricScrollView : MonoBehaviour
{
    #region Field

    #region pageDots

    [SerializeField] [Tooltip("是否使用pageDot页签")]
    public bool usePageDot;

    public byte pageDotCount;
    public Sprite DotImage;

    [Tooltip("页签和page数量保持一致")] 
    public bool pageDotMatchPageCount;
    private PageDots pageDotMgr;

    #endregion

    #region Page
    [Tooltip("滑动页签之间的间距")]
    [SerializeField]
    private float itemSpacing;
    [Tooltip("滑动页面：自己的panelPrefab")] [SerializeField]
    private GameObject[] m_Pages;

    [Tooltip("回弹弹性系数")] [SerializeField] private int resetSpeed = 3;

    [Range(0, 1.5f)] [Tooltip("单元格缩放比例，目前支持0~1.5")] [SerializeField]
    public float cellScale = 1;

    private Transform m_PageContent;
    private HorizontalLayoutGroup m_HorizontalLayoutGroup;
    private ScrollRect m_ScrollRect;

    private Coroutine pullOverCoroutine;

    //停靠开关 max127
    private sbyte PullOverIndex = -1;

    //是否需要停靠
    private bool PullOverDirty = false;

    /// <summary>
    /// 每个page所占的滑动比例点
    /// </summary>
    private float[] m_Ratios;

    /// <summary>
    /// 显示中心点
    /// </summary>
    public static Vector2 ViewMidPos;

    /// <summary>
    /// ScrollView视窗的一半
    /// </summary>
    [HideInInspector] public float harfViewWidth;

    /// <summary>
    /// 缩放影响的最大距离
    /// </summary>
    /// <returns></returns>
    private float ScaleInfluenceDistance => harfViewWidth;

    /// <summary>
    /// 缩放影响最大距离的倒数
    /// </summary>
    [HideInInspector] public float ScaleInfluenceDistanceReciprocal;

    /// <summary>
    /// 第一个item的localx坐标值
    /// </summary>
    private float firstItemLocalPosX;

    /// <summary>
    /// 第一个item的x坐标值
    /// </summary>
    private float firstItemPosX;

    private float scrollWidthReciprocal;


    /// <summary>
    /// scrollRect有效滚动距离（Content下第0个item到最后一个之间的距离）的倒数
    /// </summary>
    public float ScrollWidthReciprocal => scrollWidthReciprocal;

    public const float minScale = 0.5f;

    public const float DecreaseVelocityRate = 0.95f;

    #endregion

    #endregion

    #region Method

    // Start is called before the first frame update
    void Start()
    {
        InitMemgers();

        InitOthers();
    }

    /// <summary>
    /// 初始化成员
    /// </summary>
    private void InitMemgers()
    {
        harfViewWidth = ((RectTransform) transform).rect.width * 0.5f;
        ScaleInfluenceDistanceReciprocal = 1 / ScaleInfluenceDistance;
        pageDotMgr = GetComponentInChildren<PageDots>();
        resetSpeed = (resetSpeed %= 10) == 0 ? 3 : resetSpeed;

        InitPages();
        InitScrollView();
        StartCoroutine(InitNeedNextFrame());
    }

    /// <summary>
    /// 初始化需要scrollview规划子物体pos后再初始化的字段
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private IEnumerator InitNeedNextFrame()
    {
        firstItemPosX = m_Pages[0].transform.position.x;
        
        yield return null;
        firstItemLocalPosX = m_Pages[0].transform.localPosition.x;
        scrollWidthReciprocal =
            1 / (m_Pages[m_Pages.Length - 1].transform.position.x - m_Pages[0].transform.position.x);

        //初始化页面坐标相对scrollview的视口比例
        m_Ratios = new float[m_Pages.Length];
        m_Ratios[0] = 0;
        m_Ratios[m_Ratios.Length - 1] = 1;
        for (int i = 1; i < m_Ratios.Length - 1; i++)
        {
            m_Ratios[i] = (m_Pages[i].transform.localPosition.x - firstItemLocalPosX) * ScrollWidthReciprocal;
        }
    }

    private void InitScrollView()
    {
        m_HorizontalLayoutGroup = m_PageContent.gameObject.GetComponent<HorizontalLayoutGroup>();
        m_ScrollRect = transform.GetChild(0).GetComponent<ScrollRect>();
        m_HorizontalLayoutGroup.spacing = itemSpacing;
        var leftPadding = ScaleInfluenceDistance - ((RectTransform) m_Pages[0].transform).rect.width * 0.5;
        var rightPadding = ScaleInfluenceDistance -
                           ((RectTransform) m_Pages[m_Pages.Length - 1].transform).rect.width * 0.5;
        m_HorizontalLayoutGroup.padding.left = (int) leftPadding;
        m_HorizontalLayoutGroup.padding.right = (int) rightPadding;
        ViewMidPos = transform.position;
        //添加滑动回调事件
        m_ScrollRect.onValueChanged.AddListener(OnScroll);
        //开启停靠检测
        StartCoroutine(PullOverToMidCor());
    }

    /// <summary>
    /// ScrollView滚动回调
    /// </summary>
    /// <param name="pos"></param>
    private void OnScroll(Vector2 pos)
    {
        PullOverDirty = true;
    }


    /// <summary>
    /// 初始化content子物体页面
    /// </summary>
    private void InitPages()
    {
        m_PageContent = transform.Find("Scroll View/Viewport/Content");
        bool loadFromPrefab = true;
        
        //初始化页签数组
        if (m_Pages == null || m_Pages.Length == 0)
        {
            if (m_PageContent.childCount == 0)
            {
                Debug.LogError("Page个数不能为空，请添加page");
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#endif
            }

            m_Pages = new GameObject[m_PageContent.childCount];
            loadFromPrefab = false;
        }

        if (loadFromPrefab)
        {
            //清空content的子物体
            var childCount = m_PageContent.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Destroy(m_PageContent.GetChild(i).gameObject);
            }
        }

        //初始化页签
        for (int i = 0; i < m_Pages.Length; i++)
        {
            m_Pages[i] = loadFromPrefab
                ? Instantiate(m_Pages[i], m_PageContent)
                : m_PageContent.GetChild(i).gameObject;
            var scrollItem = m_Pages[i].GetComponent<ScrollItem>();
            if (scrollItem==null)
            {
                scrollItem = m_Pages[i].AddComponent<ScrollItem>();
            }
            scrollItem.Init(this);
        }
    }

    /// <summary>
    /// 初始化其他管理器
    /// </summary>
    private void InitOthers()
    {
        if (pageDotMatchPageCount)
        {
            pageDotCount = (byte) m_PageContent.childCount;
        }

        pageDotMgr.Init(this, usePageDot, pageDotCount, DotImage);
    }


    /// <summary>
    /// 滑动停靠
    /// </summary>
    private void PullOver()
    {
        //找到最近中点的页
        GameObject nearestMidPage = m_Pages[0];
        sbyte index = 0;
        for (int i = 1; i < m_Pages.Length; i++)
        {
            if (DistanceWithMidPos(m_Pages[i].transform.position) <
                DistanceWithMidPos(nearestMidPage.transform.position))
            {
                nearestMidPage = m_Pages[i];
                index = (sbyte) i;
            }
        }

        //开启停靠
        PullOverIndex = index;
    }

    /// <summary>
    /// 停靠到中点协程
    /// </summary>
    /// <param name="target"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    private IEnumerator PullOverToMidCor()
    {
        while (true)
        {
            if (Input.GetMouseButtonUp(0) && PullOverDirty)
                PullOver();
            yield return null;
            if (PullOverIndex == -1)
                continue;
            var recordPIndex = PullOverIndex;
            var target = GetViewItemByIndex((byte) PullOverIndex);
            var targetRatios = GetScrollRatios((byte) PullOverIndex);
            var currentRatios =
                m_ScrollRect
                    .horizontalNormalizedPosition; //(target.transform.position.x - firstItemPosX) * ScrollWidthReciprocal;
            if (targetRatios == currentRatios)
                continue;
            while (true)
            {
                var distance = DistanceWithMidPos(target.position);
                if (distance < 30)
                    break;
                //移向中点
                currentRatios = Mathf.Lerp(currentRatios, targetRatios, resetSpeed * 0.05f);
                m_ScrollRect.horizontalNormalizedPosition = currentRatios;
                yield return null;
            }

            //获取停靠后的scrollview的x值比例（0~1）
            m_ScrollRect.velocity = Vector2.zero;
            m_ScrollRect.horizontalNormalizedPosition = targetRatios;
            PullOverIndex = recordPIndex == PullOverIndex ? (sbyte) -1 : PullOverIndex;
            PullOverDirty = false;
        }
    }

    /// <summary>
    /// 计算点到view中心点的x轴距离
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private float DistanceWithMidPos(Vector2 pos)
    {
        return Mathf.Abs(ViewMidPos.x - pos.x);
    }

    /// <summary>
    /// 获取滑动点的视口比例
    /// </summary>
    /// <param name="index">页标</param>
    /// <returns></returns>
    private float GetScrollRatios(byte index)
    {
        return m_Ratios[index];
    }

    #endregion

    #region Interface

    /// <summary>
    /// 获取viewItem
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Transform GetViewItemByIndex(byte index)
    {
        if (index < 0 || index > m_Pages.Length - 1)
        {
            throw new Exception("[EricScrollView] 获取Index异常：" + index);
        }

        return m_Pages[index].transform;
    }

    /// <summary>
    /// 停靠到中间，根据index
    /// </summary>
    /// <param name="pageIndex"></param>
    public void MoveToMid(byte pageIndex)
    {
        PullOverIndex = (sbyte) pageIndex;
    }

    #endregion
}