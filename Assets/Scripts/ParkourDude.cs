using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ RequireComponent( typeof( AudioSource ) ) ]
public class ParkourDude : MonoBehaviour {

    public bool grounded;
    public Edge ground;
    public float groundAngle;
    private float minGroundAngle = 0.0f;
    private float maxGroundAngle = 30.0f;
    private Vector2 relativeGroundCheckPosition = Vector2.down * 0.9f; // Bottom edge of the dude;
    private Vector2 worldGroundCheckPosition;
    private Vector2 groundCheckHalfRectangle = new Vector2( 0.35f, 0.1f );
    public float climbToJumpCooldown = 1.0f;
    private float currentClimbToJumpCooldown = 0.0f;
    public Vector2 runningVelocity = new Vector2( 2.5f, 0.0f );

    public bool climb;
    public Edge wall;
    public float wallAngle;
    private float minClimbingAngle = 30.0f;
    private float maxClimbingAngle = 120.0f;
    private Vector2 relativeClimbCheckPosition = new Vector2( 0.35f, 0.0f ); // Right edge of the dude
    private Vector2 worldClimbCheckPosition;
    private Vector2 climbCheckHalfRectangle = new Vector2( 0.35f, 0.85f );
    public Vector2 climbingVelocity = new Vector2( 0.8f, 0.0f );

    public Vector3 velocity = Vector3.zero;
    public Vector2 gravity = new Vector2( 0.0f, -0.3f );
    public float maxFallSpeed = -10.0f;

    private Vector3 respawnPosition;

    private AudioSource audioSource;
    enum AudioPlaying { none, jump, climb, fall, run, win, respawn };
    private AudioPlaying currentAudioPlaying;
    private float soundClipChangeCooldown = 0.3f;
    private float currentSoundClipChangeCooldown = 0.0f;
    public AudioClip[] jumpSoundEffects;
    public AudioClip[] climbingSoundEffects;
    public AudioClip[] fallingSoundEffects;
    public AudioClip[] runningSoundEffects;
    public AudioClip[] respawnSoundEffects;
    public AudioClip[] winSoundEffects;

    private bool win = false;

	// Use this for initialization
	void Start () {
        updateRespawnPosition();
        audioSource = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        resetValues();
        getMovementStatesFromSurroundings();
        move();
	}

    void updateRespawnPosition () {
        respawnPosition = transform.position + Vector3.up * 2.0f;
    }

    void respawn() {
        transform.position = respawnPosition;
        velocity = Vector2.zero;
        playRespawnSound();
    }

    void resetValues() {
        currentClimbToJumpCooldown = Mathf.Max( 0.0f, currentClimbToJumpCooldown - Time.fixedDeltaTime );
        currentSoundClipChangeCooldown = Mathf.Max( 0.0f, currentSoundClipChangeCooldown - Time.fixedDeltaTime );
        grounded = false;
        climb = false;
        worldGroundCheckPosition = ( Vector2 )transform.position + relativeGroundCheckPosition;
        worldClimbCheckPosition = ( Vector2 )transform.position + relativeClimbCheckPosition;
    }

    void getMovementStatesFromSurroundings() {
        testClimb();
        if ( !climb ) {
            testGrounded();
        }
    }

    void testGrounded() {
        ground = null;
        Collider2D[] possibleGroundColliders = getPossibleGroundColliders();
        foreach ( Collider2D possibleGround in possibleGroundColliders ) {
            if ( isColliderGround( possibleGround ) ) {
                setGround( possibleGround );
            }
        }
    }

    Collider2D[] getPossibleGroundColliders() {
        Vector2 groundCheckSquareBottomLeftPosition = worldGroundCheckPosition - groundCheckHalfRectangle + new Vector2( 0.0f, groundCheckHalfRectangle.y );
        Vector2 groundCheckSquareTopRightPosition = worldGroundCheckPosition + groundCheckHalfRectangle + new Vector2( 0.0f, groundCheckHalfRectangle.y );
        return Physics2D.OverlapAreaAll( groundCheckSquareBottomLeftPosition, groundCheckSquareTopRightPosition );
    }

    bool isColliderGround( Collider2D collider ) {
        Edge edge = collider.GetComponent<Edge>();
        if ( edge != null && ( edge.isRunnable || edge.isCorner ) ) {
            float? colliderAngle = getGroundColliderAngle( collider );
            if ( colliderAngle != null ) {
                if ( colliderAngle >= minGroundAngle && colliderAngle <= maxGroundAngle ) {
                    return true;
                }
            }
        }
        return false;
    }

    float? getGroundColliderAngle( Collider2D collider ) {
        // RaycastHit2D[] hits = Physics2D.LinecastAll( transform.position, worldGroundCheckPosition * 1.1f ); // Reach a little farther just to be safe
        Vector2 groundCheckRectangleBottomLeftPosition = worldGroundCheckPosition - groundCheckHalfRectangle + new Vector2( 0.0f, groundCheckHalfRectangle.y );
        Vector2 groundCheckSquareSize = groundCheckHalfRectangle * 2.0f;
        RaycastHit2D[] hits = Physics2D.BoxCastAll( groundCheckRectangleBottomLeftPosition, groundCheckSquareSize, 0.0f, relativeGroundCheckPosition.normalized );
        foreach ( RaycastHit2D hit in hits ) {
            if ( hit.collider.gameObject == collider.gameObject ) {
                return Vector3.Angle( Vector3.up, hit.normal );
            }
        }
        return null;
    }

    void setGround( Collider2D collider ) {
        groundAngle = ( float )getGroundColliderAngle( collider );
        ground = collider.GetComponent<Edge>();
        grounded = true;
        moveToGroundPosition();
    }

    void testClimb() {
        wall = null;
        Collider2D[] possibleWallColliders = getPossibleWallColliders();
        foreach ( Collider2D possibleWall in possibleWallColliders ) {
            if ( isColliderWall( possibleWall ) ) {
                setWall( possibleWall );
            }
        }
    }

    Collider2D[] getPossibleWallColliders() {
        Vector2 wallCheckRectangleBottomLeftPosition = worldClimbCheckPosition - climbCheckHalfRectangle - new Vector2( climbCheckHalfRectangle.x, -0.2f );
        Vector2 wallCheckRectangleSize = worldClimbCheckPosition + climbCheckHalfRectangle - new Vector2( climbCheckHalfRectangle.x, -0.2f );
        return Physics2D.OverlapAreaAll( wallCheckRectangleBottomLeftPosition, wallCheckRectangleSize );
    }

    bool isColliderWall( Collider2D collider ) {
        Edge edge = collider.GetComponent<Edge>();
        if ( edge != null && ( edge.isClimbable || edge.isCorner ) ) {
            float? colliderAngle = getWallColliderAngle( collider );
            if ( colliderAngle != null ) {
                if ( colliderAngle >= minClimbingAngle && colliderAngle <= maxClimbingAngle ) {
                    return true;
                }
            }
        }
        return false;
    }

    float? getWallColliderAngle( Collider2D collider ) {
        Vector2 wallCheckRectangleBottomLeftPosition = worldClimbCheckPosition - climbCheckHalfRectangle + new Vector2( 0.0f, 0.1f ) - new Vector2( climbCheckHalfRectangle.x, 0.0f );
        Vector2 wallCheckRectangleTopRightPosition = climbCheckHalfRectangle * 2.0f;
        RaycastHit2D[] hits = Physics2D.BoxCastAll( wallCheckRectangleBottomLeftPosition, wallCheckRectangleTopRightPosition, 0.0f, relativeClimbCheckPosition.normalized );
        foreach ( RaycastHit2D hit in hits ) {
            if ( hit.collider.gameObject == collider.gameObject ) {
                return Vector3.Angle( Vector3.up, hit.normal );
            }
        }
        return null;
    }

    void setWall( Collider2D collider ) {
        wallAngle = ( float )getWallColliderAngle( collider );
        wall = collider.GetComponent<Edge>();
        climb = true;
        moveToWallPosition();
        currentClimbToJumpCooldown = climbToJumpCooldown;
    }

    void moveToGroundPosition() {
        /* // RaycastHit2D[] hits = Physics2D.LinecastAll( transform.position, worldGroundCheckPosition );
        Vector2 groundCheckRectangleBottomLeftPosition = worldGroundCheckPosition - groundCheckHalfRectangle + new Vector2( 0.0f, groundCheckHalfRectangle.y );
        Vector2 groundCheckSquareSize = groundCheckHalfRectangle * 2.0f;
        RaycastHit2D[] hits = Physics2D.BoxCastAll( groundCheckRectangleBottomLeftPosition, groundCheckSquareSize, 0.0f, relativeGroundCheckPosition.normalized );
        foreach ( RaycastHit2D hit in hits ) {
            if ( hit.collider.gameObject == ground.gameObject ) {
                transform.position = new Vector3( transform.position.x,
                                                  hit.point.y + 0.95f,
                                                  transform.position.z );
            }
        } */
        // transform.position += Vector3.up * 0.02f;
    }

    void moveToWallPosition() {
        // RaycastHit2D[] hits = Physics2D.LinecastAll( transform.position, worldClimbCheckPosition );
        Vector2 wallCheckRectangleBottomLeftPosition = worldClimbCheckPosition - climbCheckHalfRectangle - new Vector2( climbCheckHalfRectangle.x, 0.0f );
        Vector2 wallCheckRectangleSize = climbCheckHalfRectangle * 2.0f;
        RaycastHit2D[] hits = Physics2D.BoxCastAll( wallCheckRectangleBottomLeftPosition, wallCheckRectangleSize, 0.0f, relativeClimbCheckPosition.normalized );
        foreach ( RaycastHit2D hit in hits ) {
            if ( hit.collider.gameObject == wall.gameObject ) {
                transform.position = new Vector3( hit.point.x - relativeClimbCheckPosition.x,
                                                  transform.position.y,
                                                  transform.position.z );
            }
        }
    }

    void move() {
        if ( climb ) {
            if ( !win ) {
                velocity = new Vector2( climbingVelocity.x * Mathf.Cos( wallAngle * Mathf.Deg2Rad ), climbingVelocity.x * Mathf.Sin( wallAngle * Mathf.Deg2Rad ) ); //climbingVelocity;
                playClimbingSound();
                if ( wall.isCorner ) {
                    velocity += Vector3.up * 5.0f;
                    velocity += ( Vector3 )runningVelocity;
                    playJumpSound();
                }
            }
        } else {
            if ( grounded ) {
                if ( !win ) {
                    // velocity = runningVelocity;
                    velocity = new Vector2( runningVelocity.x * Mathf.Cos( groundAngle * Mathf.Deg2Rad ), runningVelocity.x * Mathf.Sin( groundAngle * Mathf.Deg2Rad ) ); //runningVelocity;
                    if ( ground.isCorner && currentClimbToJumpCooldown <= 0.0f ) {
                        velocity += Vector3.up * 5.0f;
                        playJumpSound();
                    }
                    playRunningSound();
                }
            } else {
                if ( velocity.y > maxFallSpeed ) {
                    velocity = ( Vector2 )velocity + gravity;
                    velocity.y = Mathf.Max( velocity.y, maxFallSpeed );
                }
                if ( velocity.y <= -10.0f ) {
                    playFallingSound();
                }
            }
        }

        if ( win ) {
            playWinSound();
            velocity.x = 0.0f;
            if ( grounded ) {
                velocity += Vector3.up * 1.0f;
            }
        }

        transform.position += velocity * Time.fixedDeltaTime;
    }

    private void OnTriggerEnter2D( Collider2D collision ) {
        Collider2D collider = GetComponent<Collider2D>();
        if ( Physics2D.IsTouchingLayers( collider, LayerMask.GetMask( "Lava" ) ) ) {
            respawn();
        }
        if ( Physics2D.IsTouchingLayers( collider, LayerMask.GetMask( "Checkpoint" ) ) ) {
            updateRespawnPosition();
        }
        if ( Physics2D.IsTouchingLayers( collider, LayerMask.GetMask( "Win" ) ) ) {
            win = true;
        }
    }

    private void playJumpSound() {
        if ( currentSoundClipChangeCooldown <= 0.0f ) {
            // if ( currentAudioPlaying != AudioPlaying.jump ) {
            if ( jumpSoundEffects.Length > 0 ) {
                audioSource.clip = jumpSoundEffects[ Random.Range( 0, jumpSoundEffects.Length - 1 ) ];
                audioSource.loop = false;
                audioSource.Play();
                currentAudioPlaying = AudioPlaying.jump;
            }
            // }
            currentSoundClipChangeCooldown = soundClipChangeCooldown;
        }
    }

    private void playRunningSound() {
        if ( currentSoundClipChangeCooldown <= 0.0f ) {
            if ( currentAudioPlaying != AudioPlaying.run ) {
                if ( runningSoundEffects.Length > 0 ) {
                    audioSource.clip = runningSoundEffects[ Random.Range( 0, runningSoundEffects.Length - 1 ) ];
                    audioSource.loop = true;
                    audioSource.Play();
                    currentAudioPlaying = AudioPlaying.run;
                }
            }
            // currentSoundClipChangeCooldown = soundClipChangeCooldown;
        }
    }

    private void playClimbingSound() {
        if ( currentSoundClipChangeCooldown <= 0.0f ) {
            if ( currentAudioPlaying != AudioPlaying.climb ) {
                if ( climbingSoundEffects.Length > 0 ) {
                    audioSource.clip = climbingSoundEffects[ Random.Range( 0, climbingSoundEffects.Length - 1 ) ];
                    audioSource.loop = false;
                    audioSource.Play();
                    currentAudioPlaying = AudioPlaying.climb;
                }
            }
            // currentSoundClipChangeCooldown = soundClipChangeCooldown;
        }
    }

    private void playFallingSound() {
        if ( currentSoundClipChangeCooldown <= 0.0f ) {
            if ( currentAudioPlaying != AudioPlaying.fall ) {
                if ( fallingSoundEffects.Length > 0 ) {
                    audioSource.clip = fallingSoundEffects[ Random.Range( 0, fallingSoundEffects.Length - 1 ) ];
                    audioSource.loop = false;
                    audioSource.Play();
                    currentAudioPlaying = AudioPlaying.fall;
                }
            }
            // currentSoundClipChangeCooldown = soundClipChangeCooldown;
        }
    }

    private void playRespawnSound() {
        if ( currentSoundClipChangeCooldown <= 0.0f ) {
            if ( respawnSoundEffects.Length > 0 ) {
                audioSource.clip = respawnSoundEffects[ Random.Range( 0, respawnSoundEffects.Length - 1 ) ];
                audioSource.loop = false;
                audioSource.Play();
                currentAudioPlaying = AudioPlaying.respawn;
            }
            // currentSoundClipChangeCooldown = soundClipChangeCooldown;
        }
    }

    private void playWinSound() {
        if ( currentSoundClipChangeCooldown <= 0.0f ) {
            if ( currentAudioPlaying != AudioPlaying.win ) {
                if ( winSoundEffects.Length > 0 ) {
                    audioSource.clip = winSoundEffects[ Random.Range( 0, winSoundEffects.Length - 1 ) ];
                    audioSource.loop = false;
                    audioSource.Play();
                    currentAudioPlaying = AudioPlaying.win;
                }
            }
            // currentSoundClipChangeCooldown = soundClipChangeCooldown;
        }
    }
}
