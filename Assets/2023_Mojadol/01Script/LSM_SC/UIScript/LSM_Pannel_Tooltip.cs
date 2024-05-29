using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LSM_Pannel_Tooltip : MonoBehaviour
{
    [SerializeField]private CanvasScaler cvs;
    [SerializeField]private RectTransform rt;

    [SerializeField] private TextMeshProUGUI title_txt, content_txt;
    void Start()
    {
        rt = this.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f,0.5f);
        cvs = GetComponentInParent<CanvasScaler>();
    }

    public void SettingContents(LSM_UI_Basic c)
    {
        this.title_txt.text = c.name;
        this.content_txt.text = c.tooltip;
    }

    // 출처 : https://rito15.github.io/posts/unity-study-rpg-inventory/#%EC%95%84%EC%9D%B4%ED%85%9C%EA%B3%BC-%EC%95%84%EC%9D%B4%ED%85%9C-%EB%8D%B0%EC%9D%B4%ED%84%B0
    // 소환한 툴팁이 캔버스 밖으로 나가는 일을 방지하기위함.
    public void SetRectPosition(RectTransform slotRect)
    {
        // 현재 캔버스의 크기와, 해상도에 대하여 대응을 하기위해 비율을 구한다.
        float wRatio = Screen.width / cvs.referenceResolution.x;
        float hRatio = Screen.height/ cvs.referenceResolution.y;
        float ratio = wRatio * (1f - cvs.matchWidthOrHeight) + hRatio * (cvs.matchWidthOrHeight);

        // 비율을 구했다면, 현재 슬롯의 크기가 해상도에 대비하여 얼마인지를 구한다.
        float slotwidth = slotRect.rect.width * ratio;
        float slotheight = slotRect.rect.height * ratio;

        // 틀팁의 크기에 대하여 구한다.
        float width_t = rt.rect.width * ratio;
        float height_t = rt.rect.height * ratio;

        // 초기 위치 우하단
        rt.position = slotRect.position + new Vector3(slotwidth/2 +width_t/2, -slotheight/2 - height_t/2);
        Vector2 pos = rt.position;
        

        // 우측 및 하단이 캔버스 밖으로 나갔는지 확인한다.
        bool right_ = pos.x + width_t > Screen.width;
        bool bottom_ = pos.y - height_t < 0;
        
        if (right_ && bottom_)
        {
            Debug.Log("오른, 아래 모두 잘림");
            rt.position = slotRect.position + new Vector3(-slotwidth/2-width_t/2, slotheight/2 + height_t/2); }
        else if (right_ && !bottom_)
        {
            Debug.Log("오른 잘림");
            rt.position = slotRect.position + new Vector3(-slotwidth/2-width_t/2, -slotheight/2-height_t/2); }
        else if (!right_ && bottom_)
        {
            Debug.Log("아래 잘림");
            rt.position = slotRect.position + new Vector3(slotwidth/2+width_t/2, slotheight/2+height_t/2); }

    }
}
