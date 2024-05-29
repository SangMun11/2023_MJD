using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LSM_DamagedDirection : MonoBehaviour
{
    private GameObject obj;
    private Vector3 origin_forward, origin, damaged;
    private bool setting_b;
    private Image image_d;
    private float timer;

    public void SpawnSetting(GameObject go, Vector3 d)
    {
        obj = go;
        damaged = d;
        setting_b = true;
        timer = 0;
        Destroy(this.gameObject, 1.5f);
    }
	private void Awake()
	{
		image_d = this.GetComponent<Image>();
	}
	

    void Update()
    {
        if (!setting_b)
            return;
        timer += Time.deltaTime;

        origin_forward = obj.transform.forward;
        origin = obj.transform.position;

        // xz를 면으로하는 2D좌표로 피격지점을 구함
        Vector3 dam_dir = (Vector2D_XZ(damaged) - Vector2D_XZ(origin)).normalized;
        Vector3 right_dir = Vector3.Cross(origin_forward, Vector3.up);
        float angle_d = Vector3.Angle(Vector2D_XZ(origin_forward), Vector2D_XZ(dam_dir));
        float sign_d = Mathf.Sign(Vector3.Dot(Vector2D_XZ(dam_dir), right_dir));
        float z_d = angle_d * sign_d;
        this.transform.rotation = Quaternion.Euler(0,0,z_d);

        byte dummy_a = (byte)(130 * (1-timer / 6f));
        image_d.color = new Color32(240,78,78,dummy_a);
    }

    private Vector3 Vector2D_XZ(Vector3 v) { return Vector3.Scale(v, Vector3.one - Vector3.up); }
}
