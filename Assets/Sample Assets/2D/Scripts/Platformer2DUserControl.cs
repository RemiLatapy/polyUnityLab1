using UnityEngine;

[RequireComponent(typeof(PlatformerCharacter2D))]
public class Platformer2DUserControl : MonoBehaviour 
{
	private PlatformerCharacter2D character;
    private bool jump;
	private bool m_JumpContinue;
	private int numberFrameIgnore = -1;
	private int counterFrame = 0;

	void Awake()
	{
		character = GetComponent<PlatformerCharacter2D>();
	}

    void Update ()
    {
        // Read the jump input in Update so button presses aren't missed.
#if CROSS_PLATFORM_INPUT
        if (CrossPlatformInput.GetButtonDown("Jump")) jump = true;
		m_JumpContinue = CrossPlatformInput.GetButton("Jump");
//		if (CrossPlatformInput.GetButtonUp("Jump")) Debug.Log ("UP !!!!!");
#else
		if (Input.GetButtonDown("Jump")) jump = true;
		m_JumpContinue = Input.GetButton("Jump");
//		if (Input.GetButtonUp("Jump")) Debug.Log ("UP !!!!!");
#endif

    }

	void FixedUpdate()
	{
		// Read the inputs.
		bool crouch = Input.GetKey(KeyCode.LeftControl);
		#if CROSS_PLATFORM_INPUT
		float h = CrossPlatformInput.GetAxis("Horizontal");
		#else
		float h = Input.GetAxis("Horizontal");
		#endif

		// Pass all parameters to the character control script.
		if (counterFrame > numberFrameIgnore) {
			character.Move (h, crouch);
		} else {
			counterFrame++;
		}

		character.Jump(m_JumpContinue, jump);
		character.Jetpack (m_JumpContinue);

        // Reset the jump input once it has been used.
	    jump = false;
	}

	public void escapeMove (int numberFrames)
	{
		counterFrame = 0;
		numberFrameIgnore = numberFrames;
	}
}
