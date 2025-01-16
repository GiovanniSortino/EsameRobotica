using UnityEngine;

public class MaterialSwitcher : MonoBehaviour{
    public Material normalMaterial;
    public Material thermalMaterial; 

    private Renderer objRenderer;

    void OnEnable(){
        CameraManager.OnThermalCameraToggled += SwitchMaterial;
        CameraManager.OnRedColorToggled += SwitchRedMaterial;
    }

    void OnDisable(){
        CameraManager.OnThermalCameraToggled -= SwitchMaterial;
        CameraManager.OnRedColorToggled += SwitchRedMaterial;
    }

    void Start(){
        objRenderer = GetComponent<Renderer>();
        if (objRenderer != null){
            objRenderer.material = normalMaterial;
        }
    }

    void SwitchMaterial(bool isThermalActive){  
        if (objRenderer != null){
            objRenderer.material = isThermalActive ? thermalMaterial : normalMaterial;
        }
    }

    void SwitchRedMaterial(bool isOnlyRedActive){
        if (objRenderer != null){
            if(thermalMaterial.shader.name != "Standard"){
                objRenderer.material = isOnlyRedActive ? thermalMaterial : normalMaterial;
            }else{
                objRenderer.material = normalMaterial;
            }
        }
    }
}
