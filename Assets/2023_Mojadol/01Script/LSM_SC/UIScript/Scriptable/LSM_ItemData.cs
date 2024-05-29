using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MoonHeader;

[CreateAssetMenu (menuName ="LSM_ItemData")]
public class LSM_ItemData : ScriptableObject
{
    public string name;
    public byte code;
    [Multiline] public string content;

    public byte maxCountable;
    public byte hasNum;
    public short price;

    public S_Status alphaStatus;

    public S_Status GetEffect() { return alphaStatus; }
}
