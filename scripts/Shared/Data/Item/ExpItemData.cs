using UnityEngine;

[CreateAssetMenu(fileName = "ExpItem", menuName = "Scriptable Object/ExpItemData")]
public class ExpItemData : ScriptableObject
{
    public GameObject prefab;
    public int expAmount;
}
