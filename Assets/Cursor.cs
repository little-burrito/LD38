using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cursor : MonoBehaviour {

    private float mouseSensitivity = 0.3f;

    private void Update() {
        transform.position += new Vector3( Input.GetAxis( "Mouse X" ) * mouseSensitivity, Input.GetAxis( "Mouse Y" ) * mouseSensitivity, 0.0f );
    }
}
