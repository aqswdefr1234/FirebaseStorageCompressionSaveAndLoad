using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AllObjectsData//서버에 저장하기 위해 오브젝트 정보를 통합한 json파일 클래스
{
    public List<OBJ_DataCustomParsing> allObjectsData = new List<OBJ_DataCustomParsing>();

    public AllObjectsData(List<OBJ_DataCustomParsing> _allObjectsData)
    {
        allObjectsData.AddRange(_allObjectsData);
    }
}
