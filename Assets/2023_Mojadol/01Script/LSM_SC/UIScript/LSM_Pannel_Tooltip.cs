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

    // ��ó : https://rito15.github.io/posts/unity-study-rpg-inventory/#%EC%95%84%EC%9D%B4%ED%85%9C%EA%B3%BC-%EC%95%84%EC%9D%B4%ED%85%9C-%EB%8D%B0%EC%9D%B4%ED%84%B0
    // ��ȯ�� ������ ĵ���� ������ ������ ���� �����ϱ�����.
    public void SetRectPosition(RectTransform slotRect)
    {
        // ���� ĵ������ ũ���, �ػ󵵿� ���Ͽ� ������ �ϱ����� ������ ���Ѵ�.
        float wRatio = Screen.width / cvs.referenceResolution.x;
        float hRatio = Screen.height/ cvs.referenceResolution.y;
        float ratio = wRatio * (1f - cvs.matchWidthOrHeight) + hRatio * (cvs.matchWidthOrHeight);

        // ������ ���ߴٸ�, ���� ������ ũ�Ⱑ �ػ󵵿� ����Ͽ� �������� ���Ѵ�.
        float slotwidth = slotRect.rect.width * ratio;
        float slotheight = slotRect.rect.height * ratio;

        // Ʋ���� ũ�⿡ ���Ͽ� ���Ѵ�.
        float width_t = rt.rect.width * ratio;
        float height_t = rt.rect.height * ratio;

        // �ʱ� ��ġ ���ϴ�
        rt.position = slotRect.position + new Vector3(slotwidth/2 +width_t/2, -slotheight/2 - height_t/2);
        Vector2 pos = rt.position;
        

        // ���� �� �ϴ��� ĵ���� ������ �������� Ȯ���Ѵ�.
        bool right_ = pos.x + width_t > Screen.width;
        bool bottom_ = pos.y - height_t < 0;
        
        if (right_ && bottom_)
        {
            Debug.Log("����, �Ʒ� ��� �߸�");
            rt.position = slotRect.position + new Vector3(-slotwidth/2-width_t/2, slotheight/2 + height_t/2); }
        else if (right_ && !bottom_)
        {
            Debug.Log("���� �߸�");
            rt.position = slotRect.position + new Vector3(-slotwidth/2-width_t/2, -slotheight/2-height_t/2); }
        else if (!right_ && bottom_)
        {
            Debug.Log("�Ʒ� �߸�");
            rt.position = slotRect.position + new Vector3(slotwidth/2+width_t/2, slotheight/2+height_t/2); }

    }
}
