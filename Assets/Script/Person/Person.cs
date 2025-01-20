using UnityEngine;

public class PersonComponent : MonoBehaviour{
    [Header("ID persona")]
    [SerializeField] private string ID;

    public string GetID(){
        return ID;
    }

    public void SetID(string newID){
        ID = newID;
    }

}