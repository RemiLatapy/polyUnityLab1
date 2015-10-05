using UnityEngine;

public class PlatformerCharacter2D : MonoBehaviour
{
	bool facingRight = true;							// For determining which way the player is currently facing.
	
	[SerializeField]
	float
		maxSpeed = 10f;				// The fastest the player can travel in the x axis.
	[Range(0, 1)]
	[SerializeField]
	float
		crouchSpeedMultiplier = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%

	[Range(0, 1)]
	[SerializeField]
	float
		airMoveControlDegree = .2f;
	[SerializeField]
	bool
		jetpack = true;
	[Range(3, 7)]
	[SerializeField]
	float
		jetpackForce = 4f;			// Amount of force added when the player uses the jetpack
//	[SerializeField]
//	float
//		airForce = 45f;				// Amount of force added when the player move in air.
	[Range(10, 20)]
	[SerializeField]
	float
		jumpForce = 12f;				// Amount of force added when the player jumps.	

	[Range(0, 20)]
	[SerializeField]
	float
		modularJump = 15f;		// Amount of force added when the player held down jump

	[SerializeField]
	int
		numberOfMultipleJump = 3;					// Maximum number of jumps that a player can do for a multi jump

	[Range(0, 1)]
	[SerializeField]
	float
		multipleJumpForceMultiplier = 1f;				// Amount of force added when the player multiple jumps.

	[Range(0, 1)]
	[SerializeField]
	float
		wallJumpVerticalForceMultiplier = 1f;				// Amount of force added when the player multiple jumps.

	[SerializeField]
	float
		wallJumpHorizontalForce = 5f;


//	[SerializeField]
//	bool
//		airControl = false;			// Whether or not a player can steer while jumping;

	[SerializeField]
	LayerMask
		whatIsGround;			// A mask determining what is ground to the character
	[SerializeField]
	LayerMask
		whatIsWall;
	Transform groundCheck;								// A position marking where to check if the player is grounded.
	float groundedRadius = .2f;							// Radius of the overlap circle to determine if grounded
	bool grounded = false;								// Whether or not the player is grounded.
	Transform ceilingCheck;								// A position marking where to check for ceilings
	float ceilingRadius = .01f;							// Radius of the overlap circle to determine if the player can stand up

	Transform wallCheckFront;
	Transform wallCheckBack;
	[Range(0.1f, 0.6f)]
	[SerializeField]
	float
		wallDetectionSensibility = 0.1f;
	Vector2 wallDiagArea = new Vector2 (0.1f, 0.5f);
	bool ignoreJumpAfterWallJump = false;
	Walled walled = new Walled (false);
	Animator anim;										// Reference to the player's animator component.

	int nbJump = 0;
	bool jetpackActive_;								// bool that indicates if the character is using the jetpack
	bool jump = false ;									// bool that indicates if the player is jumping

	float positionGround = 0;
	float positionCeiling = 0;
	private int skipAirMoveNumber = -1;
	private int skipAirMoveCounter = 0;
	private Transform camera;

	void Awake ()
	{
		// Setting up references.
		groundCheck = transform.Find ("GroundCheck");
		ceilingCheck = transform.Find ("CeilingCheck");
		wallCheckBack = transform.Find ("WallCheckBack");
		wallCheckFront = transform.Find ("WallCheckFront");
		anim = GetComponent<Animator> ();
		camera = GameObject.FindGameObjectWithTag ("MainCamera").transform;
		// When the game is started we want the camera to focus on the character
		camera.SendMessage ("StartVertical");
	}

	void OnValidate ()
	{
		if (numberOfMultipleJump < 0) {
			numberOfMultipleJump = 0;
		}
		wallDiagArea = new Vector2 (wallDetectionSensibility, 0.5f);
	}

	void Update ()
	{
		if (jump) {
			//Debug.Log ("StopVertical");
			camera.SendMessage ("StopVertical");
		} else {
			camera.SendMessage ("StartVertical");
		}
	}
	
	void FixedUpdate ()
	{
		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		grounded = Physics2D.OverlapCircle (groundCheck.position, groundedRadius, whatIsGround);
		anim.SetBool ("Ground", grounded);

		// Set the vertical animation
		anim.SetFloat ("vSpeed", rigidbody2D.velocity.y);
	
		walled.Set (Physics2D.OverlapArea ((Vector2)wallCheckBack.position + wallDiagArea, (Vector2)wallCheckBack.position - wallDiagArea, whatIsWall), Physics2D.OverlapArea ((Vector2)wallCheckFront.position + wallDiagArea, (Vector2)wallCheckFront.position - wallDiagArea, whatIsWall));

		// Following the formula : h = v0²/(2*g) with g the gravity less the acceleration of continuous jumping (N*m)
		float heightMax = (Mathf.Pow (jumpForce, 2) / (2 * (Physics.gravity.magnitude * rigidbody2D.gravityScale - modularJump * rigidbody2D.mass)));
		// We apply the condition grounded and if the velocity is null on y because the line where moving otherwise (when the character is taking off)
		if (grounded && rigidbody2D.velocity.y == 0) {
			positionGround = groundCheck.position.y;
			// Added 0,36 because the head is a little bit higher than the ceilingCheck
			positionCeiling = ceilingCheck.position.y + 0.36f;
		}
		Debug.DrawLine (new Vector2 (transform.position.x - 100, positionGround + heightMax), new Vector2 (transform.position.x + 100, positionGround + heightMax), Color.red);
		Debug.DrawLine (new Vector2 (transform.position.x - 100, positionCeiling + heightMax), new Vector2 (transform.position.x + 100, positionCeiling + heightMax), Color.green);
	}

	public void JetpackActivation (bool jetpackActivation) {
		if (!jetpack) {
			return;
		}

		if (jetpackActivation && !grounded && (nbJump == numberOfMultipleJump + 1)) {
			jump = false;
			jetpackActive_ = jetpackActivation;
		}
	}

	public void JetpackMotor (bool jetpackMotor)
	{
		if (jetpackActive_ && jetpackMotor) {
			rigidbody2D.velocity = new Vector2 (rigidbody2D.velocity.x, jetpackForce);
		}
	}


	
	public void Move (float move, bool crouch)
	{
		// If crouching, check to see if the character can stand up
		if (!crouch && anim.GetBool ("Crouch")) {
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if (Physics2D.OverlapCircle (ceilingCheck.position, ceilingRadius, whatIsGround))
				crouch = true;
		}

		// Set whether or not the character is crouching in the animator
		anim.SetBool ("Crouch", crouch);
		
		// Reduce the speed if crouching by the multiplier
		move = ((grounded && crouch) ? move * crouchSpeedMultiplier : move);

		if (grounded) {
			GroundMove (move);
		} else { /* if (airControl || jetpackActive_) */
			AirMove (move);
		}
	}

	void GroundMove (float move)
	{
		// The Speed animator parameter is set to the absolute value of the horizontal input.
		anim.SetFloat ("Speed", Mathf.Abs (move));
		// Move the character
		rigidbody2D.velocity = new Vector2 (move * maxSpeed, rigidbody2D.velocity.y);
		FlipOnMoving (move);
	}

	void AirMove (float move)
	{
				// no control if 0
		if (airMoveControlDegree == 0) {
			return;
		}
		if (skipAirMoveCounter > skipAirMoveNumber + 3) {
			SetAirMove (move);
			FlipOnMoving (move);
		} else
			if (skipAirMoveCounter > skipAirMoveNumber) {
			SetAirMove (move / (5 - (skipAirMoveCounter - skipAirMoveNumber)));
			FlipOnMoving (move);
			skipAirMoveCounter++;
		} else {
			skipAirMoveCounter++;
		}
	}

	void SetAirMove (float move)
	{
		// stop moving when turning
		if ((move < 0 && facingRight) || (move > 0 && !facingRight)) {
			rigidbody2D.velocity = new Vector2 (0, rigidbody2D.velocity.y);
		}
		
		// Add x force if velocity x < maxSpeed
		if (Mathf.Abs (rigidbody2D.velocity.x) < maxSpeed) {
			rigidbody2D.velocity = new Vector2 (rigidbody2D.velocity.x + move * maxSpeed * airMoveControlDegree, rigidbody2D.velocity.y);
//			rigidbody2D.AddForce (new Vector2 (move * maxSpeed * airMoveDegree, 0f));
//			rigidbody2D.velocity = new Vector2 (move * maxSpeed, rigidbody2D.velocity.y);
		}
	}

	public void Jump (bool continuousJump, bool oneJump)
	{
		if (jetpackActive_ && !grounded) {
			return;
		} else if (grounded && oneJump) {
			SingleGroundJump ();
		} else if (continuousJump && !oneJump && rigidbody2D.velocity.y > 0) {
			ContinuousJump ();
		} else if (!grounded && !walled.walled && oneJump && nbJump <= numberOfMultipleJump) {
			MultipleAirJump ();
		} else if (!grounded && oneJump && walled.walled) {
			WallJump ();
		} else if (grounded) {		
			ResetJumpVar ();
		}
	}

	void SingleGroundJump ()
	{
		jump = true;
		// Debug.Log("First normal jump");
		anim.SetBool ("Ground", false);
		// Add a vertical force to the player.
		// Use of impulse because all the force needs to be applied in one shot
		rigidbody2D.AddForce (new Vector2 (0f, jumpForce), ForceMode2D.Impulse);
//		nbJump++;
	}

	void ContinuousJump ()
	{
		// Debug.Log("Continuous jump");
		rigidbody2D.AddForce (new Vector2 (0f, modularJump));
	}

	void MultipleAirJump ()
	{
		//Debug.Log("Multiple jump");
		nbJump++;
		// Change of velocity instead of AddForce (with ForceMode2D.Impulse) because we don't want to take the previous velocity into account
		rigidbody2D.velocity = new Vector2 (rigidbody2D.velocity.x, multipleJumpForceMultiplier * jumpForce);
	}

	void WallJump ()
	{
		// Hardcore solution to avoid noisy move input
		SetSkipAirMove (15);
		//	Debug.Log("Wall jump");
		if (walled.walledFront) {
			Flip ();
		}
		rigidbody2D.velocity = (new Vector2 (rigidbody2D.velocity.x, 0f));
		rigidbody2D.AddForce (new Vector2 (0f, wallJumpVerticalForceMultiplier * jumpForce), ForceMode2D.Impulse);
		if (facingRight) {
			rigidbody2D.velocity = new Vector2 (wallJumpHorizontalForce, rigidbody2D.velocity.y);
		} else {
			rigidbody2D.velocity = new Vector2 (-wallJumpHorizontalForce, rigidbody2D.velocity.y);
		}
	}

	void SetSkipAirMove (int pSkipAirMoveNumber)
	{
		skipAirMoveCounter = 0;
		skipAirMoveNumber = pSkipAirMoveNumber;
	}

	void ResetJumpVar ()
	{
		// Debug.Log("Reset counter");
		jump = false;
		jetpackActive_ = false;
		nbJump = 0;
	}

	void FlipOnMoving (float move)
	{
		// If the input is moving the player right and the player is facing left...
		if (move > 0 && !facingRight) {
			// ... flip the player.
			Flip ();
		}
		// Otherwise if the input is moving the player left and the player is facing right...
		else if (move < 0 && facingRight) {
			// ... flip the player.
			Flip ();
		}
	}
	
	void Flip ()
	{
		// Switch the way the player is labelled as facing.
		facingRight = !facingRight;
		
		// Multiply the player's x local scale by -1.
		Vector3 theScale = transform.localScale;
		theScale.x *= -1;
		transform.localScale = theScale;
	}

	public struct Walled
	{
		public bool walledBack;
		public bool walledFront;
		public bool walled;

		public Walled (bool initialize)
		{
			walledBack = initialize;
			walledFront = initialize;
			walled = initialize;
		}

		public void Set (bool p_walledBack, bool p_walledFront)
		{
			walledBack = p_walledBack;
			walledFront = p_walledFront;
			walled = p_walledBack || p_walledFront;
		}
	}
}
