using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//중복 사용되는 정보들을 구조화 한 파일입니다.
public class Info : MonoBehaviour
{
}

public class CreepInfo  //크립 관련
{
    public float hp;
    public bool isHero;
}

public class PatternInfo    //크립의 패턴 관련
{
    public float cooltime;
    public bool isCool;
    public float dmg;
}
