using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;


// Ÿ�̸� ��ũ��Ʈ
public class LSM_TimerSc : MonoBehaviourPunCallbacks, IPunObservable
{
	public GameObject timerPannel;			// Ÿ�̸� UI
											// # Canvas�� �ڽ� ������Ʈ �� TimerPannel
    public TextMeshProUGUI timerT;			// Ÿ�̸Ӹ� ǥ������ UI
											// # Canvas�� �ڽ� ������Ʈ ���� TimerPannel�� �ڽ� ������Ʈ Timer
	public bool startTimer = false, limitTimeSetting, reverse;
    float timer, limitS;

	#region PhotonView Variable
	private PhotonView pv;
	#endregion

	#region IPunObservable Implementation -0408 PSH
	public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{

		if (stream.IsWriting)
		{
			stream.SendNext(timer);
			stream.SendNext(limitS);
			stream.SendNext(startTimer);
			stream.SendNext(limitTimeSetting);
			stream.SendNext(reverse);
			stream.SendNext(timerPannel.activeSelf);
		}
		else
		{
			this.timer = (float)stream.ReceiveNext();
			this.limitS = (float)stream.ReceiveNext(); 
			this.startTimer = (bool)stream.ReceiveNext();
			this.limitTimeSetting = (bool)stream.ReceiveNext();
			this.reverse = (bool)stream.ReceiveNext();
			timerPannel.SetActive((bool)stream.ReceiveNext());
		}
	}
	#endregion


	private void Start()
	{
		// ���� �ʱ�ȭ
		timer = 0;
		reverse = false;
		timerPannel.SetActive(false);
		startTimer = false;
		pv = this.gameObject.GetComponent<PhotonView>();
	}

	private void Update()
	{
        // UI ���� ó��
        TimerText();
        // Ÿ�̸Ӱ� ���۵Ǿ��ٸ�
        if (startTimer && PhotonNetwork.IsMasterClient)
		{
			// �������� ���Ͽ� ������ ���� Ȯ��.
			timer += Time.deltaTime * (reverse ? -1 : 1);

			// �ִ� �ð��� �������ִ��� Ȯ��. �� reverse Ȯ��.
			if (timer >= limitS && limitTimeSetting && !reverse)
			{ GameManager.Instance.TimeOutProcess(); TimerStop(); }
			else if (reverse && timer <= 0 && limitTimeSetting)
			{ GameManager.Instance.TimeOutProcess(); TimerStop(); }
		}
	}

	// Ÿ�̸� �ð��� ���� �ؽ�Ʈ ����
	public void TimerText()
	{
		timerT.text = ((timer / 60 > 0) ? (int)timer / 60 : 0) + " : " + (timer%60 < 10? "0":"")+((int)timer % 60);
	}


	// Ÿ�̸� �����ε�.
	// �Ű������� ���� Ÿ�̸� ����. -> ���ѽð��� ���� Ÿ�̸ӽ���.
	public void TimerStart(){ TimerText(); timerPannel.SetActive(true) ; startTimer = true;}
	// �Ű������� �ϳ� �ִ� Ÿ�̸� ���� -> ���ѽð��� �����ϸ�, �ش� �ð��� �帣�� TimeStop�Լ��� ȣ��
	public void TimerStart(float maxTime) { TimerStart(maxTime, false); }
	// �Ű������� �ΰ� �ִ� Ÿ�̸� ���� -> ���ѽð��� �����ϸ�, bool���� true�� ������ ��� ���� �ð��ʿ��� ���� �پ��� Ÿ�̸Ӱ� ����.
	public void TimerStart(float maxTime, bool rev) { timer = (rev? maxTime : 0); limitS = (rev? 0:maxTime); limitTimeSetting = true; reverse = rev; TimerStart(); }

	// Ÿ�̸Ӱ� ���������� ����� ��� Ÿ�̸� ���� ������ �ʱ�ȭ.
	public void TimerStop() { timer = 0; timerPannel.SetActive(false); startTimer = false; limitTimeSetting = false; limitS = 0; reverse = false; }

	// ��ŵ��ư�� Ŭ�� �� ����Ǵ� �Լ�. �ٷ� Ÿ�̸Ӱ� ����ǰ� ����.
	public void TimerOut() { 
		//GameManager.Instance.TimeOutProcess(); TimerStop();
		pv.RPC("RPC_TimerOut", RpcTarget.MasterClient);
	}

	#region RPC Methods 0408 - PSH

	[PunRPC]
	void RPC_TimerOut()
	{
		GameManager.Instance.TimeOutProcess();
		TimerStop();
	}

	#endregion

	// Getter
	public float Get() { return timer; }

}
