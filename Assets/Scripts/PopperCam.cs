using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PopperCam : MonoBehaviour 
{
    public const float POP_RESTORE_RATE = 0.3f;
    public const float NEAR_CLIP_PLANE_OFFSET = 1;
    private Camera myCamera;
    private RootCamera myRootCamera;
    private float lastPopAmount = 0;
    private float lastZoomLevel = 0;

    private List<Vector2> CAST_SCREEN_SCALES = new List<Vector2>(5)
    {
        new Vector2(0.5f,0.5f), // Center
        new Vector2(0,0), // Top Left
        new Vector2(1,0), // Top Right
        new Vector2(0,1), // Bottom Left
        new Vector2(1,1), // Bottom Right
    };

    private static bool castRay(Vector3 origin, Vector3 target, out RaycastHit hit)
    {
        return Physics.Raycast(origin, (target - origin).normalized, out hit,(target- origin).magnitude);
    }

    public void Start()
    {
        myCamera = gameObject.GetComponent<Camera>();
        myRootCamera = gameObject.GetComponent<RootCamera>();
    }

    public void Pop()
    {

        Vector3 focusPoint = myRootCamera.GetSubjectPosition();
        Vector3 clipOffset = new Vector3(0, 0, -NEAR_CLIP_PLANE_OFFSET);
        Vector3 cameraFrontPoint = gameObject.transform.position + (gameObject.transform.rotation * clipOffset);


        // Cast rays at the near clip plane, from corresponding points near the focus point,
        // and find the direct line that is the most cut off

        float largest = 0;

        foreach (Vector2 screenScale in CAST_SCREEN_SCALES)
        {
            Vector3 clipWorldPoint = myCamera.ScreenToWorldPoint(new Vector3(Screen.width * screenScale.x, Screen.height * screenScale.y, 0) + clipOffset);
            Vector3 rayStartPoint = focusPoint + (clipWorldPoint - cameraFrontPoint);
            RaycastHit hitInfo;
            if (castRay(rayStartPoint, clipWorldPoint, out hitInfo))
            {
                float cutoffAmount = (hitInfo.point - clipWorldPoint).magnitude;
                if (cutoffAmount > largest)
                {
                    largest = cutoffAmount;
                }
            }
        }

        // Then check if the player zoomed since the last frame,
        // and if so, reset our pop history so we stop tweening

        float zoomLevel = (focusPoint - gameObject.transform.position).magnitude;
        if (Mathf.Abs(zoomLevel-lastZoomLevel) > 0.001f)
        {
            lastPopAmount = 0;
        }

        // Finally, zoom the camera in (pop) by that most-cut-off amount, or the last pop amount if that's more

        float popAmount = Mathf.Max(largest, lastPopAmount);
        if (popAmount > 0)
        {
            gameObject.transform.localPosition -= new Vector3(0, 0, popAmount);
            lastPopAmount = Mathf.Max(0, popAmount - POP_RESTORE_RATE); // Shrink it for the next frame
        }

        lastZoomLevel = zoomLevel;
    }
}
