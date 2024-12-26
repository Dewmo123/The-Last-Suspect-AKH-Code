using Server;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartsManager : MonoBehaviour
{
    public Dictionary<int, Parts> partsDic = new Dictionary<int, Parts>();
    public void RemovePartsWithIndex(int index)
    {
        if(partsDic.TryGetValue(index, out var parts))
        {
            Destroy(parts.gameObject);
            partsDic.Remove(index);
        }
    }
    public void CreateParts(PartsInfoBr info)
    {
        Server.PartsType type = info.type;
        GameObject go = type switch
        {
            Server.PartsType.Body => Instantiate(Manager.Data.Body),
            Server.PartsType.Slider => Instantiate(Manager.Data.Slider),
            Server.PartsType.Magazine => Instantiate(Manager.Data.Magazine),
            Server.PartsType.Trigger => Instantiate(Manager.Data.Trigger),
            _=> null,
        };
        var parts = go.GetComponent<Parts>();
        parts.Index = info.Index;
        go.transform.position = info.Pos.FLocationToVector3();
        partsDic.Add(info.Index, parts);
    }
}
