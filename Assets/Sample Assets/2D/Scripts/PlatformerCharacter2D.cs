using UnityEngine;

public class PlatformerCharacter2D : MonoBehaviour 
{
	bool facingRight = true;							// For determining which way the player is currently facing.

	[SerializeField] float maxSpeed = 10f;				// The fastest the player can travel in the x axis on ground.
	[SerializeField] float airForce = 45f;				// Amount of force added when the player move in air.
	[SerializeField] float jumpForce = 400f;			// Amount of force added when the player jumps.	

	[Range(0, 1)]
	[SerializeField] float crouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	[Range(0, 20)]
	[SerializeField] float continueJumping = 10f;		// Amount of force added when the player held down jump
	[SerializeField] float jumpWallHorizontal = 5f;

	[SerializeField] bool airControl = false;			// Whether or not a player can steer while jumping;
	[SerializeField] LayerMask whatIsGround;			// A mask determining what is ground to the character
	[SerializeField] LayerMask whatIsWall;

	Transform groundCheck;								// A position marking where to check if the player is grounded.
	float groundedRadius = .2f;							// Radius of the overlap circle to determine if grounded
	bool grounded = false;								// Whether or not the player is grounded.
	Transform ceilingCheck;								// A position marking where to check for ceilings
	float ceilingRadius = .01f;							// Radius of the overlap circle to determine if the player can stand up

	Transform wallCheckFront;
	Transform wallCheckBack;
	Vector2 wallDiagArea = new Vector2(0.1f, 0.5f);
	bool ignoreJumpAfterWallJump = false;

	Walled walled = new Walled(false);

	Animator anim;										// Reference to the player's animator component.

	int nbJump=0;
	[SerializeField] int nbJumpMax=3;					// Maximum number of jumps that a player can do for a multi jump

	[SerializeField] float jetpackForce = 5f;			// Amount of force added when the player jumps.
	bool jetpackActive = false ;

    void Awake()
	{
		// Setting up references.
		groundCheck = transform.Find("GroundCheck");
		ceilingCheck = transform.Find("CeilingCheck");
		wallCheckBack = transform.Find ("WallCheckBack");
		wallCheckFront = transform.Find ("WallCheckFront");
		anim = GetComponent<Animator>();
	}

	void FixedUpdate()
	{
		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		grounded = Physics2D.OverlapCircle(groundCheck.position, groundedRadius, whatIsGround);
		anim.SetBool("Ground", grounded);

		// Set the vertical animation
		anim.SetFloat("vSpeed", rigidbody2D.velocity.y);

		walled.Set(Physics2D.OverlapArea ((Vector2)wallCheckBack.position + wallDiagArea, (Vector2)wallCheckBack.position - wallDiagArea, whatIsWall), Physics2D.OverlapArea ((Vector2)wallCheckFront.position + wallDiagArea, (Vector2)wallCheckFront.position - wallDiagArea, whatIsWall));

		// JetPack
		//rigidbody2D.velocity = Vector2.ClampMagnitude(rigidbody2D.velocity, maxSpeed);
		jetpackActive = Input.GetButton ("Jump");
		if (jetpackActive && !grounded && nbJump == nbJumpMax) {
			rigidbody2D.velocity = new Vector2(rigidbody2D.velocity.x, jetpackForce);
		}
	}
	
	
	public void Move(float move, bool crouch)
	{
		// If crouching, check to see if the character can stand up
		if(!crouch && anim.GetBool("Crouch"))
		{
			// If the character has a ceiling preventing them from standing up, keep them crouching
			if( Physics2D.OverlapCircle(ceilingCheck.position, ceilingRadius, whatIsGround))
				crouch = true;
		}

		// Set whether or not the character is crouching in the animator
		anim.SetBool("Crouch", crouch);

		//only control the player if grounded or airControl is turned on
		if(grounded || airControl)
		{
			// Reduce the speed if crouching by the multiplier
			move = ((grounded && crouch) ? move * crouchSpeed : move);

			if(grounded) {
				// The Speed animator parameter is set to the absolute value of the horizontal input.
				anim.SetFloat("Speed", Mathf.Abs(move));
				// Move the character
				rigidbody2D.velocity = new Vector2(move * maxSpeed, rigidbody2D.velocity.y);
			} else if (airControl) {
				AirMove(move);
			}

			if(!walled.walled || grounded) {
				FlipOnMoving(move);
			}
		}
	}

	public void Jump(bool continuousClickJump, bool oneClickJump)
	{
		// Because of reset input when jump on wall
		if(ignoreJumpAfterWallJump && continuousClickJump && oneClickJump) {
			// TODO : too late
			Debug.Log("escape");
			ignoreJumpAfterWallJump = false;
			return;
		}

		// First normal jump
		if(grounded && oneClickJump) {
			Debug.Log("First normal jump");
			// Add a vertical force to the player.
			anim.SetBool("Ground", false);
			rigidbody2D.AddForce(new Vector2(0f, jumpForce));
			nbJump++;
			return;
		}

		// Continuous jump
		if (continuousClickJump && !oneClickJump)  {
			//Debug.Log("Continuous jump");
			rigidbody2D.AddForce(new Vector2(0f, continueJumping));
			return;
		}

		// Multiple jump
		if (!grounded && !walled.walled && oneClickJump && nbJump<nbJumpMax) {
			Debug.Log("Multiple jump");
			nbJump++;
			rigidbody2D.velocity = (new Vector2(rigidbody2D.velocity.x, 0f));
			rigidbody2D.AddForce(new Vector2(0f, jumpForce));
			return;
		}

		// Wall jump
		if (!grounded && oneClickJump && walled.walled) {
			// Reset input avoid undesirable moves
			Input.ResetInputAxes();
			ignoreJumpAfterWallJump = true;
			Debug.Log("Wall jump");
			if(walled.walledFront) {
				Flip();
			}
			rigidbody2D.velocity = (new Vector2(rigidbody2D.velocity.x, 0f));
			rigidbody2D.AddForce(new Vector2(0f, jumpForce));
			if(facingRight) {
				rigidbody2D.velocity = new Vector2(jumpWallHorizontal, rigidbody2D.velocity.y);
			} else {
				rigidbody2D.velocity = new Vector2(-jumpWallHorizontal, rigidbody2D.velocity.y);
			}
			return;
		}

		// Reset counter
		if(nbJump != 0 && grounded) {		
			Debug.Log("Reset counter");
			nbJump = 0;
		}
	}

	void FlipOnMoving(float move) {
		// If the input is moving the player right and the player is facing left...
		if(move > 0 && !facingRight) {
			// ... flip the player.
			Flip();
		}
		// Otherwise if the input is moving the player left and the player is facing right...
		else if(move < 0 && facingRight) {
			// ... flip the player.
			Flip();
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

	void AirMove(float move) {
		// stop moving when turning
		if((move < 0 && facingRight) || (move > 0 && !facingRight)) {
			Debug.Log("set 0 velocity x, move :" + move);
			rigidbody2D.velocity = new Vector2(0, rigidbody2D.velocity.y);
		}

		// Add x force if velocity x < maxSpeed
		if (Mathf.Abs(rigidbody2D.velocity.x) < maxSpeed) {
			rigidbody2D.AddForce (new Vector2 (move * airForce, 0f));
		}
	}

	public struct Walled {
		public bool walledBack;
		public bool walledFront;
		public bool walled;

		public Walled(bool initialize) {
			walledBack = initialize;
			walledFront = initialize;
			walled = initialize;
		}

		public void Set(bool p_walledBack, bool p_walledFront) {
			walledBack = p_walledBack;
			walledFront = p_walledFront;
			walled = p_walledBack || p_walledFront;
		}
	}
}
