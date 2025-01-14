using UnityEngine;
using UnityEngine.UI;

public class Battery : MonoBehaviour{
    private DatabaseManager databaseManager;
    public Text batteryText;
    public int percentage = 100;
    public bool isActive = false;
    public bool isCharging = false;
    private float timer = 0f;

    void Start(){
        databaseManager = DatabaseManager.GetDatabaseManager();
        if (batteryText != null){
            batteryText.text = "Batteria: " + percentage + "%";
        }else{
            Debug.LogError("BatteryText non è stato assegnato nello script!");
        }
    }

    void Update(){
        if(isActive){
            timer += Time.deltaTime;
            if(isCharging && timer >= 0.5f){
                if (percentage >= 100){
                    timer = 0f;
                    Debug.Log("Batteria carica");
                }else{
                    percentage += 1;
                    timer = 0f;
                    UpdateBatteryText();

                    _ = databaseManager.SetBatteryLevelAsync((int)percentage);
                }
            }else if(!isCharging && timer >= 10f){
                if (percentage <= 0){
                    timer = 0f;
                    Debug.Log("Batteria scarica");
                }else{
                    percentage -= 1;
                    timer = 0f;
                    UpdateBatteryText();

                    _ = databaseManager.SetBatteryLevelAsync((int)percentage);
                }
            } 
        }
    }

    public void SetBatteryPercentage(int percentage){
        this.percentage = percentage;
        UpdateBatteryText();
    }

    void UpdateBatteryText(){
        if (batteryText != null){
            batteryText.text = "Batteria: " + Mathf.Clamp(percentage, 0f, 100f) + "%";
        }
    }
}