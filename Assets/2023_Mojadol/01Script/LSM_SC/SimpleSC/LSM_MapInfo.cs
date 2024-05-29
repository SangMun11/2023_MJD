using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LSM_MapInfo : MonoBehaviour
{
	private static LSM_MapInfo instance;

    public GameObject ground;
	private void Awake()
	{
		if (instance == null)
			instance = this;
		else
			Destroy(this.gameObject);
	}
	public static LSM_MapInfo Instance { get { return instance; } }

	public float Bottom { get { return this.transform.position.z - (5*ground.transform.localScale.z); } }
	public float Top { get { return this.transform.position.z + (5 * ground.transform.localScale.z); } }
	public float Left { get { return this.transform.position.x - (5 * ground.transform.localScale.x); } }
	public float Right { get { return this.transform.position.x + (5 * ground.transform.localScale.x); } }

	private void Start()
	{
		/*
		Debug.Log(string.Format("Top : {0}, Bottom : {1}, Left : {2}, Right : {3}", Top,Bottom,Left,Right));
		PoolManager.Instance.Get_Local_Item(0).transform.position = new Vector3(Left, 100, Top);
		PoolManager.Instance.Get_Local_Item(0).transform.position = new Vector3(Right, 100, Bottom);
		*/
	}
}
