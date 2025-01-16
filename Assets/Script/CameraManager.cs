using UnityEngine;
using System;

public class CameraManager : MonoBehaviour
{
    public Camera mainCamera;
    public Camera thermalCamera;
    public static event Action<bool> OnThermalCameraToggled;
    public static event Action<bool> OnRedColorToggled;


    private bool isThermalActive = false;
    private bool isOnlyRedActive = false;

    void Update(){
        if (Input.GetKeyDown(KeyCode.T)){
            isThermalActive = !isThermalActive;
            isOnlyRedActive = false;
            OnThermalCameraToggled?.Invoke(isThermalActive);
        }

        if (Input.GetKeyDown(KeyCode.R)){
            isOnlyRedActive = !isOnlyRedActive;
            isThermalActive = false;
            OnRedColorToggled?.Invoke(isOnlyRedActive);
        }
    }
}
