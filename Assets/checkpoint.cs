using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ RequireComponent( typeof( AudioSource ) ) ]
public class checkpoint : MonoBehaviour {

    private Color checkedColor = new Color32( 0, 170, 255, 255 );
    private AudioSource audioSource;
    bool activated = false;

    private void Start() {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter2D( Collider2D collision ) {
        if ( !activated ) {
            if ( collision.gameObject.CompareTag( "Player" ) ) {
                GetComponent<SpriteRenderer>().color = checkedColor;
                audioSource.Play();
                activated = true;
            }
        }
    }
}
