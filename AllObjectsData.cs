using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AllObjectsData//������ �����ϱ� ���� ������Ʈ ������ ������ json���� Ŭ����
{
    public List<OBJ_DataCustomParsing> allObjectsData = new List<OBJ_DataCustomParsing>();

    public AllObjectsData(List<OBJ_DataCustomParsing> _allObjectsData)
    {
        allObjectsData.AddRange(_allObjectsData);
    }
}
