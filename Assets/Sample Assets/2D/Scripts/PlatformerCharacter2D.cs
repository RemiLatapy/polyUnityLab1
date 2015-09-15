﻿using UnityEngine;

public class PlatformerCharacter2D : MonoBehaviour 
{
	bool facingRight = true;							// For determining which way the player is currently facing.

	[SerializeField] float maxSpeed = 10f;				// The fastest the player can travel in the x axis.
	[SerializeField] float jumpForce = 400f;			// Amount of force added when the player jumps.	

	[Range(0, 1)]
	[SerializeField] float crouchSpeed = .36f;			// Amount of maxSpeed applied to crouching movement. 1 = 100%
	[Range(0.8f, 1.5f)]
	[SerializeField] float airSpeed = 1f;				// Amount of maxSpeed applied to jump movement.
	[Range(0, 20)]
	[SerializeField] float continueJumping = 10f;		// Amount of force added when the player held down jump

	[SerializeField] bool airControl = false;			// Whether or not a player can steer while jumping;
	[SerializeField] LayerMask whatIsGround;			// A mask determining what is ground to the character
	
	Transform groundCheck;								// A position marking where to check if the player is grounded.
	float groundedRadius = .2f;							// Radius of the overlap circle to determine if grounded
	bool grounded = false;								// Whether or not the player is grounded.
	Transform ceilingCheck;								// A position marking where to check for ceilings
	float ceilingRadius = .01f;							// Radius of the overlap circle to determine if the player can stand up
	Animator anim;										// Reference to the player's animator component.

	int nbJump=0;
	[SerializeField] int nbJumpMax=3;					// Maximum number of jumps that a player can do for a multi jump

    void Awake()
	{
		// Setting up references.
		groundCheck = transform.Find("GroundCheck");
		ceilingCheck = transform.Find("CeilingCheck");
		anim = GetComponent<Animator>();
	}


	void FixedUpdate()
	{
		// The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
		grounded = Physics2D.OverlapCircle(groundCheck.position, groundedRadius, whatIsGround);
		anim.SetBool("Ground", grounded);

		// Set the vertical animation
		anim.SetFloat("vSpeed", rigidbody2D.velocity.y);
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
			// Reduce the speed if crouching or jumping by the multiplier
			move = ((grounded && crouch) ? move * crouchSpeed : move);
			move = ((!grounded && airControl) ? move * airSpeed : move);

			// The Speed animator parameter is set to the absolute value of the horizontal input.
			anim.SetFloat("Speed", Mathf.Abs(move));

			// Move the character
			rigidbody2D.velocity = new Vector2(move * maxSpeed, rigidbody2D.velocity.y);
			
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
	}

	public void Jump(bool continueJump, bool firstJump)
	{
		// If the player should jump...
		if(grounded && firstJump) {
			Debug.Log ("First jump called");
			// Add a vertical force to the player.
			anim.SetBool("Ground", false);
			rigidbody2D.AddForce(new Vector2(0f, jumpForce));
			nbJump++;
		}
		if (continueJump)  {
			Debug.Log("Continue Jump Called");
			rigidbody2D.AddForce(new Vector2(0f, continueJumping));
		}

		if (!grounded && firstJump && nbJump<nbJumpMax && nbJump>0){
			Debug.Log ("Multi Jump, Grounded : " + grounded);
			nbJump++;
			Debug.Log("nbDeSaut : " + nbJump);
			// Add a vertical force to the player.
			anim.SetBool("Ground", false);
			rigidbody2D.AddForce(new Vector2(0f, jumpForce));
		}

		if(nbJump == nbJumpMax) {
			Debug.Log("NbJump Reseted");
			nbJump=0;
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

		// If player if jumping, reduce is speed
		if (!grounded) {
			rigidbody2D.AddForce (new Vector2 (-10f, 0f));
		}
	}
}
