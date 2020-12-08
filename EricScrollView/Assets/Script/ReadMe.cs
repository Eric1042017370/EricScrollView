using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "ReadMe")]
public class ReadMe : ScriptableObject
{
    [Header( "调整它的属性，从而实现滑动吸附列表功能。")]
    [Header( "我们可以在它的 ScrollView/ViewPort/Content 下面生成我们自己的面板，")]
    [Header( " ")]
    [Header( "你可以将它拖到你的场景中进行设置。或是动态生成它，并设置你需要的属性。")]
    [Header( " ")]
    [Header( "使用它来生成你的滑动列表吧。")]
    [Header( "    在prefab文件夹下的EricScrollView（一下称 “它”）为主要预制体。")]
    [Header("使用前请阅读:")]
    public  string i = "";
}
