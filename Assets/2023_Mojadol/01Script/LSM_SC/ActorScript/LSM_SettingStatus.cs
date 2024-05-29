using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MoonHeader;

public class LSM_SettingStatus : MonoBehaviour
{
    public S_ActorStatus_LV[] lvStatus;
    public int maxLV;


    private static LSM_SettingStatus instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        maxLV = 8;
        lvStatus = new S_ActorStatus_LV[System.Enum.GetNames(typeof(ActorType)).Length];
        for (int i = 0; i < lvStatus.Length; i++) { lvStatus[i] = new S_ActorStatus_LV((ActorType)i, maxLV);}

        for (int i = 0; i < maxLV; i++)
        {
            lvStatus[(int)ActorType.Knight].setStatus_LV(i, (short)(380 + i * 200), (short)(500 + i * 100), (short)(60 + 30 * i));
            lvStatus[(int)ActorType.Magicion].setStatus_LV(i, (short)(380 + i * 200), (short)(400 + i * 80), (short)(48 + 24 * i));

            lvStatus[(int)ActorType.Minion_Melee].setStatus_LV(i, (short)(3 * 60 * i), (short)(100 + 50 * i), (short)(5 + 5 * i));
            lvStatus[(int)ActorType.Minion_Range].setStatus_LV(i, (short)(3 * 60 * i), (short)(70 + 35 * i), (short)(10 + 10 * i));
            lvStatus[(int)ActorType.Turret].setStatus_LV(i, (short)(3 * 60 * i), (short)(5000), (short)(45 + 25 * i));
            lvStatus[(int)ActorType.Turret_Base].setStatus_LV(i, (short)(3 * 60 * i), (short)(3000), (short)(60 + 35 * i));

            lvStatus[(int)ActorType.Creep_Golem].setStatus_LV(i, (short)(3 * 60 * i), (short)(900 + 360*i), (short)(5 + 5 * i));
            lvStatus[(int)ActorType.Creep_Magition].setStatus_LV(i, (short)(3 * 60 * i), (short)(1200 + 480 * i), (short)(25 * i));
        }
    }

    public static LSM_SettingStatus Instance { get { return instance; } }
}
