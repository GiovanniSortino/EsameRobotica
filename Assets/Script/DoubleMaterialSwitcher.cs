using UnityEngine;
 
public class DoubleMaterialSwitcher : MonoBehaviour
{
    public Material normalMaterial1;
    public Material normalMaterial2;
    public Material thermalMaterial1;
    public Material thermalMaterial2;
 
    private Renderer objRenderer;
 
    void OnEnable(){
        CameraManager.OnThermalCameraToggled += SwitchMaterial;
        CameraManager.OnRedColorToggled += SwitchRedMaterial;
    }
 
    void OnDisable(){
        CameraManager.OnThermalCameraToggled -= SwitchMaterial;
        CameraManager.OnRedColorToggled -= SwitchRedMaterial;
    }
 
    void Start(){
        objRenderer = GetComponent<Renderer>();
         if (objRenderer != null){
            objRenderer.materials = new Material[] { normalMaterial1, normalMaterial2 };
        }
    }

    void SwitchMaterial(bool isThermalActive){  
        if (objRenderer != null){
            objRenderer.materials = isThermalActive
                ? new Material[] { thermalMaterial1, thermalMaterial2 }
                : new Material[] { normalMaterial1, normalMaterial2 };
        }
    }

    void SwitchRedMaterial(bool isThermalActive){
        if (objRenderer != null){
            if(thermalMaterial2.shader.name != "Standard" && normalMaterial2.shader.name != "Standard"){
                objRenderer.materials = isThermalActive ? new Material[] { thermalMaterial1, thermalMaterial2 } : new Material[] { normalMaterial1, normalMaterial2 };
            }else{
                objRenderer.materials = new Material[] { normalMaterial1, normalMaterial2 };
            }
        }
    }

}