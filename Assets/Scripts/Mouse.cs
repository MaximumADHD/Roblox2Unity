using UnityEngine;
using System.Collections;

public enum MouseBehavior { Default, LockCenter, LockCurrentPosition };

public class Mouse : MonoBehaviour
{
	private int posX = (int)(Screen.width / 2);
	private int posY = (int)(Screen.height / 2);
	public MouseBehavior MouseBehavior = MouseBehavior.Default;
	public Texture mouseIcon;
	public Vector2 delta;
	public Vector2 position;
	public bool down = false;

	private Vector2 internalMousePos;
	private bool hasInitializedPos = true;
	
	private void updateInternalMousePos()
	{
		float h = Input.GetAxis("Mouse X");
		float v = Input.GetAxis("Mouse Y");
		delta = new Vector2(h, v);
		Vector2 nextMousePos = internalMousePos + new Vector2(h, v);
		if (nextMousePos.x >= 0)
		{
			if (nextMousePos.x <= Screen.width)
			{
				if (nextMousePos.y >= 0)
				{
					if (nextMousePos.y <= Screen.height)
					{
						internalMousePos = nextMousePos;
						return;
					}
				}
			}
		}
	}

	public void Start()
	{
		internalMousePos = new Vector2(posX, posY);
	}

	public void Update()
	{
		updateInternalMousePos();
		if (MouseBehavior.Equals(MouseBehavior.Default))
		{
			if (!hasInitializedPos)
			{
				hasInitializedPos = true;
				posX = (int)(internalMousePos.x);
				posY = (int)(internalMousePos.y);
			}
			else
			{
				posX = (int)(Mathf.Clamp(posX + delta.x, 0, Screen.width));
				posY = (int)(Mathf.Clamp(posY + delta.y, 0, Screen.height));
			}
		}
		else
		{
			if (MouseBehavior.Equals(MouseBehavior.LockCenter))
			{
				posX = (int)(Screen.width / 2);
				posY = (int)(Screen.height / 2);
			}
		}
		down = Input.GetMouseButton(0);
	}

	public void OnGUI()
	{
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
		Rect mouseRender = new Rect(posX - (mouseIcon.width / 2), posY - (mouseIcon.height / 2),mouseIcon.width,mouseIcon.height);
		position = new Vector2(posX, posY);
		GUI.DrawTexture(mouseRender, mouseIcon,ScaleMode.ScaleToFit, true);
	}
}
