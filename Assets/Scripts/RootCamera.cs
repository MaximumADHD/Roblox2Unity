using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class RootCamera : MonoBehaviour
{
    private const int MIN_Y = -80;
    private const int MAX_Y = 80;

    private Vector2 MOUSE_SENSITIVITY = new Vector2(Mathf.PI * 4,Mathf.PI * 1.9f);
    private Vector3 HEAD_OFFSET = new Vector3(0, 1.5f, 0);

    private bool ShiftLock = false;
    private bool isFirstPerson;
    private float currentZoom = 10f;
    private bool panning = false;

    public Vector3 location;
    public Vector3 focus;
    public Texture shiftLockCursor;
    public Texture shiftLockActive;
    public Texture shiftLockInactive;

    public Player cameraSubject;
    private Vector2 rotateInput;
    private Mouse cachedMouse;
    private PopperCam popperCam;
    public GameObject cameraBase;

    public bool useBetterInertialScrolling = false;
    public bool cameraEnabled = true;

    private Mouse getMouse()
    {
        if (cachedMouse == null)
        {
            cachedMouse = GameObject.FindObjectOfType<Mouse>();
        }
        return cachedMouse;
    }

    public bool userPanningTheCamera { get { return panning; } }

    public bool GetShiftLock()
    {
        return ShiftLock;
    }

    public Vector3 GetSubjectPosition()
    {
        Vector3 baseLocation = cameraSubject.character.transform.localPosition + HEAD_OFFSET;
		if (ShiftLock && !isFirstPerson)
		{
            baseLocation += cameraSubject.character.gameObject.transform.rotation * new Vector3(-1.5f, 0, 0);
		}
        return baseLocation;
    }

    public Vector3 GetCameraLook()
    {
        return cameraBase.transform.eulerAngles;
    }

    public float GetCameraZoom()
    {
        return currentZoom;
    }

    public float GetCameraActualZoom()
    {
        return Vector3.Distance(GetSubjectPosition(), gameObject.transform.position);
    }

    public Vector2 MouseTranslationToAngle(Vector2 translationVector)
    {
        float xTheta = (translationVector.x / 1920f);
        float yTheta = (translationVector.y / 1200f);
        return new Vector2(xTheta, yTheta);
    }

    public void RotateCamera(Vector2 xyRotateVector)
    {
        float xTheta = xyRotateVector.x * Mathf.Rad2Deg;
        float yTheta = xyRotateVector.y * Mathf.Rad2Deg;
		Vector3 nextAngle = cameraBase.transform.eulerAngles + new Vector3(-yTheta, xTheta, 0);
		float xFix = nextAngle.x;
		if (xFix > 180)
		{
			xFix = -90+(xFix-270);
		}
		xFix = Mathf.Clamp(xFix,MIN_Y,MAX_Y)%360;
		if (xFix != nextAngle.x)
		{
			nextAngle.Scale(new Vector3(xFix/nextAngle.x,1,1));
		}
		cameraBase.transform.eulerAngles = nextAngle;
    }

    public void UpdateMouseBehavior()
    {
        Mouse mouse = getMouse();
        MouseBehavior desiredBehavior;
        if (isFirstPerson || ShiftLock)
        {
            desiredBehavior = MouseBehavior.LockCenter;
            cameraSubject.RotationMode = RotationType.CameraRelative;
        }
        else
        {
            cameraSubject.RotationMode = RotationType.MovementRelative;
            if (panning)
            {
                desiredBehavior = MouseBehavior.LockCurrentPosition;
            }
            else
            {
                desiredBehavior = MouseBehavior.Default;
            }
        }
        if (mouse.MouseBehavior != desiredBehavior)
        {
            mouse.MouseBehavior = desiredBehavior;
        }
        if (ShiftLock)
        {
            mouse.mouseIcon = shiftLockCursor;
        }
        else
        {
            mouse.mouseIcon = cameraSubject.defaultMouseIcon;
        }
    }

    public bool IsInFirstPerson()
    {
        return isFirstPerson;
    }

    public float ZoomCamera(float desiredZoom)
    {
        currentZoom = Mathf.Clamp(desiredZoom, 0.5f, 400);
        UpdateMouseBehavior();
        return currentZoom;
    }

    private static float rk4acceleration(float direction, float p, float v)
    {
        return direction * (Mathf.Max(1f, (p / 3.3f) + 0.5f));
    }

    private float rk4Integrator(float position, float velocity, float t)
    {
        float direction;
        if (velocity < 0)
        {
            direction = -1;
        }
        else
        {
            direction = 1;
		}

        float p1 = position;
        float v1 = velocity;
        float a1 = rk4acceleration(direction, p1, v1);
        float p2 = p1 + v1 * (t / 2);
		float v2 = v1 + a1 * (t / 2);
		float a2 = rk4acceleration(direction, p2, v2);
		float p3 = p1 + v2 * (t / 2);
		float v3 = v1 + a2 * (t / 2);
		float a3 = rk4acceleration(direction, p3, v3);
		float v4 = v1 + a3 * t;

        return position + (v1 + 2 * v2 + 2 * v3 + v4) * (t / 6);
    }

    public float ZoomCameraBy(float zoomScale)
    {
        float zoom = GetCameraActualZoom();
        if (useBetterInertialScrolling)
        {
            zoom = rk4Integrator(zoom, zoomScale, 0.1f);
        }
        else
        {
            zoom = rk4Integrator(zoom, zoomScale, 1);
        }
        ZoomCamera(zoom);
        return zoom;
    }

    public float ZoomCameraFixedBy(float zoomIncrement)
    {
        return ZoomCamera(GetCameraZoom() + zoomIncrement);
    }

    // Input Hookup //

    public bool turningLeft = false;
    public bool turningRight = false;

    public bool zoomEnabled = true;
    public bool panEnabled = true;
    public bool keyPanEnabled = true;

    private const float eight2Pi = Mathf.PI / 4;

    private static float rotateVectorByAngleAndRound(Vector3 camLook, float rotateAngle, float roundAmount)
    {
        if (!camLook.Equals(new Vector3()))
        {
            camLook = camLook.normalized;
            float currentAngle = Mathf.Atan2(camLook.z, camLook.x);
            float newAngle = Mathf.Round((currentAngle + rotateAngle) / roundAmount) * roundAmount;
            return newAngle - currentAngle;
        }
        return 0;
    }

    private void OnMouse2Down()
    {
        panning = true;
        UpdateMouseBehavior();
    }

    private void OnMouse2Up()
    {
        panning = false;
        UpdateMouseBehavior();
    }

    private void OnKeyDown(KeyCode key)
    {
        if (key == KeyCode.I)
        {
            ZoomCameraBy(-5);
        }
        else if (key == KeyCode.O)
        {
            ZoomCameraBy(5);
        }
        if (keyPanEnabled)
        {
            if (key == KeyCode.LeftArrow)
            {
                turningLeft = true;
            }
            else if (key == KeyCode.RightArrow)
            {
                turningRight = true;
            }
            else if (key == KeyCode.Comma)
            {
                Vector3 lookXZ = GetCameraLook();
                lookXZ.Scale(new Vector3(1, 0, 1));
                float angle = rotateVectorByAngleAndRound(lookXZ, -eight2Pi * 0.75f, eight2Pi);
                if (Mathf.Abs(angle) > 0.001f)
                {
                    rotateInput += new Vector2(angle, 0);
                }
            }
            else if (key == KeyCode.Period)
            {
                Vector3 lookXZ = GetCameraLook();
                lookXZ.Scale(new Vector3(1, 0, 1));
                float angle = rotateVectorByAngleAndRound(lookXZ, eight2Pi * 0.75f, eight2Pi);
                if (Mathf.Abs(angle) > 0.001f)
                {
                    rotateInput += new Vector2(angle, 0);
                }
            }
            else if (key == KeyCode.PageUp)
            {
                rotateInput += new Vector2(0, 15 * Mathf.Deg2Rad);
            }
            else if (key == KeyCode.PageDown)
            {
                rotateInput += new Vector2(0, -15 * Mathf.Deg2Rad);
            }
            else if (key == KeyCode.LeftShift)
            {
                ShiftLock = !ShiftLock;
                UpdateMouseBehavior();
            }
        }
    }

    private void OnKeyUp(KeyCode key)
    {
        if (key == KeyCode.LeftArrow)
        {
            turningLeft = false;
        }
        else if (key == KeyCode.RightArrow)
        {
            turningRight = false;
        }
    }

    private void OnMouseWheel(float delta)
    {
        if (zoomEnabled)
        {
            if (useBetterInertialScrolling)
            {
                ZoomCameraBy(-delta / 15);
            }
            else
            {
                ZoomCameraBy(-delta * 1.4f);
            }
        }
    }

    private void onMouseMoved(Vector2 delta)
    {
        if ((panning && panEnabled) || isFirstPerson || ShiftLock)
        {
            Vector2 desiredXYVector = MouseTranslationToAngle(delta);
            desiredXYVector.Scale(MOUSE_SENSITIVITY);
            rotateInput += desiredXYVector;
        }
    }

    private void InputUpdate()
    {
        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            bool isDown = Input.GetKeyDown(key);
            bool isUp = Input.GetKeyUp(key);
            if (isDown)
            {
                if (key == KeyCode.Mouse1)
                {
                    OnMouse2Down();
                }
                else
                {
                    OnKeyDown(key);
                }
            }
            else if (isUp)
            {
                if (key == KeyCode.Mouse1)
                {
                    OnMouse2Up();
                }
                else
                {
                    OnKeyUp(key);
                }
            }
        }
        float mouseWheel = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(mouseWheel) > 0.05f)
        {
            if (mouseWheel < 0)
            {
                mouseWheel = -1;
            }
            else
            {
                mouseWheel = 1;
            }
            OnMouseWheel(mouseWheel);
        }
        Mouse currentMouse = getMouse();
        if (currentMouse)
        {
            if (!currentMouse.delta.Equals(Vector2.zero))
            {
                onMouseMoved(currentMouse.delta);
            }
        }
        float angle = 0;
        if (turningLeft)
        {
            angle = -120;
        }
        else if (turningRight)
        {
            angle = 120;
        }
        rotateInput += new Vector2((angle * Mathf.Deg2Rad) * Time.deltaTime,0);
    }

    public void Start()
    {
        GameObject rotator = new GameObject();
        rotator.transform.parent = transform.parent;
        rotator.name = "Camera";
        transform.parent = rotator.transform;
        rotator.transform.parent = null;
        cameraSubject.character.transform.parent = null;
        location = transform.position;
        focus = cameraSubject.character.transform.position;
        cameraBase = rotator;
        popperCam = gameObject.GetComponent<PopperCam>();
        if (popperCam == null)
        {
            popperCam = gameObject.AddComponent<PopperCam>();
        }
    }

    private bool mouseDownOnBtn = false;

    public void OnGUI()
    {
        Texture tex;
        if (ShiftLock)
        {
            tex = shiftLockActive;
        }
        else
        {
            tex = shiftLockInactive;
        }
        Rect pos = new Rect(12, Screen.height - 43, 31,31);
        GUI.DrawTexture(pos, tex);
        Mouse currentMouse = getMouse();
        Vector2 currentPos = currentMouse.position;
        bool isDown = currentMouse.down;
        if (isDown && !mouseDownOnBtn && pos.Contains(currentPos))
        {
            mouseDownOnBtn = true;
            ShiftLock = !ShiftLock;
            UpdateMouseBehavior();
        }
        else if (!isDown && mouseDownOnBtn)
        {
            mouseDownOnBtn = false;
        }
        
    }

    public void Update()
    {
        rotateInput = new Vector2();
        InputUpdate();
        if (cameraEnabled)
        {
			RotateCamera(rotateInput);
			
			Vector3 subjectPos = GetSubjectPosition();
			transform.localPosition = new Vector3(0, 0, currentZoom);
			transform.parent.position = subjectPos;

            location = transform.position;
			focus = subjectPos;

            popperCam.Pop();
            isFirstPerson = (GetCameraActualZoom() < 2f);

            SkinnedMeshRenderer renderer = cameraSubject.character.GetComponent<SkinnedMeshRenderer>();
            renderer.enabled = !isFirstPerson;
        }

    }
}
