using UnityEngine;
using System.Collections;

public enum RotationType { MovementRelative, CameraRelative }
public enum HumanoidStateType { Idle, Running, Jumping, Freefalling, Landed, Climbing }
public enum RotationAxis { X, Y, Z }
public class Player : MonoBehaviour
{
	public Texture defaultMouseIcon;
	public RotationType RotationMode;
	public float WalkSpeed = 16;
	public bool Jump = false;
	public GameObject character;
	private HumanoidStateType currentState;
	private Quaternion currentDesiredRot;
	private Vector3 movementVector;

	public GameObject leftShoulder;
	public GameObject rightShoulder;
	public GameObject leftHip;
	public GameObject rightHip;

	public AudioClip landSound;
	public AudioClip jumpSound;
	public AudioClip walkSound;
	public AudioClip fallSound;

	private float fallSpeed = 0;
	private AudioSource fallClip;
	private AudioSource walkClip;
	private bool isClimbing = false;

	//public GameObject rootJoint;
	//public GameObject neck;

	public static Vector3 XZ_VECTOR = new Vector3(1, 0, 1);

	private static Vector3 v3FromFloat(float a)
	{
		return new Vector3(a, a, a);
	}

	private static Vector3 cancelY(Vector3 a)
	{
		return new Vector3(a.x, 0, a.z);
	}

	private bool shouldClimbStairs()
	{
		Rigidbody mover = character.GetComponent<Rigidbody>();
		for (int x = -1; x <= 1; x++)
		{
			for (int z = -1;  z <= 1; z++)
			{
				for (float y = 2.6f; y >= 1; y--)
				{
					Vector3 floor = character.transform.position - new Vector3(x, y, z);
					Vector3 forward = mover.velocity.normalized;
					Debug.DrawRay(floor, forward, Color.red);
					if (Physics.Raycast(floor, forward, 1f))
					{
						Vector3 checkDown = floor + forward + new Vector3(0, 1.3f, 0);
						Ray downCast = new Ray(checkDown, Vector3.down);
						Debug.DrawRay(downCast.origin, downCast.direction, Color.red);
						RaycastHit rayInfo;
						if (Physics.Raycast(downCast, out rayInfo, y-1))
						{
							float yOffset = rayInfo.point.y - floor.y;
							if (yOffset > 0)
							{
								return true;
							}
						}
					}
				}
			}
		}
		
		return false;
	}

	private Vector3 currentMovement;

	public void MovementUpdate()
	{
		movementVector = new Vector3();
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
		{
			movementVector += new Vector3(0, 0, 1);
		}
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
		{
			movementVector += new Vector3(0, 0, -1);
		}
		if (Input.GetKey(KeyCode.A))
		{
			movementVector += new Vector3(-1, 0, 0);
		}
		if (Input.GetKey(KeyCode.D))
		{
			movementVector += new Vector3(1, 0, 0);
		}
		if (Input.GetKey(KeyCode.Space))
		{
			Jump = true;
		}

		movementVector = -movementVector.normalized;
		movementVector.Scale(v3FromFloat(WalkSpeed));

		RootCamera mainCamera = GameObject.FindObjectOfType<RootCamera>();
		Quaternion camRotation = mainCamera.cameraBase.transform.rotation;
		movementVector = camRotation * movementVector;
		currentMovement = Vector3.Lerp(movementVector, currentMovement, 0.1f);
		Rigidbody mover = character.GetComponent<Rigidbody>();
		if (currentMovement.magnitude > 0.1f)
		{
			if (shouldClimbStairs())
			{
				isClimbing = true;
				mover.velocity = new Vector3(currentMovement.x, WalkSpeed, currentMovement.z);
			}
			else
			{
				isClimbing = false;
				mover.velocity = new Vector3(currentMovement.x, mover.velocity.y, currentMovement.z);
			}
		}
		else
		{
			mover.velocity = new Vector3(0, mover.velocity.y, 0);
		}
		if (RotationMode == RotationType.CameraRelative)
		{
			currentDesiredRot = Quaternion.Euler(0, camRotation.eulerAngles.y, 0);
		}
		else if (mover.velocity.magnitude > 1)
		{
			if (currentMovement.magnitude > 0.1f)
			{
				Quaternion currentRotation = character.transform.rotation;
				Quaternion movementRotation = Quaternion.LookRotation(-cancelY(currentMovement));
				float angleBetween = Quaternion.Angle(currentRotation, movementRotation);
				currentDesiredRot = Quaternion.RotateTowards(currentRotation, movementRotation, angleBetween / 10);
			}
		}
		character.transform.rotation = currentDesiredRot;
	}

	public HumanoidStateType GetState()
	{
		return currentState;
	}

	private bool isAny(params HumanoidStateType[] states)
	{
		foreach (HumanoidStateType state in states)
		{
			if (currentState == state)
			{
				return true;
			}
		}
		return false;
	}

	private void playClip(AudioClip clip, float volume = 1)
	{
		AudioSource.PlayClipAtPoint(clip, character.transform.position, volume);
	}

	private AudioSource loadAudioSource(AudioClip clip)
	{
		AudioSource src = gameObject.AddComponent<AudioSource>();
		src.clip = clip;
		return src;
	}

	public void StatusUpdate()
	{
		Rigidbody mover = character.GetComponent<Rigidbody>();
		if (Jump)
		{
			if (currentState != HumanoidStateType.Jumping && currentState != HumanoidStateType.Freefalling)
			{
				if (isAny(HumanoidStateType.Running, HumanoidStateType.Idle))
				{
					currentState = HumanoidStateType.Jumping;
					mover.velocity = mover.velocity + new Vector3(0, 60, 0);
					playClip(jumpSound);
					walkClip.Pause();
				}
			}
		}
		Jump = false;
		if (mover.velocity.y < -0.3f && currentState != HumanoidStateType.Freefalling)
		{
			currentState = HumanoidStateType.Freefalling;
			fallSpeed = 0;
			fallClip.volume = -0.3f;
			fallClip.Play();
		}
		else if (currentState == HumanoidStateType.Freefalling)
		{
			if (isClimbing)
			{
				currentState = HumanoidStateType.Climbing;
				fallClip.Stop();
			}
			else
			{
				walkClip.Pause();
				if (Mathf.Approximately(mover.velocity.y, 0))
				{
					currentState = HumanoidStateType.Landed;
					fallClip.Stop();
					if (fallSpeed > 0.1)
					{
						float volume = Mathf.Clamp((fallSpeed - 50f) / 110f, 0, 1);
						playClip(landSound, volume);
					}
					fallSpeed = 0;
				}
				else
				{
					fallClip.volume += Time.deltaTime;
					fallSpeed = Mathf.Max(Mathf.Abs(mover.velocity.y), fallSpeed);
				}
			}
		}
		else if (currentState != HumanoidStateType.Jumping)
		{
			if (isClimbing)
			{
				currentState = HumanoidStateType.Climbing;
				walkClip.Pause();
			}
			else
			{
				if (currentState == HumanoidStateType.Climbing)
				{
					if (mover.velocity.y < 0.05f)
					{
						currentState = HumanoidStateType.Freefalling;
					}
				}
				else
				{
					float movementSpeed = cancelY(mover.velocity).magnitude;
					if (movementSpeed > 0.1)
					{
						currentState = HumanoidStateType.Running;
						walkClip.UnPause();
						walkClip.pitch = movementSpeed / 8;
					}
					else
					{
						walkClip.Pause();
						currentState = HumanoidStateType.Idle;
					}
				}
			}
		}
		else if (mover.velocity.y < 0.05f)
		{
			currentState = HumanoidStateType.Freefalling;
		}
	}

	private static Quaternion fromRotationAxis(float n, RotationAxis axis = RotationAxis.Z)
	{
		if (axis == RotationAxis.X)
		{
			return Quaternion.Euler(n, 0, 0);
		}
		else if (axis == RotationAxis.Y)
		{
			return Quaternion.Euler(0, n, 0);
		}
		else
		{
			return Quaternion.Euler(0, 0, n);
		}
	}

	private static void RotateTowards(GameObject g, float desiredAngle, float maxVelocity = 0.15f, RotationAxis axis = RotationAxis.X)
	{
		Transform t = g.transform;
		Quaternion rotGoal = fromRotationAxis(desiredAngle + 90, axis);
		maxVelocity *= Mathf.Rad2Deg;
		t.localRotation = Quaternion.RotateTowards(t.localRotation, rotGoal, maxVelocity);
	}

	public void AnimationUpdate()
	{
		if (currentState == HumanoidStateType.Jumping || currentState == HumanoidStateType.Freefalling)
		{
			RotateTowards(rightShoulder, 180, 0.4f);
			RotateTowards(leftShoulder, 180, 0.4f);
			RotateTowards(leftHip, 0);
			RotateTowards(rightHip, 0);
		}
		else
		{
			float amplitude = 0;
			float frequency = 0;
			float climbFudge = 0;
			if (currentState == HumanoidStateType.Climbing)
			{
				amplitude = 1;
				frequency = 10;
				climbFudge = Mathf.PI;
			}
			else if (currentState == HumanoidStateType.Running)
			{
				amplitude = 1;
				frequency = 10;
			}
			else
			{
				amplitude = 0.1f;
				frequency = 1;
			}
			climbFudge *= Mathf.Rad2Deg;
			float desiredAngle = Mathf.Sin(Time.time * frequency) * (amplitude * Mathf.Rad2Deg);
			RotateTowards(rightShoulder, desiredAngle+climbFudge);
			RotateTowards(leftShoulder, -desiredAngle+climbFudge);
			RotateTowards(leftHip, desiredAngle);
			RotateTowards(rightHip, -desiredAngle);
		}
	}

	public void Start()
	{
		currentState = HumanoidStateType.Idle;
		RotationMode = RotationType.MovementRelative;
		fallClip = loadAudioSource(fallSound);
		walkClip = loadAudioSource(walkSound);
		walkClip.loop = true;
		walkClip.Play();
		walkClip.Pause();
	}

	public void Update()
	{
		MovementUpdate();
		StatusUpdate();
		AnimationUpdate();
	}
}
