using PacketDotNet.LLDP;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu (menuName ="ToolTip")]
public class LSM_UI_Basic : ScriptableObject
{
    public string name;
    [Multiline]
    public string tooltip;
}
