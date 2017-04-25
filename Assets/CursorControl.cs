using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorControl : MonoBehaviour {

    private float mouseSensitivity = 0.3f;

    private void Start() {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update() {
        transform.position += new Vector3( Input.GetAxis( "Mouse X" ) * mouseSensitivity, Input.GetAxis( "Mouse Y" ) * mouseSensitivity, 0.0f );
    }
}
